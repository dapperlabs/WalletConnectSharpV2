namespace WalletConnectSharp.Common
{
    public interface IModule
    {
        string Name { get; }
        
        string Context { get; }
    }
}