using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using WalletConnectSharp.Common;
using WalletConnectSharp.Core.Interfaces;
using WalletConnectSharp.Core.Models.Relay;
using WalletConnectSharp.Crypto.Interfaces;
using WalletConnectSharp.Events;
using WalletConnectSharp.Events.Model;
using WalletConnectSharp.Network;
using WalletConnectSharp.Network.Models;
using WalletConnectSharp.Sign.Controllers;
using WalletConnectSharp.Sign.Interfaces;
using WalletConnectSharp.Sign.Models;
using WalletConnectSharp.Sign.Models.Engine;
using WalletConnectSharp.Sign.Models.Expirer;

namespace WalletConnectSharp.Sign
{
    public class Engine : IEnginePrivate, IEngine, IModule
    {
        private Dictionary<string, Type> requestTypeMapping = new Dictionary<string, Type>();
        private Dictionary<string, Type> responseTypeMapping = new Dictionary<string, Type>();

        private EventDelegator Events;

        private bool initialized = false;
        
        public ISignClient Client { get; }

        private IEnginePrivate PrivateEngineTasks => this;

        public string Name => "engine";

        public string Context
        {
            get
            {
                return Name;
            }
        }

        public Engine(ISignClient client)
        {
            this.Client = client;
            Events = new EventDelegator(this);
        }

        public async Task Init()
        {
            if (!this.initialized)
            {
                BuildRequestResponseTypeMapping();
                
                await ((IEnginePrivate) this).Cleanup();
                this.RegisterRelayerEvents();
                this.RegisterExpirerEvents();
                this.initialized = true;
            }
        }

        private async void RegisterExpirerEvents()
        {
            this.Client.Expirer.On<Expiration>(ExpirerEvents.Expired, ExpiredCallback);
        }

        private async void ExpiredCallback(object sender, GenericEvent<Expiration> e)
        {
            var target = new ExpirerTarget(e.EventData.Target);

            if (!string.IsNullOrWhiteSpace(target.Topic))
            {
                var topic = target.Topic;
                if (this.Client.Session.Keys.Contains(topic))
                {
                    await PrivateEngineTasks.DeleteSession(topic);
                    this.Client.Events.Trigger("session_expire", topic);
                } 
                else if (this.Client.Pairing.Keys.Contains(topic))
                {
                    await PrivateEngineTasks.DeletePairing(topic);
                    this.Client.Events.Trigger("pairing_expire", topic);
                }
            } 
            else if (target.Id != null)
            {
                await PrivateEngineTasks.DeleteProposal((long) target.Id);
            }
        }

