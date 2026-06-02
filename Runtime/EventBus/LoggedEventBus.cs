using System;
using System.Collections.Generic;
using Paynob.Patterns.DI;

namespace Paynob.Patterns.EventBus
{
    public class LoggedEventBus : IEventBus, IInjectable
    {
        [Inject] private ILogger Logger {get; set;}
        private const string LOGGER_PREFFIX = "[<b><color=#00DDFF>EventBus</color></b>]: ";
        public void Publish<TEvent>(TEvent @event) where TEvent : IEvent
        {
            Logger.Debug($"{LOGGER_PREFFIX}Fired event: {@event}");
            EventChannel<TEvent>.Publish(@event);
        }

        public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IEvent
        {
            Logger.Debug($"{LOGGER_PREFFIX}Subscribed to event: {typeof(TEvent).FullName} ({handler.ToString()}");
            EventChannel<TEvent>.Subscribe(handler);
        }
        public void Subscribe<TEvent, THandler>(THandler handler) where TEvent : IEvent where THandler : IEventHandler<TEvent>
        {
            Logger.Debug($"{LOGGER_PREFFIX}Subscribed to event: {typeof(TEvent).FullName} with ({handler}");
            EventChannel<TEvent>.Subscribe(handler);
        }

        public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : IEvent
        {
            Logger.Debug($"{LOGGER_PREFFIX}Unsubscribed to event: {typeof(TEvent).FullName} ({handler.ToString()}");
            EventChannel<TEvent>.Unsubscribe(handler);
        }
        public void Unsubscribe<TEvent, THandler>(THandler handler) where TEvent : IEvent where THandler : IEventHandler<TEvent>
        {
            Logger.Debug($"{LOGGER_PREFFIX}Unsubscribed to event: {typeof(TEvent).FullName} with ({handler}");
            EventChannel<TEvent>.Unsubscribe(handler);
        }

        public void Clear<TEvent>() where TEvent : IEvent
        {
            Logger.Debug($"{LOGGER_PREFFIX}Cleared event: {typeof(TEvent).FullName}");
            EventChannel<TEvent>.Clear();
        }


        /// <summary>
        /// A static holder class for each event type to manage 
        /// its handlers and publishing.
        /// </summary>
        /// <typeparam name="TEvent"></typeparam>
        private static class EventChannel<TEvent> where TEvent : IEvent
        {
            private static readonly List<IEventHandler<TEvent>> Handlers = new();
            private static readonly List<Action<TEvent>> ActionHandlers = new();
            public static void Publish(TEvent @event)
            {
                for(int i= Handlers.Count-1; i>=0; i--  )
                {
                    Handlers[i]?.Handle(@event);
                }
                for(int i= ActionHandlers.Count-1; i>=0; i--  )
                {
                    ActionHandlers[i]?.Invoke(@event);
                }
            }
            public static void Subscribe(IEventHandler<TEvent> handler)
            {
                Handlers.Add(handler);
            }
            public static void Unsubscribe(IEventHandler<TEvent> handler)
            {
                Handlers.Remove(handler);
            }
            public static void Clear()
            {
                Handlers.Clear();
                ActionHandlers.Clear();
            }

            public static void Subscribe(Action<TEvent> handler)
            {
                ActionHandlers.Add(handler);
            }

            public static void Unsubscribe(Action<TEvent> handler)
            {
                ActionHandlers.Remove(handler);
            }
        }
    }
}