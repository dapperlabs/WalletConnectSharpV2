using System.Threading.Tasks;

namespace WalletConnectSharp.Sign.Models.Engine
{
    public interface IConnectedData
    {
        string Uri { get; }

        Task<SessionStruct> Approval();
    }
}