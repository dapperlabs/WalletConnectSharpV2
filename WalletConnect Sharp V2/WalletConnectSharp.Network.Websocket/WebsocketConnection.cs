using System;
using System.Threading.Tasks;
using WalletConnectSharp.Events;
using WalletConnectSharp.Events.Model;

namespace WalletConnectSharp.Network.Websocket
{
    public class WebsocketConnection : IJsonRpcConnection
    {
        private EventDelegator _delegator;

        public EventDelegator Events
        {
            get
            {
                return _delegator;
            }
        }

        public void On<T>(string eventId, EventHandler<GenericEvent<T>> callback)
        {
            _delegator.ListenFor(eventId, callback);
        }

        public void Once<T>(string eventId, EventHandler<GenericEvent<T>> callback)
        {
            _delegator.ListenForOnce(eventId, callback);
        }

        public void Off<T>(string eventId, EventHandler<GenericEvent<T>> callback)
        {
            _delegator.RemoveListener(eventId, callback);
        }

        public void RemoveListener<T>(string eventId, EventHandler<GenericEvent<T>> callback)
        {
            _delegator.RemoveListener(eventId, callback);
        }

        public bool Connected { get; private set; }
        public bool Connecting { get; private set; }
        
        public Task Open()
        {
            throw new NotImplementedException();
        }

        public Task Open<T>(T options)
        {
            throw new NotImplementedException();
        }

        public Task Close()
        {
            throw new NotImplementedException();
        }

        public Task SendRequest<T>(IJsonRpcRequest<T> requestPayload, object context)
        {
            throw new NotImplementedException();
        }

        public Task SendResult<T>(IJsonRpcResult<T> requestPayload, object context)
        {
            throw new NotImplementedException();
        }

        public Task SendError(IJsonRpcError requestPayload, object context)
        {
            throw new NotImplementedException();
        }
    }
}