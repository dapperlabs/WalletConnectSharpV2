using System.Threading.Tasks;
using WalletConnectSharp.Network.Models;
using WalletConnectSharp.Sign.Interfaces;
using WalletConnectSharp.Sign.Models;
using WalletConnectSharp.Sign.Models.Engine;

namespace WalletConnectSharp.Sign
{
    public class Engine :IEnginePrivate,IEngine
    {
        Task<long> IEnginePrivate.SendRequest<T>(string topic, T parameters)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.SendResult<T>(long id, string topic, T result)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.SendError(long id, string topic, ErrorResponse error)
        {
            throw new System.NotImplementedException();
        }

        void IEnginePrivate.OnRelayEventRequest<T>(EngineCallback<JsonRpcRequest<T>> @event)
        {
            throw new System.NotImplementedException();
        }

        void IEnginePrivate.OnRelayEventResponse<T>(EngineCallback<JsonRpcResponse<T>> @event)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.ActivatePairing(string topic)
        {    
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.DeleteSession(string topic)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.DeletePairing(string topic)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.DeleteProposal(long id)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.SetExpiry(string topic, long expiry)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.SetProposal(long id, ProposalStruct proposal)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.Cleanup()
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.OnSessionProposeRequest(string topic, JsonRpcRequest<SessionPropose> payload)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.OnSessionProposeResponse(string topic, JsonRpcResponse<SessionProposeResponse> payload)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.OnSessionSettleRequest(string topic, JsonRpcRequest<SessionSettle> payload)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.OnSessionSettleResponse(string topic, JsonRpcResponse<bool> payload)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.OnSessionUpdateRequest(string topic, JsonRpcRequest<SessionUpdate> payload)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.OnSessionUpdateResponse(string topic, JsonRpcResponse<bool> payload)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.OnSessionExtendRequest(string topic, JsonRpcRequest<SessionExtend> payload)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.OnSessionExtendResponse(string topic, JsonRpcResponse<bool> payload)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.OnSessionPingRequest(string topic, JsonRpcRequest<SessionPing> payload)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.OnSessionPingResponse(string topic, JsonRpcResponse<bool> payload)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.OnPairingPingRequest(string topic, JsonRpcRequest<PairingPing> payload)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.OnPairingPingResponse(string topic, JsonRpcResponse<bool> payload)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.OnSessionDeleteRequest(string topic, JsonRpcRequest<SessionDelete> payload)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.OnPairingDeleteRequest(string topic, JsonRpcRequest<PairingDelete> payload)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.OnSessionRequest<T>(string topic, JsonRpcRequest<SessionRequest<T>> payload)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.OnSessionRequestResponse<T>(string topic, JsonRpcResponse<T> payload)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.OnSessionEventRequest<T>(string topic, JsonRpcRequest<SessionEvent<T>> payload)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.IsValidConnect(ConnectParams @params)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.IsValidPair(PairParams pairParams)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.IsValidSessionSettleRequest(SessionSettle settle)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.IsValidApprove(ApproveParams @params)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.IsValidReject(RejectParams @params)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.IsValidUpdate(UpdateParams @params)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.IsValidExtend(ExtendParams @params)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.IsValidRequest<T>(RequestParams<T> @params)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.IsValidRespond<T>(RespondParams<T> @params)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.IsValidPing(PingParams @params)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.IsValidEmit<T>(EmitParams<T> @params)
        {
            throw new System.NotImplementedException();
        }

        Task IEnginePrivate.IsValidDisconnect(DisconnectParams @params)
        {
            throw new System.NotImplementedException();
        }

        public async Task<IConnectedData> Connect(ConnectParams @params)
        {
            this.IsInitialized();
            await ((IEnginePrivate)this).IsValidConnect(@params);
            var topic = @params.PairingTopic;
            string uri;
            var active = false;

            if (string.IsNullOrEmpty(topic))
            {
                var pairing = this.Client.Pairing.Get(topic);
                active = pairing.Active;
            }

            if (!string.IsNullOrEmpty(topic) || !active)
            {
                CreatePairingData CreatePairing = await this.CreatePairing();
                topic = CreatePairing.Topic;
                uri = CreatePairing.Uri;

            }
            
            
        }

        private void IsInitialized()
        {
            throw new System.NotImplementedException();
        }

        private async Task<CreatePairingData> CreatePairing()
        {
            throw new System.NotImplementedException();
        }


        public Task<PairingStruct> Pair(PairParams pairParams)
        {
            throw new System.NotImplementedException();
        }

        public Task<IApprovedData> Approve(ApproveParams @params)
        {
            throw new System.NotImplementedException();
        }

        public Task Reject(RejectParams @params)
        {
            throw new System.NotImplementedException();
        }

        public Task<IAcknowledgement> Update(UpdateParams @params)
        {
            throw new System.NotImplementedException();
        }

        public Task<IAcknowledgement> Extend(ExtendParams @params)
        {
            throw new System.NotImplementedException();
        }

        public Task<TR> Request<T, TR>(RequestParams<T> @params)
        {
            throw new System.NotImplementedException();
        }

        public Task Respond<TR>(RespondParams<TR> @params)
        {
            throw new System.NotImplementedException();
        }

        public Task Emit<T>(EmitParams<T> @params)
        {
            throw new System.NotImplementedException();
        }

        public Task Ping(PingParams @params)
        {
            throw new System.NotImplementedException();
        }

        public Task Disconnect(DisconnectParams @params)
        {
            throw new System.NotImplementedException();
        }

        public SessionStruct[] Find(FindParams @params)
        {
            throw new System.NotImplementedException();
        }

        public ISignClient Client { get; }
        public Task Init()
        {
            throw new System.NotImplementedException();
        }
    }
}