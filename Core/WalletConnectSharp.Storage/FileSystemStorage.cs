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
    public class FileSystemStorage : IKeyValueStorage
    {
        public string FilePath { get; private set; }
        private Dictionary<string, object> _openWith = new Dictionary<string, object>();

        public FileSystemStorage(string filePath = null)
        {
            if (filePath == null)
            {
                var home = 
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                filePath = Path.Combine(home, ".wc", "store.json");
            }

            FilePath = filePath;
            
            Load();
        }
        
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
        
        public async Task SetItem<T>(string key, T value)
        {
            _openWith[key] = value;
            await Save();
        }
        public async Task RemoveItem(string key)
        {
            _openWith.Remove(key);
            await Save();
        }

        public Task<bool> HasItem(string key)
        {
            return Task.FromResult(_openWith.ContainsKey(key));
        }

        public async Task Clear()
        {
            _openWith.Clear();
            await Save();
        }

        private async Task Save()
        {
            var path = Path.GetDirectoryName(FilePath);
            if (path != null && !Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            
            var json = JsonConvert.SerializeObject(_openWith, new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.All
            });

            await File.WriteAllTextAsync(FilePath, json, Encoding.UTF8);
        }

        private async void Load()
        {
            if (!File.Exists(FilePath))
                return;
            
            var json = await File.ReadAllTextAsync(FilePath, Encoding.UTF8);
            _openWith = JsonConvert.DeserializeObject<Dictionary<string, object>>(json, new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Auto
            });
        }
    }
}