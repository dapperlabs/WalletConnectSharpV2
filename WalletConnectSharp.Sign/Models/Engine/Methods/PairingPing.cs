using System.Collections.Generic;
using WalletConnectSharp.Common.Utils;
using WalletConnectSharp.Sign.Interfaces;

namespace WalletConnectSharp.Sign.Models.Engine.Methods
{
    [WcMethod("wc_pairingPing")]
    [RpcRequestOptions(Clock.THIRTY_SECONDS, false, 1002)]
    [RpcResponseOptions(Clock.THIRTY_SECONDS, false, 1003)]
    public class PairingPing : Dictionary<string, object>, IWcMethod
    {
    }
}