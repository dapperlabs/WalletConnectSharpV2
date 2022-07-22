namespace WalletConnectSharp.Crypto.Interfaces
{
    public interface IDecryptParameters
    {
        string SymKey { get; }
        string Encoded { get; }
    }
}