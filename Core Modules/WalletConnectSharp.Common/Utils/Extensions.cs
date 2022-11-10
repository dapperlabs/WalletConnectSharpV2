using System;
using System.Collections.Generic;
using System.Reflection;
using System.Web;

namespace WalletConnectSharp.Common.Utils
{
    public static class Extensions
    {
        public static Dictionary<string, object> AsDictionary(this object obj, bool enforceLowercase = true)
        {
            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            if (obj != null)
            {
                foreach (PropertyInfo propertyDescriptor in obj.GetType().GetProperties())
                {
                    object value = propertyDescriptor.GetValue(obj, null);
                    var key = enforceLowercase ? propertyDescriptor.Name.ToLower() : propertyDescriptor.Name;
                    
                    dict.Add(key, value);
                }
            }

            return dict;
        }
        
        public static bool IsNumericType(this object o)
        {   
            switch (Type.GetTypeCode(o.GetType()))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                    return true;
                default:
                    return false;
            }
        }
        
        public static string AddQueryParam(
            this string source, string key, string value)
        {
            string delim;
            if ((source == null) || !source.Contains("?"))
            {
                delim = "?";
            }
            else if (source.EndsWith("?") || source.EndsWith("&"))
            {
                delim = string.Empty;
            }
            else
            {
                delim = "&";
            }

            return source + delim + HttpUtility.UrlEncode(key)
                   + "=" + HttpUtility.UrlEncode(value);
        }
    }
}