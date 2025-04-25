namespace Paynob.Patterns.Memento{
    ///The Originator is the object that creates a Memento and uses it to restore its state.
    ///It is responsible for saving and restoring its state to and from the Memento.
    public interface IOriginator<T>
    {
        Memento<T> SaveStateToMemento();
        void RestoreStateFromMemento(Memento<T> memento);
    }

    ///The Originator is the object that creates a Memento and uses it to restore its state.
    ///It is responsible for saving and restoring its state to and from the Memento.
    public class Originator<T> : IOriginator<T>
    {
        public T State { get; set; }

        public virtual Memento<T> SaveStateToMemento()
        {
            return new Memento<T>(State);
        }

        public virtual void RestoreStateFromMemento(Memento<T> memento)
        {
            State = memento.State;
        }
    }
}