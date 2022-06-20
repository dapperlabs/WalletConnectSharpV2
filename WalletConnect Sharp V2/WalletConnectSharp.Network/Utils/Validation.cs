using System.Text.RegularExpressions;

namespace WalletConnectSharp.Network
{
    public static class Validation
    {
        public const string WS_REGEX = "^wss?:";
        public const string HTTP_REGEX = "^https?:";

        public static string GetUrlProtocol(string url)
        {
            return Regex.Match(url, "/^\\w+:/", RegexOptions.IgnoreCase).Value;
        }

        public static bool MatchRegexProtocol(string url, string regex)
        {
            var protocol = GetUrlProtocol(url);
            if (protocol == null) return false;
            return Regex.IsMatch(protocol, regex);
        }

        public static bool IsWsUrl(string url)
        {
            return MatchRegexProtocol(url, WS_REGEX);
        }

        public static bool IsHttpUrl(string url)
        {
            return MatchRegexProtocol(url, HTTP_REGEX);
        }

        public static bool IsLocalhost(string url)
        {
            return Regex.IsMatch(url, "wss?://localhost(:d{2,5})?");
        }
    }
}