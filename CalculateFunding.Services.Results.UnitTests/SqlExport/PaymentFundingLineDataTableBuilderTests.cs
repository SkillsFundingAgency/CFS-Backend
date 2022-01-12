using CalculateFunding.Common.TemplateMetadata.Enums;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Results.SqlExport;
using CalculateFunding.Services.SqlExport;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using FundingLine = CalculateFunding.Common.TemplateMetadata.Models.FundingLine;

namespace CalculateFunding.Services.Results.UnitTests.SqlExport
{
    [TestClass]
    public class PaymentFundingLineDataTableBuilderTests
        : DataTableBuilderTest<PaymentFundingLineDataTableBuilder>
    {
        private uint fundingLineTemplateIdOne;
        private uint fundingLineTemplateIdTwo;
        private uint fundingLineTemplateIdThree;
        private uint fundingLineTemplateIdFour;

        private string fundingLineOneName;
        private string fundingLineTwoName;
        private string fundingLineThreeName;
        private string fundingLineFourName;

        [TestInitialize]
        public void SetUp()
        {
            fundingLineTemplateIdOne = 1;
            fundingLineTemplateIdTwo = 2;
            fundingLineTemplateIdThree = 3;
            fundingLineTemplateIdFour = 4;

            fundingLineOneName = "1";
            fundingLineTwoName = "2";
            fundingLineThreeName = "3";
            fundingLineFourName = "4";

            FundingLine paymentFundingLineOne = NewFundingLine(_ => _.WithFundingLineType(FundingLineType.Payment)
                .WithTemplateLineId(fundingLineTemplateIdOne)
                .WithName(fundingLineOneName));
            FundingLine paymentFundingLineTwo = NewFundingLine(_ => _.WithFundingLineType(FundingLineType.Payment)
                .WithTemplateLineId(fundingLineTemplateIdTwo)
                .WithName(fundingLineTwoName));
            FundingLine paymentFundingLineThree = NewFundingLine(_ => _.WithFundingLineType(FundingLineType.Payment)
                .WithTemplateLineId(fundingLineTemplateIdThree)
                .WithName(fundingLineThreeName));
            FundingLine paymentFundingLineFour = NewFundingLine(_ => _.WithFundingLineType(FundingLineType.Payment)
                .WithTemplateLineId(fundingLineTemplateIdFour)
                .WithName(fundingLineFourName));

            IEnumerable<FundingLine> fundingLines = new []{ paymentFundingLineOne, paymentFundingLineTwo, paymentFundingLineThree, paymentFundingLineFour };
            DataTableBuilder = new PaymentFundingLineDataTableBuilder(fundingLines, new SqlNameGenerator(), SpecificationIdentifierName);
        }

        [TestMethod]
        public void MapsPaymentFundingLinesIntoDataTable()
        {
            FundingLineResult fundingLineResultOne = NewFundingLineResult(_ => _.WithFundingLine(NewReference(r => r.WithId(fundingLineTemplateIdOne.ToString()).WithName(fundingLineOneName))).WithValue(NewRandomNumber()));
            FundingLineResult fundingLineResultTwo = NewFundingLineResult(_ => _.WithFundingLine(NewReference(r => r.WithId(fundingLineTemplateIdTwo.ToString()).WithName(fundingLineTwoName))).WithValue(NewRandomNumber()));
            FundingLineResult fundingLineResultThree = NewFundingLineResult(_ => _.WithFundingLine(NewReference(r => r.WithId(fundingLineTemplateIdThree.ToString()).WithName(fundingLineThreeName))).WithValue(NewRandomNumber()));
            FundingLineResult fundingLineResultFour = NewFundingLineResult(_ => _.WithFundingLine(NewReference(r => r.WithId(fundingLineTemplateIdFour.ToString()).WithName(fundingLineFourName))).WithValue(NewRandomNumber()));

            ProviderResult rowOne = NewProviderResult(_ => _.WithFundingLineResults(fundingLineResultOne, fundingLineResultTwo).WithProviderSummary(NewProviderSummary()).WithSpecificationId(SpecificationId));
            ProviderResult rowTwo = NewProviderResult(_ => _.WithFundingLineResults(fundingLineResultThree,fundingLineResultFour).WithProviderSummary(NewProviderSummary()).WithSpecificationId(SpecificationId));

            WhenTheRowsAreAdded(rowOne, rowTwo);

            ThenTheDataTableHasColumnsMatching(NewDataColumn<string>("ProviderId", maxLength: 128),
                NewDataColumn<decimal>($"FL_{fundingLineTemplateIdOne}_{fundingLineOneName}", allowNull: true),
                NewDataColumn<decimal>($"FL_{fundingLineTemplateIdTwo}_{fundingLineTwoName}", allowNull: true),
                NewDataColumn<decimal>($"FL_{fundingLineTemplateIdThree}_{fundingLineThreeName}", allowNull: true),
                NewDataColumn<decimal>($"FL_{fundingLineTemplateIdFour}_{fundingLineFourName}", allowNull: true));
            AndTheDataTableHasRowsMatching(
                NewRow(
                    rowOne.Provider.Id,
                    fundingLineResultOne.Value,
                    fundingLineResultTwo.Value),
                NewRow(
                    rowTwo.Provider.Id,
                    fundingLineResultThree.Value,
                    fundingLineResultFour.Value));
            AndTheTableNameIs($"[dbo].[{SpecificationIdentifierName}_PaymentFundingLines]");
        }
    }
}
