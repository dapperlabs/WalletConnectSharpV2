using Newtonsoft.Json;
using WalletConnectSharp.Sign.Interfaces;

namespace WalletConnectSharp.Sign.Models.Engine
{
    [WcMethod("wc_sessionEvent")]
    public class SessionEvent<T> : IWcMethod
    {
        [JsonProperty("chainId")]
        public string ChainId { get; set; }
        
        [JsonProperty("event")]
        public EventData<T> Event { get; set; }
    }
}