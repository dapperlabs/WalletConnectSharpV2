using Newtonsoft.Json;

namespace WalletConnectSharp.Network
{
    public interface IJsonRpcProviderMessage<T>
    {
        [JsonProperty("type")]
        string Type { get; }
        
        [JsonProperty("data")]
        T Data { get; }
    }
}