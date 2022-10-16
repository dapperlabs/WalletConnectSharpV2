using Newtonsoft.Json;
using WalletConnectSharp.Common.Utils;
using WalletConnectSharp.Sign.Interfaces;

namespace WalletConnectSharp.Sign.Models.Engine.Methods
{
    [WcMethod("wc_pairingDelete")]
    [RpcRequestOptions(Clock.ONE_DAY, false, 1000)]
    [RpcResponseOptions(Clock.ONE_DAY, false, 1001)]
    public class PairingDelete : IWcMethod
    {
        [JsonProperty("code")]
        public long Code { get; set; }
        
        [JsonProperty("message")]
        public string Message { get; set; }
    }
}