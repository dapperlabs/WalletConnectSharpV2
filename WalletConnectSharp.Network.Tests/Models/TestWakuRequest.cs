using WalletConnectSharp.Network.Models;

namespace WalletConnectSharp.Network.Tests.Models
{
    public class TestWakuRequest : JsonRpcRequest<TopicData>
    {
        public TestWakuRequest(string topic)
        {
            Method = "waku_subscribe";
            this.Params = new TopicData()
            {
                Topic = topic
            };
        }
    }
}