namespace WalletConnectSharp.Network.Models
{
    public abstract class BaseJsonRpcRequest<T> : IRequestArguments<T>
    {
        public abstract string Method { get; }
        public T Params { get; protected set; }

        protected BaseJsonRpcRequest()
        {
        }

        protected BaseJsonRpcRequest(T @params)
        {
            Params = @params;
        }
    }
}