using Newtonsoft.Json;
using WalletConnectSharp.Common.Utils;
using WalletConnectSharp.Core.Models.Relay;
using WalletConnectSharp.Sign.Interfaces;

namespace WalletConnectSharp.Sign.Models.Engine.Methods
{
    [WcMethod("wc_sessionPropose")]
    [RpcRequestOptions(Clock.FIVE_MINUTES, true, 1100)]
    [RpcResponseOptions(Clock.FIVE_MINUTES, false, 1101)]
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