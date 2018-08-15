using System;

namespace CalculateFunding.Services.Core.Extensions
{
    public static class TryParseExtensions
    {
        public delegate bool ParseDelegate<T>(string s, out T result);

		private static T? TryParse<T>(this string value, ParseDelegate<T> parse) where T : struct
	    {
		    if (string.IsNullOrEmpty(value))
		    {
			    return null;
		    }

		    T result;
		    if (parse(value, out result))
		    {
			    return result;
		    }

		    return null;
	    }

	    public static int? TryParseInt32(this string value)
	    {
		    return TryParse<int>(value, int.TryParse);
	    }

	    public static Int64? TryParseInt64(this string value)
	    {
		    return TryParse<Int64>(value, Int64.TryParse);
	    }

	    public static bool? TryParseBoolean(this string value)
	    {
		    return TryParse<bool>(value, bool.TryParse);
	    }

	    public static Double? TryParseDouble(this string value)
	    {
		    return TryParse<Double>(value, Double.TryParse);
	    }

	    public static Decimal? TryParseDecimal(this string value)
	    {
		    return TryParse<Decimal>(value, Decimal.TryParse);
	    }

	    public static DateTime? TryParseDateTime(this string value)
	    {
		    return TryParse<DateTime>(value, DateTime.TryParse);
	    }

	    public static char? TryParseChar(this string value)
	    {
		    return TryParse<char>(value, char.TryParse);
	    }

	    public static float? TryParseFloat(this string value)
	    {
		    return TryParse<float>(value, float.TryParse);
	    }

	    public static byte? TryParseByte(this string value)
	    {
		    return TryParse<byte>(value, byte.TryParse);
	    }
	}
}
