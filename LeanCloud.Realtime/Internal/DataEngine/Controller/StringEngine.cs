using System;
using System.Linq;

namespace LeanCloud.Realtime.Internal
{
    internal static class StringEngine
    {
        internal static string Random(this string str,int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyz";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
