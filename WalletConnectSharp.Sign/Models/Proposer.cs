using Newtonsoft.Json;

namespace WalletConnectSharp.Sign.Models
{
    public class Proposer
    {
        [JsonProperty("publicKey")]
        public string PublicKey { get; set; }
        
        [JsonProperty("metadata")]
        public Metadata Metadata { get; set; }
    }
}