using Newtonsoft.Json;
using WalletConnectSharp.Common.Utils;
using WalletConnectSharp.Sign.Interfaces;

namespace WalletConnectSharp.Sign.Models.Engine.Methods
{
    [WcMethod("wc_sessionUpdate")]
    [RpcRequestOptions(Clock.ONE_DAY, false, 1104)]
    [RpcResponseOptions(Clock.ONE_DAY, false, 1105)]
    public class SessionUpdate : IWcMethod
    {
        [JsonProperty("namespaces")]
        public Namespaces Namespaces { get; set; }
    }
}