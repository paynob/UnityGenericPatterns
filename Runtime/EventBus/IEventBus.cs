using System;

namespace Paynob.Patterns.EventBus
{
    public interface IEventBus
    {
        void Publish<TEvent>(TEvent @event) where TEvent : IEvent;
        void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IEvent;
        void Subscribe<TEvent,THandler>(THandler handler) where TEvent : IEvent where THandler : IEventHandler<TEvent>;
        void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : IEvent;
        void Unsubscribe<TEvent,THandler>(THandler handler) where TEvent : IEvent where THandler : IEventHandler<TEvent>;
        void Clear<TEvent>() where TEvent : IEvent;
    }
}