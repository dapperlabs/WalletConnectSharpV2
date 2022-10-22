using System;
using System.Threading.Tasks;
using WalletConnectSharp.Core.Interfaces;
using WalletConnectSharp.Crypto;
using WalletConnectSharp.Events;
using WalletConnectSharp.Sign.Controllers;
using WalletConnectSharp.Sign.Interfaces;
using WalletConnectSharp.Sign.Models;
using WalletConnectSharp.Sign.Models.Engine;
using WalletConnectSharp.Storage;

namespace WalletConnectSharp.Sign
{
    public class WalletConnectSignClient : ISignClient
    {
        public static readonly string PROTOCOL = "wc";
        public static readonly int VERSION = 2;
        public static readonly string CONTEXT = "client";

        public static readonly string StoragePrefix = $"{PROTOCOL}@{VERSION}:{CONTEXT}";


        public string Name { get; }

        public string Context { get; }

        public EventDelegator Events { get; }
        
        public Metadata Metadata { get; }
        public ICore Core { get; }
        public IEngine Engine { get; }
        public IPairing Pairing { get; }
        public ISession Session { get; }
        public IProposal Proposal { get; }
        public IJsonRpcHistoryFactory History { get; }
        public IExpirer Expirer { get; }
        public SignClientOptions Options { get; }

        public string Protocol
        {
            get
            {
                return PROTOCOL;
            }
        }

        public int Version
        {
            get
            {
                return VERSION;
            }
        }

        public static async Task<WalletConnectSignClient> Init(SignClientOptions options)
        {
            var client = new WalletConnectSignClient(options);
            await client.Initialize();

            return client;
        }

        private WalletConnectSignClient(SignClientOptions options)
        {
            if (options == null || options.Metadata == null)
                throw new ArgumentException("The Metadata field must be set in the SignClientOptions object");
            else
                Metadata = options.Metadata;
            
            Options = options;
            
            if (string.IsNullOrWhiteSpace(options.Name))
                Name = CONTEXT;
            else
                Name = options.Name;

            if (string.IsNullOrWhiteSpace(options.LoggerContext))
                Context = CONTEXT;
            else
                Context = options.LoggerContext;

            // Setup storage
            if (options.Storage == null)
            {
                var storage = new FileSystemStorage();
                options.Storage = storage;

                // If keychain is also not set, use the same storage instance
                options.KeyChain ??= new KeyChain(storage);
            }

            if (options.Core != null)
                Core = options.Core;
            else
                Core = new Core.Core(options);

            Events = new EventDelegator(this);
            
            Pairing = new Pairing(Core);
            Session = new Session(Core);
            Proposal = new Proposal(Core);
            History = new JsonRpcHistoryFactory(Core);
            Expirer = new Expirer(Core);
            Engine = new Engine(this);
        }

        public Task<ConnectedData> Connect(ConnectParams @params)
        {
            return Engine.Connect(@params);
        }

        public Task<PairingStruct> Pair(PairParams pairParams)
        {
            return Engine.Pair(pairParams);
        }

        public Task<IApprovedData> Approve(ApproveParams @params)
        {
            return Engine.Approve(@params);
        }

        public Task Reject(RejectParams @params)
        {
            return Engine.Reject(@params);
        }

        public Task<IAcknowledgement> Update(UpdateParams @params)
        {
            return Engine.Update(@params);
        }

        public Task<IAcknowledgement> Extend(ExtendParams @params)
        {
            return Engine.Extend(@params);
        }

        public Task<TR> Request<T, TR>(RequestParams<T> @params)
        {
            return Engine.Request<T, TR>(@params);
        }

        public Task Respond<T, TR>(RespondParams<TR> @params) where T : IWcMethod
        {
            return Engine.Respond<T, TR>(@params);
        }

        public Task Emit<T>(EmitParams<T> @params)
        {
            return Engine.Emit(@params);
        }

        public Task Ping(PingParams @params)
        {
            return Engine.Ping(@params);
        }

        public Task Disconnect(DisconnectParams @params)
        {
            return Engine.Disconnect(@params);
        }

        public SessionStruct[] Find(FindParams @params)
        {
            return Engine.Find(@params);
        }

        private async Task Initialize()
        {
            await this.Core.Start();
            await Pairing.Init();
            await Session.Init();
            await Proposal.Init();

            await Expirer.Init();
            await Engine.Init();
        }
    }
}