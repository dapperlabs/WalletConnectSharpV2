using System;
using Newtonsoft.Json;
using WalletConnectSharp.Network.Models;
using WalletConnectSharp.Sign.Interfaces;

namespace WalletConnectSharp.Sign.Models.Engine
{
    [WcMethod("wc_sessionRequest", typeof(JsonRpcPayload))]
    public class SessionRequest<T> : IWcMethod
    {
        [JsonProperty("chainId")]
        public string ChainId { get; set; }
        
        [JsonProperty("request")]
        public JsonRpcRequest<T> Request { get; set; }
    }
}