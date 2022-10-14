using System;
using Newtonsoft.Json;
using WalletConnectSharp.Core.Models.Relay;
using WalletConnectSharp.Sign.Interfaces;

namespace WalletConnectSharp.Sign.Models.Engine
{
    [WcMethod("wc_sessionSettle", typeof(bool))]
    public class SessionSettle : IWcMethod
    {
        [JsonProperty("relay")]
        public ProtocolOptions Relay { get; set; }
        
        [JsonProperty("namespaces")]
        public Namespaces Namespaces { get; set; }
        
        [JsonProperty("expiry")]
        public long Expiry { get; set; }
        
        [JsonProperty("controller")]
        public Participant Controller { get; set; }
    }
}