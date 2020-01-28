using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Variations.Changes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Changes
{
    [TestClass]
    public class TransferRemainingProfilesToSuccessorChangeTests : ZeroRemainingProfilesChangeTestBase
    {
        [TestInitialize]
        public void SetUp()
        {
            Change = new TransferRemainingProfilesToSuccessorChange(VariationContext);
            
            VariationContext.SuccessorRefreshState = VariationContext.RefreshState.DeepCopy();
        }
        
        [TestMethod]
        public async Task RecordsErrorIfNoVariationPointersForSpecificationId()
        {
            await WhenTheChangeIsApplied();
            
            ThenTheErrorWasRecorded($"Unable to transfer remaining profiles for provider id {VariationContext.ProviderId}");
            AndNoVariationChangesWereQueued();
        }

        [TestMethod]
        public async Task RecordsErrorIfNoMatchingSuccessorFundingLineForAVariationPointer()
        {
            string fundingLineId = NewRandomString();
            int year = 2020;
            int occurence = 1;
            string typeValue = "January";

            GivenTheVariationPointersForTheSpecification(NewVariationPointer(_ => _.WithFundingLineId(fundingLineId)
                .WithOccurence(occurence)
                .WithYear(year)
                .WithTypeValue(typeValue)));
            AndTheFundingLines(NewFundingLine(_ => _.WithFundingLineCode(fundingLineId)
                .WithDistributionPeriods(NewDistributionPeriod(dp => dp.WithProfilePeriods(
                    NewProfilePeriod(1, 2020, "January", 2973864M))))));

            await WhenTheChangeIsApplied();
            
            ThenTheErrorWasRecorded($"Did not locate a funding line for variation pointer with fundingLineId {fundingLineId}");
            AndNoVariationChangesWereQueued();
        }

        [TestMethod]
        public async Task TransfersAmountsFromProfilesAfterProfileVariationPointersInSpecificationOnEachMatchingFundingLine()
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
            
            ProfilePeriod successorPeriodOne = NewProfilePeriod(0, 2020, "January", periodOneAmount);
            ProfilePeriod successorPeriodTwo = NewProfilePeriod(1, 2020, "January", 100M);
            ProfilePeriod successorPeriodThree = NewProfilePeriod(0, 2020, "February", 101M);
            ProfilePeriod successorPeriodFour = NewProfilePeriod(1, 2020, "January", 102M);
            ProfilePeriod successorPeriodFive = NewProfilePeriod(2, 2020, "January", 103M);
            
            AndTheSuccessorFundingLines(NewFundingLine(_ => _.WithFundingLineCode(fundingLineOneId)
                .WithDistributionPeriods(NewDistributionPeriod(dp => dp.WithProfilePeriods(successorPeriodOne, successorPeriodTwo, successorPeriodThree)),
                    NewDistributionPeriod(dp => dp.WithProfilePeriods(successorPeriodFour, successorPeriodFive)))));
            
            await WhenTheChangeIsApplied();
            
            ThenTheProfilePeriodAmountShouldBe(successorPeriodOne, periodOneAmount);
            AndTheProfilePeriodAmountShouldBe(successorPeriodTwo, 2973864M + 100M);
            AndTheProfilePeriodAmountShouldBe(successorPeriodThree, 123764M + 101M);
            AndTheProfilePeriodAmountShouldBe(successorPeriodFour, 6487234M + 102M);
            AndTheProfilePeriodAmountShouldBe(successorPeriodFive, 1290837M + 103M);
        }

        [TestMethod]
        public async Task RecordsErrorIfNoMatchingProfilePeriodForAVariationPointer()
        {
            string fundingLineId = NewRandomString();
            
            GivenTheVariationPointersForTheSpecification(NewVariationPointer(_ => _.WithFundingLineId(fundingLineId)));
            AndTheFundingLines(NewFundingLine(_ => _.WithFundingLineCode(fundingLineId)));
            AndTheSuccessorFundingLines(NewFundingLine(_ => _.WithFundingLineCode(fundingLineId)));

            await WhenTheChangeIsApplied();
            
            ThenTheErrorWasRecorded($"Did not locate profile period corresponding to variation pointer for funding line id {fundingLineId}");
            AndNoVariationChangesWereQueued();
        }
    }
}