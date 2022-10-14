using Newtonsoft.Json;
using WalletConnectSharp.Sign.Interfaces;

namespace WalletConnectSharp.Sign.Models.Engine
{
    [WcMethod("wc_pairingDelete")]
    public class PairingDelete : IWcMethod
    {
        [JsonProperty("code")]
        public long Code { get; set; }
        
        [JsonProperty("message")]
        public string Message { get; set; }
    }
}