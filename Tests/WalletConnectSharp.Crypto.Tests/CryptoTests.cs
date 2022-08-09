using WalletConnectSharp.Crypto.Tests.Models;
using Xunit;

namespace WalletConnectSharp.Crypto.Tests
{
    public class CryptoTests : IClassFixture<CryptoFixture>
    {
        private CryptoFixture _cryptoFixture;

        public Crypto Crypto
        {
            get
            {
                return _cryptoFixture.Crypto;
            }
        }

        public CryptoTests(CryptoFixture cryptoFixture)
        {
            this._cryptoFixture = cryptoFixture;
        }
        
        [Fact]
        public async void TestEncryptDecrypt()
        {
            var message = "This is a test message";

            var key = await Crypto.GenerateKeyPair();
            var otherKey = await Crypto.GenerateKeyPair();
            var symKey = await Crypto.GenerateSharedKey(key, otherKey);

            var encrypted = await Crypto.Encrypt(symKey, message);

            var decrypted = await Crypto.Decrypt(symKey, encrypted);

            Assert.Equal(message, decrypted);
        }

        [Fact]
        public async void TestEncodeDecode()
        {
            var message = new TestWakuRequest("test");
            
            var key = await Crypto.GenerateKeyPair();
            var otherKey = await Crypto.GenerateKeyPair();
            var symKey = await Crypto.GenerateSharedKey(key, otherKey);

            var encoded = await Crypto.Encode(symKey, message);
            var decoded = await Crypto.Decode<TestWakuRequest>(symKey, encoded);
            
            Assert.Equal(message.Params.Topic, decoded.Params.Topic);
        }
        
        
    }
}