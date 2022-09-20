using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WalletConnectSharp.Storage.Interfaces;

namespace WalletConnectSharp.Storage
{
    public class InMemoryStorage : IKeyValueStorage
    {
        protected Dictionary<string, object> Entries = new Dictionary<string, object>();

        public virtual Task<string[]> GetKeys()
        {
            return Task.FromResult(Entries.Keys.ToArray());
        }

        public virtual async Task<T[]> GetEntriesOfType<T>()
        {
            return (await GetEntries()).OfType<T>().ToArray();
        }

        public virtual Task<object[]> GetEntries()
        {
            return Task.FromResult(Entries.Values.ToArray());
        }

        public virtual Task<T> GetItem<T>(string key)
        {
            return Task.FromResult(Entries[key] is T ? (T)Entries[key] : default);
        }
        
        public virtual Task SetItem<T>(string key, T value)
        {
            Entries[key] = value;
            return Task.CompletedTask;
        }
        public virtual Task RemoveItem(string key)
        {
            Entries.Remove(key);
            return Task.CompletedTask;
        }

        public virtual Task<bool> HasItem(string key)
        {
            return Task.FromResult(Entries.ContainsKey(key));
        }

        public virtual Task Clear()
        {
            Entries.Clear();
            return Task.CompletedTask;
        }
    }
}