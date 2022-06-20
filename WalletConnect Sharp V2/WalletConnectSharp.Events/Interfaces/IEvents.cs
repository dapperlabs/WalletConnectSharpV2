using System;
using WalletConnectSharp.Events.Model;

namespace WalletConnectSharp.Events.Interfaces
{
    /// <summary>
    /// An interface that represents a class that triggers events that can be listened to.
    /// </summary>
    public interface IEvents
    {
        EventDelegator Events { get; }

        void On<T>(string eventId, EventHandler<GenericEvent<T>> callback);

        void Once<T>(string eventId, EventHandler<GenericEvent<T>> callback);

        void Off<T>(string eventId, EventHandler<GenericEvent<T>> callback);

        void RemoveListener<T>(string eventId, EventHandler<GenericEvent<T>> callback);
    }
}