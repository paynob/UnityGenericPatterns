namespace Paynob.Patterns.Command
{
    /// <summary>
    /// Defines the basic contract for a command object.
    /// A command encapsulates an action to be performed on a receiver object.
    /// The generic type parameter T specifies the type of the receiver.
    /// </summary>
    /// <typeparam name="T">The type of the receiver object that the command operates on.</typeparam>
    public interface ICommand<T>
    {
        /// <summary>
        /// Executes the command on the specified receiver.
        /// </summary>
        /// <param name="receiver">The object that will perform the action.</param>
        void Execute(T receiver);

        /// <summary>
        /// Undoes the command's effect on the specified receiver.
        /// This is an optional operation and may not be implemented by all commands.
        /// </summary>
        /// <param name="receiver">The object to revert the action on.</param>
        void Undo(T receiver);
    }
}