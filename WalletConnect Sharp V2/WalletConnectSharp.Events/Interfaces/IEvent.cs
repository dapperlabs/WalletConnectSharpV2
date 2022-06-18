namespace WalletConnectSharp.Events
{
    public interface IEvent<in T>
    {
        void SetData(T data);
    }
}