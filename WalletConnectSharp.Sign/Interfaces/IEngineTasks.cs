using System.Threading.Tasks;
using WalletConnectSharp.Sign.Models;
using WalletConnectSharp.Sign.Models.Engine;

namespace WalletConnectSharp.Sign.Interfaces
{
    public interface IEngineTasks
    {
        Task<ConnectedData> Connect(ConnectParams @params);

        Task<PairingStruct> Pair(PairParams pairParams);

        Task<IApprovedData> Approve(ApproveParams @params);

        Task Reject(RejectParams @params);

        Task<IAcknowledgement> Update(UpdateParams @params);

        Task<IAcknowledgement> Extend(ExtendParams @params);

        Task<TR> Request<T, TR>(RequestParams<T> @params);

        Task Respond<T, TR>(RespondParams<TR> @params) where T : IWcMethod;

        Task Emit<T>(EmitParams<T> @params);

        Task Ping(PingParams @params);

        Task Disconnect(DisconnectParams @params);

        SessionStruct[] Find(FindParams @params);
    }
}