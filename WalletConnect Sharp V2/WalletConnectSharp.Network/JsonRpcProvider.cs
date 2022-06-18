using System;
using System.Threading.Tasks;
using WalletConnectSharp.Events;
using WalletConnectSharp.Events.Model;

namespace WalletConnectSharp.Network
{
    public class JsonRpcProvider : IJsonRpcProvider
    {
        public EventDelegator Events { get; }
        
        public Task Connect(string connection)
        {
            throw new System.NotImplementedException();
        }

        public Task Connect(IJsonRpcConnection connection)
        {
            throw new System.NotImplementedException();
        }

        public Task Connect<T>(T @params)
        {
            throw new NotImplementedException();
        }

        public Task Disconnect()
        {
            throw new System.NotImplementedException();
        }

        public Task<TR> Request<T, TR>(IRequestArguments<T> request, object context)
        {
            throw new System.NotImplementedException();
        }

        public void On<T>(string eventId, EventHandler<GenericEvent<T>> callback)
        {
            throw new NotImplementedException();
        }

        public void On<T>(string eventId, EventHandler<JsonRpcResponseEvent<T>> callback) where T : JsonRpcResponse
        {
            throw new NotImplementedException();
        }

        public void On<T>(string eventId, EventHandler<JsonRpcRequestEvent<T>> callback) where T : JsonRpcRequest
        {
            throw new NotImplementedException();
        }

        public void Once<T>(string eventId, EventHandler<GenericEvent<T>> callback)
        {
            throw new NotImplementedException();
        }

        public void Once<T>(string eventId, EventHandler<JsonRpcResponseEvent<T>> callback) where T : JsonRpcResponse
        {
            throw new NotImplementedException();
        }

        public void Once<T>(string eventId, EventHandler<JsonRpcRequestEvent<T>> callback) where T : JsonRpcRequest
        {
            throw new NotImplementedException();
        }

        public void Off<T>(string eventId, EventHandler<GenericEvent<T>> callback)
        {
            throw new NotImplementedException();
        }

        public void Off<T>(string eventId, EventHandler<JsonRpcResponseEvent<T>> callback) where T : JsonRpcResponse
        {
            throw new NotImplementedException();
        }

        public void Off<T>(string eventId, EventHandler<JsonRpcRequestEvent<T>> callback) where T : JsonRpcRequest
        {
            throw new NotImplementedException();
        }

        public void RemoveListener<T>(string eventId, EventHandler<GenericEvent<T>> callback)
        {
            throw new NotImplementedException();
        }

        public void RemoveListener<T>(string eventId, EventHandler<JsonRpcResponseEvent<T>> callback) where T : JsonRpcResponse
        {
            throw new NotImplementedException();
        }

        public void RemoveListener<T>(string eventId, EventHandler<JsonRpcRequestEvent<T>> callback) where T : JsonRpcRequest
        {
            throw new NotImplementedException();
        }
    }
}