using System;
using System.Threading.Tasks;
using WalletConnectSharp.Core.Interfaces;
using WalletConnectSharp.Core.Models;
using WalletConnectSharp.Crypto;
using WalletConnectSharp.Crypto.Interfaces;
using WalletConnectSharp.Events;
using WalletConnectSharp.Events.Model;
using WalletConnectSharp.Storage;
using WalletConnectSharp.Storage.Interfaces;

namespace WalletConnectSharp.Core
{
    public class Core : ICore
    {
        public static readonly string STORAGE_PREFIX = ICore.Protocol + "@" + ICore.Version + ":core:";
        
        public string Name
        {
            get
            {
                return "core";
            }
        }

        public string Context
        {
            get
            {
                return "core";
            }
        }

        public EventDelegator Events { get; }
        public bool Initialized { get; private set; }
        public string RelayUrl { get; }
        public string ProjectId { get; }
        public IHeartBeat HeartBeat { get; }
        public ICrypto Crypto { get; }
        public IRelayer Relayer { get; }
        public IKeyValueStorage Storage { get; }
        
        public Core(CoreOptions options = null)
        {
            if (options == null)
            {
                options = new CoreOptions()
                {
                    KeyChain = new KeyChain(new InMemoryStorage()),
                    LoggerContext = Context, //TODO Add logger
                    ProjectId = null,
                    RelayUrl = null,
                    Storage = new InMemoryStorage()
                };
            }

            ProjectId = options.ProjectId;
            RelayUrl = options.RelayUrl;
            Crypto = new Crypto.Crypto(options.KeyChain);
            Storage = options.Storage;
            
            Events = new EventDelegator(this);
        }

        public async Task Start()
        {
            if (Initialized) return;

            await Initialize();
        }

        private async Task Initialize()
        {
            await Crypto.Init();
            await Relayer.Init();
            await HeartBeat.Init();
            Initialized = true;
        }
    }
}