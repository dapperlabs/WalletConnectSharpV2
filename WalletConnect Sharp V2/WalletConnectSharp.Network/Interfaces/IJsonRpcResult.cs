using Newtonsoft.Json;

namespace WalletConnectSharp.Network
{
    public interface IJsonRpcResult<T> : IJsonRpcPayload
    {
        [JsonProperty("result")]
        T Result { get; }
    }
}