using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Parse.Infrastructure.Utilities
{
    internal static class WebUtilities
    {
        internal static string BuildQueryString(IDictionary<string, object> parameters) => String.Join("&", (from pair in parameters let valueString = pair.Value as string select $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(String.IsNullOrEmpty(valueString) ? JsonUtilities.Encode(pair.Value) : valueString)}").ToArray());

        internal static IDictionary<string, string> DecodeQueryString(string queryString)
        {
            Dictionary<string, string> query = new Dictionary<string, string> { };

            foreach (string pair in queryString.Split('&'))
            {
                string[] parts = pair.Split(new char[] { '=' }, 2);
                query[parts[0]] = parts.Length == 2 ? Uri.UnescapeDataString(parts[1].Replace("+", " ")) : null;
            }

            return query;
        }
    }
}
