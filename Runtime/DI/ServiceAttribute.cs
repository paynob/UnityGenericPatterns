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

        public ServiceAttribute(ServiceLifetime lifetime = ServiceLifetime.Singleton)
        {
            Lifetime = lifetime;
        }
    }

    public enum ServiceLifetime
    {
        Singleton,
        Transient
    }
}
