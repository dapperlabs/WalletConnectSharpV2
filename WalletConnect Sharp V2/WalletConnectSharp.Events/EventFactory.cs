using System;
using System.Collections.Generic;

namespace WalletConnectSharp.Events
{
    public class EventFactory<T>
    {
        private static EventFactory<T> _instance;

        private IEventProvider<T> _eventProvider;

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

        public void Register(IEventProvider<T> provider)
        {
            if (_eventProvider != null)
                throw new Exception("Provider for type " + typeof(T) + " already set");
            
            _eventProvider = provider;
        }

        public IEventProvider<T> Provider
        {
            get
            {
                return _eventProvider;
            }
        }
    }
}