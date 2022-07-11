using System;
using System.IO;
using WalletConnectSharp.Network.Tests.Models;
using WalletConnectSharp.Network.Websocket;
using Websocket.Client;
using Xunit;

namespace WalletConnectSharp.Network.Tests
{

    public class WebsocketTests
    {
        private static readonly TestWakuRequest TEST_WAKU_REQUEST =
            new TestWakuRequest("ca838d59a3a3fe3824dab9ca7882ac9a2227c5d0284c88655b261a2fe85db270");
        private static readonly TestWakuRequest TEST_BAD_WAKU_REQUEST =
            new TestWakuRequest("");
        private static readonly string TEST_RANDOM_HOST = "random.domain.that.does.not.exist";
        private static readonly string GOOD_WS_URL = "wss://staging.walletconnect.org";
        private static readonly string BAD_WS_URL = "ws://" + TEST_RANDOM_HOST;

        [Fact]
        public async void ConnectAndRequest()
        {
            var connection = new WebsocketConnection(GOOD_WS_URL);
            var provider = new JsonRpcProvider(connection);
            await provider.Connect();

            var result = await provider.Request<TopicData, string>(TEST_WAKU_REQUEST);
            
            Assert.True(result.Length > 0);
        }
        
        [Fact]
        public async void RequestWithoutConnect()
        {
            var connection = new WebsocketConnection(GOOD_WS_URL);
            var provider = new JsonRpcProvider(connection);

            var result = await provider.Request<TopicData, string>(TEST_WAKU_REQUEST);
            
            Assert.True(result.Length > 0);
        }

        [Fact]
        public async void ThrowOnJsonRpcError()
        {
            var connection = new WebsocketConnection(GOOD_WS_URL);
            var provider = new JsonRpcProvider(connection);

            await Assert.ThrowsAsync<IOException>(() => provider.Request<TopicData, string>(TEST_BAD_WAKU_REQUEST));
        }

        [Fact]
        public async void ThrowsOnUnavailableHost()
        {
            var connection = new WebsocketConnection(BAD_WS_URL);
            var provider = new JsonRpcProvider(connection);
            
            await Assert.ThrowsAsync<TimeoutException>(() => provider.Request<TopicData, string>(TEST_WAKU_REQUEST));
        }

        [Fact]
        public async void ReconnectsWithNewProvidedHost()
        {
            var connection = new WebsocketConnection(BAD_WS_URL);
            var provider = new JsonRpcProvider(connection);
            Assert.Equal(BAD_WS_URL, provider.Connection.Url);
            await provider.Connect(GOOD_WS_URL);
            Assert.Equal(GOOD_WS_URL, provider.Connection.Url);
            
            var result = await provider.Request<TopicData, string>(TEST_WAKU_REQUEST);
            
            Assert.True(result.Length > 0);
        }

        [Fact]
        public async void DoesNotDoubleRegisterListeners()
        {
            var connection = new WebsocketConnection(GOOD_WS_URL);
            var provider = new JsonRpcProvider(connection);
            
            var expectedDisconnectCount = 3;
            var disconnectCount = 0;

            provider.On<DisconnectionInfo>("disconnect", (_, __) => disconnectCount++);

            await provider.Connect();
            await provider.Disconnect();
            await provider.Connect();
            await provider.Disconnect();
            await provider.Connect();
            await provider.Disconnect();
            
            Assert.Equal(expectedDisconnectCount, disconnectCount);
        }
    }
}