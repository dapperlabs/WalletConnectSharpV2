using System;
using Newtonsoft.Json;
using WalletConnectSharp.Sign.Interfaces;

namespace WalletConnectSharp.Sign.Models.Engine
{
    [WcMethod("wc_sessionUpdate", typeof(bool))]
    public class SessionUpdate : IWcMethod
    {
        [JsonProperty("namespaces")]
        public Namespaces Namespaces { get; set; }
    }
}