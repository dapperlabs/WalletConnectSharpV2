using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WalletConnectSharp.Storage.Interfaces;

namespace WalletConnectSharp.Storage
{
    public class InMemoryStorage : IKeyValueStorage
    {
        protected Dictionary<string, object> _entries = new Dictionary<string, object>();

        public virtual Task<string[]> GetKeys()
        {
            return Task.FromResult(_entries.Keys.ToArray());
        }

        public virtual async Task<T[]> GetEntriesOfType<T>()
        {
            return (await GetEntries()).OfType<T>().ToArray();
        }

        public virtual Task<object[]> GetEntries()
        {
            return Task.FromResult(_entries.Values.ToArray());
        }

        public virtual Task<T> GetItem<T>(string key)
        {
            return Task.FromResult(_entries[key] is T ? (T)_entries[key] : default);
        }
        
        public virtual async Task SetItem<T>(string key, T value)
        {
            _entries[key] = value;
        }
        public virtual async Task RemoveItem(string key)
        {
            _entries.Remove(key);
        }

        public virtual Task<bool> HasItem(string key)
        {
            return Task.FromResult(_entries.ContainsKey(key));
        }

        public virtual async Task Clear()
        {
            _entries.Clear();
        }
    }
}