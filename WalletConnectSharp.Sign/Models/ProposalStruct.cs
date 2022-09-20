using Newtonsoft.Json;
using WalletConnectSharp.Core.Interfaces;
using WalletConnectSharp.Core.Models.Relay;

namespace WalletConnectSharp.Sign.Models
{
    public struct ProposalStruct : IKeyHolder<long>
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonIgnore]
        public long Key
        {
            get
            {
                return Id;
            }
        }
        
        [JsonProperty("expiry")]
        public long Expiry { get; set; }
        
        [JsonProperty("relays")]
        public ProtocolOptions[] Relays { get; set; }
        
        [JsonProperty("proposer")]
        public Proposer Proposer { get; set; }
        
        [JsonProperty("requiredNamespaces")]
        public RequiredNamespaces RequiredNamespaces { get; set; }
        
        [JsonProperty("pairingTopic")]
        public string PairingTopic { get; set; }
    }
}