namespace CalculateFunding.Profiling.ConsoleConfig.Dtos
{
	using System;

	public class FundingStreamPeriodProfilePattern
    {
        public FundingStreamPeriodProfilePattern(string fundingStreamPeriodCode, DateTime fundingStreamPeriodStartDate, DateTime fundingStreamPeriodEndDate, bool reProfilePastPeriods, bool calculateBalancingPayment, ProfilePeriodPattern[] profilePattern)
        {
            FundingStreamPeriodCode = fundingStreamPeriodCode;
            FundingStreamPeriodStartDate = fundingStreamPeriodStartDate;
            FundingStreamPeriodEndDate = fundingStreamPeriodEndDate;
            ReProfilePastPeriods = reProfilePastPeriods;
            CalculateBalancingPayment = calculateBalancingPayment;
            ProfilePattern = profilePattern;
        }

        public string FundingStreamPeriodCode { get; }

        public DateTime FundingStreamPeriodStartDate { get; }

        public DateTime FundingStreamPeriodEndDate { get; }

        public bool ReProfilePastPeriods { get; }

        public bool CalculateBalancingPayment { get; }

        public ProfilePeriodPattern[] ProfilePattern { get; }
    }
}