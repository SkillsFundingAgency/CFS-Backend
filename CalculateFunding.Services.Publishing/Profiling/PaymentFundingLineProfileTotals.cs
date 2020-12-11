using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.Profiling
{
    public class PaymentFundingLineProfileTotals : IEnumerable<ProfileTotal>
    {
        private readonly ProfileTotal[] _profileTotals;
        
        public PaymentFundingLineProfileTotals(PublishedProviderVersion publishedProviderVersion)
        {
            _profileTotals = publishedProviderVersion
                .FundingLines.Where(_ => _.Type == FundingLineType.Payment)
                .SelectMany(paymentFundingLine => new YearMonthOrderedProfilePeriods(paymentFundingLine))
                .GroupBy(orderProfilePeriod => new
                {
                    orderProfilePeriod.Year,
                    orderProfilePeriod.TypeValue,
                    orderProfilePeriod.Occurrence
                })
                .Select(grouping => new ProfileTotal
                {
                    Occurrence = grouping.Key.Occurrence,
                    Year = grouping.Key.Year,
                    TypeValue = grouping.Key.TypeValue,
                    Value = grouping.Sum(profilePeriod => profilePeriod.ProfiledValue)
                })
                .ToArray();
        }

        public PaymentFundingLineProfileTotals(
            PublishedProviderVersion publishedProviderVersion,
            string fundingLineId)
        {
            _profileTotals = publishedProviderVersion
                .FundingLines.Where(_ => _.Type == FundingLineType.Payment)
                .Where(_ => _.FundingLineCode == fundingLineId)
                .SelectMany(paymentFundingLine => new YearMonthOrderedProfilePeriods(paymentFundingLine))
                .GroupBy(orderProfilePeriod => new
                {
                    orderProfilePeriod.Year,
                    orderProfilePeriod.TypeValue,
                    orderProfilePeriod.Occurrence,
                    orderProfilePeriod.DistributionPeriodId
                })
                .Select(grouping => new ProfileTotal
                {
                    Occurrence = grouping.Key.Occurrence,
                    Year = grouping.Key.Year,
                    TypeValue = grouping.Key.TypeValue,
                    Value = grouping.Sum(profilePeriod => profilePeriod.ProfiledValue),
                    PeriodType = grouping.FirstOrDefault().Type.ToString(),
                    DistributionPeriodId = grouping.Key.DistributionPeriodId
                })
                .ToArray();
        }

        public IEnumerator<ProfileTotal> GetEnumerator() => _profileTotals
            .AsEnumerable()
            .GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}