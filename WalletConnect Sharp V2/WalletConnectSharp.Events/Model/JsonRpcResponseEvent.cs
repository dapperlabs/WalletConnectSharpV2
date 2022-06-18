namespace WalletConnectSharp.Events.Model
{
    public class JsonRpcResponseEvent<T> : GenericEvent<T> where T : JsonRpcResponse
    {
    }
}