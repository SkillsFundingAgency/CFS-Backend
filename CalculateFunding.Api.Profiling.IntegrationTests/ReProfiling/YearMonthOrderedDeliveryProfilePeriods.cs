using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Services.Core.Extensions;

namespace CalculateFunding.Api.Profiling.IntegrationTests.ReProfiling
{
    public class YearMonthOrderedDeliveryProfilePeriods : IEnumerable<DeliveryProfilePeriod>
    {
        private readonly IEnumerable<DeliveryProfilePeriod> _orderedProfilePeriods;

        public YearMonthOrderedDeliveryProfilePeriods(IEnumerable<DeliveryProfilePeriod> periods)
        {
            _orderedProfilePeriods = (periods ?? Array.Empty<DeliveryProfilePeriod>())
                .Where(_ => _.Type == PeriodType.CalendarMonth)
                .OrderBy(_ => _.Year)
                .ThenBy(_ => _.TypeValue.ToMonthNumber())
                .ThenBy(_ => _.Occurrence)
                .ToArray();
        }

        public IEnumerator<DeliveryProfilePeriod> GetEnumerator() => _orderedProfilePeriods.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}