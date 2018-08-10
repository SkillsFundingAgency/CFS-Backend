using System;

namespace CalculateFunding.Services.Core.Extensions
{
    public static class TryParseExtensions
    {
        private delegate bool ParseDelegate<T>(string s, out T result);

        private static T TryParse<T>(this string value, ParseDelegate<T> parse) where T : struct
        {
            T result;
            parse(value, out result);
            return result;
        }

        public static int TryParseInt32(this string value)
        {
            return TryParse<int>(value, int.TryParse);
        }

        public static Int64 TryParseInt64(this string value)
        {
            return TryParse<Int64>(value, Int64.TryParse);
        }

        public static bool TryParseBoolean(this string value)
        {
            return TryParse<bool>(value, bool.TryParse);
        }

        public static Double TryParseDoube(this string value)
        {
            return TryParse<Double>(value, Double.TryParse);
        }

        public static Decimal TryParseDecimal(this string value)
        {
            return TryParse<Decimal>(value, Decimal.TryParse);
        }

        public static DateTime TryParseDateTime(this string value)
        {
            return TryParse<DateTime>(value, DateTime.TryParse);
        }
    }
}
