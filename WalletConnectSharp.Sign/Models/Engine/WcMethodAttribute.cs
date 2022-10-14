using System;

namespace WalletConnectSharp.Sign.Models.Engine
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class WcMethodAttribute : Attribute
    {
        public string MethodName { get; }
        
        public Type ResponseType { get; }

        public WcMethodAttribute(string method, Type responseType)
        {
            MethodName = method;
            ResponseType = responseType;
        }
    }
}