﻿using System;
using System.Collections.Generic;

namespace CalculateFunding.Models.Results
{
    [Obsolete]
    public class PublishedProviderResultExisting
    {
        public string Id { get; set; }

        public AllocationLineStatus Status { get; set; }

        public string AllocationLineId { get; set; }

        public decimal? Value { get; set; }

        public string ProviderId { get; set; }

        public ProviderSummary Provider { get; set; }

        public int Minor { get; set; }

        public int Major { get; set; }

        public int Version { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }

        public PublishedAllocationLineResultVersion Published { get; set; }

        public IEnumerable<ProfilingPeriod> ProfilePeriods { get; set; }

        public IEnumerable<FinancialEnvelope> FinancialEnvelopes { get; set; }

        public bool HasResultBeenVaried { get; set; }
    }
}
