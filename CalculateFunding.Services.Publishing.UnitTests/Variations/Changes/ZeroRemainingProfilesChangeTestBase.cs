using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Changes
{
    public abstract class ZeroRemainingProfilesChangeTestBase : VariationChangeTestBase
    {
        protected void ThenTheProfilePeriodAmountShouldBe(ProfilePeriod profilePeriod, decimal expectedAmount)
        {
            AndTheProfilePeriodAmountShouldBe(profilePeriod, expectedAmount);
        }
    }
}