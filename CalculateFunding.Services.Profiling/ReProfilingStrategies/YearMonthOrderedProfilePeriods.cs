using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Services.Core.Extensions;
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
                .ThenBy(_ => _.TypeValue.ToMonthNumber())
                .ThenBy(_ => _.Occurrence)
                .ToArray();
        }

        public IEnumerator<TPeriod> GetEnumerator() => _orderedProfilePeriods.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}