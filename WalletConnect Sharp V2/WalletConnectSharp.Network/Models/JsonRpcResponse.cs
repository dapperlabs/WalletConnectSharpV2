namespace WalletConnectSharp.Network.Models
{
    /// <summary>
    /// Represents a full JSON RPC response with the given result type of T
    /// </summary>
    /// <typeparam name="T">The type of the result property for this JSON RPC response</typeparam>
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