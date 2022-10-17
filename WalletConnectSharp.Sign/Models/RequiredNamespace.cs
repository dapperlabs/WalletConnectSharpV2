using Newtonsoft.Json;

namespace WalletConnectSharp.Sign.Models
{
    public class RequiredNamespace : BaseRequiredNamespace
    {
        [JsonProperty("extension")]
        public BaseRequiredNamespace[] Extension { get; set; }
    }
}