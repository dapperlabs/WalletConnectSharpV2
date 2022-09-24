using Newtonsoft.Json;

namespace WalletConnectSharp.Core.Models.Relay
{
    public class PublishOptions : ProtocolOptionHolder
    {
        [JsonProperty("ttl")]
        public int TTL { get; set; }
        
        [JsonProperty("prompt")]
        public bool Prompt { get; set; }
        
        [JsonProperty("tag")]
        public int Tag { get; set; }
    }
}