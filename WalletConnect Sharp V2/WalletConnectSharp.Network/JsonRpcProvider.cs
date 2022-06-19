using System;
using System.IO;
using System.Threading.Tasks;
using WalletConnectSharp.Events;
using WalletConnectSharp.Events.Model;
using WalletConnectSharp.Network.Models;

namespace WalletConnectSharp.Network
{
    public class JsonRpcProvider : IJsonRpcProvider
    {
        private IJsonRpcConnection _connection;
        private EventDelegator _delegator;
        private bool _hasRegisteredEventListeners;

        public EventDelegator Events
        {
            get
            {
                return _delegator;
            }
        }

        public JsonRpcProvider(IJsonRpcConnection connection)
        {
            this._delegator = new EventDelegator();
            this._connection = connection;
            if (this._connection.Connected)
            {
                RegisterEventListeners();
            }
        }

        public async Task Connect(string connection)
        {
            if (this._connection.Connected)
            {
                await this._connection.Close();
            }

            await this._connection.Open(connection);

            FinalizeConnection(this._connection);
        }

        public async Task Connect(IJsonRpcConnection connection)
        {
            if (this._connection == connection && connection.Connected) return;
            if (this._connection.Connected)
            {
                await this._connection.Close();
            }

            await connection.Open();

            FinalizeConnection(connection);
        }

        private void FinalizeConnection(IJsonRpcConnection connection)
        {
            this._connection = connection;
            RegisterEventListeners();
            Events.Trigger<object>("connect", null);
        }

        public async Task Connect<T>(T @params)
        {
            if (typeof(string).IsAssignableFrom(typeof(T)))
            {
                await Connect(@params as string);
                return;
            }

            if (typeof(IJsonRpcConnection).IsAssignableFrom(typeof(T)))
            {
                await Connect(@params as IJsonRpcConnection);
                return;
            }

            await _connection.Open(@params);
            
            FinalizeConnection(_connection);
        }

        public async Task Disconnect()
        {
            await _connection.Close();
        }

        public async Task<TR> Request<T, TR>(IRequestArguments<T> requestArgs, object context)
        {
            if (!_connection.Connected)
            {
                await Connect(_connection);
            }

            long? id = null;
            if (requestArgs is IJsonRpcRequest<T>)
            {
                id = ((IJsonRpcRequest<T>)requestArgs).Id;
            }
            var request = new JsonRpcRequest<T>(requestArgs.Method, requestArgs.Params, id);

            TaskCompletionSource<TR> requestTask = new TaskCompletionSource<TR>(TaskCreationOptions.None);
            
            Events.ListenFor(request.Id.ToString(),
                delegate(object sender, GenericEvent<IJsonRpcResult<TR>> @event)
                {
                    var result = @event.Response;

                    if (result.Error != null)
                    {
                        requestTask.SetException(new IOException(result.Error.Message));
                    }
                    else
                    {
                        requestTask.SetResult(result.Result);
                    }
                });

            await _connection.SendRequest(request, context);

            await requestTask.Task;

            return requestTask.Task.Result;
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

        protected void RegisterEventListeners()
        {
            if (_hasRegisteredEventListeners) return;
            
            _connection.On<IJsonRpcPayload>("payload", OnPayload);
            _connection.On<dynamic>("close", OnConnectionDisconnected);
            _connection.On<Exception>("error", OnConnectionError);
            _hasRegisteredEventListeners = true;
        }

        private void OnConnectionError(object sender, GenericEvent<Exception> e)
        {
            Events.Trigger("error", e.Response);
        }

        private void OnConnectionDisconnected(object sender, GenericEvent<dynamic> e)
        {
            Events.Trigger("disconnect", e.Response as object);
        }

        private void OnPayload(object sender, GenericEvent<IJsonRpcPayload> e)
        {
            var payload = e.Response;

            Events.Trigger("payload", payload);

            if (typeof(IJsonRpcRequest<>).IsInstanceOfType(payload))
            {
                IJsonRpcRequest<dynamic> request = (IJsonRpcRequest<dynamic>)payload;

                Events.Trigger("message", request);
            }
            else
            {
                Events.Trigger(payload.Id.ToString(), payload);
            }
        }
    }
}