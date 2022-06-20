using System;
using System.Threading.Tasks;
using WalletConnectSharp.Events.Interfaces;

namespace WalletConnectSharp.Network
{
    public interface IJsonRpcConnection : IEvents, IDisposable
    {
        bool Connected { get; }
        
        bool Connecting { get; }

        Task Open();
        
        Task Open<T>(T options);

        Task Close();
        
        Task SendRequest<T>(IJsonRpcRequest<T> requestPayload, object context);

        Task SendResult<T>(IJsonRpcResult<T> requestPayload, object context);
        
        Task SendError(IJsonRpcError requestPayload, object context);
    }
}