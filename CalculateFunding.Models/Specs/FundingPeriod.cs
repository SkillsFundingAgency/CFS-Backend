using System;

namespace CalculateFunding.Models.Specs
{
    public class FundingPeriod : Reference
    {
        public DateTimeOffset StartDate { get; set; }

        public DateTimeOffset EndDate { get; set; }

        public string Type { get; set; }
    }
}