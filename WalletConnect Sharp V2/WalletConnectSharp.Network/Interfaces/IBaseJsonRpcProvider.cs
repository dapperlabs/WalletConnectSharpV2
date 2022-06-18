using System.Threading.Tasks;
using WalletConnectSharp.Events.Interfaces;

namespace WalletConnectSharp.Network
{
    public interface IBaseJsonRpcProvider : IEvents
    {
        Task Connect<T>(T @params);
        
        Task Disconnect();
        
        Task<TR> Request<T, TR>(IRequestArguments<T> request, object context);
    }
}