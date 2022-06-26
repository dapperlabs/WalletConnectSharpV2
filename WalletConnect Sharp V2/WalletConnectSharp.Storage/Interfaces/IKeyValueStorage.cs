using System;
using System.Threading.Tasks;

namespace WalletConnectSharp.Storage.Interfaces
{
    public interface IKeyValueStorage
    {
        Task<string[]> GetKeys();

        Task<T[]> GetEntriesOfType<T>();

        Task<object[]> GetEntries();

        Task<T> GetItem<T>(string key);

        Task SetItem<T>(string key, T value);

        Task RemoveItem(string key);
    }
}