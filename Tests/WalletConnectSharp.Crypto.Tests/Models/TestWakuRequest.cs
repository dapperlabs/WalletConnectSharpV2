using System.Collections.Generic;
using WalletConnectSharp.Network.Models;

namespace WalletConnectSharp.Crypto.Tests.Models
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

        private sealed class TestWakuRequestEqualityComparer : IEqualityComparer<TestWakuRequest>
        {
            public bool Equals(TestWakuRequest x, TestWakuRequest y)
            {
                return y == null && x == null || y != null && x != null && x.Params.Topic.Equals(y.Params.Topic);
            }

            public int GetHashCode(TestWakuRequest obj)
            {
                return obj.Params.Topic.GetHashCode();
            }
        }

        public static IEqualityComparer<TestWakuRequest> TestWakuRequestComparer { get; } = new TestWakuRequestEqualityComparer();
    }
}