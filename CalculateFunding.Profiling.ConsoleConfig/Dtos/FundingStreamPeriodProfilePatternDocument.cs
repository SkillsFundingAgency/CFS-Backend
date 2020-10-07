namespace CalculateFunding.Profiling.ConsoleConfig.Dtos
{
	using System;

	public class FundingStreamPeriodProfilePatternDocument : FundingStreamPeriodProfilePattern
    {
        public FundingStreamPeriodProfilePatternDocument(
            string fundingStreamPeriodCode, 
            DateTime fundingStreamPeriodStartDate, 
            DateTime fundingStreamPeriodEndDate, 
            bool reProfilePastPeriods, 
            bool calculateBalancingPayment, 
            ProfilePeriodPattern[] profilePattern, 
            string id, 
            DateTime logged) 
            : base(fundingStreamPeriodCode, fundingStreamPeriodStartDate, fundingStreamPeriodEndDate, reProfilePastPeriods, calculateBalancingPayment, profilePattern)
        { 
            this.id = id;
            this.logged = logged;
        }

        // names are for CosmosDB to recognise document ID and logged
        // ReSharper disable once InconsistentNaming
        public string id { get; }
        // ReSharper disable once InconsistentNaming
        public DateTime logged { get; }

        public static FundingStreamPeriodProfilePatternDocument CreateFromPattern(
            FundingStreamPeriodProfilePattern pattern)
        {
            return new FundingStreamPeriodProfilePatternDocument(
                pattern.FundingStreamPeriodCode,
                pattern.FundingStreamPeriodStartDate,
                pattern.FundingStreamPeriodEndDate,
                pattern.ReProfilePastPeriods,
                pattern.CalculateBalancingPayment,
                pattern.ProfilePattern,
                id: pattern.FundingStreamPeriodCode,
                logged: DateTime.UtcNow);
        }
    }
}