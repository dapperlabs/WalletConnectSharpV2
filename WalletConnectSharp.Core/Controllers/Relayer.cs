using System;
using System.Threading.Tasks;
using WalletConnectSharp.Core.Interfaces;
using WalletConnectSharp.Core.Models.Relay;
using WalletConnectSharp.Events;
using WalletConnectSharp.Events.Model;
using WalletConnectSharp.Network;

namespace WalletConnectSharp.Core.Controllers
{
    public class Relayer : IRelayer
    {
        public EventDelegator Events { get; }

        public string Name { get; }
        public string Context { get; }
        public ICore Core { get; }
        public ISubscriber Subscriber { get; }
        public IPublisher Publisher { get; }
        public IMessageTracker Messages { get; }
        public IJsonRpcProvider Provider { get; }
        public bool Connected { get; }
        public bool Connecting { get; }
        public Task Init()
        {
            throw new NotImplementedException();
        }

        public Task Publish(string topic, string message, PublishOptions opts = null)
        {
            throw new NotImplementedException();
        }

        public Task<string> Subscribe(string topic, SubscribeOptions opts = null)
        {
            throw new NotImplementedException();
        }

        public Task Unsubscribe(string topic, UnsubscribeOptions opts = null)
        {
            throw new NotImplementedException();
        }
    }
}