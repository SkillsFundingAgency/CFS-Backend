using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.SqlExport;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.SqlExport
{
    [TestClass]
    public class PaymentFundingLineDataTableBuilderTests : DataTableBuilderTest<PaymentFundingLineDataTableBuilder>
    {
        [TestInitialize]
        public void SetUp()
        {
            DataTableBuilder = new PaymentFundingLineDataTableBuilder();
        }

        [TestMethod]
        public void MapsPaymentFundingLinesIntoDataTable()
        {
            uint fundingLineTemplateIdOne = NewRandomUnsignedNumber();
            uint fundingLineTemplateIdTwo = NewRandomUnsignedNumber();
            
            string fundingLineOneName = NewRandomString();
            string fundingLineTwoName = NewRandomString();
            
            FundingLine paymentFundingLineOne = NewFundingLine(_ => _.WithOrganisationGroupingReason(OrganisationGroupingReason.Payment)
                .WithTemplateLineId(fundingLineTemplateIdOne)
                .WithName(fundingLineOneName));
            FundingLine paymentFundingLineTwo = NewFundingLine(_ => _.WithOrganisationGroupingReason(OrganisationGroupingReason.Payment)
                .WithTemplateLineId(fundingLineTemplateIdTwo)
                .WithName(fundingLineOneName));
            FundingLine paymentFundingLineThree = NewFundingLine(_ => _.WithOrganisationGroupingReason(OrganisationGroupingReason.Payment)
                .WithTemplateLineId(fundingLineTemplateIdOne)
                .WithName(fundingLineOneName));
            FundingLine paymentFundingLineFour = NewFundingLine(_ => _.WithOrganisationGroupingReason(OrganisationGroupingReason.Payment)
                .WithTemplateLineId(fundingLineTemplateIdTwo)
                .WithName(fundingLineTwoName));

            PublishedProviderVersion rowOne = NewPublishedProviderVersion(_ => _.WithFundingLines(paymentFundingLineOne,
                NewFundingLine(fundingLineBuilder => fundingLineBuilder.WithOrganisationGroupingReason(OrganisationGroupingReason.Information)),
                paymentFundingLineTwo)
                .WithFundingStreamId(FundingStreamId)
                .WithFundingPeriodId(FundingPeriodId));
            PublishedProviderVersion rowTwo = NewPublishedProviderVersion(_ => _.WithFundingLines(paymentFundingLineThree,
                NewFundingLine(fundingLineBuilder => fundingLineBuilder.WithOrganisationGroupingReason(OrganisationGroupingReason.Information)),
                paymentFundingLineFour)
                .WithFundingStreamId(FundingStreamId)
                .WithFundingPeriodId(FundingPeriodId));
            
            WhenTheRowsAreAdded(rowOne, rowTwo);
            
            ThenTheDataTableHasColumnsMatching(NewDataColumn<string>("PublishedProviderId", maxLength: 128),
                NewDataColumn<decimal>($"FL_{fundingLineTemplateIdOne}_{fundingLineOneName}", allowNull: true),
                NewDataColumn<decimal>($"FL_{fundingLineTemplateIdTwo}_{fundingLineTwoName}", allowNull: true));
            AndTheDataTableHasRowsMatching(NewRow(rowOne.PublishedProviderId, paymentFundingLineOne.Value, paymentFundingLineTwo.Value),
                NewRow(rowTwo.PublishedProviderId, paymentFundingLineThree.Value, paymentFundingLineFour.Value));
            AndTheTableNameIs($"[dbo].[{FundingStreamId}_{FundingPeriodId}_PaymentFundingLines]");
        }
    }
}