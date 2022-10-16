using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace WalletConnectSharp.Common
{
    public class WalletConnectException : Exception
    {
        [JsonProperty("code")]
        public uint Code { get; private set; }
        
        [JsonProperty("type")]
        public string Type { get; private set; }

        public WalletConnectException(string message, ErrorType type) : base(message)
        {
            Code = (uint) type;
            Type = Enum.GetName(typeof(ErrorType), type);
        }

        public WalletConnectException(string message, Exception innerException, ErrorType type) : base(message, innerException)
        {
            Code = (uint) type;
            Type = Enum.GetName(typeof(ErrorType), type);
        }

        public static WalletConnectException FromType(ErrorType type, object @params = null, Exception innerException = null)
        {
            string errorMessage = SdkErrors.MessageFromType(type, @params);

            if (innerException != null)
                return new WalletConnectException(errorMessage, innerException, type);
            return new WalletConnectException(errorMessage, type);
        }
    }
}