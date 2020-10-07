namespace CalculateFunding.Profiling.ConsoleConfig.Dtos
{
	using System;

	public class ProfilePeriodPattern
    {
        public ProfilePeriodPattern(string periodType, string period, DateTime periodStartDate, DateTime periodEndDate, int periodYear, int occurrence, string distributionPeriod, decimal periodPatternPercentage)
        {
            PeriodType = periodType;
            Period = period;
            PeriodStartDate = periodStartDate;
            PeriodEndDate = periodEndDate;
            PeriodYear = periodYear;
            Occurrence = occurrence;
            DistributionPeriod = distributionPeriod;
            PeriodPatternPercentage = periodPatternPercentage;
        }

        public string PeriodType { get; }

        public string Period { get; }

        public DateTime PeriodStartDate { get; }

        public DateTime PeriodEndDate { get; }

        public int PeriodYear { get; }

        public int Occurrence { get; }

        public string DistributionPeriod { get; }

        public decimal PeriodPatternPercentage { get; }
    }
}