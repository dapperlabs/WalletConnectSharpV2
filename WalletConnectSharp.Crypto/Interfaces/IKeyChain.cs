using System.Collections.Generic;
using System.Threading.Tasks;
using WalletConnectSharp.Common;
using WalletConnectSharp.Storage.Interfaces;

namespace WalletConnectSharp.Crypto.Interfaces
{
    public interface IKeyChain : IModule
    {
        IReadOnlyDictionary<string, string> Keychain { get; }
        
        IKeyValueStorage Storage { get; }

        Task Init();

        Task<bool> Has(string tag);

        Task Set(string tag, string key);

        Task<string> Get(string tag);

        Task Delete(string tag);
    }
}