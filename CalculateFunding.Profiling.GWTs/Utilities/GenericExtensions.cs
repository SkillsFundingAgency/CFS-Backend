namespace CalculateFunding.Profiling.GWTs.Utilities
{
	using System;
	using System.Globalization;

	public static class GenericExtensions
    {
        public static string ParseStringIfPresent(this string input)
        {
            if (string.Equals(input, "?"))
            {
                return null;
            }

            return input;
        }

        public static DateTime? ParseDateTimeIfPresent(this string input, string format = "dd/MM/yyyy")
        {
            DateTime? result = null;

            if (!string.Equals(input, "?") && !string.IsNullOrEmpty(input))
            {
                result = DateTime.ParseExact(input, format, CultureInfo.InvariantCulture);
            }

            return result;
        }
    }
}