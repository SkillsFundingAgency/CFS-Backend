using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Variations.Changes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CalculateFunding.Models.Publishing;
using System.Threading.Tasks;
using FluentAssertions;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System;

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
            ProfilePeriod[] fundingLineOnePeriods = GetProfilePeriods(2,2, 2973864M);
            ProfilePeriod[] fundingLineTwoPeriods = GetProfilePeriods(1, 2, 2973864M);
            ProfilePeriod[] fundingLineThreePeriods = GetProfilePeriods(2, 3, 2973864M);

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

            AndTheFundingLines(new[] { fundingLineOne,
                fundingLineTwo,
                fundingLineThree});

            await Change.Apply(VariationsApplication);

            ThenProfilePeriodsShouldBeZeroAmount(fundingLineOnePeriods.Concat(fundingLineTwoPeriods).ToArray());

            ThenFundingLineValueShouldBeZeroAmount(fundingLineOne, fundingLineTwo);

            fundingLineThreePeriods.ToList().ForEach(_ =>
            {
                AndTheProfilePeriodAmountShouldBe(_, 2973864M);
            });

            fundingLineThree.Value
                .Should()
                .Be(17843184M);
        }

        private ProfilePeriod[] GetProfilePeriods(int numberOfPeriods, int numberOfOccurences, decimal? value)
        {
            List<ProfilePeriod> profilePeriods = new List<ProfilePeriod>();

            for (int i = 1; i < numberOfPeriods; i++)
            {
                for(int j = 0; j < numberOfOccurences; j++)
                {
                    profilePeriods.Add(NewProfilePeriod(j, 2020, CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(i), value));
                }
            }

            return profilePeriods.ToArray();
        }

        private void ThenFundingLineValueShouldBeZeroAmount(params FundingLine[] fundingLines)
        {
            fundingLines.ToList().ForEach(_ =>
            {
                _.Value
                .Should()
                .Be(0);
            });
        }

        private void ThenProfilePeriodsShouldBeZeroAmount(params ProfilePeriod[] profilePeriods)
        {
            AndTheProfilePeriodsAmountShouldBe(profilePeriods, 0);
        }
    }
}
