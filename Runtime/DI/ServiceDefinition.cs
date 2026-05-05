using System;

namespace Paynob.Patterns.DI{
    public class ServiceDefinition
    {
        public Type ServiceType;
        public Type ImplementationType;
        public ServiceLifetime Lifetime = ServiceLifetime.Singleton;
        public object SingletonInstance;
    }
}