using System;
using System.Collections.Generic;
using WalletConnectSharp.Sign.Interfaces;

namespace WalletConnectSharp.Sign.Models.Engine
{
    [WcMethod("wc_sessionExtend", typeof(bool))]
    public class SessionExtend : Dictionary<string, object>, IWcMethod
    {
    }
}