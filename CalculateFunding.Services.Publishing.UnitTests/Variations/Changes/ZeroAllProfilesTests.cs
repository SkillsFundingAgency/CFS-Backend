using CalculateFunding.Services.Publishing.Variations.Changes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CalculateFunding.Models.Publishing;
using System.Threading.Tasks;
using FluentAssertions;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Changes
{
    [TestClass]
    public class ZeroAllProfilesTests : VariationChangeTestBase
    {
        [TestInitialize]
        public void SetUp()
        {
            Change = new ZeroAllProfiles(VariationContext);
        }

        [TestMethod]
        public async Task ZerosAllProfiles()
        {
            ProfilePeriod[] fundingLineOnePeriods = CreateProfilePeriods(2,2, 2973864M).ToArray();
            ProfilePeriod[] fundingLineTwoPeriods = CreateProfilePeriods(1, 2, 2973864M).ToArray();
            ProfilePeriod[] fundingLineThreePeriods = CreateProfilePeriods(2, 3, 2973864M).ToArray();

            FundingLine fundingLineOne = NewFundingLine(_ => _.WithFundingLineCode(NewRandomString())
                .WithFundingLineType(FundingLineType.Payment)
                .WithDistributionPeriods(NewDistributionPeriod(dp => dp.WithProfilePeriods(fundingLineOnePeriods))));

            FundingLine fundingLineTwo = NewFundingLine(_ => _.WithFundingLineCode(NewRandomString())
                .WithFundingLineType(FundingLineType.Payment)
                .WithDistributionPeriods(NewDistributionPeriod(dp => dp.WithProfilePeriods(fundingLineTwoPeriods))));

            FundingLine fundingLineThree = NewFundingLine(_ => _.WithFundingLineCode(NewRandomString())
                .WithFundingLineType(FundingLineType.Information)
                .WithDistributionPeriods(NewDistributionPeriod(dp => dp.WithProfilePeriods(fundingLineThreePeriods)))
                .WithValue(decimal.Multiply(decimal.Multiply(2, 3), 2973864M)));

            AndTheFundingLines(fundingLineOne,
                fundingLineTwo,
                fundingLineThree);

            await Change.Apply(VariationsApplication);

            ThenProfilePeriodsShouldBeZeroAmount(fundingLineOnePeriods);
            AndTheProfilePeriodsShouldBeZeroAmount(fundingLineTwoPeriods);
            AndFundingLinesValuesShouldBeZeroAmount(fundingLineOne, fundingLineTwo);
            AndTheProfilePeriodsAmountShouldBe(fundingLineThreePeriods, 2973864M);            

            fundingLineThree.Value
                .Should()
                .Be(17843184M);
        }

        private IEnumerable<ProfilePeriod> CreateProfilePeriods(int numberOfPeriods, int numberOfOccurrences, decimal? value)
        {
            for (int i = 1; i < numberOfPeriods; i++)
            {
                for(int j = 0; j < numberOfOccurrences; j++)
                {
                    yield return NewProfilePeriod(j, 2020, CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(i), value);
                }
            }
        }

        private void AndFundingLinesValuesShouldBeZeroAmount(params FundingLine[] fundingLines)
        {
            foreach (FundingLine fundingLine in fundingLines)
            {
                fundingLine.Value
                    .Should()
                    .Be(0);
            }
        }

        private void ThenProfilePeriodsShouldBeZeroAmount(params ProfilePeriod[] profilePeriods)
        {
            AndTheProfilePeriodsAmountShouldBe(profilePeriods, 0);
        }

        private void AndTheProfilePeriodsShouldBeZeroAmount(params ProfilePeriod[] profilePeriods)
            => ThenProfilePeriodsShouldBeZeroAmount(profilePeriods);
    }
}
