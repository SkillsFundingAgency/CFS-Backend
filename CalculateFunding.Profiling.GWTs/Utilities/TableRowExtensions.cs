namespace CalculateFunding.Profiling.GWTs.Utilities
{
	using System;
	using System.Globalization;
	using TechTalk.SpecFlow;

	public static class TableRowExtensions
    {
        public static DateTime? ParseDateTimeIfPresent(this TableRow row, string rowHeading)
        {
            DateTime? result = null;

            if (row.ContainsKey(rowHeading)
                && row[rowHeading] != "?")
            {
                result = DateTime.ParseExact(row[rowHeading], "dd/MM/yyyy", CultureInfo.InvariantCulture);
            }

            return result;
        }

        public static decimal ParseDecimal(this TableRow row, string rowHeading)
        {
            return decimal.Parse(row[rowHeading]);
        }

        public static int ParseInt(this TableRow row, string rowHeading)
        {
            return int.Parse(row[rowHeading]);
        }
    }
}