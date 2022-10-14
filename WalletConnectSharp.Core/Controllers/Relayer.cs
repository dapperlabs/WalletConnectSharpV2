using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WalletConnectSharp.Common;
using WalletConnectSharp.Core.Interfaces;
using WalletConnectSharp.Core.Models.Relay;
using WalletConnectSharp.Core.Models.Subscriber;
using WalletConnectSharp.Core.Utils;
using WalletConnectSharp.Events;
using WalletConnectSharp.Events.Model;
using WalletConnectSharp.Network;
using WalletConnectSharp.Network.Models;
using WalletConnectSharp.Network.Websocket;

namespace WalletConnectSharp.Core.Controllers
{
    public class Relayer : IRelayer
    {
        public static readonly string DEFAULT_RELAY_URL = "wss://relay.walletconnect.com";

        // TODO Pull this from assembly version
        public static readonly string SDK_VERSION = "2.0.0-rc.1";
        public EventDelegator Events { get; }

        public string Name
        {
            get
            {
                return "relayer";
            }
        }

        public string Context
        {
            get
            {
                return "relayer";
            }
        }

        public ICore Core { get; }
        public ISubscriber Subscriber { get; }
        public IPublisher Publisher { get; }
        public IMessageTracker Messages { get; }
        public IJsonRpcProvider Provider { get; private set; }
        
        public bool Connected { get; }
        public bool Connecting { get; }
        
        private string relayUrl;
        private string projectId;
        private bool initialized;
        
        public Relayer(RelayerOptions opts)
        {
            Events = new EventDelegator(this);
            Core = opts.Core;
            Messages = new MessageTracker(Core);
            Subscriber = new Subscriber(this);
            Publisher = new Publisher(this);

            relayUrl = opts.RelayUrl;
            if (string.IsNullOrWhiteSpace(relayUrl))
            {
                relayUrl = DEFAULT_RELAY_URL;
            }

            projectId = opts.ProjectId;
        }
        
        public async Task Init()
        {
            Provider = CreateProvider();

            await Task.WhenAll(
                Messages.Init(), Provider.Connect(), Subscriber.Init()
            );

            RegisterEventListeners();
            
            initialized = true;
        }

        protected virtual IJsonRpcProvider CreateProvider()
        {
            return new JsonRpcProvider(
                new WebsocketConnection(
                    FormatRelayRpcUrl(
                        IRelayer.Protocol,
                        IRelayer.Version.ToString(),
                        relayUrl,
                        SDK_VERSION,
                        projectId
                    )
                )
            );
        }

        protected virtual void RegisterEventListeners()
        {
            Provider.On<string>(ProviderEvents.RawRequestMessage, (sender, @event) =>
            {
                OnProviderPayload(@event.EventData);
            });
            
            Provider.On(ProviderEvents.Connect, () =>
            {
                Events.Trigger(RelayerEvents.Connect, new object());
            });
            
            Provider.On(ProviderEvents.Disconnect, async () =>
            {
                Events.Trigger(RelayerEvents.Disconnect, new object());

                // Attempt to reconnect after one second
                await Task.Delay(1000);
                await Provider.Connect();
            });

            Provider.On<object>(ProviderEvents.Error, (sender, @event) =>
            {
                Events.Trigger(RelayerEvents.Error, @event.EventData);
            });
        }

        protected virtual async void OnProviderPayload(string payloadJson)
        {
            var payload = JsonConvert.DeserializeObject<JsonRpcPayload>(payloadJson);

            if (payload != null && payload.IsRequest && payload.Method.EndsWith("_subscription"))
            {
                var @event = JsonConvert.DeserializeObject<JsonRpcRequest<JsonRpcSubscriptionParams>>(payloadJson);

                var messageEvent = new MessageEvent()
                {
                    Message = @event.Params.Data.Message,
                    Topic = @event.Params.Data.Topic
                };

                await AcknowledgePayload(payload);
                await OnMessageEvent(messageEvent);
            }
        }

        protected virtual async Task<bool> ShouldIgnoreMessageEvent(MessageEvent messageEvent)
        {
            if (!(await Subscriber.IsSubscribed(messageEvent.Topic))) return true;

            var exists = Messages.Has(messageEvent.Topic, messageEvent.Message);
            return exists;
        }

        protected virtual Task RecordMessageEvent(MessageEvent messageEvent)
        {
            return Messages.Set(messageEvent.Topic, messageEvent.Message);
        }

        protected virtual async Task OnMessageEvent(MessageEvent messageEvent)
        {
            if (await ShouldIgnoreMessageEvent(messageEvent)) return;

            Events.Trigger(RelayerEvents.Message, messageEvent);
            await RecordMessageEvent(messageEvent);
        }

        protected virtual async Task AcknowledgePayload(JsonRpcPayload payload)
        {
            var response = new JsonRpcResponse<bool>()
            {
                Id = payload.Id,
                Result = true
            };
            await Provider.Connection.SendResult(response, this);
        }

        protected virtual string FormatUA(string protocol, string version, string sdkVersion)
        {
            var os = Environment.OSVersion.Platform.ToString();
            var osVersion = Environment.OSVersion.Version.ToString();

            var osInfo = string.Format("-", os, osVersion);
            var environment = "WalletConnectSharpv2:" + Environment.Version;

            var sdkType = "C#";

            return string.Join("/", string.Join("-", protocol, version), string.Join("-", sdkType, sdkVersion), osInfo,
                environment);
        }

        protected virtual string FormatRelayRpcUrl(string protocol, string version, string relayUrl, string sdkVersion,
            string projectId)
        {
            var splitUrl = relayUrl.Split("?");
            var ua = FormatUA(protocol, version, sdkVersion);

            var currentParameters = UrlUtils.ParseQs(splitUrl.Length > 1 ? splitUrl[1] : "");
            
            currentParameters.Add("ua", ua);
            currentParameters.Add("projectId", projectId);

            var formattedParameters = UrlUtils.StringifyQs(currentParameters);

            return splitUrl[0] + "?" + formattedParameters;
        }

        public async Task Publish(string topic, string message, PublishOptions opts = null)
        {
            IsInitialized();
            await Publisher.Publish(topic, message, opts);
            await RecordMessageEvent(new MessageEvent()
            {
                Topic = topic,
                Message = message
            });
        }

        public Task<string> Subscribe(string topic, SubscribeOptions opts = null)
        {
            IsInitialized();
            return Subscriber.Subscribe(topic, opts);
        }

        public Task Unsubscribe(string topic, UnsubscribeOptions opts = null)
        {
            IsInitialized();
            return Subscriber.Unsubscribe(topic, opts);
        }

        protected virtual void IsInitialized()
        {
            if (!initialized)
            {
                throw WalletConnectException.FromType(ErrorType.NOT_INITIALIZED, Name);
            }
        }
    }
}