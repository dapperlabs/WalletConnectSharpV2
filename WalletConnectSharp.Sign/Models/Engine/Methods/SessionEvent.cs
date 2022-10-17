using Newtonsoft.Json;
using WalletConnectSharp.Common.Utils;
using WalletConnectSharp.Sign.Interfaces;

namespace WalletConnectSharp.Sign.Models.Engine.Methods
{
    [WcMethod("wc_sessionEvent")]
    [RpcRequestOptions(Clock.FIVE_MINUTES, true, 1110)]
    [RpcResponseOptions(Clock.FIVE_MINUTES, false, 1111)]
    public class SessionEvent<T> : IWcMethod
    {
        [JsonProperty("chainId")]
        public string ChainId { get; set; }
        
        [JsonProperty("event")]
        public EventData<T> Event { get; set; }
    }
}