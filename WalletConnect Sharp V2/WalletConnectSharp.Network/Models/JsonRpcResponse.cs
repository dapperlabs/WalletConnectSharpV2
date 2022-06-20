namespace WalletConnectSharp.Network.Models
{
    public class JsonRpcResponse<T> : IJsonRpcResult<T>
    {
        public long Id { get; set; }

        public string JsonRPC
        {
            get
            {
                return "2.0";
            }
        }

        public ErrorResponse Error { get; set; }
        public T Result { get; set; }

        public JsonRpcResponse()
        {
        }

        public JsonRpcResponse(long id, ErrorResponse error, T result)
        {
            Id = id;
            Error = error;
            Result = result;
        }
    }
}