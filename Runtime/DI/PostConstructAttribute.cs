namespace Paynob.Patterns.DI
{
    using System;

    /// <summary>
    /// Attribute used to mark methods that should be invoked after dependency injection
    /// has been completed. The marked method will be called after all fields and properties
    /// have been injected.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class PostConstructAttribute : Attribute    {    }
}
