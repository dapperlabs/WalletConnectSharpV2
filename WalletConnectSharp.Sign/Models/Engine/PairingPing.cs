using System.Collections.Generic;
using WalletConnectSharp.Sign.Interfaces;

namespace WalletConnectSharp.Sign.Models.Engine
{
    [WcMethod("wc_pairingPing")]
    public class PairingPing : Dictionary<string, object>, IWcMethod
    {
        
    }
}