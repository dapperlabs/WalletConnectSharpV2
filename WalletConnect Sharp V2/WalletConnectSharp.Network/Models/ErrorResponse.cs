using Newtonsoft.Json;

namespace WalletConnectSharp.Network.Models
{
    public class ErrorResponse
    {
        [JsonProperty("code")]
        public long Code;

        [JsonProperty("message")]
        public string Message;

        [JsonProperty("data")]
        public string Data;
    }
}