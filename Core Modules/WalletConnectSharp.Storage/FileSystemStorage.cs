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
    public class FileSystemStorage : InMemoryStorage
    {
        public string FilePath { get; private set; }
        
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
        
        public override async Task SetItem<T>(string key, T value)
        {
            await base.SetItem<T>(key, value);
            await Save();
        }
        public override async Task RemoveItem(string key)
        {
            await base.RemoveItem(key);
            await Save();
        }

        public override async Task Clear()
        {
            await base.Clear();
            await Save();
        }

        private async Task Save()
        {
            var path = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrWhiteSpace(path) && !Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            
            var json = JsonConvert.SerializeObject(Entries, new JsonSerializerSettings()
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
            Entries = JsonConvert.DeserializeObject<Dictionary<string, object>>(json, new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Auto
            });
        }
    }
}