using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NSec.Cryptography;
using WalletConnectSharp.Common;
using WalletConnectSharp.Crypto.Interfaces;
using WalletConnectSharp.Network;
using WalletConnectSharp.Storage;
using WalletConnectSharp.Storage.Interfaces;

namespace WalletConnectSharp.Crypto
{
    public class Crypto : ICrypto
    {
        public string Name
        {
            get
            {
                return "crypto";
            }
        }

        public string Context
        {
            get
            {
                //TODO Replace with logger context
                return "walletconnectsharp";
            }
        }
        
        public IKeyChain KeyChain { get; private set; }
        
        public IKeyValueStorage Storage { get; private set; }

        private bool _initialized;

        public Crypto(IKeyChain keyChain = null, IKeyValueStorage storage = null)
        {
            storage ??= new DictStorage();
            keyChain ??= new KeyChain(storage);

            this.KeyChain = keyChain;
            this.Storage = storage;
        }
        public async Task Init()
        {
            if (!this._initialized)
            {
                await this.KeyChain.Init();
                this._initialized = true;
            }
        }

        public Task<bool> HasKeys(string tag)
        {
            this.IsInitialized();
            return this.KeyChain.Has(tag);
        }

        public Task<string> GenerateKeyPair()
        {
            this.IsInitialized();

            var options = new KeyCreationParameters
            {
                ExportPolicy = KeyExportPolicies.AllowPlaintextArchiving
            };
            
            var keypair = Key.Create(SignatureAlgorithm.Ed25519, options);

            var publicKey = keypair.PublicKey.Export(KeyBlobFormat.NSecPublicKey).ToHex();
            var privateKey = keypair.Export(KeyBlobFormat.NSecPrivateKey).ToHex();
            
            return this.SetPrivateKey(publicKey, privateKey);
        }

        public async Task<string> GenerateSharedKey(string selfPublicKey, string peerPublicKey, string overrideTopic = null)
        {
            var privateKey = await GetPrivateKey(selfPublicKey);
            var sharedKey = DeriveSharedKey(privateKey, peerPublicKey);
            var symKey = DeriveSymmetricKey(sharedKey);

            var symKeyRaw = symKey.Export(KeyBlobFormat.NSecPrivateKey);
            return await SetSymKey(symKeyRaw.ToHex());
        }

        public async Task<string> SetSymKey(string symKey, string overrideTopic = null)
        {
            string topic = overrideTopic ?? HashKey(symKey);
            await this.KeyChain.Set(topic, symKey);

            return topic;
        }

        public Task DeleteKeyPair(string publicKey)
        {
            this.IsInitialized();
            return this.KeyChain.Delete(publicKey);
        }

        public Task DeleteSymKey(string topic)
        {
            this.IsInitialized();
            return this.KeyChain.Delete(topic);
        }

        public async Task<string> Encrypt(string topic, string message)
        {
            this.IsInitialized();
            var symKey = await GetSymKey(topic);

            return EncryptAndSerialize(symKey, message);
        }

        public async Task<string> Decrypt(string topic, string encoded)
        {
            this.IsInitialized();
            var symKey = await GetSymKey(topic);

            return DeserializeAndDecrypt(symKey, encoded);
        }

        public async Task<string> Encode(string topic, IJsonRpcPayload payload)
        {
            this.IsInitialized();
            bool hasKeys = await this.HasKeys(topic);
            var message = JsonConvert.SerializeObject(payload);
            var result = hasKeys ? await this.Encrypt(topic, message) : Encoding.UTF8.GetBytes(message).ToHex();
            return result;
        }

        public async Task<T> Decode<T>(string topic, string encoded) where T : IJsonRpcPayload
        {
            this.IsInitialized();
            bool hasKeys = await this.HasKeys(topic);
            var message = hasKeys
                ? await this.Decrypt(topic, encoded)
                : Encoding.UTF8.GetString(encoded.HexToByteArray());
            var payload = JsonConvert.DeserializeObject<T>(message);

            return payload;
        }

        private async Task<string> SetPrivateKey(string publicKey, string privateKey)
        {
            await KeyChain.Set(publicKey, privateKey);

            return publicKey;
        }

        private Task<string> GetPrivateKey(string publicKey)
        {
            return KeyChain.Get(publicKey);
        }

        private Task<string> GetSymKey(string topic)
        {
            return KeyChain.Get(topic);
        }

        private void IsInitialized()
        {
            if (!this._initialized)
            {
                throw WalletConnectException.FromType(ErrorType.NOT_INITIALIZED, new {Name});
            }
        }

        private string HashKey(string key)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(key.HexToByteArray()).ToHex();
            }
        }

        private SharedSecret DeriveSharedKey(string privateKeyA, string publicKeyB)
        {
            using (var keyA = Key.Import(SignatureAlgorithm.Ed25519, privateKeyA.HexToByteArray(),
                       KeyBlobFormat.NSecPrivateKey))
            {
                var keyB = PublicKey.Import(SignatureAlgorithm.Ed25519, publicKeyB.HexToByteArray(),
                    KeyBlobFormat.NSecPublicKey);
                
                var options = new SharedSecretCreationParameters
                {
                    ExportPolicy = KeyExportPolicies.AllowPlaintextArchiving
                };
                
                using (var sharedKey = KeyAgreementAlgorithm.X25519.Agree(keyA, keyB, options))
                {
                    return sharedKey;
                }
            }
        }

        private Key DeriveSymmetricKey(SharedSecret secretKey)
        {
            return KeyDerivationAlgorithm.HkdfSha512.DeriveKey(secretKey, Array.Empty<byte>(), Array.Empty<byte>(),
                AeadAlgorithm.ChaCha20Poly1305);
        }

        private string EncryptAndSerialize(string symKey, string message, string iv = null)
        {
            byte[] rawIv;
            if (iv == null)
            {
                rawIv = new byte[12];
                new Random().NextBytes(rawIv);
            }
            else
            {
                rawIv = iv.HexToByteArray();
            }
            
            Key key = Key.Import(AeadAlgorithm.ChaCha20Poly1305, symKey.HexToByteArray(), KeyBlobFormat.NSecPrivateKey);

            var encrypted = AeadAlgorithm.ChaCha20Poly1305.Encrypt(key, iv.HexToByteArray(), Array.Empty<byte>(),
                Encoding.UTF8.GetBytes(message));

            return rawIv.Concat(encrypted).ToArray().ToHex();
        }

        private string DeserializeAndDecrypt(string symKey, string encoded)
        {
            var rawIv = encoded.HexToByteArray().Take(12).ToArray();
            var @sealed = encoded.HexToByteArray().Skip(12).ToArray();

            Key key = Key.Import(AeadAlgorithm.ChaCha20Poly1305, symKey.HexToByteArray(), KeyBlobFormat.NSecPrivateKey);

            var rawDecrypted = AeadAlgorithm.ChaCha20Poly1305.Decrypt(key, rawIv, Array.Empty<byte>(), @sealed);

            if (rawDecrypted != null) return Encoding.UTF8.GetString(rawDecrypted);
            return null;
        }
    }
}
