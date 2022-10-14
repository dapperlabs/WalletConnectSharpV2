using System;
using Newtonsoft.Json;
using WalletConnectSharp.Core.Models.Relay;
using WalletConnectSharp.Sign.Interfaces;

namespace WalletConnectSharp.Sign.Models.Engine
{
    [WcMethod("wc_sessionPropose", typeof(SessionProposeResponse))]
    public class SessionPropose : IWcMethod
    {
        [JsonProperty("relays")]
        public ProtocolOptions[] Relays { get; set; }
        
        [JsonProperty("requiredNamespaces")]
        public RequiredNamespaces RequiredNamespaces { get; set; }
        
        [JsonProperty("proposer")]
        public Participant Proposer { get; set; }
    }
}