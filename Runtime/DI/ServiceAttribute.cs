namespace Paynob.Patterns.DI
{
    using System;

    /// <summary>
    /// Attribute used to mark classes that should be automatically registered 
    /// in the dependency injection container.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ServiceAttribute : Attribute
    {
        /// <summary>
        /// The lifetime of the service (e.g., Singleton or Transient).
        /// </summary>
        public ServiceLifetime Lifetime { get; }
        /// <summary>
        /// An optional name for the service, allowing for multiple registrations of the same type with different names.
        /// </summary>
        public string Name { get; }

        public ServiceAttribute(string name = null,ServiceLifetime lifetime = ServiceLifetime.Singleton)
        {
            Lifetime = lifetime;
            Name = name;
        }
    }

    public enum ServiceLifetime
    {
        Singleton,
        Transient
    }
}
