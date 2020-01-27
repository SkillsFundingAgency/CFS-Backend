using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Variations.Changes;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Changes
{
    [TestClass]
    public class ZeroRemainingProfilesChangeTests : VariationChangeTestBase
    {
        private ZeroRemainingProfilesChange _change;

        [TestInitialize]
        public void SetUp()
        {
            _change = new ZeroRemainingProfilesChange(VariationContext);
        }

        [TestMethod]
        public async Task RecordsErrorIfNoVariationPointersForSpecificationId()
        {
            await WhenTheChangeIsApplied();
            
            ThenTheErrorWasRecorded($"Unable to zero profiles for provider id {VariationContext.ProviderId}");
            AndNoVariationChangesWereQueued();
        }

        [TestMethod]
        public async Task RecordsErrorIfNoMatchingFundingLineForAVariationPointer()
        {
            string fundingLineId = NewRandomString();
            
            GivenTheVariationPointersForTheSpecification(NewVariationPointer(_ => _.WithFundingLineId(fundingLineId)));

            await WhenTheChangeIsApplied();
            
            ThenTheErrorWasRecorded($"Did not locate a funding line for variation pointer with fundingLineId {fundingLineId}");
            AndNoVariationChangesWereQueued();
        }
        
        [TestMethod]
        public async Task RecordsErrorIfNoMatchingProfilePeriodForAVariationPointer()
        {
            string fundingLineId = NewRandomString();
            
            GivenTheVariationPointersForTheSpecification(NewVariationPointer(_ => _.WithFundingLineId(fundingLineId)));
            AndTheFundingLines(NewFundingLine(_ => _.WithFundingLineCode(fundingLineId)));

            await WhenTheChangeIsApplied();
            
            ThenTheErrorWasRecorded($"Did not locate profile period corresponding to variation pointer for funding line id {fundingLineId}");
            AndNoVariationChangesWereQueued();
        }

        [TestMethod]
        public async Task ZerosProfilesAfterProfileVariationPointersInSpecificationOnEachMatchingFundingLine()
        {
            string fundingLineOneId = NewRandomString();
            string fundingLineTwoId = NewRandomString();
            int year = 2020;
            int occurence = 1;
            string typeValue = "January";
            
            GivenTheVariationPointersForTheSpecification(NewVariationPointer(_ => _.WithFundingLineId(fundingLineOneId)
                .WithOccurence(occurence)
                .WithYear(year)
                .WithTypeValue(typeValue)),
                NewVariationPointer(_ => _.WithFundingLineId(fundingLineTwoId)
                    .WithOccurence(occurence)
                    .WithYear(year)
                    .WithTypeValue(typeValue)));

            decimal periodOneAmount = 293487M;
            ProfilePeriod periodOne = NewProfilePeriod(0, 2020, "January", periodOneAmount);
            ProfilePeriod periodTwo = NewProfilePeriod(1, 2020, "January", 2973864M);
            ProfilePeriod periodThree = NewProfilePeriod(0, 2020, "February", 123764M);
            ProfilePeriod periodFour = NewProfilePeriod(1, 2020, "January", 6487234M);
            ProfilePeriod periodFive = NewProfilePeriod(2, 2020, "January", 1290837M);
            
            AndTheFundingLines(NewFundingLine(_ => _.WithFundingLineCode(fundingLineOneId)
                .WithDistributionPeriods(NewDistributionPeriod(dp => dp.WithProfilePeriods(periodOne, periodTwo, periodThree)),
                    NewDistributionPeriod(dp => dp.WithProfilePeriods(periodFour, periodFive)))));
            
            await WhenTheChangeIsApplied();
            
            ThenProfilePeriodsShouldBeZeroAmount(periodTwo, periodThree, periodFive, periodFour);
            AndTheProfilePeriodAmountShouldBe(periodOne, periodOneAmount);
        }

        private void ThenProfilePeriodsShouldBeZeroAmount(params ProfilePeriod[] profilePeriods)
        {
            foreach (ProfilePeriod profilePeriod in profilePeriods)
            {
                profilePeriod
                    .ProfiledValue
                    .Should()
                    .Be(0);
            }
        }

        private void AndTheProfilePeriodAmountShouldBe(ProfilePeriod profilePeriod, decimal expectedAmount)
        {
            profilePeriod
                .ProfiledValue
                .Should()
                .Be(expectedAmount);
        }

        private async Task WhenTheChangeIsApplied()
        {
            await _change.Apply(VariationsApplication);
        }
    }
}