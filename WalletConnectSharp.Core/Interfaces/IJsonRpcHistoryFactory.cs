namespace WalletConnectSharp.Core.Interfaces
{
    public interface IJsonRpcHistoryFactory
    {
        ICore Core { get; }
        
        IJsonRpcHistory<T, TR> JsonRpcHistoryOfType<T, TR>();
    }
}