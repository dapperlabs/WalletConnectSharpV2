using Newtonsoft.Json;
using WalletConnectSharp.Events.Utils;

namespace WalletConnectSharp.Network.Models
{
    public class JsonRpcRequest<T> : IJsonRpcRequest<T>
    {
        public string Method { get; private set; }
        public T Params { get; private set; }
        public long Id { get; private set; }

        public string JsonRPC
        {
            get
            {
                return "2.0";
            }
        }

        public JsonRpcRequest(string method, T param, long? id = null)
        {
            if (id == null)
            {
                id = RpcPayloadId.Generate();
            }

            this.Method = method;
            this.Params = param;
            this.Id = (long)id;
        }
    }
}