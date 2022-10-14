using System;
using System.Collections.Generic;
using WalletConnectSharp.Sign.Interfaces;

namespace WalletConnectSharp.Sign.Models.Engine
{
    [WcMethod("wc_sessionPing", typeof(bool))]
    public class SessionPing : Dictionary<string, object>, IWcMethod
    {
    }
}