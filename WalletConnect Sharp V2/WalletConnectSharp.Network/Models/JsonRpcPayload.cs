using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WalletConnectSharp.Network.Models
{
    public class JsonRpcPayload : IJsonRpcPayload
    {
        public long Id { get; set; }
        public string JsonRPC { get; set; }
        
        [JsonExtensionData]
        private IDictionary<string, JToken> _extraStuff;

        public bool IsRequest
        {
            get
            {
                return _extraStuff.ContainsKey("method");
            }
        }

        public bool IsResponse
        {
            get
            {
                return _extraStuff.ContainsKey("result");
            }
        }

        public bool IsError
        {
            get
            {
                return _extraStuff.ContainsKey("error");
            }
        }

        public JsonRpcPayload()
        {
        }

        public JsonRpcPayload(long id, string jsonRpc)
        {
            Id = id;
            JsonRPC = jsonRpc;
        }
    }
}