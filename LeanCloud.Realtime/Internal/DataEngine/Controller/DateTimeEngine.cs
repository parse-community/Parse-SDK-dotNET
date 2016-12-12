using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeanCloud.Realtime.Internal
{
    internal static class DateTimeEngine
    {
        public static long UnixTimeStampSeconds(this DateTime date)
        {
            long unixTimestamp = (long)(date.ToUniversalTime().Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            return unixTimestamp;
        }

        public static DateTime UnixTimeStampSeconds(this double ts)
        {
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(ts).ToLocalTime();
            return dtDateTime;
        }
    }
}
