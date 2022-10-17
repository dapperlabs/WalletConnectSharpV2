using System.Collections.Generic;
using WalletConnectSharp.Common.Utils;
using WalletConnectSharp.Sign.Interfaces;

namespace WalletConnectSharp.Sign.Models.Engine.Methods
{
    [WcMethod("wc_sessionPing")]
    [RpcRequestOptions(Clock.THIRTY_SECONDS, false, 1114)]
    [RpcResponseOptions(Clock.THIRTY_SECONDS, false, 1115)]
    public class SessionPing : Dictionary<string, object>, IWcMethod
    {
    }
}