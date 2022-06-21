using System;
using System.Threading.Tasks;
using WalletConnectSharp.Crypto.Interfaces;
using WalletConnectSharp.Network;

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

        private bool initialized;

        public Crypto(IKeyChain keyChain = null)
        {
            if (keyChain == null)
            {
                keyChain = new KeyChain();
            }

            this.KeyChain = keyChain;
        }
        public Task Init()
        {
            throw new NotImplementedException();
        }

        public Task<bool> HasKeys(string tag)
        {
            throw new NotImplementedException();
        }

        public Task<string> GenerateKeyPair()
        {
            throw new NotImplementedException();
        }

        public Task<string> GenerateSharedKey(string selfPublicKey, string peerPublicKey, string overrideTopic = null)
        {
            throw new NotImplementedException();
        }

        public Task<string> SetSymKey(string symKey, string overrideTopic = null)
        {
            throw new NotImplementedException();
        }

        public Task DeleteKeyPair(string publicKey)
        {
            throw new NotImplementedException();
        }

        public Task DeleteSymKey(string topic)
        {
            throw new NotImplementedException();
        }

        public Task<string> Encrypt(string topic, string message)
        {
            throw new NotImplementedException();
        }

        public Task<string> Decrypt(string topic, string encoded)
        {
            throw new NotImplementedException();
        }

        public Task<string> Encode(string topic, IJsonRpcPayload payload)
        {
            throw new NotImplementedException();
        }

        public Task<IJsonRpcPayload> Decode(string topic, string encoded)
        {
            throw new NotImplementedException();
        }
    }
}
