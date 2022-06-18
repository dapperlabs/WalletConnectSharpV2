using Newtonsoft.Json;

namespace WalletConnectSharp.Network
{
    public interface IJsonRpcPayload
    {
        [JsonProperty("id")]
        long Id { get; }
        
        [JsonProperty("jsonrpc")]
        string JsonRPC { get; }
    }
}