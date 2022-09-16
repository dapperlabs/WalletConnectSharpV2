using System;
using System.Threading;
using System.Threading.Tasks;
using WalletConnectSharp.Core.Interfaces;
using WalletConnectSharp.Core.Models.Heartbeat;
using WalletConnectSharp.Events;
using WalletConnectSharp.Events.Model;

namespace WalletConnectSharp.Core.Controllers
{
    public class HeartBeat : IHeartBeat
    {
        public static readonly object PULSE_OBJECT = new object();
        
        public EventDelegator Events { get; }
        
        public CancellationToken HeartBeatCancellationToken { get; private set; }
        
        public int Interval { get; }
        
        public string Name { get; }
        public string Context { get; }

        public HeartBeat(HeartBeatOptions opts = null)
        {
            if (opts == null)
            {
                opts = new HeartBeatOptions()
                {
                    Interval = 5000 // 5 seconds
                };
            }
            
            Events = new EventDelegator(this);

            Interval = opts.Interval;
        }
        
        public void On<T>(string eventId, EventHandler<GenericEvent<T>> callback)
        {
            Events.ListenFor(eventId, callback);
        }

        public void Once<T>(string eventId, EventHandler<GenericEvent<T>> callback)
        {
            Events.ListenForOnce(eventId, callback);
        }

        public void Off<T>(string eventId, EventHandler<GenericEvent<T>> callback)
        {
            Events.RemoveListener(eventId, callback);
        }

        public void RemoveListener<T>(string eventId, EventHandler<GenericEvent<T>> callback)
        {
            Events.RemoveListener(eventId, callback);
        }

        public Task Init()
        {
            HeartBeatCancellationToken = new CancellationToken();

            return Task.Run(async () =>
            {
                while (!HeartBeatCancellationToken.IsCancellationRequested)
                {
                    Pulse();

                    await Task.Delay(Interval, HeartBeatCancellationToken);
                }
            }, HeartBeatCancellationToken);
        }

        private void Pulse()
        {
            Events.Trigger(HeartbeatEvents.Pulse, PULSE_OBJECT);
        }
    }
}