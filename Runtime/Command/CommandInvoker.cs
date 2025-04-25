using System.Collections.Generic;

namespace Paynob.Patterns.Command{
    /// <summary>
    /// Responsible for invoking commands and optionally managing a history of executed commands
    /// for undo/redo functionality..
    /// </summary>
    /// <typeparam name="T">The type of the receiver object that the commands will act upon.</typeparam>
    public class CommandInvoker<T>
    {
        private struct CommandHistoryEntry
        {
            public ICommand<T> Command;
            public T Receiver;
        }
        private readonly Stack<CommandHistoryEntry> _commandHistory = new Stack<CommandHistoryEntry>();
        private readonly Stack<CommandHistoryEntry> _redoHistory = new Stack<CommandHistoryEntry>();

        /// <summary>
        /// Executes the given command on the specified receiver and adds it to the command history,
        /// clearing the redo history.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="receiver">The object that will perform the command's action.</param>
        public void ExecuteCommand(ICommand<T> command, T receiver)
        {
            command.Execute(receiver);
            _commandHistory.Push(new CommandHistoryEntry(){Command= command, Receiver= receiver});
            _redoHistory.Clear(); // Executing a new command invalidates the redo history
        }

        /// <summary>
        /// Undoes the last executed command, if any, moving it to the redo history.
        /// </summary>
        public void UndoLastCommand()
        {
            if (_commandHistory.Count > 0)
            {
                var lastCommand = _commandHistory.Pop();
                lastCommand.Command.Undo(lastCommand.Receiver);
                _redoHistory.Push(lastCommand);
            }
        }

        /// <summary>
        /// Redoes the last undone command, if any, moving it back to the command history.
        /// </summary>
        public void RedoLastCommand()
        {
            if (_redoHistory.Count > 0)
            {
                var historyEntry = _redoHistory.Pop();
                historyEntry.Command.Execute(historyEntry.Receiver);
                _commandHistory.Push(historyEntry);
            }
        }
    }
}