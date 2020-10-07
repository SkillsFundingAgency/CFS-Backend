using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Profiling.Models
{
    public class DistributionPeriods
    {
        public DistributionPeriods()
        {
        }

        public DistributionPeriods(decimal value, string distributionPeriodCode)
        {
            Value = value;
            DistributionPeriodCode = distributionPeriodCode;
        }
        public decimal Value { get; set; }

        public string DistributionPeriodCode { get; set; }
    }
}
