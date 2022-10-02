using Newtonsoft.Json;
using WalletConnectSharp.Core.Models.Relay;

namespace WalletConnectSharp.Sign.Models.Engine
{
    public class RpcOpts
    {
        [JsonProperty("req")]
        public PublishOptions Req { get; set; }
        
        [JsonProperty("res")]
        public PublishOptions Res { get; set; }
    }
}