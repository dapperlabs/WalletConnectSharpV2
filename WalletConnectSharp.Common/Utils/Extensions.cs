using System.Collections.Generic;
using Newtonsoft.Json;

namespace WalletConnectSharp.Common
{
    public static class Extensions
    {
        public static Dictionary<string, TValue> ToDictionary<TValue>(this object obj)
        {
            var json = JsonConvert.SerializeObject(obj);
            return JsonConvert.DeserializeObject<Dictionary<string, TValue>>(json);
        }
    }
}