using System.Collections.Generic;
using System.Threading.Tasks;
using WalletConnectSharp.Common;

namespace WalletConnectSharp.Crypto.Interfaces
{
    public interface IKeyChain : IService
    {
        IReadOnlyDictionary<string, string> Keychain { get; }

        Task Init();

        bool Has(string tag);

        Task Set(string tag, string key);

        string Get(string tag);

        Task Delete(string tag);
    }
}