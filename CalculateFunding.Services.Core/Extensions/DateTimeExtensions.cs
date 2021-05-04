using System;
using System.Globalization;

namespace CalculateFunding.Services.Core.Extensions
{
    public static class DateTimeExtensions
    {
        public static DateTime TrimToTheSecond(this DateTime dateTime)
        {
            return TrimTicks(dateTime, TimeSpan.TicksPerSecond);
        }

        public static DateTime TrimToTheMinute(this DateTime dateTime)
        {
            return TrimTicks(dateTime, TimeSpan.TicksPerMinute);
        }

        private static DateTime TrimTicks(DateTime dateTime, long modulus)
        {
            return dateTime.AddTicks(-(dateTime.Ticks % modulus));
        }

        public static DateTimeOffset? TrimToTheMinute(this DateTimeOffset? dateTimeOffset)
        {
            return dateTimeOffset?.AddTicks(-(dateTimeOffset.Value.Ticks % TimeSpan.TicksPerMinute));
        }

        public static int ToMonthNumber(this string monthName)
        {
            return DateTime.ParseExact(monthName, "MMMM", CultureInfo.InvariantCulture)
                .Month * 100;
        }
    }
}