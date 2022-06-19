using Newtonsoft.Json;

namespace WalletConnectSharp.Network
{
    public interface IJsonRpcResult<T> : IJsonRpcError
    {
        [JsonProperty("result")]
        T Result { get; }
    }
}