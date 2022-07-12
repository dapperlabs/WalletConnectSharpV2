using System.Threading.Tasks;
using WalletConnectSharp.Common;
using WalletConnectSharp.Network;

namespace WalletConnectSharp.Crypto.Interfaces
{
    /// <summary>
    /// A module that handles both key management and encoding/decoding of data with a given topic/keypair.
    ///
    /// A module holds one IKeyChain and stores generated keys in that keychain. The storage of generated keys
    /// is handled by the IKeyChain.
    /// </summary>
    public interface ICrypto : IModule
    {
        /// <summary>
        /// The IKeyChain this crypto module will use to store/retrieve key pairs
        /// </summary>
        IKeyChain KeyChain { get; }

        /// <summary>
        /// Initialize the crypto module. This should initialize the current KeyCHain if present.
        ///
        /// This call is asynchronous, since initializing the IKeyChain requires asynchronous initialization.
        /// </summary>
        /// <returns>The crypto module initialization task</returns>
        Task Init();

        /// <summary>
        /// Check if a keypair with a given tag is stored in this crypto module. This should
        /// check the backing keychain.
        /// </summary>
        /// <param name="tag">The tag of the keychain to look for</param>
        /// <returns>A </returns>
        Task<bool> HasKeys(string tag);

        Task<string> GenerateKeyPair();

        Task<string> GenerateSharedKey(string selfPublicKey, string peerPublicKey, string overrideTopic = null);

        Task<string> SetSymKey(string symKey, string overrideTopic = null);

        Task DeleteKeyPair(string publicKey);

        Task DeleteSymKey(string topic);

        Task<string> Encrypt(string topic, string message);

        Task<string> Decrypt(string topic, string encoded);

        Task<string> Encode(string topic, IJsonRpcPayload payload);

        Task<T> Decode<T>(string topic, string encoded) where T : IJsonRpcPayload;
    }
}