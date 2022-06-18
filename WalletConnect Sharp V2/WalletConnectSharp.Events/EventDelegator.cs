using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using WalletConnectSharp.Events.Model;

namespace WalletConnectSharp.Events
{
    public class EventDelegator : IDisposable
    {
        private Dictionary<string, List<IEventProvider>> Listeners = new Dictionary<string, List<IEventProvider>>();

        public void ListenFor<T>(string eventId, EventHandler<GenericEvent<T>> callback)
        {  
            EventManager<T, GenericEvent<T>>.Instance.EventTriggers[eventId] += callback;

            SubscribeProvider(eventId, EventFactory.Instance.ProviderFor<T>());
        }

        private void SubscribeProvider(string eventId, IEventProvider provider)
        {
            List<IEventProvider> listProvider;
            if (!Listeners.ContainsKey(eventId))
            {
                //Debug.Log("Adding new EventProvider list for " + eventId);
                listProvider = new List<IEventProvider>();
                Listeners.Add(eventId, listProvider);
            }
            else
            {
                listProvider = Listeners[eventId];
            }
            listProvider.Add(provider);
        }
        
        public bool Trigger<T>(string topic, T obj)
        {
            return Trigger(topic, JsonConvert.SerializeObject(obj));
        }


        public bool Trigger(string topic, string json)
        {
            if (Listeners.ContainsKey(topic))
            {
                var providerList = Listeners[topic];

                for (int i = 0; i < providerList.Count; i++)
                {
                    var provider = providerList[i];
                    
                    provider.PropagateEvent(topic, json);
                }

                return providerList.Count > 0;
            }

            return false;
        }

        public void Dispose()
        {
            Clear();
        }

        public void Clear()
        {
            Listeners.Clear();
        }
    }
}