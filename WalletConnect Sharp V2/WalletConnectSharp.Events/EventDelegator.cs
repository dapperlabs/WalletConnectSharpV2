using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using WalletConnectSharp.Events.Model;

namespace WalletConnectSharp.Events
{
    public class EventDelegator
    {
        public void ListenFor<T>(string eventId, EventHandler<GenericEvent<T>> callback)
        {  
            EventManager<T, GenericEvent<T>>.Instance.EventTriggers[eventId] += callback;
        }

        public void RemoveListener<T>(string eventId, EventHandler<GenericEvent<T>> callback)
        {
            EventManager<T, GenericEvent<T>>.Instance.EventTriggers[eventId] -= callback;
        }
        
        public void ListenForOnce<T>(string eventId, EventHandler<GenericEvent<T>> callback)
        {
            EventHandler<GenericEvent<T>> wrappedCallback = null;
            
            wrappedCallback = delegate(object sender, GenericEvent<T> @event)
            {
                callback(sender, @event);
                RemoveListener(eventId, wrappedCallback);
                
            };
            
            EventManager<T, GenericEvent<T>>.Instance.EventTriggers[eventId] += wrappedCallback;
        }
        
        public void ListenForAndDeserialize<TR>(string eventId, EventHandler<GenericEvent<TR>> callback)
        {
            ListenFor<TR>(eventId, callback);
            
            ListenFor<string>(eventId, delegate(object sender, GenericEvent<string> @event)
            {
                //Attempt to Deserialize
                var converted = JsonConvert.DeserializeObject<TR>(@event.Response);
                
                //When we convert, we trigger same eventId with required type TR
                Trigger(eventId, converted);
            });
        }
        
        public bool Trigger<T>(string topic, T json)
        {
            bool wasTriggered = false;
            //Find all EventFactories of type T
            var inheritedT = from assembly in AppDomain.CurrentDomain.GetAssemblies()
                from type in assembly.GetTypes()
                where type.IsSubclassOf(typeof(T))
                select type;

            var allPossibleTypes = inheritedT.Concat(typeof(T).GetInterfaces()).Append(typeof(T)).Append(typeof(object));
            
            foreach (var type in allPossibleTypes)
            {
                var genericFactoryType = typeof(EventFactory<>).MakeGenericType(type);

                var instanceProperty = genericFactoryType.GetProperty("Instance");

                var genericFactory = instanceProperty.GetValue(null, null);

                var genericProviderProperty = genericFactoryType.GetProperty("Provider");

                var genericProvider = genericProviderProperty.GetValue(genericFactory);

                if (genericProvider != null)
                {
                    MethodInfo propagateEventMethod = genericProvider.GetType().GetMethod("PropagateEvent");

                    propagateEventMethod.Invoke(genericProvider, new object[] { topic, json });
                    wasTriggered = true;
                }
            }

            return wasTriggered;
        }
    }
}