using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Paynob.Patterns.DI{
    public sealed class DependencyContainer
    {
        private readonly Dictionary<Type, ServiceDefinition> _services = new();
        private readonly HashSet<Type> _resolving = new();
        private readonly Dictionary<Type, ConstructorInfo> _constructorCache = new();
        private readonly Dictionary<Type, InjectionMetadata> _injectionCache = new();

        private class InjectionMetadata
        {
            public List<FieldInfo> Fields = new();
            public List<PropertyInfo> Properties = new();
            public List<MethodInfo> Methods = new();
        }

        public void RegisterServices(params Assembly[] assemblies)
        {
            if (null == assemblies || assemblies.Length == 0){
                assemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => !IsFrameworkAssembly(a)).ToArray();
            }

            foreach (var assembly in assemblies)
            {
                RegisterFromAssembly(assembly);
            }
        }
        // Escaneo automático de clases con [Service]
        public void RegisterFromAssembly(Assembly assembly)
        {
            foreach (var type in GetLoadableTypes(assembly).Where(t => t.IsClass && !t.IsAbstract) )
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
                    
                    Register(iface, type, attr.Lifetime);
                    registeredAnyInterface = true;
                }

                if (!registeredAnyInterface)
                {
                    Register(type, type, attr.Lifetime);
                }
            }
        }

        public void Register(Type serviceType, Type implementationType, ServiceLifetime lifetime)
        {
            _services[serviceType] = new ServiceDefinition
            {
                ServiceType = serviceType,
                ImplementationType = implementationType,
                Lifetime = lifetime
            };
        }

        public void Register<TService, TImplementation>(ServiceLifetime lifetime) where TImplementation : TService
        {
            Register(typeof(TService), typeof(TImplementation), lifetime);
        }

        public void Register<T>(ServiceLifetime lifetime)
        {
            Register<T, T>(lifetime);
        }

        public void Inject(object target)
        {
            var type = target.GetType();
            IEnumerable<FieldInfo> fields;
            IEnumerable<PropertyInfo> properties;
            IEnumerable<MethodInfo> methods;


            if (!_injectionCache.TryGetValue(type, out var metadata))
            {
                metadata = new InjectionMetadata();
                fields = type
                    .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(f => f.GetCustomAttribute<InjectAttribute>() != null);
                metadata.Fields.AddRange(fields);

                properties = type
                    .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(p => p.GetCustomAttribute<InjectAttribute>() != null && p.CanWrite);
                metadata.Properties.AddRange(properties);

                methods = type
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(m => m.GetCustomAttribute<InjectAttribute>() != null);
                metadata.Methods.AddRange(methods);

                
                _injectionCache[type] = metadata;
            }
            else
            {
                fields = metadata.Fields;
                properties = metadata.Properties;
                methods = metadata.Methods;
            }          

            foreach (var field in fields)
            {
                var dependency = Resolve(field.FieldType);
                field.SetValue(target, dependency);
            }

            foreach (var property in properties)
            {
                var dependency = Resolve(property.PropertyType);
                property.SetValue(target, dependency);
            }

            foreach (var method in methods)
            {
                var parameters = method
                    .GetParameters()
                    .Select(p => Resolve(p.ParameterType))
                    .ToArray();

                method.Invoke(target, parameters);
            }
        }

        public object Resolve(Type type)
        {
            if (!_services.TryGetValue(type, out var definition))
            {
                // Si no está registrado directamente, intentamos resolver genéricos abiertos
                if (!TryResolveGenericDefinition(type, out definition))
                {
                    throw new Exception($"Service of type '{type.FullName}' is not registered. Check if the [Service] attribute is missing or the assembly is not being scanned.");
                }
            }

            if (definition.Lifetime == ServiceLifetime.Singleton)
            {
                if (definition.SingletonInstance == null)
                {
                    definition.SingletonInstance = CreateInstance(definition);
                }

                return definition.SingletonInstance;
            }

            return CreateInstance(definition);
        }

        private bool TryResolveGenericDefinition(Type type, out ServiceDefinition definition)
        {
            definition = null;
            if (type.IsGenericType && _services.TryGetValue(type.GetGenericTypeDefinition(), out var openDefinition))
            {
                definition = new ServiceDefinition
                {
                    ServiceType = type,
                    ImplementationType = openDefinition.ImplementationType.MakeGenericType(type.GetGenericArguments()),
                    Lifetime = openDefinition.Lifetime
                };
                // Cacheamos la definición cerrada para la próxima vez
                _services[type] = definition;
                return true;
            }
            return false;
        }

        public T Resolve<T>()
        {
            return (T)Resolve(typeof(T));
        }

        private object CreateInstance(ServiceDefinition definition)
        {
            var type = definition.ImplementationType;

            if (_resolving.Contains(type))
                throw new Exception($"Circular dependency detected: {type.Name}");

            _resolving.Add(type);

            var instance = CreateWithConstructor(type);

            Inject(instance);

            _resolving.Remove(type);

            return instance;
        }

        private object CreateWithConstructor(Type type)
        {
            if (!_constructorCache.TryGetValue(type, out var constructor))
            {
                var ctors = type.GetConstructors();
                
                // Buscamos primero un constructor marcado con [Inject], si no, usamos el que tenga más parámetros
                constructor = ctors.FirstOrDefault(c => c.GetCustomAttribute<InjectAttribute>() != null) 
                              ?? ctors.OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();

                if (constructor == null)
                    throw new Exception($"No public constructor found for type {type.Name}");

                _constructorCache[type] = constructor;
            }

            var parameters = constructor
                .GetParameters()
                .Select(p => Resolve(p.ParameterType))
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
    }
}