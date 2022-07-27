using System;

namespace WalletConnectSharp.Crypto.Tests
{
    public class CryptoFixture : IDisposable
    {
        public Crypto Crypto { get; private set; }
        
        public CryptoFixture()
        {
            Crypto = new Crypto();

            Init();
        }

        private async void Init()
        {
            await Crypto.Init();
        }
        
        public void Dispose()
        {
            Crypto.Storage.Clear();
        }
    }
}