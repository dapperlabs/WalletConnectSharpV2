using System.Threading.Tasks;
using WalletConnectSharp.Events.Interfaces;

namespace WalletConnectSharp.Network
{
    public interface IJsonRpcConnection : IEvents
    {
        bool Connected { get; }
        
        bool Connecting { get; }

        Task Open<T>(T options);

        Task Close();
        
        Task SendRequest<T>(T requestPayload, object context) where T : IJsonRpcRequest<T>;
        
        Task SendResult<T>(T requestPayload, object context) where T : IJsonRpcResult<T>;
        
        Task SendError<T>(T requestPayload, object context) where T : IJsonRpcError;
    }
}