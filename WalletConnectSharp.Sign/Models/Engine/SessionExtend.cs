using System.Collections.Generic;
using WalletConnectSharp.Sign.Interfaces;

namespace WalletConnectSharp.Sign.Models.Engine
{
    [WcMethod("wc_sessionExtend")]
    public class SessionExtend : Dictionary<string, object>, IWcMethod
    {
        
    }
}