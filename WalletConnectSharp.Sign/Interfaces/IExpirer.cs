using System.Threading.Tasks;
using WalletConnectSharp.Common;
using WalletConnectSharp.Events.Interfaces;
using WalletConnectSharp.Sign.Models.Expirer;

namespace WalletConnectSharp.Sign.Interfaces
{
    public interface IExpirer : IModule, IEvents
    {
        int Length { get; }
        
        string[] Keys { get; }
        
        Expiration[] Values { get; }

        Task Init();

        bool Has(string key);

        bool Has(int key);

        void Set(string key, int expiry);

        void Set(int key, int expiry);

        Expiration Get(string key);

        Expiration Get(int key);

        void Delete(string key);

        void Delete(int key);
    }
}