using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WalletConnectSharp.Common;
using WalletConnectSharp.Common.Utils;
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
using WalletConnectSharp.Sign.Models.Engine.Methods;
using WalletConnectSharp.Sign.Models.Expirer;

namespace WalletConnectSharp.Sign
{
    public class Engine : IEnginePrivate, IEngine, IModule
    {
        private const long ProposalExpiry = Clock.THIRTY_DAYS;
        private const long SessionExpiry = Clock.SEVEN_DAYS;
        private const int KeyLength = 32;
        
        private EventDelegator Events;

        private bool initialized = false;
        
        public ISignClient Client { get; }

        private IEnginePrivate PrivateThis => this;

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
                await PrivateThis.Cleanup();
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
                    await PrivateThis.DeleteSession(topic);
                    this.Client.Events.Trigger("session_expire", topic);
                } 
                else if (this.Client.Pairing.Keys.Contains(topic))
                {
                    await PrivateThis.DeletePairing(topic);
                    this.Client.Events.Trigger("pairing_expire", topic);
                }
            } 
            else if (target.Id != null)
            {
                await PrivateThis.DeleteProposal((long) target.Id);
            }
        }

        private void RegisterRelayerEvents()
        {
            // Register all Request Types
            HandleMessageType<PairingDelete, bool>(PrivateThis.OnPairingDeleteRequest, null);
            HandleMessageType<PairingPing, bool>(PrivateThis.OnPairingPingRequest, PrivateThis.OnPairingPingResponse);
            HandleMessageType<SessionPropose, SessionProposeResponse>(PrivateThis.OnSessionProposeRequest, PrivateThis.OnSessionProposeResponse);
            HandleMessageType<SessionSettle, bool>(PrivateThis.OnSessionSettleRequest, PrivateThis.OnSessionSettleResponse);
            HandleMessageType<SessionUpdate, bool>(PrivateThis.OnSessionUpdateRequest, PrivateThis.OnSessionUpdateResponse);
            HandleMessageType<SessionExtend, bool>(PrivateThis.OnSessionExtendRequest, PrivateThis.OnSessionExtendResponse);
            HandleMessageType<SessionDelete, bool>(PrivateThis.OnSessionDeleteRequest, null);
            HandleMessageType<SessionPing, bool>(PrivateThis.OnSessionPingRequest, PrivateThis.OnSessionPingResponse);

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
        public void HandleSessionRequestMessageType<T, TR>()
        {
            HandleMessageType<SessionRequest<T>, TR>((s, request) => PrivateThis.OnSessionRequest<T, TR>(s, request), (s, response) => PrivateThis.OnSessionRequestResponse<TR>(s, response));
        }
        
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
            var method = MethodForType<T>();
            
            async void RequestCallback(object sender, GenericEvent<MessageEvent> e)
            {
                var topic = e.EventData.Topic;
                var message = e.EventData.Message;

                var payload = await this.Client.Core.Crypto.Decode<JsonRpcRequest<T>>(topic, message);
                
                (await this.Client.History.JsonRpcHistoryOfType<T, TR>()).Set(topic, payload, null);

                if (requestCallback != null)
                    await requestCallback(topic, payload);
            }
            
            async void ResponseCallback(object sender, GenericEvent<MessageEvent> e)
            {
                var topic = e.EventData.Topic;
                var message = e.EventData.Message;

                var payload = await this.Client.Core.Crypto.Decode<JsonRpcResponse<TR>>(topic, message);

                await (await this.Client.History.JsonRpcHistoryOfType<T, TR>()).Resolve(payload);

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
                    var record = await (await this.Client.History.JsonRpcHistoryOfType<T, TR>()).Get(topic, payload.Id);

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

        public string MethodForType<T>()
        {
            var attributes = typeof(T).GetCustomAttributes(typeof(WcMethodAttribute), true);
            if (attributes.Length != 1)
                throw new Exception($"Type {typeof(T).FullName} has no WcMethod attribute!");

            var method = attributes.Cast<WcMethodAttribute>().First().MethodName;

            return method;
        }

        public PublishOptions RpcRequestOptionsForType<T>()
        {
            var attributes = typeof(T).GetCustomAttributes(typeof(RpcRequestOptionsAttribute), true);
            if (attributes.Length != 1)
                throw new Exception($"Type {typeof(T).FullName} has no RpcRequestOptions attribute!");

            var opts = attributes.Cast<RpcRequestOptionsAttribute>().First();

            return new PublishOptions()
            {
                Prompt = opts.Prompt,
                Tag = opts.Tag,
                TTL = opts.TTL
            };
        }
        
        public PublishOptions RpcResponseOptionsForType<T>()
        {
            var attributes = typeof(T).GetCustomAttributes(typeof(RpcResponseOptionsAttribute), true);
            if (attributes.Length != 1)
                throw new Exception($"Type {typeof(T).FullName} has no RpcResponseOptions attribute!");

            var opts = attributes.Cast<RpcResponseOptionsAttribute>().First();

            return new PublishOptions()
            {
                Prompt = opts.Prompt,
                Tag = opts.Tag,
                TTL = opts.TTL
            };
        }

        async Task<long> IEnginePrivate.SendRequest<T, TR>(string topic, T parameters)
        {
            var method = MethodForType<T>();

            var payload = new JsonRpcRequest<T>(method, parameters);

            var message = await this.Client.Core.Crypto.Encode(topic, payload);

            var opts = RpcRequestOptionsForType<T>();
            
            (await this.Client.History.JsonRpcHistoryOfType<T, TR>()).Set(topic, payload, null);

            // await is intentionally omitted here because of a possible race condition
            // where a response is received before the publish call is resolved
#pragma warning disable CS4014
            this.Client.Core.Relayer.Publish(topic, message, opts);
#pragma warning restore CS4014

            return payload.Id;
        }

        async Task IEnginePrivate.SendResult<T, TR>(long id, string topic, TR result)
        {
            var payload = new JsonRpcResponse<TR>(id, null, result);
            var message = await this.Client.Core.Crypto.Encode(topic, payload);
         
            var opts = RpcResponseOptionsForType<T>();
            await this.Client.Core.Relayer.Publish(topic, message, opts);
            await (await this.Client.History.JsonRpcHistoryOfType<T, TR>()).Resolve(payload);
        }

        async Task IEnginePrivate.SendError<T, TR>(long id, string topic, ErrorResponse error)
        {
            var payload = new JsonRpcResponse<TR>(id, error, default);
            var message = await this.Client.Core.Crypto.Encode(topic, payload);
            var opts = RpcResponseOptionsForType<T>();
            await this.Client.Core.Relayer.Publish(topic, message, opts);
            await (await this.Client.History.JsonRpcHistoryOfType<T, TR>()).Resolve(payload);
        }

        async Task IEnginePrivate.ActivatePairing(string topic)
        {
            var expiry = Clock.CalculateExpiry(ProposalExpiry);
            await this.Client.Pairing.Update(topic, new PairingStruct()
            {
                Active = true,
                Expiry = expiry
            });

            await PrivateThis.SetExpiry(topic, expiry);
        }

        async Task IEnginePrivate.DeleteSession(string topic, bool expirerHasDeleted = false)
        {
            var session = this.Client.Session.Get(topic);
            var self = session.Self;

            await this.Client.Core.Relayer.Unsubscribe(topic);
            await Task.WhenAll(
                this.Client.Session.Delete(topic, ErrorResponse.FromErrorType(ErrorType.USER_DISCONNECTED)),
                this.Client.Core.Crypto.DeleteKeyPair(self.PublicKey),
                this.Client.Core.Crypto.DeleteSymKey(topic),
                expirerHasDeleted ? Task.CompletedTask : this.Client.Expirer.Delete(topic)
            );
        }

        async Task IEnginePrivate.DeletePairing(string topic, bool expirerHasDeleted = false)
        {
            await this.Client.Core.Relayer.Unsubscribe(topic);
            await Task.WhenAll(
                this.Client.Pairing.Delete(topic, ErrorResponse.FromErrorType(ErrorType.USER_DISCONNECTED)),
                this.Client.Core.Crypto.DeleteSymKey(topic),
                expirerHasDeleted ? Task.CompletedTask : this.Client.Expirer.Delete(topic)
            );
        }

        Task IEnginePrivate.DeleteProposal(long id, bool expirerHasDeleted = false)
        {
            return Task.WhenAll(
                this.Client.Proposal.Delete(id, ErrorResponse.FromErrorType(ErrorType.USER_DISCONNECTED)),
                expirerHasDeleted ? Task.CompletedTask : this.Client.Expirer.Delete(id)
            );
        }

        async Task IEnginePrivate.SetExpiry(string topic, long expiry)
        {
            if (this.Client.Pairing.Keys.Contains(topic))
            {
                await this.Client.Pairing.Update(topic, new PairingStruct()
                {
                    Expiry = expiry
                });
            } 
            else if (this.Client.Session.Keys.Contains(topic))
            {
                await this.Client.Session.Update(topic, new SessionStruct()
                {
                    Expiry = expiry
                });
            }
            this.Client.Expirer.Set(topic, expiry);
        }

        async Task IEnginePrivate.SetProposal(long id, ProposalStruct proposal)
        {
            await this.Client.Proposal.Set(id, proposal);
            if (proposal.Expiry != null)
                this.Client.Expirer.Set(id, (long)proposal.Expiry);
        }

        Task IEnginePrivate.Cleanup()
        {
            List<string> sessionTopics = (from session in this.Client.Session.Values.Where(e => e.Expiry != null) where Clock.IsExpired(session.Expiry.Value) select session.Topic).ToList();
            List<string> pairingTopics = (from pair in this.Client.Pairing.Values.Where(e => e.Expiry != null) where Clock.IsExpired(pair.Expiry.Value) select pair.Topic).ToList();
            List<long> proposalIds = (from p in this.Client.Proposal.Values.Where(e => e.Expiry != null) where Clock.IsExpired(p.Expiry.Value) select p.Id.Value).ToList();

            return Task.WhenAll(
                sessionTopics.Select(t => PrivateThis.DeleteSession(t)).Concat(
                    pairingTopics.Select(t => PrivateThis.DeletePairing(t))
                ).Concat(
                    proposalIds.Select(id => PrivateThis.DeleteProposal(id))
                )
            );
        }

        async Task IEnginePrivate.OnSessionProposeRequest(string topic, JsonRpcRequest<SessionPropose> payload)
        {
            var @params = payload.Params;
            var id = payload.Id;
            try
            {
                var expiry = Clock.CalculateExpiry(Clock.FIVE_MINUTES);
                var proposal = new ProposalStruct()
                {
                    Id = id,
                    PairingTopic = topic,
                    Expiry = expiry,
                    Proposer = @params.Proposer,
                    Relays = @params.Relays,
                    RequiredNamespaces = @params.RequiredNamespaces
                };
                await PrivateThis.SetProposal(id, proposal);
                this.Client.Events.Trigger("session_proposal", new JsonRpcRequest<ProposalStruct>()
                {
                    Id = id,
                    Params = proposal
                });
            }
            catch (WalletConnectException e)
            {
                await PrivateThis.SendError<SessionPropose, SessionProposeResponse>(id, topic,
                    ErrorResponse.FromException(e));
            }
        }

        async Task IEnginePrivate.OnSessionProposeResponse(string topic, JsonRpcResponse<SessionProposeResponse> payload)
        {
            var id = payload.Id;
            if (payload.IsError)
            {
                await this.Client.Proposal.Delete(id, ErrorResponse.FromErrorType(ErrorType.USER_DISCONNECTED));
                this.Events.Trigger("session_connect", payload);
            }
            else
            {
                var result = payload.Result;
                var proposal = this.Client.Proposal.Get(id);
                var selfPublicKey = proposal.Proposer.PublicKey;
                var peerPublicKey = result.ResponderPublicKey;

                var sessionTopic = await this.Client.Core.Crypto.GenerateSharedKey(
                    selfPublicKey,
                    peerPublicKey
                );
                var subscriptionId = await this.Client.Core.Relayer.Subscribe(sessionTopic);
                await PrivateThis.ActivatePairing(topic);
            }
        }

        async Task IEnginePrivate.OnSessionSettleRequest(string topic, JsonRpcRequest<SessionSettle> payload)
        {
            var id = payload.Id;
            var @params = payload.Params;
            try
            {
                await PrivateThis.IsValidSessionSettleRequest(@params);
                var relay = @params.Relay;
                var controller = @params.Controller;
                var expiry = @params.Expiry;
                var namespaces = @params.Namespaces;

                var session = new SessionStruct()
                {
                    Topic = topic,
                    Relay = relay,
                    Expiry = expiry,
                    Namespaces = namespaces,
                    Acknowledged = true,
                    Controller = controller.PublicKey,
                    Self = new Participant()
                    {
                        Metadata = this.Client.Metadata,
                        PublicKey = ""
                    },
                    Peer = new Participant()
                    {
                        PublicKey = controller.PublicKey,
                        Metadata = controller.Metadata
                    }
                };
                await PrivateThis.SendResult<SessionSettle, bool>(payload.Id, topic, true);
                this.Events.Trigger("session_connect", session);
            }
            catch (WalletConnectException e)
            {
                await PrivateThis.SendError<SessionSettle, bool>(id, topic, ErrorResponse.FromException(e));
            }
        }

        async Task IEnginePrivate.OnSessionSettleResponse(string topic, JsonRpcResponse<bool> payload)
        {
            var id = payload.Id;
            if (payload.IsError)
            {
                await this.Client.Session.Delete(topic, ErrorResponse.FromErrorType(ErrorType.USER_DISCONNECTED));
                this.Events.Trigger($"session_approve{id}", payload);
            }
            else
            {
                await this.Client.Session.Update(topic, new SessionStruct()
                {
                    Acknowledged = true
                });
                this.Events.Trigger($"session_approve{id}", payload); 
            }
        }

        async Task IEnginePrivate.OnSessionUpdateRequest(string topic, JsonRpcRequest<SessionUpdate> payload)
        {
            var @params = payload.Params;
            var id = payload.Id;
            try
            {
                await PrivateThis.IsValidUpdate(new UpdateParams()
                {
                    Namespaces = @params.Namespaces,
                    Topic = topic
                });

                await this.Client.Session.Update(topic, new SessionStruct()
                {
                    Namespaces = @params.Namespaces
                });

                await PrivateThis.SendResult<SessionUpdate, bool>(id, topic, true);
                this.Client.Events.Trigger("session_update", new SessionUpdateEvent()
                {
                    Id = id,
                    Topic = topic,
                    Params = @params
                });
            }
            catch (WalletConnectException e)
            {
                await PrivateThis.SendError<SessionUpdate, bool>(id, topic, ErrorResponse.FromException(e));
            }
        }

        async Task IEnginePrivate.OnSessionUpdateResponse(string topic, JsonRpcResponse<bool> payload)
        {
            var id = payload.Id;
            this.Events.Trigger($"session_update{id}", payload);
        }

        async Task IEnginePrivate.OnSessionExtendRequest(string topic, JsonRpcRequest<SessionExtend> payload)
        {
            var id = payload.Id;
            try
            {
                await PrivateThis.IsValidExtend(new ExtendParams()
                {
                    Topic = topic
                });
                await PrivateThis.SetExpiry(topic, Clock.CalculateExpiry(SessionExpiry));
                await PrivateThis.SendResult<SessionExtend, bool>(id, topic, true);
                this.Client.Events.Trigger("session_extend", new SessionEvent()
                {
                    Id = id,
                    Topic = topic
                });
            }
            catch (WalletConnectException e)
            {
                await PrivateThis.SendError<SessionExtend, bool>(id, topic, ErrorResponse.FromException(e));
            }
        }

        async Task IEnginePrivate.OnSessionExtendResponse(string topic, JsonRpcResponse<bool> payload)
        {
            var id = payload.Id;
            this.Events.Trigger($"session_extend{id}", payload);
        }

        async Task IEnginePrivate.OnSessionPingRequest(string topic, JsonRpcRequest<SessionPing> payload)
        {
            var id = payload.Id;
            try
            {
                await PrivateThis.IsValidPing(new PingParams()
                {
                    Topic = topic
                });
                await PrivateThis.SendResult<SessionPing, bool>(id, topic, true);
                this.Client.Events.Trigger("session_ping", new SessionEvent()
                {
                    Id = id,
                    Topic = topic
                });
            }
            catch (WalletConnectException e)
            {
                await PrivateThis.SendError<SessionPing, bool>(id, topic, ErrorResponse.FromException(e));
            }
        }

        async Task IEnginePrivate.OnSessionPingResponse(string topic, JsonRpcResponse<bool> payload)
        {
            var id = payload.Id;
            
            // put at the end of the stack to avoid a race condition
            // where session_ping listener is not yet initialized
            await Task.Delay(500);

            this.Events.Trigger($"session_ping{id}", payload);
        }

        async Task IEnginePrivate.OnPairingPingRequest(string topic, JsonRpcRequest<PairingPing> payload)
        {
            var id = payload.Id;
            try
            {
                await PrivateThis.IsValidPing(new PingParams()
                {
                    Topic = topic
                });

                await PrivateThis.SendResult<PairingPing, bool>(id, topic, true);
                this.Client.Events.Trigger("pairing_ping", new SessionEvent()
                {
                    Topic = topic,
                    Id = id
                });
            }
            catch (WalletConnectException e)
            {
                await PrivateThis.SendError<PairingPing, bool>(id, topic, ErrorResponse.FromException(e));
            }
        }

        async Task IEnginePrivate.OnPairingPingResponse(string topic, JsonRpcResponse<bool> payload)
        {
            var id = payload.Id;
            
            // put at the end of the stack to avoid a race condition
            // where session_ping listener is not yet initialized
            await Task.Delay(500);

            this.Events.Trigger($"pairing_ping{id}", payload);
        }

        async Task IEnginePrivate.OnSessionDeleteRequest(string topic, JsonRpcRequest<SessionDelete> payload)
        {
            var id = payload.Id;
            try
            {
                await PrivateThis.IsValidDisconnect(new DisconnectParams()
                {
                    Topic = topic,
                    Reason = payload.Params
                });

                await PrivateThis.SendResult<SessionDelete, bool>(id, topic, true);
                await PrivateThis.DeleteSession(topic);
                this.Client.Events.Trigger("session_delete", new SessionEvent()
                {
                    Topic = topic,
                    Id = id
                });
            }
            catch (WalletConnectException e)
            {
                await PrivateThis.SendError<SessionDelete, bool>(id, topic, ErrorResponse.FromException(e));
            }
        }

        async Task IEnginePrivate.OnPairingDeleteRequest(string topic, JsonRpcRequest<PairingDelete> payload)
        {
            var id = payload.Id;
            try
            {
                await PrivateThis.IsValidDisconnect(new DisconnectParams()
                {
                    Topic = topic,
                    Reason = payload.Params
                });

                await PrivateThis.SendResult<PairingDelete, bool>(id, topic, true);
                await PrivateThis.DeletePairing(topic);
                this.Client.Events.Trigger("pairing_delete", new SessionEvent()
                {
                    Topic = topic,
                    Id = id
                });
            }
            catch (WalletConnectException e)
            {
                await PrivateThis.SendError<PairingDelete, bool>(id, topic, ErrorResponse.FromException(e));
            }
        }

        async Task IEnginePrivate.OnSessionRequest<T, TR>(string topic, JsonRpcRequest<SessionRequest<T>> payload)
        {
            var id = payload.Id;
            var @params = payload.Params;
            try
            {
                await PrivateThis.IsValidRequest(new RequestParams<T>()
                {
                    Topic = topic,
                    ChainId = @params.ChainId,
                    Request = @params.Request
                });
                this.Client.Events.Trigger("session_request", new SessionRequestEvent<T>()
                {
                    Topic = topic,
                    Id = id,
                    ChainId = @params.ChainId,
                    Request = @params.Request
                });
            }
            catch (WalletConnectException e)
            {
                await PrivateThis.SendError<SessionRequest<T>, TR>(id, topic, ErrorResponse.FromException(e));
            }
        }

        async Task IEnginePrivate.OnSessionRequestResponse<T>(string topic, JsonRpcResponse<T> payload)
        {
            var id = payload.Id;
            this.Events.Trigger($"session_request{id}", payload);
        }

        async Task IEnginePrivate.OnSessionEventRequest<T>(string topic, JsonRpcRequest<SessionEvent<T>> payload)
        {
            var id = payload.Id;
            var @params = payload.Params;
            try
            {
                await PrivateThis.IsValidEmit(new EmitParams<T>()
                {
                    Topic = topic,
                    ChainId = @params.ChainId,
                    Event = @params.Event
                });
                this.Client.Events.Trigger("session_event", new EmitEvent<T>()
                {
                    Topic = topic,
                    Id = id,
                    Params = @params
                });
            }
            catch (WalletConnectException e)
            {
                await PrivateThis.SendError<SessionEvent<T>, object>(id, topic, ErrorResponse.FromException(e));
            }
        }

        async Task IEnginePrivate.IsValidConnect(ConnectParams @params)
        {
            if (@params == null)
                throw WalletConnectException.FromType(ErrorType.MISSING_OR_INVALID, $"Connect() params: {JsonConvert.SerializeObject(@params)}");

            var pairingTopic = @params.PairingTopic;
            var requiredNamespaces = @params.RequiredNamespaces;
            var relays = @params.Relays;

            if (pairingTopic != null)
                await IsValidPairingTopic(pairingTopic);
        }

        async Task IsValidPairingTopic(string topic)
        {
            if (string.IsNullOrWhiteSpace(topic))
                throw WalletConnectException.FromType(ErrorType.MISSING_OR_INVALID,
                    $"pairing topic should be a string {topic}");

            if (!this.Client.Pairing.Keys.Contains(topic))
                throw WalletConnectException.FromType(ErrorType.NO_MATCHING_KEY,
                    $"pairing topic doesn't exist {topic}");

            if (Clock.IsExpired(this.Client.Pairing.Get(topic).Expiry.Value))
            {
                await PrivateThis.DeletePairing(topic);
                throw WalletConnectException.FromType(ErrorType.EXPIRED, $"pairing topic: {topic}");
            }
        }

        async Task IsValidSessionTopic(string topic)
        {
            if (string.IsNullOrWhiteSpace(topic))
                throw WalletConnectException.FromType(ErrorType.MISSING_OR_INVALID,
                    $"session topic should be a string {topic}");
            
            if (!this.Client.Session.Keys.Contains(topic))
                throw WalletConnectException.FromType(ErrorType.NO_MATCHING_KEY,
                    $"session topic doesn't exist {topic}");
            
            if (Clock.IsExpired(this.Client.Session.Get(topic).Expiry.Value))
            {
                await PrivateThis.DeleteSession(topic);
                throw WalletConnectException.FromType(ErrorType.EXPIRED, $"session topic: {topic}");
            }
        }

        async Task IsValidProposalId(long id)
        {
            if (!this.Client.Proposal.Keys.Contains(id))
                throw WalletConnectException.FromType(ErrorType.NO_MATCHING_KEY,
                    $"proposal id doesn't exist {id}");
            
            if (Clock.IsExpired(this.Client.Proposal.Get(id).Expiry.Value))
            {
                await PrivateThis.DeleteProposal(id);
                throw WalletConnectException.FromType(ErrorType.EXPIRED, $"proposal id: {id}");
            }
        }

        async Task IsValidSessionOrPairingTopic(string topic)
        {
            if (this.Client.Session.Keys.Contains(topic)) await this.IsValidSessionTopic(topic);
            else if (this.Client.Pairing.Keys.Contains(topic)) await this.IsValidPairingTopic(topic);
            else if (string.IsNullOrWhiteSpace(topic))
                throw WalletConnectException.FromType(ErrorType.MISSING_OR_INVALID,
                    $"session or pairing topic should be a string {topic}");
            else
            {
                throw WalletConnectException.FromType(ErrorType.NO_MATCHING_KEY,
                    $"session or pairing topic doesn't exist {topic}");
            }
        }

        async Task IEnginePrivate.IsValidPair(PairParams pairParams)
        {
            // TODO Implement
        }

        async Task IEnginePrivate.IsValidSessionSettleRequest(SessionSettle settle)
        {
            // TODO Implement
        }

        async Task IEnginePrivate.IsValidApprove(ApproveParams @params)
        {
            // TODO Implement
        }

        async Task IEnginePrivate.IsValidReject(RejectParams @params)
        {
            // TODO Implement
        }

        async Task IEnginePrivate.IsValidUpdate(UpdateParams @params)
        {
            // TODO Implement
        }

        async Task IEnginePrivate.IsValidExtend(ExtendParams @params)
        {
            // TODO Implement
        }

        async Task IEnginePrivate.IsValidRequest<T>(RequestParams<T> @params)
        {
            // TODO Implement
        }

        async Task IEnginePrivate.IsValidRespond<T>(RespondParams<T> @params)
        {
            // TODO Implement
        }

        async Task IEnginePrivate.IsValidPing(PingParams @params)
        {
            // TODO Implement
        }

        async Task IEnginePrivate.IsValidEmit<T>(EmitParams<T> @params)
        {
            // TODO Implement
        }

        async Task IEnginePrivate.IsValidDisconnect(DisconnectParams @params)
        {
            // TODO Implement
        }

        private UriParameters ParseUri(string uri)
        {
            var pathStart = uri.IndexOf(":", StringComparison.Ordinal);
            int? pathEnd = uri.IndexOf("?", StringComparison.Ordinal) != -1 ? uri.IndexOf("?", StringComparison.Ordinal) : (int?)null;
            var protocol = uri.Substring(0, pathStart);

            string path;
            if (pathEnd != null) path = uri.Substring(pathStart + 1, (int)pathEnd);
            else path = uri.Substring(pathStart + 1);

            var requiredValues = path.Split("@");
            string queryString = pathEnd != null ? uri.Substring((int)pathEnd) : "";
            var queryParams = Regex.Matches(queryString, "([^?=&]+)(=([^&]*))?").Cast<Match>()
                .ToDictionary(x => x.Groups[1].Value, x => x.Groups[3].Value);

            var result = new UriParameters()
            {
                Protocol = protocol,
                Topic = requiredValues[0],
                Version = int.Parse(requiredValues[1]),
                SymKey = queryParams["symKey"],
                Relay = new ProtocolOptions()
                {
                    Protocol = queryParams["relay-protocol"],
                    Data = queryParams["relay-data"]
                }
            };

            return result;
        }

        public async Task<ConnectedData> Connect(ConnectParams @params)
        {
            this.IsInitialized();
            await PrivateThis.IsValidConnect(@params);
            var pairingTopic = @params.PairingTopic;
            var requiredNamespaces = @params.RequiredNamespaces;
            var relays = @params.Relays;
            var topic = @params.PairingTopic;
            string uri = "";
            var active = false;

            if (string.IsNullOrEmpty(topic))
            {
                var pairing = this.Client.Pairing.Get(topic);
                if (pairing.Active != null)
                    active = pairing.Active.Value;
            }

            if (!string.IsNullOrEmpty(topic) || !active)
            {
                CreatePairingData CreatePairing = await this.CreatePairing();
                topic = CreatePairing.Topic;
                uri = CreatePairing.Uri;
            }

            var publicKey = await this.Client.Core.Crypto.GenerateKeyPair();
            var proposal = new SessionPropose()
            {
                RequiredNamespaces = requiredNamespaces,
                Relays = relays != null
                    ? new[] { relays }
                    : new[]
                    {
                        new ProtocolOptions()
                        {
                            Protocol = RelayProtocols.Default
                        }
                    },
                Proposer = new Participant()
                {
                    PublicKey = publicKey,
                    Metadata = this.Client.Metadata
                }
            };

            TaskCompletionSource<SessionStruct> approvalTask = new TaskCompletionSource<SessionStruct>();
            this.Events.ListenForOnce<SessionStruct>("session_connect", async (sender, e) =>
            {
                var session = e.EventData;
                session.Self.PublicKey = publicKey;
                var completeSession = new SessionStruct()
                {
                    Acknowledged = session.Acknowledged,
                    Controller = session.Controller,
                    Expiry = session.Expiry,
                    RequiredNamespaces = requiredNamespaces,
                    Namespaces = session.Namespaces,
                    Peer = session.Peer,
                    Relay = session.Relay,
                    Self = session.Self,
                    Topic = session.Topic
                };
                await PrivateThis.SetExpiry(session.Topic, session.Expiry.Value);
                if (!string.IsNullOrWhiteSpace(topic))
                {
                    await this.Client.Pairing.Update(topic, new PairingStruct()
                    {
                        PeerMetadata = session.Peer.Metadata
                    });
                }
                approvalTask.SetResult(completeSession);
            });
            
            this.Events.ListenForOnce<JsonRpcResponse<SessionProposeResponse>>("session_connect", (sender, e) =>
            {
                if (e.EventData.IsError)
                {
                    approvalTask.SetException(WalletConnectException.FromType((ErrorType)e.EventData.Error.Code));
                }
            });

            if (string.IsNullOrWhiteSpace(topic))
            {
                throw WalletConnectException.FromType(ErrorType.NO_MATCHING_KEY, $"connect() pairing topic: {topic}");
            }

            var id = await PrivateThis.SendRequest<SessionPropose, bool>(topic, proposal);
            var expiry = Clock.CalculateExpiry(Clock.FIVE_MINUTES);

            await PrivateThis.SetProposal(id, new ProposalStruct()
            {
                Expiry = expiry,
                Id = id,
                Proposer = proposal.Proposer,
                Relays = proposal.Relays,
                RequiredNamespaces = proposal.RequiredNamespaces
            });

            return new ConnectedData()
            {
                Uri = uri,
                Approval = approvalTask.Task
            };
        }

        private void IsInitialized()
        {
            if (!initialized)
            {
                throw WalletConnectException.FromType(ErrorType.NOT_INITIALIZED, Name);
            }
        }

        private async Task<CreatePairingData> CreatePairing()
        {
            byte[] symKeyRaw = new byte[KeyLength];
            RandomNumberGenerator.Fill(symKeyRaw);
            var symKey = symKeyRaw.ToHex();
            var topic = await this.Client.Core.Crypto.SetSymKey(symKey);
            var expiry = Clock.CalculateExpiry(Clock.FIVE_MINUTES);
            var relay = new ProtocolOptions()
            {
                Protocol = RelayProtocols.Default
            };
            var pairing = new PairingStruct()
            {
                Topic = topic,
                Expiry = expiry,
                Relay = relay,
                Active = false,
            };
            var uri = $"{this.Client.Protocol}:{topic}@{this.Client.Version}?"
                .AddQueryParam("symKey", symKey)
                .AddQueryParam("relay-protocol", relay.Protocol)
                .AddQueryParam("relay-data", relay.Data);

            await this.Client.Pairing.Set(topic, pairing);
            await this.Client.Core.Relayer.Subscribe(topic);
            await PrivateThis.SetExpiry(topic, expiry);

            return new CreatePairingData()
            {
                Topic = topic,
                Uri = uri
            };
        }


        public async Task<PairingStruct> Pair(PairParams pairParams)
        {
            IsInitialized();
            await PrivateThis.IsValidPair(pairParams);
            var uriParams = ParseUri(pairParams.Uri);

            var topic = uriParams.Topic;
            var symKey = uriParams.SymKey;
            var relay = uriParams.Relay;
            var expiry = Clock.CalculateExpiry(Clock.FIVE_MINUTES);
            var pairing = new PairingStruct()
            {
                Topic = topic,
                Relay = relay,
                Expiry = expiry,
                Active = false,
            };

            await this.Client.Pairing.Set(topic, pairing);
            await this.Client.Core.Crypto.SetSymKey(symKey, topic);
            await this.Client.Core.Relayer.Subscribe(topic, new SubscribeOptions()
            {
                Relay = relay
            });
            await PrivateThis.SetExpiry(topic, expiry);

            return pairing;
        }

        public async Task<IApprovedData> Approve(ApproveParams @params)
        {
            IsInitialized();
            await PrivateThis.IsValidApprove(@params);
            var id = @params.Id;
            var relayProtocol = @params.RelayProtocol;
            var namespaces = @params.Namespaces;
            var proposal = this.Client.Proposal.Get(id);
            var pairingTopic = proposal.PairingTopic;
            var proposer = proposal.Proposer;
            var requiredNamespaces = proposal.RequiredNamespaces;

            var selfPublicKey = await this.Client.Core.Crypto.GenerateKeyPair();
            var peerPublicKey = proposer.PublicKey;
            var sessionTopic = await this.Client.Core.Crypto.GenerateSharedKey(
                selfPublicKey,
                peerPublicKey
            );

            var sessionSettle = new SessionSettle()
            {
                Relay = new ProtocolOptions()
                {
                    Protocol = relayProtocol != null ? relayProtocol : "irn"
                },
                Namespaces = namespaces,
                Controller = new Participant()
                {
                    PublicKey = selfPublicKey,
                    Metadata = this.Client.Metadata
                },
                Expiry = Clock.CalculateExpiry(SessionExpiry)
            };

            await this.Client.Core.Relayer.Subscribe(sessionTopic);
            var requestId = await PrivateThis.SendRequest<SessionSettle, bool>(sessionTopic, sessionSettle);

            var acknowledgedTask = new TaskCompletionSource<SessionStruct>();
            
            this.Events.ListenForOnce<JsonRpcResponse<bool>>($"session_approve{requestId}", (sender, e) =>
            {
                if (e.EventData.IsError)
                    acknowledgedTask.SetException(WalletConnectException.FromType((ErrorType)e.EventData.Error.Code));
                else
                    acknowledgedTask.SetResult(this.Client.Session.Get(sessionTopic));
            });

            var session = new SessionStruct()
            {
                Topic = sessionTopic,
                Acknowledged = false,
                Self = sessionSettle.Controller,
                Peer = proposer,
                Controller = selfPublicKey,
                Expiry = sessionSettle.Expiry,
                Namespaces = sessionSettle.Namespaces,
                Relay = sessionSettle.Relay,
                RequiredNamespaces = requiredNamespaces
            };

            await this.Client.Session.Set(sessionTopic, session);
            await PrivateThis.SetExpiry(sessionTopic, Clock.CalculateExpiry(SessionExpiry));
            if (!string.IsNullOrWhiteSpace(pairingTopic))
                await this.Client.Pairing.Update(pairingTopic, new PairingStruct()
                {
                    PeerMetadata = session.Peer.Metadata
                });

            if (!string.IsNullOrWhiteSpace(pairingTopic) && id > 0)
            {
                await PrivateThis.SendResult<SessionPropose, SessionProposeResponse>(id, pairingTopic,
                    new SessionProposeResponse()
                    {
                        Relay = new ProtocolOptions()
                        {
                            Protocol = relayProtocol != null ? relayProtocol : "irn"
                        },
                        ResponderPublicKey = selfPublicKey
                    });
                await this.Client.Proposal.Delete(id, ErrorResponse.FromErrorType(ErrorType.USER_DISCONNECTED));
                await PrivateThis.ActivatePairing(pairingTopic);
            }
            
            return IApprovedData.FromTask(sessionTopic, acknowledgedTask.Task);
        }

        public async Task Reject(RejectParams @params)
        {
            IsInitialized();
            await PrivateThis.IsValidReject(@params);
            var id = @params.Id;
            var reason = @params.Reason;
            var proposal = this.Client.Proposal.Get(id);
            var pairingTopic = proposal.PairingTopic;

            if (!string.IsNullOrWhiteSpace(pairingTopic))
            {
                await PrivateThis.SendError<SessionPropose, SessionProposeResponse>(id, pairingTopic, reason);
                await this.Client.Proposal.Delete(id, ErrorResponse.FromErrorType(ErrorType.USER_DISCONNECTED));
            }
        }

        public async Task<IAcknowledgement> Update(UpdateParams @params)
        {
            IsInitialized();
            await PrivateThis.IsValidUpdate(@params);
            var topic = @params.Topic;
            var namespaces = @params.Namespaces;
            var id = await PrivateThis.SendRequest<SessionUpdate, bool>(topic, new SessionUpdate()
            {
                Namespaces = namespaces
            });

            TaskCompletionSource<bool> acknowledgedTask = new TaskCompletionSource<bool>();
            this.Events.ListenForOnce<JsonRpcResponse<bool>>($"session_update{id}", (sender, e) =>
            {
                if (e.EventData.IsError)
                    acknowledgedTask.SetException(WalletConnectException.FromType((ErrorType)e.EventData.Error.Code));
                else
                    acknowledgedTask.SetResult(e.EventData.Result);
            });

            await this.Client.Session.Update(topic, new SessionStruct()
            {
                Namespaces = namespaces
            });
            
            return IAcknowledgement.FromTask(acknowledgedTask.Task);
        }

        public async Task<IAcknowledgement> Extend(ExtendParams @params)
        {
            IsInitialized();
            await PrivateThis.IsValidExtend(@params);
            var topic = @params.Topic;
            var id = await PrivateThis.SendRequest<SessionExtend, bool>(topic, new SessionExtend());
            
            TaskCompletionSource<bool> acknowledgedTask = new TaskCompletionSource<bool>();
            this.Events.ListenForOnce<JsonRpcResponse<bool>>($"session_extend{id}", (sender, e) =>
            {
                if (e.EventData.IsError)
                    acknowledgedTask.SetException(WalletConnectException.FromType((ErrorType)e.EventData.Error.Code));
                else
                    acknowledgedTask.SetResult(e.EventData.Result);
            });

            await PrivateThis.SetExpiry(topic, Clock.CalculateExpiry(SessionExpiry));
            
            return IAcknowledgement.FromTask(acknowledgedTask.Task);
        }

        public async Task<TR> Request<T, TR>(RequestParams<T> @params)
        {
            IsInitialized();
            await PrivateThis.IsValidRequest(@params);
            var chainId = @params.ChainId;
            var request = @params.Request;
            var topic = @params.Topic;
            HandleSessionRequestMessageType<T, TR>();
            
            var id = await PrivateThis.SendRequest<SessionRequest<T>, TR>(topic, new SessionRequest<T>()
            {
                ChainId = chainId,
                Request = request
            });
            
            var taskSource = new TaskCompletionSource<TR>();
            this.Events.ListenForOnce<JsonRpcResponse<TR>>($"session_request{id}", (sender, e) =>
            {
                if (e.EventData.IsError)
                    taskSource.SetException(WalletConnectException.FromType((ErrorType)e.EventData.Error.Code));
                else
                    taskSource.SetResult(e.EventData.Result);
            });

            return await taskSource.Task;
        }

        public async Task Respond<T, TR>(RespondParams<TR> @params) where T : IWcMethod
        {
            IsInitialized();
            await PrivateThis.IsValidRespond(@params);
            var topic = @params.Topic;
            var response = @params.Response;
            var id = response.Id;
            if (response.IsError)
            {
                await PrivateThis.SendError<T, TR>(id, topic, response.Error);
            }
            else
            {
                await PrivateThis.SendResult<T, TR>(id, topic, response.Result);
            }
        }

        public async Task Emit<T>(EmitParams<T> @params)
        {
            IsInitialized();
            var topic = @params.Topic;
            var @event = @params.Event;
            var chainId = @params.ChainId;
            await PrivateThis.SendRequest<SessionEvent<T>, object>(topic, new SessionEvent<T>()
            {
                ChainId = chainId,
                Event = @event
            });
        }

        public async Task Ping(PingParams @params)
        {
            IsInitialized();
            await PrivateThis.IsValidPing(@params);

            var topic = @params.Topic;
            if (this.Client.Session.Keys.Contains(topic))
            {
                var id = await PrivateThis.SendRequest<SessionPing, bool>(topic, new SessionPing());
                var done = new TaskCompletionSource<bool>();
                this.Events.ListenForOnce<JsonRpcResponse<bool>>($"session_ping{id}", (sender, e) =>
                {
                    if (e.EventData.IsError)
                        done.SetException(WalletConnectException.FromType((ErrorType)e.EventData.Error.Code));
                    else
                        done.SetResult(e.EventData.Result);
                });
                await done.Task;
            } 
            else if (this.Client.Pairing.Keys.Contains(topic))
            {
                var id = await PrivateThis.SendRequest<PairingPing, bool>(topic, new PairingPing());
                var done = new TaskCompletionSource<bool>();
                this.Events.ListenForOnce<JsonRpcResponse<bool>>($"pairing_ping{id}", (sender, e) =>
                {
                    if (e.EventData.IsError)
                        done.SetException(WalletConnectException.FromType((ErrorType)e.EventData.Error.Code));
                    else
                        done.SetResult(e.EventData.Result);
                });
                await done.Task;
            }
        }

        public async Task Disconnect(DisconnectParams @params)
        {
            IsInitialized();
            await PrivateThis.IsValidDisconnect(@params);
            var topic = @params.Topic;
            
            var error = ErrorResponse.FromErrorType(ErrorType.USER_DISCONNECTED);
            if (this.Client.Session.Keys.Contains(topic))
            {
                await PrivateThis.SendRequest<SessionDelete, bool>(topic, new SessionDelete()
                {
                    Code = error.Code,
                    Message = error.Message,
                    Data = error.Data
                });
                await PrivateThis.DeleteSession(topic);
            } 
            else if (this.Client.Pairing.Keys.Contains(topic))
            {
                await PrivateThis.SendRequest<PairingDelete, bool>(topic, new PairingDelete()
                {
                    Code = error.Code,
                    Data = error.Data,
                    Message = error.Message
                });
                await PrivateThis.DeletePairing(topic);
            }
        }

        private bool HasOverlap(string[] a, string[] b)
        {
            var matches = a.Where(x => b.Contains(x));
            return matches.Count() == a.Length;
        }

        private string[] GetAccountsChains(string[] accounts)
        {
            List<string> chains = new List<string>();
            foreach (var account in accounts)
            {
                var values = account.Split(":");
                var chain = values[0];
                var chainId = values[1];
                
                chains.Add($"{chain}:{chainId}");
            }

            return chains.ToArray();
        }

        private bool IsSessionCompatible(SessionStruct session, FindParams @params)
        {
            var requiredNamespaces = @params.RequiredNamespaces;
            var compatible = true;

            var sessionKeys = session.Namespaces.Keys.ToArray();
            var paramsKeys = requiredNamespaces.Keys.ToArray();

            if (!HasOverlap(paramsKeys, sessionKeys)) return false;

            foreach (var key in sessionKeys)
            {
                var value = session.Namespaces[key];
                var accounts = value.Accounts;
                var methods = value.Methods;
                var events = value.Events;
                var extension = value.Extension;
                var chains = GetAccountsChains(accounts);
                var requiredNamespace = requiredNamespaces[key];

                if (!HasOverlap(requiredNamespace.Chains, chains) ||
                    !HasOverlap(requiredNamespace.Methods, methods) ||
                    !HasOverlap(requiredNamespace.Events, events))
                    compatible = false;

                if (compatible && extension != null && extension.Length > 0)
                {
                    foreach (var extensionNamespace in extension)
                    {
                        var extAccounts = extensionNamespace.Accounts;
                        var extMethods = extensionNamespace.Methods;
                        var extEvents = extensionNamespace.Events;
                        var extChains = GetAccountsChains(extAccounts);
                        var overlap = false;

                        if (requiredNamespace.Extension != null)
                            overlap = requiredNamespace.Extension.Any(re =>
                                HasOverlap(re.Chains, extChains) && HasOverlap(re.Methods, extMethods) &&
                                HasOverlap(re.Events, extEvents));

                        if (!overlap) compatible = false;
                    }
                }
            }

            return compatible;
        }

        public SessionStruct[] Find(FindParams @params)
        {
            IsInitialized();
            return this.Client.Session.Values.Where(s => IsSessionCompatible(s, @params)).ToArray();
        }
    }
}