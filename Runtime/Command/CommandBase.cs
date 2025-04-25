namespace Paynob.Patterns.Command
{
    /// <summary>
    /// Provides an abstract base class for concrete command implementations.
    /// This class implements the ICommand interface and can contain common logic or default implementations
    /// for command operations. The generic type parameter T specifies the type of the receiver.
    /// </summary>
    /// <typeparam name="T">The type of the receiver object that the command operates on.</typeparam>
    public abstract class CommandBase<T> : ICommand<T>
    {
        /// <inheritdoc />
        public abstract void Execute(T receiver);

        /// <inheritdoc />
        public virtual void Undo(T receiver) { /* Default implementation: do nothing */ }
    }
}