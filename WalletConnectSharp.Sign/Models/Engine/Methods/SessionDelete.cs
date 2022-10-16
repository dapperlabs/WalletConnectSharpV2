using Newtonsoft.Json;
using WalletConnectSharp.Common.Utils;
using WalletConnectSharp.Sign.Interfaces;

namespace WalletConnectSharp.Sign.Models.Engine.Methods
{
    [WcMethod("wc_sessionDelete")]
    [RpcRequestOptions(Clock.ONE_DAY, false, 1112)]
    [RpcResponseOptions(Clock.ONE_DAY, false, 1113)]
    public class SessionDelete : IWcMethod
    {
        [JsonProperty("code")]
        public long Code { get; set; }
        
        [JsonProperty("message")]
        public string Message { get; set; }
    }
}