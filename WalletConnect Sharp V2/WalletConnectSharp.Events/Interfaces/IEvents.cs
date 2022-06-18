using System;
using WalletConnectSharp.Events.Model;

namespace WalletConnectSharp.Events.Interfaces
{
    public interface IEvents
    {
        EventDelegator Events { get; }

        void On<T>(string eventId, EventHandler<GenericEvent<T>> callback);

        void Once<T>(string eventId, EventHandler<GenericEvent<T>> callback);

        void Off<T>(string eventId, EventHandler<GenericEvent<T>> callback);

        void RemoveListener<T>(string eventId, EventHandler<GenericEvent<T>> callback);
    }
}