using Newtonsoft.Json;

namespace WalletConnectSharp.Network.Models
{
    /// <summary>
    /// Indicates an error
    /// </summary>
    public class ErrorResponse
    {
        /// <summary>
        /// The error code of this error
        /// </summary>
        [JsonProperty("code")]
        public long Code;

        /// <summary>
        /// The message for this error
        /// </summary>
        [JsonProperty("message")]
        public string Message;

        /// <summary>
        /// Any extra data for this error
        /// </summary>
        [JsonProperty("data")]
        public string Data;
    }
}