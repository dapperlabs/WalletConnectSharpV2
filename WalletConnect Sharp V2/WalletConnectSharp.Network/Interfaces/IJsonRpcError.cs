using Newtonsoft.Json;
using WalletConnectSharp.Network.Models;

namespace WalletConnectSharp.Network
{
    public interface IJsonRpcError : IJsonRpcPayload
    {
        [JsonProperty("error")]
        ErrorResponse Error { get; }
    }
}