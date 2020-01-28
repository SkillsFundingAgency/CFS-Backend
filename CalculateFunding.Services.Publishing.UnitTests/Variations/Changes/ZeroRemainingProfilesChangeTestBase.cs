using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Changes
{
    public abstract class ZeroRemainingProfilesChangeTestBase : VariationChangeTestBase
    {
        protected IVariationChange Change;

        [TestMethod]
        public async Task RecordsErrorIfNoMatchingFundingLineForAVariationPointer()
        {
            string fundingLineId = NewRandomString();
            
            GivenTheVariationPointersForTheSpecification(NewVariationPointer(_ => _.WithFundingLineId(fundingLineId)));

            await WhenTheChangeIsApplied();
            
            ThenTheErrorWasRecorded($"Did not locate a funding line for variation pointer with fundingLineId {fundingLineId}");
            AndNoVariationChangesWereQueued();
        }

        protected async Task WhenTheChangeIsApplied()
        {
            await Change.Apply(VariationsApplication);
        }

        protected void ThenTheProfilePeriodAmountShouldBe(ProfilePeriod profilePeriod, decimal expectedAmount)
        {
            AndTheProfilePeriodAmountShouldBe(profilePeriod, expectedAmount);     
        }

        protected void AndTheProfilePeriodAmountShouldBe(ProfilePeriod profilePeriod, decimal expectedAmount)
        {
            profilePeriod
                .ProfiledValue
                .Should()
                .Be(expectedAmount);
        }
    }
}