using System.Threading.Tasks;
using WalletConnectSharp.Sign.Models;
using WalletConnectSharp.Sign.Models.Engine;

namespace WalletConnectSharp.Sign.Interfaces
{
    public interface IEngine : IEngineTasks
    {
        ISignClient Client { get; }

        Task Init();
    }
}