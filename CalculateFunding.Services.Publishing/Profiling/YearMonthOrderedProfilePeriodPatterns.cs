using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.Profiling
{
    public class YearMonthOrderedProfilePeriodPatterns : IEnumerable<ProfilePeriodPattern>
    {
        private readonly IEnumerable<ProfilePeriodPattern> _orderedProfilePeriodPatterns;

        public YearMonthOrderedProfilePeriodPatterns(IEnumerable<ProfilePeriodPattern> profilePeriodPatterns)
        {
            _orderedProfilePeriodPatterns = profilePeriodPatterns.Where(_ => _.PeriodType == PeriodType.CalendarMonth)
                    .OrderBy(_ => _.PeriodYear)
                    .ThenBy(_ => MonthNumberFor(_.Period))
                    .ThenBy(_ => _.Occurrence)
                    .ToArray() ?? Array.Empty<ProfilePeriodPattern>();
        }

        public IEnumerator<ProfilePeriodPattern> GetEnumerator()
        {
            return _orderedProfilePeriodPatterns.GetEnumerator();
        }
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private static int MonthNumberFor(string monthName)
        {
            return DateTime.ParseExact(monthName, "MMMM", CultureInfo.InvariantCulture)
                .Month;
        }
    }
}