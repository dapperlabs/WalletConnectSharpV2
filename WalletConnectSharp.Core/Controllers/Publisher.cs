using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WalletConnectSharp.Common.Utils;
using WalletConnectSharp.Core.Interfaces;
using WalletConnectSharp.Core.Models.Heartbeat;
using WalletConnectSharp.Core.Models.Publisher;
using WalletConnectSharp.Core.Models.Relay;
using WalletConnectSharp.Core.Utils;
using WalletConnectSharp.Events;
using WalletConnectSharp.Events.Model;
using WalletConnectSharp.Network.Models;

namespace WalletConnectSharp.Core.Controllers
{
    public class Publisher : IPublisher
    {
        public EventDelegator Events { get; }
        public IRelayer Relayer { get; }

        public string Name
        {
            get
            {
                return "publisher";
            }
        }

        public string Context
        {
            get
            {
                return "publisher";
            }
        }

        protected Dictionary<string, PublishParams> queue = new Dictionary<string, PublishParams>();

        public Publisher(IRelayer relayer)
        {
            Events = new EventDelegator(this);
            Relayer = relayer;
            
            RegisterEventListeners();
        }

        private void RegisterEventListeners()
        {
            Relayer.Core.HeartBeat.On<object>(HeartbeatEvents.Pulse, (_, __) => CheckQueue());
        }

        private async void CheckQueue()
        {
            foreach (var (_, @params) in queue)
            {
                var hash = HashUtils.HashMessage(@params.Message);
                await RpcPublish(@params.Topic, @params.Message, @params.Options.TTL, @params.Options.Relay,
                    @params.Options.Prompt, @params.Options.TTL);
                OnPublish(hash);
            }
        }

        private void OnPublish(string hash)
        {
            this.queue.Remove(hash);
        }

        protected Task RpcPublish(string topic, string message, long ttl, ProtocolOptions relay, bool prompt = false,
            long? tag = null)
        {
            var api = RelayProtocols.GetRelayProtocol(relay.Protocol);
            var request = new RequestArguments<RelayPublishParams>()
            {
                Method = api.Publish,
                Params = new RelayPublishParams()
                {
                    Message = message,
                    Topic = topic,
                    TTL = ttl,
                    Prompt = prompt,
                    Tag = tag
                }
            };

            return this.Relayer.Provider.Request<RelayPublishParams, object>(request, this);
        }

        public async Task Publish(string topic, string message, PublishOptions opts = null)
        {
            if (opts == null)
            {
                opts = new PublishOptions()
                {
                    Prompt = false,
                    Relay = new ProtocolOptions()
                    {
                        Protocol = RelayProtocols.Default
                    },
                    Tag = 0,
                    TTL = Clock.SIX_HOURS,
                };
            }
            else
            {
                if (opts.Relay == null)
                {
                    opts.Relay = new ProtocolOptions()
                    {
                        Protocol = RelayProtocols.Default
                    };
                }
            }

            var @params = new PublishParams()
            {
                Message = message,
                Options = opts,
                Topic = topic
            };

            var hash = HashUtils.HashMessage(message);
            queue.Add(hash, @params);
            await RpcPublish(topic, message, @params.Options.TTL, @params.Options.Relay, @params.Options.Prompt,
                @params.Options.Tag);
            OnPublish(hash);
        }
    }
}