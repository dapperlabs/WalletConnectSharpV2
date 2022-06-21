using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using WalletConnectSharp.Common;
using WalletConnectSharp.Crypto.Interfaces;

namespace WalletConnectSharp.Crypto
{
    public class KeyChain : IKeyChain
    {
        private Dictionary<string, string> _keyChain;
        
        public IReadOnlyDictionary<string, string> Keychain => new ReadOnlyDictionary<string, string>(_keyChain);

        public string Name
        {
            get
            {
                return "keychain";
            }
        }

        public string Context
        {
            get
            {
                //TODO Set to logger context
                return "walletconnectsharp";
            }
        }

        public string Version
        {
            get
            {
                return "0.3";
            }
        }

        public string StorageKey => this._storagePrefix + this.Version + "//" + this.Name;

        private bool _initialized = false;
        private readonly string _storagePrefix = Constants.CORE_STORAGE_PREFIX;

        public async Task Init()
        {
            if (!this._initialized)
            {
                //TODO Grab keychain from storage
                var keyChain = await GetKeyChain();
                if (keyChain != null)
                {
                    this._keyChain = keyChain;
                }

                this._initialized = true;
            }
        }

        private async Task<Dictionary<string, string>> GetKeyChain()
        {
            //TODO Grab from storage
            return new Dictionary<string, string>();
        }

        private async Task SaveKeyChain()
        {
            //TODO Save keychain to storage
        }

        public Task<bool> Has(string tag)
        {
            this.IsInitialized();
            return Task.FromResult(this._keyChain.ContainsKey(tag));
        }

        public async Task Set(string tag, string key)
        {
            this.IsInitialized();
            if (await Has(tag))
            {
                this._keyChain[tag] = key;
            }
            else
            {
                this._keyChain.Add(tag, key);
            }

            await SaveKeyChain();
        }

        public async Task<string> Get(string tag)
        {
            this.IsInitialized();

            if (!await Has(tag))
            {
                throw WalletConnectException.FromType(ErrorType.NO_MATCHING_KEY, new {tag});
            }

            return this._keyChain[tag];
        }

        public async Task Delete(string tag)
        {
            this.IsInitialized();
            
            if (!await Has(tag))
            {
                throw WalletConnectException.FromType(ErrorType.NO_MATCHING_KEY, new {tag});
            }

            _keyChain.Remove(tag);

            await this.SaveKeyChain();
        }

        private void IsInitialized()
        {
            if (!this._initialized)
            {
                throw WalletConnectException.FromType(ErrorType.NOT_INITIALIZED, new {Name});
            }
        }
    }
}