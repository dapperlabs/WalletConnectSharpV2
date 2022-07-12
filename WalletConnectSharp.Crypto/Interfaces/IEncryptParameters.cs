namespace WalletConnectSharp.Crypto.Interfaces
{
    public interface IEncryptParameters
    {
        string Message { get; }
        
        string SymKey { get; }
        
        string IV { get; }
    }
}