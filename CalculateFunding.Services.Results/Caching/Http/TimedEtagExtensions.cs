using System;
using System.Linq;

namespace CalculateFunding.Services.Results.Caching.Http
{
    public static class TimedEtagExtensions
    {
        public static string ToETagString(this DateTimeOffset dateTimeOffset)
        {
            byte[] dateBytes = BitConverter.GetBytes(dateTimeOffset.UtcDateTime.Ticks);
            byte[] offsetBytes = BitConverter.GetBytes((short)dateTimeOffset.Offset.TotalHours);

            return Convert.ToBase64String(dateBytes.Concat(offsetBytes).ToArray());
        }
    }
}