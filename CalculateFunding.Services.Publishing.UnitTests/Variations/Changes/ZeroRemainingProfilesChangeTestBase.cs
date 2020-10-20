using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Changes
{
    public abstract class ZeroRemainingProfilesChangeTestBase : VariationChangeTestBase
    {
        [TestMethod]
        public async Task RecordsErrorIfNoVariationPointersUnableToBeObtained()
        {
            GivenTheVariationPointersForTheSpecificationReturnInternalServerError();

            await WhenTheChangeIsApplied();

            ThenTheErrorWasRecorded($"Unable to obtain variation pointers");
            AndNoVariationChangesWereQueued();
        }

        protected void ThenTheProfilePeriodAmountShouldBe(ProfilePeriod profilePeriod, decimal expectedAmount)
        {
            AndTheProfilePeriodAmountShouldBe(profilePeriod, expectedAmount);
        }
    }
}