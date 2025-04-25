namespace Paynob.Patterns.Memento{
    public class Memento<T>
    {
        public T State { get; private set; }
        public Memento( T state ) => State = state;
    }
}