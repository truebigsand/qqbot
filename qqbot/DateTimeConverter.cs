using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qqbot
{
    public static class DateTimeConverter
    {
        public static DateTime ToDateTime(string timeStamp)
        {
            DateTime dtStart = TimeZoneInfo.ConvertTime(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), TimeZoneInfo.Local);
            return dtStart.Add(TimeSpan.FromSeconds(long.Parse(timeStamp)));
        }
    }
}
