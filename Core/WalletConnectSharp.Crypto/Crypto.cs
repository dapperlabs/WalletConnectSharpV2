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
    /// <summary>
    /// The crypto module handles storing key pairs in storage. The storage module to use
    /// must be given to the crypto module instance
    /// </summary>
    public class Crypto : ICrypto
    {
        /// <summary>
        /// The name of the crypto module
        /// </summary>
        public string Name
        {
            get
            {
                return "crypto";
            }
        }

        /// <summary>
        /// The current context of this module instance
        /// </summary>
        public string Context
        {
            get
            {
                //TODO Replace with logger context
                return "walletconnectsharp";
            }
        }
        
        /// <summary>
        /// The current KeyChain this crypto module instance is using
        /// </summary>
        public IKeyChain KeyChain { get; private set; }
        
        /// <summary>
        /// The current storage module this crypto module instance is using
        /// </summary>
        public IKeyValueStorage Storage { get; private set; }

        private bool _initialized;

        /// <summary>
        /// Create a new instance of the crypto module, with a given storage module.
        /// </summary>
        /// <param name="storage">The storage module to use to load the keychain from</param>
        public Crypto(IKeyValueStorage storage)
        {
            storage ??= new FileSystemStorage();

            this.KeyChain = new KeyChain(storage);
            this.Storage = storage;
        }
        
        /// <summary>
        /// Create a new instance of the crypto module, with a given keychain.
        /// </summary>
        /// <param name="keyChain">The keychain to use for this crypto module</param>
        public Crypto(IKeyChain keyChain)
        {
            keyChain ??= new KeyChain(new FileSystemStorage());

            this.KeyChain = keyChain;
            this.Storage = keyChain.Storage;
        }

        /// <summary>
        /// Create a new instance of the crypto module using an empty keychain stored in-memory using a Dictionary
        /// </summary>
        public Crypto() : this(new FileSystemStorage())
        {
        }

        /// <summary>
        /// Initialize the crypto module, this does nothing if the module has already
        /// been initialized
        ///
        /// Initializing the module will invoke Init() on the backing KeyChain
        /// </summary>
        public async Task Init()
        {
            if (!this._initialized)
            {
                await this.KeyChain.Init();
                this._initialized = true;
            }
        }

        /// <summary>
        /// Check if a keypair with a given tag is stored in this crypto module. This should
        /// check the backing keychain.
        /// </summary>
        /// <param name="tag">The tag of the keychain to look for</param>
        /// <returns>True if the backing KeyChain has a keypair for the given tag</returns>
        public Task<bool> HasKeys(string tag)
        {
            this.IsInitialized();
            return this.KeyChain.Has(tag);
        }

        /// <summary>
        /// Generate a new keypair, storing the public/private key pair as the tag in the backing KeyChain. This will
        /// save the public/private keypair in the backing KeyChain
        /// </summary>
        /// <returns>The public key of the generated keypair</returns>
        public Task<string> GenerateKeyPair()
        {
            this.IsInitialized();

            var options = new KeyCreationParameters
            {
                ExportPolicy = KeyExportPolicies.AllowPlaintextArchiving
            };

            using (var keypair = Key.Create(KeyAgreementAlgorithm.X25519, options))
            {
                var publicKey = keypair.PublicKey.Export(KeyBlobFormat.NSecPublicKey).ToHex();
                var privateKey = keypair.Export(KeyBlobFormat.NSecPrivateKey).ToHex();

                return this.SetPrivateKey(publicKey, privateKey);
            }
        }

        /// <summary>
        /// Generate a shared Sym key given two public keys. One of the public keys (selfPublicKey) is the public key
        /// we have generated a private key for in the backing KeyChain. The peer's public key (peerPublicKey) is used
        /// to generate the Sym key
        /// </summary>
        /// <param name="selfPublicKey">The public key to use, this keypair must be stored in the backing KeyChain</param>
        /// <param name="peerPublicKey">The Peer's public key. This public key does not exist in the backing KeyChain</param>
        /// <param name="overrideTopic"></param>
        /// <returns>The generated Sym key</returns>
        public async Task<string> GenerateSharedKey(string selfPublicKey, string peerPublicKey, string overrideTopic = null)
        {
            var privateKey = await GetPrivateKey(selfPublicKey);
            using (var sharedKey = DeriveSharedKey(privateKey, peerPublicKey))
            {
                using (var symKey = DeriveSymmetricKey(sharedKey))
                {
                    var symKeyRaw = symKey.Export(KeyBlobFormat.NSecSymmetricKey);

                    return await SetSymKey(symKeyRaw.ToHex(), overrideTopic);
                }
            }
        }

        /// <summary>
        /// Store the Sym key in the backing KeyChain, optionally for a given topic. If no topic is given,
        /// then the KeyChain tag for the Sym key will be the hash of the key.
        /// </summary>
        /// <param name="symKey">The Sym key to store</param>
        /// <param name="overrideTopic">An optional topic to use as the KeyChain tag</param>
        /// <returns>The tag used to store the Sym key in the KeyChain</returns>
        public async Task<string> SetSymKey(string symKey, string overrideTopic = null)
        {
            string topic = overrideTopic ?? HashKey(symKey);
            await this.KeyChain.Set(topic, symKey);

            return topic;
        }

        /// <summary>
        /// Delete a keypair from the backing KeyChain
        /// </summary>
        /// <param name="publicKey">The public key of the keypair to delete</param>
        /// <returns>An async task</returns>
        public Task DeleteKeyPair(string publicKey)
        {
            this.IsInitialized();
            return this.KeyChain.Delete(publicKey);
        }

        /// <summary>
        /// Delete a Sym key with the given topic/tag from the backing KeyChain.
        /// </summary>
        /// <param name="topic">The topic/tag of the Sym key to delete</param>
        /// <returns>An async task</returns>
        public Task DeleteSymKey(string topic)
        {
            this.IsInitialized();
            return this.KeyChain.Delete(topic);
        }

        /// <summary>
        /// Encrypt a message with the given topic's Sym key. 
        /// </summary>
        /// <param name="topic">The topic of the Sym key to use to encrypt the message</param>
        /// <param name="message">The message to encrypt</param>
        /// <returns>The encrypted message from an async task</returns>
        public async Task<string> Encrypt(string topic, string message)
        {
            this.IsInitialized();
            var symKey = await GetSymKey(topic);

            return EncryptAndSerialize(symKey, message);
        }

        /// <summary>
        /// Decrypt an encrypted message using the given topic's Sym key.
        /// </summary>
        /// <param name="topic">The topic of the Sym key to use to decrypt the message</param>
        /// <param name="encoded">The message to decrypt</param>
        /// <returns>The decrypted message from an async task</returns>
        public async Task<string> Decrypt(string topic, string encoded)
        {
            this.IsInitialized();
            var symKey = await GetSymKey(topic);

            return DeserializeAndDecrypt(symKey, encoded);
        }

        /// <summary>
        /// Encode a JsonRpcPayload message by encrypting the contents using the given topic's Sym key. If the topic
        /// has no Sym key, then the contents are not encrypted and instead are simply converted to Json -> Hex
        /// </summary>
        /// <param name="topic">The topic of the Sym key to use to encrypt the IJsonRpcPayload</param>
        /// <param name="payload">The payload to encode and encrypt</param>
        /// <returns>The encoded and encrypted IJsonRpcPayload from an async task</returns>
        public async Task<string> Encode(string topic, IJsonRpcPayload payload)
        {
            this.IsInitialized();
            bool hasKeys = await this.HasKeys(topic);
            var message = JsonConvert.SerializeObject(payload);
            var result = hasKeys ? await this.Encrypt(topic, message) : Encoding.UTF8.GetBytes(message).ToHex();
            return result;
        }

        /// <summary>
        /// Decode an encoded/encrypted message to a IJsonRpcPayload using the given topic's Sym key. If the topic
        /// has no Sym key, then the contents are not decrypted and instead are simply converted Hex -> Json
        /// </summary>
        /// <param name="topic">The topic of the Sym key to use</param>
        /// <param name="encoded">The encoded/encrypted message to decrypt</param>
        /// <typeparam name="T">The type of the IJsonRpcPayload to convert the encoded Json to</typeparam>
        /// <returns>The decoded, decrypted and deserialized object of type T from an async task</returns>
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
            using (var keyA = Key.Import(KeyAgreementAlgorithm.X25519, privateKeyA.HexToByteArray(),
                       KeyBlobFormat.NSecPrivateKey))
            {
                var keyB = PublicKey.Import(KeyAgreementAlgorithm.X25519, publicKeyB.HexToByteArray(),
                    KeyBlobFormat.NSecPublicKey);
                
                var options = new SharedSecretCreationParameters
                {
                    ExportPolicy = KeyExportPolicies.AllowPlaintextArchiving
                };

                return KeyAgreementAlgorithm.X25519.Agree(keyA, keyB, options);
            }
        }

        private Key DeriveSymmetricKey(SharedSecret secretKey)
        {
            var options = new KeyCreationParameters()
            {
                ExportPolicy = KeyExportPolicies.AllowPlaintextArchiving
            };
            
            return KeyDerivationAlgorithm.HkdfSha512.DeriveKey(secretKey, Array.Empty<byte>(), Array.Empty<byte>(),
                AeadAlgorithm.ChaCha20Poly1305, options);
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

            using (var key = Key.Import(AeadAlgorithm.ChaCha20Poly1305, symKey.HexToByteArray(),
                       KeyBlobFormat.NSecSymmetricKey))
            {
                var encrypted = AeadAlgorithm.ChaCha20Poly1305.Encrypt(key, new ReadOnlySpan<byte>(rawIv),
                    Array.Empty<byte>(),
                    Encoding.UTF8.GetBytes(message));

                return rawIv.Concat(encrypted).ToArray().ToHex();
            }
        }

        private string DeserializeAndDecrypt(string symKey, string encoded)
        {
            var rawIv = encoded.HexToByteArray().Take(12).ToArray();
            var @sealed = encoded.HexToByteArray().Skip(12).ToArray();

            using (var key = Key.Import(AeadAlgorithm.ChaCha20Poly1305, symKey.HexToByteArray(),
                       KeyBlobFormat.NSecSymmetricKey))
            {
                var rawDecrypted = AeadAlgorithm.ChaCha20Poly1305.Decrypt(key, rawIv, Array.Empty<byte>(), @sealed);

                if (rawDecrypted != null) return Encoding.UTF8.GetString(rawDecrypted);
                return null;
            }
        }
    }
}
