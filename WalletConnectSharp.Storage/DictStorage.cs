using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WalletConnectSharp.Storage.Interfaces;

namespace WalletConnectSharp.Storage
{
    public class DictStorage : IKeyValueStorage
    {
        private readonly Dictionary<string, object> _openWith = new Dictionary<string, object>();
        public Task<string[]> GetKeys()
        {
            return Task.FromResult(_openWith.Keys.ToArray());
        }

        public async Task<T[]> GetEntriesOfType<T>()
        {
            return (await GetEntries()).OfType<T>().ToArray();
        }

        public Task<object[]> GetEntries()
        {
            return Task.FromResult(_openWith.Values.ToArray());
        }

        public Task<T> GetItem<T>(string key)
        {
            return Task.FromResult(_openWith[key] is T ? (T)_openWith[key] : default);
        }
        
        public Task SetItem<T>(string key, T value)
        {
            _openWith[key] = value;
            return Task.CompletedTask;
        }
        public Task RemoveItem(string key)
        {
            _openWith.Remove(key);
            return Task.CompletedTask;
        }
    }
}