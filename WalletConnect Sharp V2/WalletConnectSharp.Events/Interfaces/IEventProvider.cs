namespace WalletConnectSharp.Events
{
    public interface IEventProvider
    {
        void PropagateEvent(string topic, string responseJson);
    }
}