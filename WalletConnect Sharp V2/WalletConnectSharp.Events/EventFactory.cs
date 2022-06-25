using System;
using System.Collections.Generic;

namespace WalletConnectSharp.Events
{
    /// <summary>
    /// A class that simply holds the IEventProvider for a given event data type T. This is needed to keep the
    /// different event listeners (same eventId but different event data types) separate at runtime.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EventFactory<T>
    {
        private static EventFactory<T> _instance;

        private IEventProvider<T> _eventProvider;

        /// <summary>
        /// Get the EventFactory for the event data type T
        /// </summary>
        public static EventFactory<T> Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new EventFactory<T>();
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// Get the current EventProvider for the event data type T
        /// </summary>
        /// <exception cref="Exception">Internally only. When this value is set more than once</exception>
        public IEventProvider<T> Provider
        {
            get
            {
                return _eventProvider;
            }
            internal set
            {            
                if (_eventProvider != null)
                    throw new Exception("Provider for type " + typeof(T) + " already set");
                
                _eventProvider = value;
            }
        }
    }
}