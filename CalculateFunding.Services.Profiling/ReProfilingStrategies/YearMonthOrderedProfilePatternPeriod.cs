using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Profiling.Models;

namespace CalculateFunding.Services.Profiling.ReProfilingStrategies
{
    public class YearMonthOrderedProfilePatternPeriod : IEnumerable<ProfilePeriodPattern>
    {
        private readonly IEnumerable<ProfilePeriodPattern> _orderedProfilePeriods;

        public YearMonthOrderedProfilePatternPeriod(IEnumerable<ProfilePeriodPattern> periods)
        {
            _orderedProfilePeriods = (periods ?? Array.Empty<ProfilePeriodPattern>())
                .Where(_ => _.PeriodType == PeriodType.CalendarMonth)
                .OrderBy(_ => _.PeriodYear)
                .ThenBy(_ => _.Period.ToMonthNumber())
                .ThenBy(_ => _.Occurrence)
                .ToArray();
        }

        public IEnumerator<ProfilePeriodPattern> GetEnumerator() => _orderedProfilePeriods.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}