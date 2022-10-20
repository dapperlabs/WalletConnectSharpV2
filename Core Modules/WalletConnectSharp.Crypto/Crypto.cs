using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Merkator.BitCoin;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using NSec.Cryptography;
using WalletConnectSharp.Common;
using WalletConnectSharp.Common.Utils;
using WalletConnectSharp.Crypto.Encoder;
using WalletConnectSharp.Crypto.Interfaces;
using WalletConnectSharp.Crypto.Models;
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
        private const string CRYPTO_CLIENT_SEED = "client_ed25519_seed";
        
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
        public Task<string> Encrypt(EncryptParams @params)
        {
            this.IsInitialized();

            var typeRaw = Bases.Base10.Decode($"{@params.Type}");
            var iv = @params.Iv;
            
            byte[] rawIv;
            if (iv == null)
            {
                rawIv = new byte[12];
                RandomNumberGenerator.Fill(rawIv);
            }
            else
            {
                rawIv = iv.HexToByteArray();
            }

            var type1 = @params.Type == 1;
            var senderPublicKey = !string.IsNullOrWhiteSpace(@params.SenderPublicKey)
                ? @params.SenderPublicKey.HexToByteArray()
                : null;

            var format = type1 ? KeyBlobFormat.NSecSymmetricKey : KeyBlobFormat.RawSymmetricKey;

            byte[] encrypted;
            using (var key = Key.Import(AeadAlgorithm.ChaCha20Poly1305, @params.SymKey.HexToByteArray(), format))
            {
                encrypted = AeadAlgorithm.ChaCha20Poly1305.Encrypt(key, new ReadOnlySpan<byte>(rawIv),
                    Array.Empty<byte>(),
                    Encoding.UTF8.GetBytes(@params.Message));
            }

            if (type1)
            {
                if (senderPublicKey == null)
                    throw new ArgumentException("Missing sender public key for type1 envelope");
                
                return Task.FromResult(Convert.ToBase64String(
                    typeRaw.Concat(senderPublicKey).Concat(rawIv).Concat(encrypted).ToArray()
                ));
            }

            return Task.FromResult(Convert.ToBase64String(
                typeRaw.Concat(rawIv).Concat(encrypted).ToArray()
            ));
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
        public async Task<string> Encode(string topic, IJsonRpcPayload payload, EncodeOptions options = null)
        {
            this.IsInitialized();

            if (options == null)
            {
                options = new EncodeOptions()
                {
                    Type = 0
                };
            }

            var isTypeOne = options.Type == 1 && !string.IsNullOrWhiteSpace(options.ReceiverPublicKey) &&
                            !string.IsNullOrWhiteSpace(options.SenderPublicKey);

            if (isTypeOne)
            {
                var selfPublicKey = options.SenderPublicKey;
                var peerPublicKey = options.ReceiverPublicKey;
                topic = await GenerateSharedKey(selfPublicKey, peerPublicKey);
            }

            var symKey = await GetSymKey(topic);
            var type = options.Type;
            var senderPublicKey = options.SenderPublicKey;
            var message = JsonConvert.SerializeObject(payload);
            var results = await Encrypt(new EncryptParams()
            {
                Message = message,
                Type = type,
                SenderPublicKey = senderPublicKey,
                SymKey = symKey
            });

            return results;
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

        private const string MULTICODEC_ED25519_ENCODING = "base58btc";
        private const string MULTICODEC_ED25519_BASE = "z";
        private const string MULTICODEC_ED25519_HEADER = "K36";
        private const int MULTICODEC_ED25519_LENGTH = 32;
        private const string DID_DELIMITER = ":";
        private const string DID_PREFIX = "did";
        private const string DID_METHOD = "key";
        private const long CRYPTO_JWT_TTL = Clock.ONE_DAY;
        private const string JWT_DELIMITER = ".";
        private static readonly Encoding DATA_ENCODING = Encoding.UTF8;
        private static readonly Encoding JSON_ENCODING = Encoding.UTF8;

        public async Task<string> GetClientId()
        {
            IsInitialized();
            var seed = await this.GetClientSeed();
            var keyPair = KeypairFromSeed(seed);
            var clientId = EncodeIss(keyPair.PublicKey);
            return clientId;
        }

        public async Task<string> SignJwt(string aud)
        {
            IsInitialized();
            var seed = await GetClientSeed();
            var keyPair = KeypairFromSeed(seed);
            byte[] subRaw = new byte[32];
            RandomNumberGenerator.Fill(subRaw);
            var sub = subRaw.ToHex();
            var ttl = CRYPTO_JWT_TTL;
            var iat = Clock.Now();

            // sign JWT
            var header = IridiumJWTHeader.DEFAULT;
            var iss = EncodeIss(keyPair.PublicKey);
            var exp = iat + ttl;
            var payload = new IridiumJWTPayload()
            {
                Iss = iss,
                Sub = sub,
                Aud = aud,
                Iat = iat,
                Exp = exp
            };

            var data = DATA_ENCODING.GetBytes(
                string.Join(JWT_DELIMITER, EncodeJson(header), EncodeJson(payload))
            );

            var signature = SignatureAlgorithm.Ed25519.Sign(keyPair, data);
            return EncodeJwt(new IridiumJWTSigned()
            {
                Header = header,
                Payload = payload,
                Signature = signature
            });
        }

        private string EncodeJwt(IridiumJWTSigned data)
        {
            return string.Join(JWT_DELIMITER, 
                EncodeJson(data.Header), 
                EncodeJson(data.Payload),
                EncodeSig(data.Signature)
            );
        }

        private string EncodeSig(byte[] signature)
        {
            return Base64UrlEncoder.Encode(signature);
        }

        private string EncodeJson<T>(T data)
        {
            return Base64UrlEncoder.Encode(
                JSON_ENCODING.GetBytes(
                    JsonConvert.SerializeObject(data)
                )
            );
        }

        private string EncodeIss(PublicKey publicKey)
        {
            var publicKeyRaw = publicKey.Export(KeyBlobFormat.RawPublicKey);
            var header = Base58Encoding.Decode(MULTICODEC_ED25519_HEADER);
            var multicodec = MULTICODEC_ED25519_BASE + Base58Encoding.Encode(header.Concat(publicKeyRaw).ToArray());

            return string.Join(DID_DELIMITER, DID_PREFIX, DID_METHOD, multicodec);
        }

        private Key KeypairFromSeed(byte[] seed)
        {
            var options = new KeyCreationParameters()
            {
                ExportPolicy = KeyExportPolicies.AllowPlaintextExport
            };
            return Key.Import(SignatureAlgorithm.Ed25519, seed, KeyBlobFormat.RawPrivateKey, options);
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

        private async Task<byte[]> GetClientSeed()
        {
            var seed = "";
            try
            {
                seed = await this.KeyChain.Get(CRYPTO_CLIENT_SEED);
            }
            catch (Exception e)
            {
                byte[] seedRaw = new byte[32];
                RandomNumberGenerator.Fill(seedRaw);
                seed = seedRaw.ToHex();
                await this.KeyChain.Set(CRYPTO_CLIENT_SEED, seed);
            }

            return seed.HexToByteArray();
        }
    }
}
