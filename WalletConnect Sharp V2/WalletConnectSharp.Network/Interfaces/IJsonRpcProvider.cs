using System;
using System.Threading.Tasks;

namespace WalletConnectSharp.Network
{
    public interface IJsonRpcProvider : IBaseJsonRpcProvider
    {
        Task Connect(string connection);

        Task Connect(IJsonRpcConnection connection);
    }
}
