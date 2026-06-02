namespace Paynob.Patterns.DI
{
    using System;

    /// <summary>
    /// Attribute used to mark fields or properties that should be injected 
    /// by the dependency injection system.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Parameter, Inherited = true, AllowMultiple = false)]
    public sealed class InjectAttribute : Attribute
    {
        /// <summary>
        /// An optional name for the injection, allowing for multiple registrations of the same type with different names.
        /// </summary>
        public string Name { get; }

        public InjectAttribute(string name = null)
        {
            Name = name;
        }
    }
}
