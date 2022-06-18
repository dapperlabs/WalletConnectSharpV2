using Newtonsoft.Json;

namespace WalletConnectSharp.Network
{
    public interface IJsonRpcRequest<T> : IRequestArguments<T>, IJsonRpcPayload { }
}