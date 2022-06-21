using System.Collections.Generic;
using System.Threading.Tasks;
using WalletConnectSharp.Crypto.Interfaces;

namespace WalletConnectSharp.Crypto
{
    public class KeyChain : IKeyChain
    {
        public IReadOnlyDictionary<string, string> Keychain { get; }
        public string Name { get; }
        public string Context { get; }
        public Task Init()
        {
            throw new System.NotImplementedException();
        }

        public bool Has(string tag)
        {
            throw new System.NotImplementedException();
        }

        public Task Set(string tag, string key)
        {
            throw new System.NotImplementedException();
        }

        public string Get(string tag)
        {
            throw new System.NotImplementedException();
        }

        public Task Delete(string tag)
        {
            throw new System.NotImplementedException();
        }
    }
}