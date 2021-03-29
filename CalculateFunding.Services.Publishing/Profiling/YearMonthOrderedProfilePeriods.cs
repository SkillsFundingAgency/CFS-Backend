using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.Profiling
{
    public class YearMonthOrderedProfilePeriods : IEnumerable<ProfilePeriod>
    {
        private readonly IEnumerable<ProfilePeriod> _orderedProfilePeriods;

        public YearMonthOrderedProfilePeriods(FundingLine fundingLine)
        {
            _orderedProfilePeriods = fundingLine.DistributionPeriods?
                .SelectMany(_ => _.ProfilePeriods ?? Array.Empty<ProfilePeriod>())
                .Where(_ => _.Type == ProfilePeriodType.CalendarMonth)
                .OrderBy(_ => _.Year )
                .ThenBy(_ => MonthNumberFor(_.TypeValue))
                .ThenBy(_ => _.Occurrence)
                .ToArray() ?? Array.Empty<ProfilePeriod>();
        }

        public IEnumerator<ProfilePeriod> GetEnumerator()
        {
            return _orderedProfilePeriods.GetEnumerator();
        }
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private static int MonthNumberFor(string monthName)
        {
            return DateTime.ParseExact(monthName, "MMMM", CultureInfo.InvariantCulture)
                .Month * 100;
        }
    }
}