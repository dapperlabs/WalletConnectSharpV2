using Newtonsoft.Json;

namespace WalletConnectSharp.Network
{
    public class JsonRpcProviderMessage<T>
    {
        [JsonProperty("type")]
        public string Type { get; private set; }
        
        [JsonProperty("type")]
        public T Data { get; private set; }

        public JsonRpcProviderMessage(string type, T data)
        {
            Type = type;
            Data = data;
        }
    }
}