using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using WalletConnectSharp.Common;
using WalletConnectSharp.Events.Model;

namespace WalletConnectSharp.Events
{
    /// <summary>
    /// A class that can delegate the process of both listening for specific events (by their event id) with a static
    /// event data type and triggering events (by their event id) with any event data
    ///
    /// Event listeners subscribe to events by their eventId and by the event data type the event contains. This means
    /// event listeners are statically typed and will never receive an event that its callback cannot cast safely
    /// (this includes subclasses, interfaces and object).
    /// </summary>
    public class EventDelegator : IModule
    {
        public string Name { get; private set; }
        public string Context { get; private set; }

        public EventDelegator(IModule parent)
        {
            this.Name = parent + ":event-delegator";
            this.Context = parent.Context;
        }

        /// <summary>
        /// Listen for a given event by it's eventId and trigger the parameter-less callback. This
        /// callback will be triggered for all event data types emitted with the eventId given. 
        /// </summary>
        /// <param name="eventId">The eventId of the event to listen to</param>
        /// <param name="callback">The callback to invoke when the event is triggered</param>
        public void ListenFor(string eventId, Action callback)
        {
            ListenFor<object>(eventId, (sender, @event) =>
            {
                callback();
            });
        }
        
        /// <summary>
        /// Listen for a given event by it's eventId and event data type T
        /// </summary>
        /// <param name="eventId">The eventId of the event to listen to</param>
        /// <param name="callback">The callback to invoke when the event is triggered</param>
        /// <typeparam name="T">The type of event data the callback MUST be given</typeparam>
        public void ListenFor<T>(string eventId, EventHandler<GenericEvent<T>> callback)
        {  
            EventManager<T, GenericEvent<T>>.InstanceOf(Context).EventTriggers[eventId] += callback;
        }

        /// <summary>
        /// Remove a specific callback that is listening to a specific event by it's eventId and event data type T
        /// </summary>
        /// <param name="eventId">The eventId of the event to stop listening to</param>
        /// <param name="callback">The callback that is unsubscribing</param>
        /// <typeparam name="T">The type of event data the callback MUST be given</typeparam>
        public void RemoveListener<T>(string eventId, EventHandler<GenericEvent<T>> callback)
        {
            EventManager<T, GenericEvent<T>>.InstanceOf(Context).EventTriggers[eventId] -= callback;
        }
        
        /// <summary>
        /// Listen for a given event by it's eventId and event data type T. When the event is triggered,
        /// stop listening for the event. This effectively ensures the callback is only invoked once.
        /// </summary>
        /// <param name="eventId">The eventId of the event to listen to</param>
        /// <param name="callback">The callback to invoke when the event is triggered. The callback will only be invoked once.</param>
        /// <typeparam name="T">The type of event data the callback MUST be given</typeparam>
        public void ListenForOnce<T>(string eventId, EventHandler<GenericEvent<T>> callback)
        {
            EventHandler<GenericEvent<T>> wrappedCallback = null;
            
            wrappedCallback = delegate(object sender, GenericEvent<T> @event)
            {
                callback(sender, @event);
                RemoveListener(eventId, wrappedCallback);
                
            };
            
            EventManager<T, GenericEvent<T>>.InstanceOf(Context).EventTriggers[eventId] += wrappedCallback;
        }
        
        /// <summary>
        /// Listen for a given event by it's eventId and for a event data containing a json string. When this event
        /// is triggered, the event data containing the json string is deserialized to the given type TR and the eventId
        /// is retriggered with the new deserialized event data. 
        /// </summary>
        /// <param name="eventId">The eventId of the event to listen to</param>
        /// <param name="callback">The callback to invoked with the deserialized event data</param>
        /// <typeparam name="TR">The desired event data type that MUST be deserialized to</typeparam>
        public void ListenForAndDeserialize<TR>(string eventId, EventHandler<GenericEvent<TR>> callback)
        {
            ListenFor<TR>(eventId, callback);
            
            ListenFor<string>(eventId, delegate(object sender, GenericEvent<string> @event)
            {
                try
                {
                    //Attempt to Deserialize
                    var converted = JsonConvert.DeserializeObject<TR>(@event.Response);

                    //When we convert, we trigger same eventId with required type TR
                    Trigger(eventId, converted);
                }
                catch (Exception e)
                {
                    //Propagate any exceptions to the event callback
                    Trigger(eventId, e);
                }
            });
        }
        
        /// <summary>
        /// Trigger an event by its eventId, providing a typed event data. This will invoke the provided callbacks
        /// of the event listeners listening to this eventId and looking for the given event data type T. This will
        /// also trigger event listeners looking for any sub-type of T, such as subclasses or interfaces.
        /// </summary>
        /// <param name="eventId">The eventId of the event to trigger</param>
        /// <param name="eventData">The event data to trigger the event with</param>
        /// <typeparam name="T">The type of the event data</typeparam>
        /// <returns>true if any event listeners were triggered, otherwise false</returns>
        public bool Trigger<T>(string eventId, T eventData)
        {
            return TriggerType(eventId, eventData, typeof(T));
        }

        public bool TriggerType(string eventId, object eventData, Type typeToTrigger)
        {
            IEnumerable<Type> allPossibleTypes;
            bool wasTriggered = false;
            
            if (typeToTrigger == typeof(object))
            {
                // If the type of object was given, then only
                // trigger event listeners listening to the object type explicitly
                allPossibleTypes = new[] { typeof(object) };
            }
            else
            {
                //Find all EventFactories that inherit from type T
                var inheritedT = from assembly in AppDomain.CurrentDomain.GetAssemblies()
                    from type in assembly.GetTypes()
                    where type.IsSubclassOf(typeToTrigger)
                    select type;

                // Create list of types that include types inherit from type T, type T, and type object
                allPossibleTypes = inheritedT.Concat(typeToTrigger.GetInterfaces()).Append(typeToTrigger)
                    .Append(typeof(object));
            }

            foreach (var type in allPossibleTypes)
            {
                var genericFactoryType = typeof(EventFactory<>).MakeGenericType(type);

                var instanceProperty = genericFactoryType.GetMethod("InstanceOf");
                if (instanceProperty == null) continue;
                
                var genericFactory = instanceProperty.Invoke(null, new object[] { Context });

                var genericProviderProperty = genericFactoryType.GetProperty("Provider");
                if (genericProviderProperty == null) continue;
                
                var genericProvider = genericProviderProperty.GetValue(genericFactory);
                if (genericProvider == null) continue;
                
                MethodInfo propagateEventMethod = genericProvider.GetType().GetMethod("PropagateEvent");
                if (propagateEventMethod == null) continue;
                
                propagateEventMethod.Invoke(genericProvider, new object[] { eventId, eventData });
                wasTriggered = true;
            }

            return wasTriggered;
        }
    }
}