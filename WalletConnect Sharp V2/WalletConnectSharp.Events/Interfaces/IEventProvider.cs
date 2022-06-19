namespace WalletConnectSharp.Events
{
    public interface IEventProvider<in T>
    {
        void PropagateEvent(string topic, T eventData);
    }
}