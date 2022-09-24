using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WalletConnectSharp.Common;
using WalletConnectSharp.Core.Interfaces;
using WalletConnectSharp.Network.Models;

namespace WalletConnectSharp.Core.Controllers
{
    public class Store<TKey, TValue> : IStore<TKey, TValue> where TValue : IKeyHolder<TKey>
    {
        private bool initialized;
        private Dictionary<TKey, TValue> map = new Dictionary<TKey, TValue>();
        private TValue[] cached = Array.Empty<TValue>();
        public ICore Core { get; }
        
        public string StoragePrefix { get; }
        
        public Func<TValue, TKey> GetKey { get; }

        public string Version
        {
            get
            {
                return "0.3";
            }
        }
        public string Name { get; }
        public string Context { get; }

        public string StorageKey
        {
            get
            {
                return StoragePrefix + Version + "//" + Name;
            }
        }

        public int Length
        {
            get
            {
                return map.Count;
            }
        }

        public TKey[] Keys
        {
            get
            {
                return map.Keys.ToArray();
            }
        }

        public TValue[] Values
        {
            get
            {
                return map.Values.ToArray();
            }
        }

        public Store(ICore core, string name, string storagePrefix = null, Func<TValue, TKey> getKey = null)
        {
            Name = name;
            Context = name;
            Core = core;

            if (storagePrefix == null)
                StoragePrefix = WalletConnectSharp.Core.Core.STORAGE_PREFIX;
            else
                StoragePrefix = storagePrefix;

            GetKey = getKey;
        }

        public async Task Init()
        {
            if (!initialized)
            {
                await Restore();

                foreach (var value in cached)
                {
                    if (value != null)
                        map.Add(value.Key, value);
                }

                cached = Array.Empty<TValue>();
                initialized = true;
            }
        }

        public Task Set(TKey key, TValue value)
        {
            IsInitialized();

            if (map.ContainsKey(key))
            {
                return Update(key, value);
            }
            map.Add(key, value);
            return Persist();
        }

        public TValue Get(TKey key)
        {
            IsInitialized();
            var value = GetData(key);
            return value;
        }

        public Task Update(TKey key, TValue update)
        {
            IsInitialized();
            
            // Partial updates aren't allowed in C#
            // So Update will just replace the value

            map.Remove(key);
            map.Add(key, update);

            return Persist();
        }

        public Task Delete(TKey key, ErrorResponse reason)
        {
            IsInitialized();

            if (!map.ContainsKey(key)) return Task.CompletedTask;

            map.Remove(key);

            return Persist();
        }

        protected virtual Task SetDataStore(TValue[] data)
        {
            return Core.Storage.SetItem<TValue[]>(StorageKey, data);
        }

        protected virtual async Task<TValue[]> GetDataStore()
        {
            if (await Core.Storage.HasItem(StorageKey))
                return await Core.Storage.GetItem<TValue[]>(StorageKey);

            return Array.Empty<TValue>();
        }

        protected virtual TValue GetData(TKey key)
        {
            if (!map.ContainsKey(key))
            {
                throw WalletConnectException.FromType(ErrorType.NO_MATCHING_KEY, $"{Name}: {key}");
            }

            return map[key];
        }

        protected virtual Task Persist()
        {
            return SetDataStore(Values);
        }

        protected virtual async Task Restore()
        {
            var persisted = await GetDataStore();
            if (persisted == null) return;
            if (persisted.Length == 0) return;
            if (map.Count > 0)
            {
                throw WalletConnectException.FromType(ErrorType.RESTORE_WILL_OVERRIDE, Name);
            }

            cached = persisted;
        }

        protected virtual void IsInitialized()
        {
            if (!initialized)
            {
                throw WalletConnectException.FromType(ErrorType.NOT_INITIALIZED, Name);
            }
        }
    }
}