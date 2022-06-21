using System.Threading.Tasks;
using WalletConnectSharp.Common;
using WalletConnectSharp.Network;

namespace WalletConnectSharp.Crypto.Interfaces
{
    public interface ICrypto : IService
    {
        IKeyChain KeyChain { get; }

        Task Init();

        Task<bool> HasKeys(string tag);

        Task<string> GenerateKeyPair();

        Task<string> GenerateSharedKey(string selfPublicKey, string peerPublicKey, string overrideTopic = null);

        Task<string> SetSymKey(string symKey, string overrideTopic = null);

        Task DeleteKeyPair(string publicKey);

        Task DeleteSymKey(string topic);

        Task<string> Encrypt(string topic, string message);

        Task<string> Decrypt(string topic, string encoded);

        Task<string> Encode(string topic, IJsonRpcPayload payload);

        Task<IJsonRpcPayload> Decode(string topic, string encoded);
    }
}