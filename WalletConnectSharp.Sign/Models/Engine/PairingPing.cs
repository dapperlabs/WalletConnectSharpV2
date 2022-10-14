using System;
using System.Collections.Generic;
using WalletConnectSharp.Sign.Interfaces;

namespace WalletConnectSharp.Sign.Models.Engine
{
    [WcMethod("wc_pairingPing", typeof(bool))]
    public class PairingPing : Dictionary<string, object>, IWcMethod
    {
    }
}