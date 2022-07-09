using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WalletConnectSharp.Common;
using WalletConnectSharp.Events;
using WalletConnectSharp.Events.Model;
using WalletConnectSharp.Network.Models;

namespace WalletConnectSharp.Network
{
    /// <summary>
    /// A full implementation of the IJsonRpcProvider interface using the EventDelegator
    /// </summary>
    public class JsonRpcProvider : IJsonRpcProvider, IService
    {
        private IJsonRpcConnection _connection;
        private EventDelegator _delegator;
        private bool _hasRegisteredEventListeners;

        public string Name
        {
            get
            {
                return "json-rpc-provider";
            }
        }

        public string Context
        {
            get
            {
                //TODO Get context from logger
                return "walletconnectsharp";
            }
        }

        public EventDelegator Events
        {
            get
            {
                return _delegator;
            }
        }

        public JsonRpcProvider(IJsonRpcConnection connection)
        {
            this._delegator = new EventDelegator(this);
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
            Events.Trigger("connect", connection);
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

        public async Task Connect()
        {
            await Connect(_connection);
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
            
            Events.ListenForAndDeserialize(request.Id.ToString(),
                delegate(object sender, GenericEvent<JsonRpcResponse<TR>> @event)
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
            
            _connection.On<string>("payload", OnPayload);
            _connection.On<object>("close", OnConnectionDisconnected);
            _connection.On<Exception>("error", OnConnectionError);
            _hasRegisteredEventListeners = true;
        }

        private void OnConnectionError(object sender, GenericEvent<Exception> e)
        {
            Events.Trigger("error", e.Response);
        }

        private void OnConnectionDisconnected(object sender, GenericEvent<object> e)
        {
            Events.Trigger("disconnect", e.Response as object);
        }

        private void OnPayload(object sender, GenericEvent<string> e)
        {
            var json = e.Response;

            var payload = JsonConvert.DeserializeObject<JsonRpcPayload>(json);

            if (payload == null)
            {
                throw new IOException("Invalid payload: " + json);
            }
            
            Events.Trigger("payload", payload);
            
            if (payload.IsRequest)
            {
                Events.Trigger("message", json);
            }
            else
            {
                Events.Trigger(payload.Id.ToString(), json);
            }
        }
    }
}