using System;
using WalletConnectSharp.Events.Model;

namespace WalletConnectSharp.Events.Interfaces
{
    public interface IEvents
    {
        EventDelegator Events { get; }

        void On<T>(string eventId, EventHandler<GenericEvent<T>> callback);

        void On<T>(string eventId, EventHandler<JsonRpcResponseEvent<T>> callback) where T : JsonRpcResponse;
        
        void On<T>(string eventId, EventHandler<JsonRpcRequestEvent<T>> callback) where T : JsonRpcRequest;
        
        void Once<T>(string eventId, EventHandler<GenericEvent<T>> callback);

        void Once<T>(string eventId, EventHandler<JsonRpcResponseEvent<T>> callback) where T : JsonRpcResponse;
        
        void Once<T>(string eventId, EventHandler<JsonRpcRequestEvent<T>> callback) where T : JsonRpcRequest;

        void Off<T>(string eventId, EventHandler<GenericEvent<T>> callback);

        void Off<T>(string eventId, EventHandler<JsonRpcResponseEvent<T>> callback) where T : JsonRpcResponse;
        
        void Off<T>(string eventId, EventHandler<JsonRpcRequestEvent<T>> callback) where T : JsonRpcRequest;
        
        void RemoveListener<T>(string eventId, EventHandler<GenericEvent<T>> callback);

        void RemoveListener<T>(string eventId, EventHandler<JsonRpcResponseEvent<T>> callback) where T : JsonRpcResponse;
        
        void RemoveListener<T>(string eventId, EventHandler<JsonRpcRequestEvent<T>> callback) where T : JsonRpcRequest;
    }
}