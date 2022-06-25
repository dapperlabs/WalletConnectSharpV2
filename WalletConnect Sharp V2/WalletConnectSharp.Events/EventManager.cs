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
        private static EventManager<T, TEventArgs> _instance;

        /// <summary>
        /// The current EventTriggers this EventManager has 
        /// </summary>
        public EventHandlerMap<TEventArgs> EventTriggers { get; private set; }

        /// <summary>
        /// Get the current instance of the EventManager for the given type T and TEventArgs.
        /// </summary>
        public static EventManager<T, TEventArgs> Instance
        {
            get 
            {
                if (_instance == null)
                {
                    _instance = new EventManager<T, TEventArgs>();
                }
                
                return _instance; 
            }
        }

        private EventManager()
        {
            EventTriggers = new EventHandlerMap<TEventArgs>(CallbackBeforeExecuted);
            
            EventFactory<T>.Instance.Provider = this;
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