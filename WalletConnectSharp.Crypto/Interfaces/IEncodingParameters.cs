namespace WalletConnectSharp.Crypto.Interfaces
{
    public interface IEncodingParameters
    {
        uint[] Sealed { get; }
        
        uint[] Iv { get; }
    }
}