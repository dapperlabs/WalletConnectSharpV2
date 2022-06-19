using Newtonsoft.Json;

namespace WalletConnectSharp.Events
{
    public class EventManager<T, TEventArgs> : IEventProvider<T> where TEventArgs : IEvent<T>, new()
    {
        private static EventManager<T, TEventArgs> _instance;

        public EventHandlerMap<TEventArgs> EventTriggers;

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
            
            EventFactory<T>.Instance.Register(this);
        }
        
        private void CallbackBeforeExecuted(object sender, TEventArgs e)
        {
        }

        public void PropagateEvent(string topic, T response)
        {
            if (EventTriggers.Contains(topic))
            {
                var eventTrigger = EventTriggers[topic];

                if (eventTrigger != null)
                {
                    //var response = JsonConvert.DeserializeObject<T>(responseJson);
                    var eventArgs = new TEventArgs();
                    eventArgs.SetData(response);
                    eventTrigger(this, eventArgs);
                }
            }
        }
    }
}