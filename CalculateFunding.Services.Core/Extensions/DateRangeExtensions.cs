using System;
using System.Collections.Generic;

namespace CalculateFunding.Services.Core.Extensions
{
    /// <summary>
    ///     Helpers to work with date ranges
    /// </summary>
    public static class DateRangeExtensions
    {
        /// <summary>
        ///     Calculate the months from the start date to the end date
        ///     inclusive and return as strings formatted as MMMM yyyy
        /// </summary>
        public static IEnumerable<string> GetMonthsBetween(DateTimeOffset startDate,
            DateTimeOffset endDate,
            string format = "MMMM yyyy")
        {
            startDate = FirstOfTheMonth(startDate);
            endDate = FirstOfTheMonth(endDate);

            while (startDate <= endDate)
            {
                yield return startDate.ToString(format);

                startDate = startDate.AddMonths(1);
            }
        }

        private static DateTimeOffset FirstOfTheMonth(DateTimeOffset startDate) => new DateTimeOffset(new DateTime(startDate.Year, startDate.Month, 1));
    }
}