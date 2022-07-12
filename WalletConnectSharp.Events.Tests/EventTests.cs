using System;
using System.Threading;
using System.Threading.Tasks;
using WalletConnectSharp.Common;
using WalletConnectSharp.Common.Model;
using WalletConnectSharp.Events.Model;
using Xunit;

namespace WalletConnectSharp.Events.Tests
{
    public class EventDelegatorTests : IClassFixture<MockService>
    {
        private MockService _service;

        public EventDelegatorTests(MockService service)
        {
            _service = service;
        }
        
        [Fact]
        public async void EventsPropagate()
        {
            var events = new EventDelegator(_service);

            TaskCompletionSource<string> eventCallbackTask = new TaskCompletionSource<string>();

            string eventId = Guid.NewGuid().ToString();
            
            events.ListenFor<string>(eventId, delegate(object? sender, GenericEvent<string> @event)
            {
                eventCallbackTask.SetResult(@event.Response);
            });
            var eventData = Guid.NewGuid().ToString();

            Task.Run(delegate
            {
                Thread.Sleep(500);
                events.Trigger(eventId, eventData);
            });

            Assert.Equal(eventData, (await eventCallbackTask.Task));

            
        }
    }
}