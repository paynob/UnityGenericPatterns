namespace Paynob.Patterns.DI
{
    using System;

    /// <summary>
    /// Attribute used to mark fields or properties that should be injected 
    /// by the dependency injection system.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Constructor | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class InjectAttribute : Attribute    {    }
}
