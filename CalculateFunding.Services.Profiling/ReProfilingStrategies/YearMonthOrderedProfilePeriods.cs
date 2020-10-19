using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CalculateFunding.Services.Profiling.Models;

namespace CalculateFunding.Services.Profiling.ReProfilingStrategies
{
    public class YearMonthOrderedProfilePeriods<TPeriod> : IEnumerable<TPeriod>
    where TPeriod : IProfilePeriod
    {
        private readonly IEnumerable<TPeriod> _orderedProfilePeriods;
        public YearMonthOrderedProfilePeriods(IEnumerable<TPeriod> periods)
        {
            _orderedProfilePeriods = (periods ?? Array.Empty<TPeriod>())
                .Where(_ => _.Type == PeriodType.CalendarMonth)
                .OrderBy(_ => _.Year)
                .ThenBy(_ => MonthNumberFor(_.TypeValue))
                .ThenBy(_ => _.Occurrence)
                .ToArray();
        }

        public IEnumerator<TPeriod> GetEnumerator()
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