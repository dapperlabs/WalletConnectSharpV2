using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace WalletConnectSharp.Events
{
    /// <summary>
    /// An EventProvider that triggers any events by their eventId using C# EventHandlers and the EventHandlerMap.
    /// Each EventManager is denoted by the event data type T that it handles and the EventHandlers event args type
    /// TEventArgs that is triggered.
    /// </summary>
    /// <typeparam name="T">The type of the event data that this EventManager handles</typeparam>
    /// <typeparam name="TEventArgs">The type of the EventHandler's args this EventManager triggers. Must be a type of IEvent</typeparam>
    public class EventManager<T, TEventArgs> : IEventProvider<T> where TEventArgs : IEvent<T>, new()
    {
        private static Dictionary<string, EventManager<T, TEventArgs>> _instances =
            new Dictionary<string, EventManager<T, TEventArgs>>();

        /// <summary>
        /// The current EventTriggers this EventManager has 
        /// </summary>
        public EventHandlerMap<TEventArgs> EventTriggers { get; private set; }
        public string Context { get; private set; }

        private EventManager(string context)
        {
            this.Context = context;
            
            EventTriggers = new EventHandlerMap<TEventArgs>(CallbackBeforeExecuted);
            
            EventFactory<T>.InstanceOf(context).Provider = this;
        }

        /// <summary>
        /// Get the current instance of the EventManager for the given type T and TEventArgs.
        /// </summary>
        public static EventManager<T, TEventArgs> InstanceOf(string context)
        {
            if (!_instances.ContainsKey(context))
                _instances.Add(context, new EventManager<T, TEventArgs>(context));

            return _instances[context];
        }

        private void CallbackBeforeExecuted(object sender, TEventArgs e)
        {
        }

        /// <summary>
        /// Trigger an event by its eventId providing event data of a specific type
        /// </summary>
        /// <param name="eventId">The eventId of the event to trigger</param>
        /// <param name="eventData">The event data to trigger with this event</param>
        public void PropagateEvent(string eventId, T eventData)
        {
            if (EventTriggers.Contains(eventId))
            {
                var eventTrigger = EventTriggers[eventId];

                if (eventTrigger != null)
                {
                    //var response = JsonConvert.DeserializeObject<T>(responseJson);
                    var eventArgs = new TEventArgs();
                    eventArgs.SetData(eventData);
                    eventTrigger(this, eventArgs);
                }
            }
        }
    }
}