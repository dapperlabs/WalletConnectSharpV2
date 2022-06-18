using Newtonsoft.Json;

namespace WalletConnectSharp.Network
{
    public interface IRequestArguments<T>
    {
        [JsonProperty("method")]
        string Method { get; }
        
        [JsonProperty("params")]
        T Params { get; }
    }
}