        private void BuildRequestResponseTypeMapping()
        {
            requestTypeMapping.Clear();
            responseTypeMapping.Clear();
            
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in asm.GetTypes())
                {
                    var attributes = type.GetCustomAttributes(typeof(WcMethodAttribute), true);

                    if (attributes.Length == 0) continue;
                    if (attributes.Length > 1) throw new Exception("Only one attribute can be defined");

                    var attribute = (WcMethodAttribute)attributes[0];

                    var methodName = attribute.MethodName;
                    
                    requestTypeMapping.Add(methodName, type);
                    responseTypeMapping.Add(methodName, attribute.ResponseType);
                }
            }
        }

        private void RegisterRelayerEvents()
        {
            // Register all Request Types
            HandleMessageType<PairingDelete, bool>(PrivateEngineTasks.OnPairingDeleteRequest, null);
            HandleMessageType<PairingPing, bool>(PrivateEngineTasks.OnPairingPingRequest, PrivateEngineTasks.OnSessionRequestResponse);
            HandleMessageType<SessionPropose, SessionProposeResponse>(PrivateEngineTasks.OnSessionProposeRequest, PrivateEngineTasks.OnSessionProposeResponse);
            HandleMessageType<SessionSettle, bool>(PrivateEngineTasks.OnSessionSettleRequest, PrivateEngineTasks.OnSessionSettleResponse);
            HandleMessageType<SessionUpdate, bool>(PrivateEngineTasks.OnSessionUpdateRequest, PrivateEngineTasks.OnSessionUpdateResponse);
            HandleMessageType<SessionExtend, bool>(PrivateEngineTasks.OnSessionExtendRequest, PrivateEngineTasks.OnSessionExtendResponse);
            HandleMessageType<SessionDelete, bool>(PrivateEngineTasks.OnSessionDeleteRequest, null);
            HandleMessageType<SessionPing, bool>(PrivateEngineTasks.OnSessionPingRequest, PrivateEngineTasks.OnSessionPingResponse);

            this.Client.Core.Relayer.On<MessageEvent>(RelayerEvents.Message, RelayerMessageCallback);
        }

        private async void RelayerMessageCallback(object sender, GenericEvent<MessageEvent> e)
        {
            var topic = e.EventData.Topic;
            var message = e.EventData.Message;

            var payload = await this.Client.Core.Crypto.Decode<JsonRpcPayload>(topic, message);
            if (payload.IsRequest)
            {
                Events.Trigger($"request_{payload.Method}", e.EventData);
            }
            else if (payload.IsResponse)
            {
                Events.Trigger($"response_raw", new DecodedMessageEvent()
                {
                    Topic = topic,
                    Message = message,
                    Payload = payload
                });
            }
        }

        // TODO Deal with later?
        public void HandleSessionRequestMessageType<T, TR>(Func<string, JsonRpcRequest<SessionRequest<T>>, Task> requestCallback, Func<string, JsonRpcResponse<TR>, Task> responseCallback)
        {
            HandleMessageType(requestCallback, responseCallback);
        }
        
        public void HandleEventMessageType<T>(Func<string, JsonRpcRequest<SessionEvent<T>>, Task> requestCallback, Func<string, JsonRpcResponse<bool>, Task> responseCallback)
        {
            HandleMessageType(requestCallback, responseCallback);
        }

        public void HandleMessageType<T, TR>(Func<string, JsonRpcRequest<T>, Task> requestCallback, Func<string, JsonRpcResponse<TR>, Task> responseCallback)
        {
            var attributes = typeof(T).GetCustomAttributes(typeof(WcMethodAttribute), true);
            if (attributes.Length != 1)
                throw new Exception($"Type {typeof(T).FullName} has no WcMethod attribute!");

            var method = attributes.Cast<WcMethodAttribute>().First().MethodName;
            
            async void RequestCallback(object sender, GenericEvent<MessageEvent> e)
            {
                var topic = e.EventData.Topic;
                var message = e.EventData.Message;

                var payload = await this.Client.Core.Crypto.Decode<JsonRpcRequest<T>>(topic, message);
                
                this.Client.History.JsonRpcHistoryOfType<T, TR>().Set(topic, payload, null);

                if (requestCallback != null)
                    await requestCallback(topic, payload);
            }
            
            async void ResponseCallback(object sender, GenericEvent<MessageEvent> e)
            {
                var topic = e.EventData.Topic;
                var message = e.EventData.Message;

                var payload = await this.Client.Core.Crypto.Decode<JsonRpcResponse<TR>>(topic, message);

                await this.Client.History.JsonRpcHistoryOfType<T, TR>().Resolve(payload);

                if (responseCallback != null)
                    await responseCallback(topic, payload);
            }

            async void InspectResponseRaw(object sender, GenericEvent<DecodedMessageEvent> e)
            {
                var topic = e.EventData.Topic;
                var message = e.EventData.Message;

                var payload = e.EventData.Payload;

                try
                {
                    var record = await this.Client.History.JsonRpcHistoryOfType<T, TR>().Get(topic, payload.Id);

                    // ignored if we can't find anything in the history
                    if (record == null) return;
                    var resMethod = record.Request.Method;
                    
                    // Trigger the true response event, which will trigger ResponseCallback
                    Events.Trigger($"response_{resMethod}", new MessageEvent()
                    {
                        Topic = topic,
                        Message = message
                    });
                }
                catch
                {
                    // ignored if we can't find anything in the history
                }
            }

            Events.ListenFor<MessageEvent>($"request_{method}", RequestCallback);
            
            Events.ListenFor<MessageEvent>($"response_{method}", ResponseCallback);
            
            // Handle response_raw in this context
            // This will allow us to examine response_raw in every typed context registered
            Events.ListenFor<DecodedMessageEvent>($"response_raw", InspectResponseRaw);
        }

        Task<long> IEnginePrivate.SendRequest<T>(string topic, T parameters)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.SendResult<T>(long id, string topic, T result)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.SendError(long id, string topic, ErrorResponse error)
        {
            throw new System.NotImplementedException();
        }

        void IEnginePrivate.OnRelayEventRequest<T>(EngineCallback<JsonRpcRequest<T>> @event)
        {
            throw new System.NotImplementedException();
        }

        void IEnginePrivate.OnRelayEventResponse<T>(EngineCallback<JsonRpcResponse<T>> @event)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.ActivatePairing(string topic)
        {    
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.DeleteSession(string topic)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.DeletePairing(string topic)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.DeleteProposal(long id)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.SetExpiry(string topic, long expiry)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.SetProposal(long id, ProposalStruct proposal)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.Cleanup()
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.OnSessionProposeRequest(string topic, JsonRpcRequest<SessionPropose> payload)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.OnSessionProposeResponse(string topic, JsonRpcResponse<SessionProposeResponse> payload)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.OnSessionSettleRequest(string topic, JsonRpcRequest<SessionSettle> payload)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.OnSessionSettleResponse(string topic, JsonRpcResponse<bool> payload)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.OnSessionUpdateRequest(string topic, JsonRpcRequest<SessionUpdate> payload)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.OnSessionUpdateResponse(string topic, JsonRpcResponse<bool> payload)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.OnSessionExtendRequest(string topic, JsonRpcRequest<SessionExtend> payload)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.OnSessionExtendResponse(string topic, JsonRpcResponse<bool> payload)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.OnSessionPingRequest(string topic, JsonRpcRequest<SessionPing> payload)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.OnSessionPingResponse(string topic, JsonRpcResponse<bool> payload)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.OnPairingPingRequest(string topic, JsonRpcRequest<PairingPing> payload)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.OnPairingPingResponse(string topic, JsonRpcResponse<bool> payload)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.OnSessionDeleteRequest(string topic, JsonRpcRequest<SessionDelete> payload)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.OnPairingDeleteRequest(string topic, JsonRpcRequest<PairingDelete> payload)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.OnSessionRequest<T>(string topic, JsonRpcRequest<SessionRequest<T>> payload)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.OnSessionRequestResponse<T>(string topic, JsonRpcResponse<T> payload)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.OnSessionEventRequest<T>(string topic, JsonRpcRequest<SessionEvent<T>> payload)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.IsValidConnect(ConnectParams @params)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.IsValidPair(PairParams pairParams)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.IsValidSessionSettleRequest(SessionSettle settle)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.IsValidApprove(ApproveParams @params)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.IsValidReject(RejectParams @params)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.IsValidUpdate(UpdateParams @params)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.IsValidExtend(ExtendParams @params)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.IsValidRequest<T>(RequestParams<T> @params)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.IsValidRespond<T>(RespondParams<T> @params)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.IsValidPing(PingParams @params)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.IsValidEmit<T>(EmitParams<T> @params)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.IsValidDisconnect(DisconnectParams @params)
        {
            throw new System.NotImplementedException();
        }

        public async Task<IConnectedData> Connect(ConnectParams @params)
        {
            this.IsInitialized();
            await ((IEnginePrivate)this).IsValidConnect(@params);
            var topic = @params.PairingTopic;
            string uri;
            var active = false;

            if (string.IsNullOrEmpty(topic))
            {
                var pairing = this.Client.Pairing.Get(topic);
                active = pairing.Active;
            }

            if (!string.IsNullOrEmpty(topic) || !active)
            {
                CreatePairingData CreatePairing = await this.CreatePairing();
                topic = CreatePairing.Topic;
                uri = CreatePairing.Uri;

            }
            
            
        }

        private void IsInitialized()
        {
            throw new System.NotImplementedException();
        }

        private async Task<CreatePairingData> CreatePairing()
        {
            throw new System.NotImplementedException();
        }


        public Task<PairingStruct> Pair(PairParams pairParams)
        {
            throw new System.NotImplementedException();
        }

        public Task<IApprovedData> Approve(ApproveParams @params)
        {
            throw new System.NotImplementedException();
        }

        public Task Reject(RejectParams @params)
        {
            throw new System.NotImplementedException();
        }

        public Task<IAcknowledgement> Update(UpdateParams @params)
        {
            throw new System.NotImplementedException();
        }

        public Task<IAcknowledgement> Extend(ExtendParams @params)
        {
            throw new System.NotImplementedException();
        }

        public Task<TR> Request<T, TR>(RequestParams<T> @params)
        {
            throw new System.NotImplementedException();
        }

        public Task Respond<TR>(RespondParams<TR> @params)
        {
            throw new System.NotImplementedException();
        }

        public Task Emit<T>(EmitParams<T> @params)
        {
            throw new System.NotImplementedException();
        }

        public Task Ping(PingParams @params)
        {
            throw new System.NotImplementedException();
        }

        public Task Disconnect(DisconnectParams @params)
        {
            throw new System.NotImplementedException();
        }

        public SessionStruct[] Find(FindParams @params)
        {
            throw new System.NotImplementedException();
        }
    }
}