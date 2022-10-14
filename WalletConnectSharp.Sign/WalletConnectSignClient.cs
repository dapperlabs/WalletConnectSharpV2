namespace WalletConnectSharp.Sign
{
    public class WalletConnectSignClient
    {
        public static readonly string Protocol = "wc";
        public static readonly int Version = 2;
        public static readonly string Context = "client";

        public static readonly string StoragePrefix = $"{Protocol}@{Version}:{Context}";
        
        
    }
}