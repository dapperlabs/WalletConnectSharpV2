using Newtonsoft.Json;

namespace WalletConnectSharp.Sign.Models
{
    public class Namespace : BaseNamespace
    {
        [JsonProperty("extension")]
        public BaseNamespace[] Extension { get; set; }
    }
}