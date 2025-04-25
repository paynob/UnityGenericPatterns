namespace Paynob.Patterns.Memento{
    using System.Collections.Generic;
    ///<summary>
    /// The Caretaker class is responsible for managing the mementos. It allows saving and restoring mementos.
    /// It does not modify the memento's state and only keeps track of the mementos.
    ///</summary>
    /// <typeparam name="T">The type of the state to be saved in the memento.</typeparam>
    public class Caretaker<T> {
        private readonly LinkedList<Memento<T>> _mementos = new LinkedList<Memento<T>>();

        public void Save( Memento<T> memento ) => _mementos.AddLast( memento );

        public T Restore() {
            if( _mementos.Last == null ) {
                throw new System.InvalidOperationException( "No mementos available." );
            }
            var memento = _mementos.Last.Value;
            _mementos.RemoveLast( );
            return memento.State;
        }
    }
}