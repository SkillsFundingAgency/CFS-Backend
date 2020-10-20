using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Variations.Changes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Changes
{
    [TestClass]
    public class ZeroInitialPaymentProfilesChangeTests : VariationChangeTestBase
    {
        [TestInitialize]
        public void SetUp()
        {
            Change = new ZeroInitialPaymentProfilesChange(VariationContext);
        }

        [TestMethod]
        public async Task SkipsProcessingPeriodIfAVariationPointerIsSet()
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
            decimal periodTwoAmount = 2973864M;
            decimal periodThreeAmount = 123764M;
            decimal periodFourAmount = 6487234M;
            decimal periodFiveAmount = 1290837M;

            ProfilePeriod periodOne = NewProfilePeriod(0, 2020, "January", periodOneAmount);
            ProfilePeriod periodTwo = NewProfilePeriod(1, 2020, "January", periodTwoAmount);
            ProfilePeriod periodThree = NewProfilePeriod(0, 2020, "February", periodThreeAmount);
            ProfilePeriod periodFour = NewProfilePeriod(1, 2020, "January", periodFourAmount);
            ProfilePeriod periodFive = NewProfilePeriod(2, 2020, "January", periodFiveAmount);

            AndTheFundingLines(NewFundingLine(_ => _.WithFundingLineCode(fundingLineOneId)
                .WithDistributionPeriods(NewDistributionPeriod(dp => dp.WithProfilePeriods(periodOne, periodTwo, periodThree)),
                    NewDistributionPeriod(dp => dp.WithProfilePeriods(periodFour, periodFive)))));

            await WhenTheChangeIsApplied();

            AndTheProfilePeriodAmountShouldBe(periodOne, periodOneAmount);
            AndTheProfilePeriodAmountShouldBe(periodTwo, periodTwoAmount);
            AndTheProfilePeriodAmountShouldBe(periodThree, periodThreeAmount);
            AndTheProfilePeriodAmountShouldBe(periodFour, periodFourAmount);
            AndTheProfilePeriodAmountShouldBe(periodFive, periodFiveAmount);

            AndNoVariationChangesWereQueued();
        }

        [TestMethod]
        public async Task ZerosProfilesWhenNoVariationsPointersAreSetInSpecificationOnEachMatchingFundingLine()
        {
            string fundingLineOneId = NewRandomString();
            int year = 2020;

            GivenTheVariationPointersForTheSpecification(null);

            decimal periodOneAmount = 293487M;

            ProfilePeriod periodOne = NewProfilePeriod(0, year, "January", periodOneAmount);
            ProfilePeriod periodTwo = NewProfilePeriod(1, year, "January", 2973864M);
            ProfilePeriod periodThree = NewProfilePeriod(0, year, "February", 123764M);
            ProfilePeriod periodFour = NewProfilePeriod(1, year, "January", 6487234M);
            ProfilePeriod periodFive = NewProfilePeriod(2, year, "January", 1290837M);

            AndTheFundingLines(NewFundingLine(_ => _.WithFundingLineCode(fundingLineOneId)
            .WithOrganisationGroupingReason(OrganisationGroupingReason.Payment)
                .WithDistributionPeriods(NewDistributionPeriod(dp => dp.WithProfilePeriods(periodOne, periodTwo, periodThree)),
                    NewDistributionPeriod(dp => dp.WithProfilePeriods(periodFour, periodFive)))));

            await WhenTheChangeIsApplied();

            ThenProfilePeriodsShouldBeZeroAmount(periodOne, periodTwo, periodThree, periodFour, periodFive);
        }

        [TestMethod]
        public async Task RecordsErrorIfNoVariationPointersUnableToBeObtained()
        {
            GivenTheVariationPointersForTheSpecificationReturnInternalServerError();

            await WhenTheChangeIsApplied();

            ThenTheErrorWasRecorded($"Unable to obtain variation pointers");
            AndNoVariationChangesWereQueued();
        }

        private void ThenProfilePeriodsShouldBeZeroAmount(params ProfilePeriod[] profilePeriods)
        {
            AndTheProfilePeriodsAmountShouldBe(profilePeriods, 0M);
        }

        protected void ThenTheProfilePeriodAmountShouldBe(ProfilePeriod profilePeriod, decimal expectedAmount)
        {
            AndTheProfilePeriodAmountShouldBe(profilePeriod, expectedAmount);
        }
    }
}