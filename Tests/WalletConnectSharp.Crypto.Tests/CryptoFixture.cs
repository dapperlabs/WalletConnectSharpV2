using System;
using System.Threading.Tasks;
using WalletConnectSharp.Storage;

namespace WalletConnectSharp.Crypto.Tests
{
    public class CryptoFixture : IDisposable
    {
        public Crypto PeerA { get; private set; }
        
        public Crypto PeerB { get; private set; }
        
        public CryptoFixture()
        {
            var storageA = new FileSystemStorage(".tests.peer.a");
            var storageB = new FileSystemStorage(".tests.peer.b");
            
            PeerA = new Crypto(storageA);
            PeerB = new Crypto(storageB);

            Init();
        }

        private async void Init()
        {
            await Task.WhenAll(PeerA.Init(), PeerB.Init());
        }
        
        public void Dispose()
        {
            PeerA.Storage.Clear();
            PeerB.Storage.Clear();
        }
    }
}