using System;
using System.Collections.Generic;
using System.Data;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.SqlExport;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace CalculateFunding.Services.Publishing.UnitTests.SqlExport
{
    [TestClass]
    public class ProviderPaymentFundingLineDataTableBuilderTests : DataTableBuilderTest<ProviderPaymentFundingLineDataTableBuilder>
    {
        [TestMethod]
        public void GivenCurrentPublishedProviderVersion_MapsPaymentFundingLinesIntoDataTable()
        {
            DataTableBuilder = new ProviderPaymentFundingLineDataTableBuilder();

            PublishedProviderVersion rowOne = NewPublishedProviderVersion(_ => _.WithFundingStreamId(FundingStreamId)
                .WithProviderId(NewRandomStringWithMaxLength(32))
                .WithFundingPeriodId(FundingPeriodId)
                .WithFundingStreamId(FundingStreamId)
                .WithFundingLines(
                    NewFundingLine(fl => fl.WithFundingLineType(FundingLineType.Payment).WithFundingLineCode("FL1").WithName("FundingLineOne").WithValue(1).WithTemplateLineId(1)),
                    NewFundingLine(fl => fl.WithFundingLineType(FundingLineType.Payment).WithFundingLineCode("FL2").WithName("FundingLineTwo").WithValue(2).WithTemplateLineId(2))
                )
                .WithAuthor(NewAuthor(auth => auth.WithName(NewRandomStringWithMaxLength(32))))
                );
            PublishedProviderVersion rowTwo = NewPublishedProviderVersion(_ => _.WithFundingStreamId(FundingStreamId)
                .WithProviderId(NewRandomStringWithMaxLength(32))
                .WithFundingPeriodId(FundingPeriodId)
                .WithFundingStreamId(FundingStreamId)
                .WithFundingLines(
                    NewFundingLine(fl => fl.WithFundingLineType(FundingLineType.Payment).WithFundingLineCode("FL3").WithName("FundingLineThree").WithValue(3).WithTemplateLineId(3)),
                    NewFundingLine(fl => fl.WithFundingLineType(FundingLineType.Payment).WithFundingLineCode("FL4").WithName("FundingLineFour").WithValue(4).WithTemplateLineId(4))
                )
                .WithAuthor(NewAuthor(auth => auth.WithName(NewRandomStringWithMaxLength(32))))
                );

            WhenTheRowsAreAdded(rowOne, rowTwo);

            ThenTheDataTableHasColumnsMatching(
                GetDataColumns());

            IEnumerable<object[]> dataRowsOne = rowOne.FundingLines.Select(fl => GetDataRow(rowOne, fl));
            IEnumerable<object[]> dataRowsTwo = rowTwo.FundingLines.Select(fl => GetDataRow(rowTwo, fl));
            object[][] dataRows = dataRowsOne.Concat(dataRowsTwo).ToArray();

            AndTheDataTableHasRowsMatching(dataRows);

            AndTheTableNameIs($"[dbo].[{FundingStreamId}_{FundingPeriodId}_ProviderPaymentFundingLinesAllVersions]");
        }

        private static object[] GetDataRow(
            PublishedProviderVersion publishedProviderVersion,
            FundingLine fundingLine)
        {
            List<object> dataRowValues = new()
            {
                publishedProviderVersion.PublishedProviderId,
                publishedProviderVersion.ProviderId,
                publishedProviderVersion.MajorVersion.ToString(),
                fundingLine.FundingLineCode,
                fundingLine.Name,
                fundingLine.Value,
                publishedProviderVersion.IsIndicative,
                publishedProviderVersion.Date.UtcDateTime,
                publishedProviderVersion.Author.Name,
            };


            return dataRowValues.ToArray();
        }

        private DataColumn[] GetDataColumns()
        {
            List<DataColumn> dataColumns = new()
            {
                NewDataColumn<string>("PublishedProviderId", 128),
                NewDataColumn<string>("ProviderId", 32),
                NewDataColumn<string>("MajorVersion", 32),
                NewDataColumn<string>("FundingLineCode", 32),
                NewDataColumn<string>("FundingLineName", 64),
                NewDataColumn<decimal>("FundingValue"),
                NewDataColumn<bool>("IsIndicative"),
                NewDataColumn<DateTime>("LastUpdated"),
                NewDataColumn<string>("LastUpdatedBy", 256)
            };

            return dataColumns.ToArray();
        }

        private static Reference NewAuthor(Action<ReferenceBuilder> setUp = null)
        {
            ReferenceBuilder referenceBuilder = new();

            setUp?.Invoke(referenceBuilder);
            
            return referenceBuilder.Build();
        }
    }
}