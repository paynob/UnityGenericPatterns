namespace Paynob.Patterns.EventBus
{
    /// <summary>
    /// Marker interface for events. Any class that represents an 
    /// event should implement this interface.
    /// </summary>
    public interface IEvent {}

    /// <summary>
    /// Interface for event handlers. Any class that wants to handle 
    /// a specific event type should implement this interface, 
    /// specifying the event type it handles.
    /// The Handle method will be called when the event is published, 
    /// allowing the handler to react to the event data.
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    public interface IEventHandler<TEvent> where TEvent : IEvent
    {
        void Handle(TEvent eventData);
    }
}