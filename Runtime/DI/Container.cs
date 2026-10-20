using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Paynob.Patterns.DI
{
    public sealed class Container : IDisposable
    {
        /// <summary>
        /// Dependency injection container that manages service registrations and resolutions.
        /// Supports singleton and transient lifetimes, named registrations, parent-child
        /// container hierarchies, constructor/property/method injection, and prefab/instance
        /// registrations for Unity objects.
        /// </summary>
        private readonly Dictionary<Type, List<ServiceDefinition>> _services = new();
        private readonly HashSet<Type> _resolving = new();
        private readonly Dictionary<Type, ConstructorInfo> _constructorCache = new();
        private readonly Dictionary<Type, InjectionMetadata> _injectionCache = new();

        private Container _parent;
        private ILogger _logger;
        private readonly string _name;

        /// <summary>
        /// Creates the root container and registers the container instance itself
        /// under the default name "main".
        /// </summary>
        public Container()
        {
            _name = "main";
            RegisterInstance(this, _name);
        }

        /// <summary>
        /// Internal constructor used to create child containers with a specific name.
        /// </summary>
        private Container(string name)
        {
            _name = name;
        }

        /// <summary>
        /// Creates a child container that inherits the logger and references this container
        /// as its parent. Child containers can resolve services from their parent.
        /// </summary>
        /// <param name="name">Name to assign to the child container.</param>
        /// <returns>The created child <see cref="Container"/> instance.</returns>
        public Container CreateChildContainer(string name)
        {
            var child = new Container(name);
            child._parent = this;
            child._logger = this._logger;
            return child;
        }

        private class InjectionMetadata
        {
            public struct FieldInjection { public FieldInfo Field; public string Name; }
            public struct PropertyInjection { public PropertyInfo Property; public string Name; }
            public struct MethodInjection 
            { 
                public MethodInfo Method; 
                public ParameterInjection[] Parameters; 
            }
            public struct ParameterInjection { public Type Type; public string Name; }

            public List<FieldInjection> Fields = new();
            public List<PropertyInjection> Properties = new();
            public List<MethodInjection> Methods = new();
            public MethodInfo PostConstructMethod;
            public bool IsEmpty;
        }

        /// <summary>
        /// Registers services by loading assemblies specified by their names.
        /// </summary>
        /// <param name="assembliesNames">Array of assembly names to load and scan.</param>
        public void RegisterServices(string[] assembliesNames)
        {
            RegisterServices(assembliesNames.Select(s => Assembly.Load(s)).ToArray());
        }
        public void RegisterServices(params Assembly[] assemblies)
        {
            if (null == assemblies || assemblies.Length == 0)
            {
                assemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => !IsFrameworkAssembly(a)).ToArray();
            }

            foreach (var assembly in assemblies)
            {
                RegisterFromAssembly(assembly);
            }
        }
        /// <summary>
        /// Scans an assembly and registers types marked with <see cref="ServiceAttribute"/>.
        /// Types implementing interfaces are registered for those interfaces; otherwise the
        /// concrete type is registered.
        /// </summary>
        /// <param name="assembly">Assembly to scan for services.</param>
        public void RegisterFromAssembly(Assembly assembly)
        {
            foreach (var type in GetLoadableTypes(assembly).Where(t => t.IsClass && !t.IsAbstract))
            {
                var attr = type.GetCustomAttribute<ServiceAttribute>();
                if (attr == null)
                    continue;

                var interfaces = type.GetInterfaces();
                bool registeredAnyInterface = false;

                foreach (var iface in interfaces)
                {
                    if (IsFrameworkAssembly(iface.Assembly))
                        continue;

                    Register(iface, type, attr.Name, attr.Lifetime);
                    registeredAnyInterface = true;
                }

                if (!registeredAnyInterface)
                {
                    Register(type, type, attr.Name, attr.Lifetime);
                }
            }
        }

        private void RegisterAllInterfaces(Type implementationType, string name = null, ServiceLifetime lifetime = ServiceLifetime.Singleton, object instance = null, object prefab = null)
        {
            var interfaces = implementationType.GetInterfaces();
            bool registeredAnyInterface = false;

            foreach (var iface in interfaces)
            {
                if (IsFrameworkAssembly(iface.Assembly))
                    continue;

                Register(iface, implementationType, name, lifetime, instance, prefab);
                registeredAnyInterface = true;
            }

            if (!registeredAnyInterface)
            {
                Register(implementationType, implementationType, name, lifetime, instance, prefab);
            }
        }

        private void Register(Type serviceType, Type implementationType, string name = null, ServiceLifetime lifetime = ServiceLifetime.Singleton, object instance = null, object prefab = null)
        {
            TryRegister(serviceType, implementationType, instance, prefab, lifetime, name);

            string source = prefab != null ? "prefab " : (instance != null ? "instance " : "");
            _logger?.Log($"Registered {source}service {serviceType.FullName} -> {implementationType.FullName} ({lifetime})");
        }

        public void Register<TService, TImplementation>(string name = null, ServiceLifetime lifetime = ServiceLifetime.Singleton) where TImplementation : TService
        {
            Register(typeof(TService), typeof(TImplementation), name, lifetime);
        }

        public void Register<T>(string name = null, ServiceLifetime lifetime = ServiceLifetime.Singleton, params Type[] asTypes)
        {

            if (asTypes != null && asTypes.Length > 0)
            {
                foreach (var type in asTypes)
                    Register(type, typeof(T), name, ServiceLifetime.Singleton, null, null);
            }
            else
            {
                Register<T, T>(name, lifetime);
            }
        }

        public void RegisterInstance<T>(T instance, string name = null, params Type[] asTypes)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));

            if (asTypes != null && asTypes.Length > 0)
            {
                foreach (var type in asTypes)
                    Register(type, instance.GetType(), name, ServiceLifetime.Singleton, instance, null);
            }
            else
            {
                Register(typeof(T), instance.GetType(), name, ServiceLifetime.Singleton, instance, null);
            }
        }

        public void RegisterPrefab<T>(T prefab, string name = null, params Type[] asTypes) where T : UnityEngine.Object
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));

            if (asTypes != null && asTypes.Length > 0)
            {
                foreach (var type in asTypes)
                    Register(type, prefab.GetType(), name, ServiceLifetime.Singleton, null, prefab);
            }
            else
            {
                Register(typeof(T), prefab.GetType(), name, ServiceLifetime.Singleton, null, prefab);
            }
        }

        private ServiceDefinition TryRegister(Type serviceType, Type implementationType, object instance = null, object prefab = null, ServiceLifetime lifetime = ServiceLifetime.Singleton, string name = null)
        {
            List<ServiceDefinition> definitions;
            if (_services.TryGetValue(serviceType, out definitions))
            {
                if (definitions.Any(d => d.Name == name))
                {
                    string currentName = string.IsNullOrEmpty(name) ? "(anonymous)" : $"with name '{name}'";

                    throw new NotSupportedException(
                        $"[DI] Service of type '{serviceType.FullName}' {currentName} is already registered. " +
                        $"Cannot register multiple definitions with the same name for the same service type.");
                }
            }
            else
            {
                definitions = new List<ServiceDefinition>();
                _services[serviceType] = definitions;
            }

            var definition = new ServiceDefinition
            {
                ServiceType = serviceType,
                ImplementationType = implementationType,
                Lifetime = lifetime,
                SingletonInstance = instance,
                Name = name,
                PrefabSource = prefab,
                RequiresInjection = instance != null
            };

            definitions.Add(definition);

            CheckUpdateLogger(serviceType);

            return definition;
        }

        public void Dispose()
        {
            foreach (var definitions in _services.Values)
            {
                foreach (var def in definitions)
                {
                    if (def.SingletonInstance is IDisposable disposable && def.SingletonInstance != this)
                    {
                        disposable.Dispose();
                    }
                }
            }
            _services.Clear();
            _constructorCache.Clear();
            _injectionCache.Clear();
        }

        private void CheckUpdateLogger(Type type)
        {
            if (_logger == null && typeof(ILogger) == type)
            {
                _logger = (ILogger)Resolve(type);
            }
        }

        /// <summary>
        /// Injects dependencies into the provided target object by filling fields,
        /// setting writable properties, invoking methods annotated for injection,
        /// and calling a parameterless post-construct method if present.
        /// </summary>
        /// <param name="target">The object to perform injection on.</param>
        public void Inject(object target)
        {
            var type = target.GetType();

            if (!_injectionCache.TryGetValue(type, out var metadata))
            {
                metadata = new InjectionMetadata();

                var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var f in fields)
                {
                    var attr = f.GetCustomAttribute<InjectAttribute>();
                    if (attr != null) metadata.Fields.Add(new InjectionMetadata.FieldInjection { Field = f, Name = attr.Name });
                }

                var props = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var p in props)
                {
                    var attr = p.GetCustomAttribute<InjectAttribute>();
                    if (attr != null && p.CanWrite) metadata.Properties.Add(new InjectionMetadata.PropertyInjection { Property = p, Name = attr.Name });
                }

                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var m in methods)
                {
                    var attr = m.GetCustomAttribute<InjectAttribute>();
                    if (attr != null)
                    {
                        var parameters = m.GetParameters().Select(p => {
                            var pAttr = p.GetCustomAttribute<InjectAttribute>();
                            return new InjectionMetadata.ParameterInjection { Type = p.ParameterType, Name = pAttr?.Name };
                        }).ToArray();
                        metadata.Methods.Add(new InjectionMetadata.MethodInjection { Method = m, Parameters = parameters });
                    }
                }

                var postConstructMethods = type
                    .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(m => m.GetCustomAttribute<PostConstructAttribute>() != null)
                    .ToList();

                if (postConstructMethods.Count > 1)
                    throw new Exception($"Type '{type.FullName}' has {postConstructMethods.Count} methods marked with [PostConstruct]. Only one method is allowed per type hierarchy to ensure a single initialization point.");

                var postConstructMethod = postConstructMethods.FirstOrDefault();
                if (postConstructMethod != null && postConstructMethod.GetParameters().Length > 0)
                {
                    throw new Exception($"[PostConstruct] method '{postConstructMethod.Name}' on type '{type.FullName}' must be parameterless.");
                }

                metadata.PostConstructMethod = postConstructMethod;

                metadata.IsEmpty = metadata.Fields.Count == 0 &&
                                  metadata.Properties.Count == 0 &&
                                  metadata.Methods.Count == 0 &&
                                  metadata.PostConstructMethod == null;

                _injectionCache[type] = metadata;
            }

            if (metadata.IsEmpty) return;

            foreach (var field in metadata.Fields)
            {
                field.Field.SetValue(target, Resolve(field.Field.FieldType, field.Name));
            }

            foreach (var property in metadata.Properties)
            {
                property.Property.SetValue(target, Resolve(property.Property.PropertyType, property.Name));
            }

            foreach (var method in metadata.Methods)
            {
                var parameters = method.Parameters
                    .Select(p => Resolve(p.Type, p.Name))
                    .ToArray();
                method.Method.Invoke(target, parameters);
            }

            metadata.PostConstructMethod?.Invoke(target, null);
        }

        public T Resolve<T>(string name = null)
        {
            return (T)Resolve(typeof(T), name);
        }

        /// <summary>
        /// Resolves a service by type and optional name. Supports resolving collections
        /// (arrays and generic collection interfaces) and will search the parent container
        /// when a service is not registered in this container.
        /// </summary>
        /// <param name="type">Service type to resolve.</param>
        /// <param name="name">Optional registration name for named services.</param>
        /// <returns>Resolved service instance.</returns>
        public object Resolve(Type type, string name = null)
        {
            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();
                if (genericDef == typeof(List<>) ||
                    genericDef == typeof(IEnumerable<>) ||
                    genericDef == typeof(IList<>) ||
                    genericDef == typeof(ICollection<>) ||
                    genericDef == typeof(IReadOnlyList<>))
                {
                    return ResolveCollection(type);
                }
            }

            if (type.IsArray && type.GetArrayRank() == 1)
            {
                return ResolveArray(type);
            }

            if (!_services.TryGetValue(type, out var definitions) || definitions.Count == 0)
            {
                if (_parent != null)
                {
                    return _parent.Resolve(type, name);
                }

                if (!TryResolveGenericDefinition(type, name, out var openDefinition))
                {
                    string nameInfo = string.IsNullOrEmpty(name) ? "" : $" with name '{name}'";
                    throw new Exception($"Service of type '{type.FullName}'{nameInfo} is not registered. Check if the [Service] attribute is missing or the assembly is not being scanned.");
                }

                _services.TryGetValue(type, out definitions);
            }

            ServiceDefinition definition = null;

            if (string.IsNullOrEmpty(name))
            {
                if (definitions.Count > 1)
                {
                    throw new Exception($"[DI] Ambiguity detected. Multiple services are registered for type '{type.FullName}', but you didn't specify a 'name' to differentiate them.");
                }
                definition = definitions[0];
            }
            else
            {
                definition = definitions.FirstOrDefault(d => d.Name == name);
                if (definition == null)
                {
                    throw new Exception($"[DI] Service of type '{type.FullName}' with name '{name}' was not found.");
                }
            }

            if (definition.Lifetime == ServiceLifetime.Singleton)
            {
                if (definition.SingletonInstance == null)
                {
                    definition.SingletonInstance = CreateInstance(definition);
                }
                EnsureInstanceInjected(definition);
                return definition.SingletonInstance;
            }

            return CreateInstance(definition);
        }

        private bool TryResolveGenericDefinition(Type type, string name, out ServiceDefinition definition)
        {
            definition = null;
            if (type.IsGenericType && _services.TryGetValue(type.GetGenericTypeDefinition(), out var openDefinitions))
            {
                // Buscamos si entre las definiciones genéricas abiertas coincide el nombre pedido
                var matchingOpenDef = openDefinitions.FirstOrDefault(d => d.Name == name);
                if (matchingOpenDef == null) return false;

                definition = new ServiceDefinition
                {
                    ServiceType = type,
                    ImplementationType = matchingOpenDef.ImplementationType.MakeGenericType(type.GetGenericArguments()),
                    Lifetime = matchingOpenDef.Lifetime,
                    Name = name
                };

                // Cacheamos la nueva definición cerrada en la lista correspondiente
                if (!_services.TryGetValue(type, out var list))
                {
                    list = new List<ServiceDefinition>();
                    _services[type] = list;
                }
                list.Add(definition);

                return true;
            }
            return false;
        }

        private void EnsureInstanceInjected(ServiceDefinition definition)
        {
            if (definition.SingletonInstance != null && definition.RequiresInjection && !definition.IsInjected)
            {
                Inject(definition.SingletonInstance);
                definition.IsInjected = true;
            }
        }

        private object ResolveCollection(Type collectionType)
        {
            Type innerType = collectionType.GetGenericArguments()[0];
            IList listInstance = (IList)ResolveList(innerType);

            if (collectionType.IsAssignableFrom(listInstance.GetType()))
                return listInstance;

            return listInstance;
        }

        private object ResolveArray(Type arrayType)
        {
            Type innerType = arrayType.GetElementType();
            IList listInstance = (IList)ResolveList(innerType);
            var array = Array.CreateInstance(innerType, listInstance.Count);
            listInstance.CopyTo(array, 0);
            return array;
        }
        private object ResolveList(Type innerType)
        {
            Type listType = typeof(List<>).MakeGenericType(innerType);
            IList listInstance = (IList)Activator.CreateInstance(listType);

            // 1. Add local registrations
            if (_services.TryGetValue(innerType, out var definitions))
            {
                foreach (var def in definitions)
                {
                    if (def.Lifetime == ServiceLifetime.Singleton)
                    {
                        if (def.SingletonInstance == null)
                        {
                            def.SingletonInstance = CreateInstance(def);
                        }
                        EnsureInstanceInjected(def);
                        listInstance.Add(def.SingletonInstance);
                    }
                    else
                    {
                        listInstance.Add(CreateInstance(def));
                    }
                }
            }

            // 2. Include parent container registrations to support hierarchical composition
            if (_parent != null)
            {
                IList parentList = (IList)_parent.Resolve(typeof(List<>).MakeGenericType(innerType));
                foreach (var item in parentList)
                {
                    // Evitamos duplicados si el padre y el hijo comparten instancias por herencia externa
                    if (!ContainsReference(listInstance, item))
                    {
                        listInstance.Add(item);
                    }
                }
            }

            return listInstance;
        }

        private bool ContainsReference(IList list, object item)
        {
            foreach (var current in list)
            {
                if (ReferenceEquals(current, item))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Returns true when the container can resolve the specified type and optional name.
        /// This method checks local registrations, parent containers, and open generic definitions.
        /// </summary>
        /// <param name="type">Type to check.</param>
        /// <param name="name">Optional registration name.</param>
        /// <returns>True if resolvable; otherwise false.</returns>
        public bool CanResolve(Type type, string name = null)
        {
            if (type.IsArray && type.GetArrayRank() == 1)
            {
                return true;
                //return CanResolve(type.GetElementType(), name);
            }

            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();
                if (genericDef == typeof(List<>) ||
                    genericDef == typeof(IEnumerable<>) ||
                    genericDef == typeof(IList<>) ||
                    genericDef == typeof(ICollection<>) ||
                    genericDef == typeof(IReadOnlyList<>))
                {
                    return true;
                    //return CanResolve(type.GetGenericArguments()[0], name);
                }
            }

            if (_services.TryGetValue(type, out var definitions))
            {
                if (string.IsNullOrEmpty(name)) return definitions.Count > 0;
                return definitions.Any(d => d.Name == name);
            }

            if (_parent != null && _parent.CanResolve(type, name)) return true;

            if (type.IsGenericType && _services.TryGetValue(type.GetGenericTypeDefinition(), out var openDefinitions))
            {
                if (string.IsNullOrEmpty(name)) return openDefinitions.Count > 0;
                return openDefinitions.Any(d => d.Name == name);
            }

            return false;
        }

        private object CreateInstance(ServiceDefinition definition)
        {
            var type = definition.ImplementationType;

            if (_resolving.Contains(type))
                throw new Exception($"Circular dependency detected: {type.Name}");

            _resolving.Add(type);

            try
            {
                object instance;

                if (definition.PrefabSource != null && definition.PrefabSource is UnityEngine.Object prefab)
                {
                    _logger?.Log($"Instancing prefab {prefab.name} [{type}]");
                    instance = UnityEngine.Object.Instantiate(prefab);
                }
                else if (typeof(MonoBehaviour).IsAssignableFrom(type))
                {
                    _logger?.Log($"Instancing MonoBehaviour {type.Name}");
                    var go = new GameObject($"[Service] {type.Name}");
                    instance = go.AddComponent(type);
                }
                else
                {
                    _logger?.Log($"Instancing {type.Name}");
                    instance = CreateWithConstructor(type);
                }

                Inject(instance);
                definition.IsInjected = true;
                return instance;
            }
            finally
            {
                _resolving.Remove(type);
            }
        }

        private object CreateWithConstructor(Type type)
        {
            if (!_constructorCache.TryGetValue(type, out var constructor))
            {
                var ctors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
                var injectCtors = ctors.Where(c => c.GetCustomAttribute<InjectAttribute>() != null).ToArray();

                if (injectCtors.Length > 1)
                {
                    throw new Exception($"Type '{type.FullName}' contains more than one constructor marked with [Inject]. Only one injected constructor is allowed.");
                }

                constructor = injectCtors.SingleOrDefault();

                if (constructor != null && !constructor.GetParameters().All(p => CanResolve(p.ParameterType)))
                {
                    var missing = constructor.GetParameters()
                        .Where(p => !CanResolve(p.ParameterType))
                        .Select(p => p.ParameterType.FullName)
                        .ToArray();

                    throw new Exception($"Constructor '{constructor}' for type '{type.FullName}' has unresolved parameters: {string.Join(", ", missing)}.");
                }

                constructor ??= ctors.Where(c => c.GetParameters().All(p => CanResolve(p.ParameterType)))
                                      .OrderByDescending(c => c.GetParameters().Length)
                                      .FirstOrDefault();

                if (constructor == null)
                    throw new Exception($"No public constructor for type '{type.FullName}' can be satisfied by the current container registrations.");

                _constructorCache[type] = constructor;
            }

            var parameters = constructor
                .GetParameters()
                .Select(p => {
                    var attr = p.GetCustomAttribute<InjectAttribute>();
                    return Resolve(p.ParameterType, attr?.Name);
                })
                .ToArray();

            return constructor.Invoke(parameters);
        }
        

        IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null);
            }
        }
        bool IsFrameworkAssembly(Assembly assembly)
        {
            var name = assembly.GetName().Name;

            return
                name.StartsWith("System") ||
                name.StartsWith("Microsoft") ||
                name.StartsWith("Unity") ||
                name.StartsWith("Mono") ||
                name.StartsWith("netstandard") ||
                name.StartsWith("mscorlib");
        }

        /// <summary>
        /// Returns the container's name and, when present, a textual indicator of its parent chain.
        /// </summary>
        /// <returns>String representation of the container.</returns>
        public override string ToString()
        {
            string parentName = _parent == null ? string.Empty : $" --> {_parent}";
            return $"{_name}{parentName}";
        }
    }
}