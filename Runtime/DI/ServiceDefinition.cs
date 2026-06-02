using System;

namespace Paynob.Patterns.DI{
    public class ServiceDefinition
    {
        public Type ServiceType;
        public Type ImplementationType;
        public string Name;
        public ServiceLifetime Lifetime = ServiceLifetime.Singleton;
        public object SingletonInstance;
        public object PrefabSource;
        public bool RequiresInjection;
        public bool IsInjected;
    }
}