using System;

namespace WalletConnectSharp.Sign.Models.Engine
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class RpcRequestOptionsAttribute : RpcOptionsAttribute
    {
        public RpcRequestOptionsAttribute(long ttl, bool prompt, int tag) : base(ttl, prompt, tag)
        {
        }
    }
}