using System.Collections.Generic;
using WalletConnectSharp.Common.Utils;
using WalletConnectSharp.Sign.Interfaces;

namespace WalletConnectSharp.Sign.Models.Engine.Methods
{
    [WcMethod("wc_sessionExtend")]
    [RpcRequestOptions(Clock.ONE_DAY, false, 1106)]
    [RpcResponseOptions(Clock.ONE_DAY, false, 1107)]
    public class SessionExtend : Dictionary<string, object>, IWcMethod
    {
    }
}