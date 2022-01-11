using System;

namespace CalculateFunding.Services.Publishing.Profiling
{
    public class ProfileTotal
    {
        public int Year { get; set; }

        public string TypeValue { get; set; }

        public int Occurrence { get; set; }

        public decimal Value { get; set; }

        public string PeriodType { get; set; }

        public bool IsPaid { get; set; }

        public int InstallmentNumber { get; set; }

        public decimal? ProfileRemainingPercentage { get; set; }

        public decimal? ProfilePercentage { get; set; }

        public DateTimeOffset? ActualDate { get; set; }

        public string DistributionPeriod { get; set; }

        public string DistributionPeriodId { get; set; }
    }
}