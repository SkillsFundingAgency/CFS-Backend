using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.SqlExport;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using CommonModels = CalculateFunding.Common.TemplateMetadata.Models;

namespace CalculateFunding.Services.Publishing.UnitTests.SqlExport
{
    [TestClass]
    public class PaymentFundingLineDataTableBuilderTests : DataTableBuilderTest<PaymentFundingLineDataTableBuilder>
    {
        private IEnumerable<CommonModels.FundingLine> _fundingLines;

        [TestInitialize]
        public void SetUp()
        {
            _fundingLines = AsArray(
                new CommonModels.FundingLine
                {
                    Name = NewRandomString(),
                    TemplateLineId = 1,
                    Type = Common.TemplateMetadata.Enums.FundingLineType.Payment
                },
                new CommonModels.FundingLine
                {
                    Name = NewRandomString(),
                    TemplateLineId = 2,
                    Type = Common.TemplateMetadata.Enums.FundingLineType.Payment
                },
                new CommonModels.FundingLine
                {
                    Name = NewRandomString(),
                    TemplateLineId = 3,
                    Type = Common.TemplateMetadata.Enums.FundingLineType.Payment
                });

            DataTableBuilder = new PaymentFundingLineDataTableBuilder(_fundingLines);
        }

        [TestMethod]
        public void MapsPaymentFundingLinesIntoDataTable()
        {
            uint fundingLineTemplateIdOne = 1;
            uint fundingLineTemplateIdTwo = 2;
            
            string fundingLineOneName = NewRandomString();
            string fundingLineTwoName = NewRandomString();
            
            FundingLine paymentFundingLineOne = NewFundingLine(_ => _.WithFundingLineType(FundingLineType.Payment)
                .WithTemplateLineId(fundingLineTemplateIdOne)
                .WithName(fundingLineOneName)
                .WithValue(NewRandomNumber()));
            FundingLine paymentFundingLineTwo = NewFundingLine(_ => _.WithFundingLineType(FundingLineType.Payment)
                .WithTemplateLineId(fundingLineTemplateIdTwo)
                .WithName(fundingLineOneName));
            FundingLine paymentFundingLineThree = NewFundingLine(_ => _.WithFundingLineType(FundingLineType.Payment)
                .WithTemplateLineId(fundingLineTemplateIdOne)
                .WithName(fundingLineOneName)
                .WithValue(NewRandomNumber()));
            FundingLine paymentFundingLineFour = NewFundingLine(_ => _.WithFundingLineType(FundingLineType.Payment)
                .WithTemplateLineId(fundingLineTemplateIdTwo)
                .WithName(fundingLineTwoName));

            PublishedProviderVersion rowOne = NewPublishedProviderVersion(_ => _.WithFundingLines(paymentFundingLineOne,
                NewFundingLine(fundingLineBuilder => fundingLineBuilder.WithFundingLineType(FundingLineType.Information)),
                paymentFundingLineTwo)
                .WithFundingStreamId(FundingStreamId)
                .WithFundingPeriodId(FundingPeriodId));
            PublishedProviderVersion rowTwo = NewPublishedProviderVersion(_ => _.WithFundingLines(paymentFundingLineThree,
                NewFundingLine(fundingLineBuilder => fundingLineBuilder.WithFundingLineType(FundingLineType.Information)),
                paymentFundingLineFour)
                .WithFundingStreamId(FundingStreamId)
                .WithFundingPeriodId(FundingPeriodId));
            
            WhenTheRowsAreAdded(rowOne, rowTwo);

            List<System.Data.DataColumn> expectedColumns = _fundingLines.Select(_ => NewDataColumn<decimal>($"FL_{_.TemplateLineId}_{_.Name}", allowNull: true)).ToList();
            expectedColumns.Insert(0, NewDataColumn<string>("PublishedProviderId", maxLength: 128));

            ThenTheFundingLineDataTableHasColumnsMatching(expectedColumns.ToArray());
            AndTheDataTableHasRowsMatching(NewRow(rowOne.PublishedProviderId, paymentFundingLineOne.Value, paymentFundingLineTwo.Value, null),
                NewRow(rowTwo.PublishedProviderId, paymentFundingLineThree.Value, paymentFundingLineFour.Value, null));
            AndTheTableNameIs($"[dbo].[{FundingStreamId}_{FundingPeriodId}_PaymentFundingLines]");
        }

        [TestMethod]
        public void MapsPaymentFundingLinesIntoDataTable_ExcludingOldTemplateItems()
        {
            uint fundingLineTemplateIdOne = 1;
            // This TemplateLineId is not in current template version, so shouldn't be included in Datatable
            uint fundingLineTemplateIdTwo = 34;

            string fundingLineOneName = NewRandomString();
            string fundingLineTwoName = NewRandomString();

            FundingLine paymentFundingLineOne = NewFundingLine(_ => _.WithFundingLineType(FundingLineType.Payment)
                .WithTemplateLineId(fundingLineTemplateIdOne)
                .WithName(fundingLineOneName)
                .WithValue(NewRandomNumber()));
            FundingLine paymentFundingLineTwo = NewFundingLine(_ => _.WithFundingLineType(FundingLineType.Payment)
                .WithTemplateLineId(fundingLineTemplateIdTwo)
                .WithName(fundingLineOneName)
                .WithValue(NewRandomNumber()));
            FundingLine paymentFundingLineThree = NewFundingLine(_ => _.WithFundingLineType(FundingLineType.Payment)
                .WithTemplateLineId(fundingLineTemplateIdOne)
                .WithName(fundingLineOneName)
                .WithValue(NewRandomNumber()));
            FundingLine paymentFundingLineFour = NewFundingLine(_ => _.WithFundingLineType(FundingLineType.Payment)
                .WithTemplateLineId(fundingLineTemplateIdTwo)
                .WithName(fundingLineTwoName)
                .WithValue(NewRandomNumber()));

            PublishedProviderVersion rowOne = NewPublishedProviderVersion(_ => _.WithFundingLines(paymentFundingLineOne,
                NewFundingLine(fundingLineBuilder => fundingLineBuilder.WithFundingLineType(FundingLineType.Information)),
                paymentFundingLineTwo)
                .WithFundingStreamId(FundingStreamId)
                .WithFundingPeriodId(FundingPeriodId));
            PublishedProviderVersion rowTwo = NewPublishedProviderVersion(_ => _.WithFundingLines(paymentFundingLineThree,
                NewFundingLine(fundingLineBuilder => fundingLineBuilder.WithFundingLineType(FundingLineType.Information)),
                paymentFundingLineFour)
                .WithFundingStreamId(FundingStreamId)
                .WithFundingPeriodId(FundingPeriodId));

            WhenTheRowsAreAdded(rowOne, rowTwo);

            List<System.Data.DataColumn> expectedColumns = _fundingLines.Select(_ => NewDataColumn<decimal>($"FL_{_.TemplateLineId}_{_.Name}", allowNull: true)).ToList();
            expectedColumns.Insert(0, NewDataColumn<string>("PublishedProviderId", maxLength: 128));

            ThenTheFundingLineDataTableHasColumnsMatching(expectedColumns.ToArray());
            AndTheDataTableHasRowsMatching(NewRow(rowOne.PublishedProviderId, paymentFundingLineOne.Value, null, null),
                NewRow(rowTwo.PublishedProviderId, paymentFundingLineThree.Value, null, null));
            AndTheTableNameIs($"[dbo].[{FundingStreamId}_{FundingPeriodId}_PaymentFundingLines]");
        }
    }
}