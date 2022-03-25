using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.SqlExport;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VariationReason = CalculateFunding.Models.Publishing.VariationReason;

namespace CalculateFunding.Services.Publishing.UnitTests.SqlExport
{
    [TestClass]
    public class PublishedProviderVersionDataTableBuilderTests : DataTableBuilderTest<PublishedProviderVersionDataTableBuilder>
    {
        private const int StatementMajorVersion = 1;
        private const int PaymentMajorVersion = 2;
        private const int ContractingMajorVersion = 3;
        private const int ChannelCodeOneMajorVersion = 4;

        [DataRow(SqlExportSource.CurrentPublishedProviderVersion, true)]
        [DataRow(SqlExportSource.CurrentPublishedProviderVersion, false)]
        [DataRow(SqlExportSource.ReleasedPublishedProviderVersion, true)]
        [DataRow(SqlExportSource.ReleasedPublishedProviderVersion, false)]
        [DataTestMethod]
        public void GivenCurrentPublishedProviderVersion_MapsPaymentFundingLinesIntoDataTable(
            SqlExportSource sqlExportSource,
            bool latestReleasedVersionChannelPopulationEnabled)
        {
            List<ProviderVersionInChannel> providerVersionInChannels = new()
            {
                NewProviderVersionInChannel(_ => _.WithChannelCode("Statement").WithMajorVersion(StatementMajorVersion)),
                NewProviderVersionInChannel(_ => _.WithChannelCode("Payment").WithMajorVersion(PaymentMajorVersion)),
                NewProviderVersionInChannel(_ => _.WithChannelCode("Contracting").WithMajorVersion(ContractingMajorVersion)),
                NewProviderVersionInChannel(_ => _.WithChannelCode("ChannelCodeOne").WithMajorVersion(ChannelCodeOneMajorVersion)),
            };

            DataTableBuilder = new PublishedProviderVersionDataTableBuilder(
                providerVersionInChannels,
                sqlExportSource,
                latestReleasedVersionChannelPopulationEnabled);

            PublishedProviderVersion rowOne = NewPublishedProviderVersion(_ => _.WithFundingStreamId(FundingStreamId)
                .WithProviderId(NewRandomString().Substring(0, 32))
                .WithFundingPeriodId(FundingPeriodId)
                .WithTotalFunding(NewRandomUnsignedNumber())
                .WithAuthor(NewAuthor(auth => auth.WithName(NewRandomStringWithMaxLength(32))))
                .WithVariationReasons(new List<VariationReason> { VariationReason.AuthorityFieldUpdated, VariationReason.CalculationValuesUpdated })
                .WithProvider(NewProvider(prov => 
                    prov.WithStatus(NewRandomStringWithMaxLength(32)))));
            PublishedProviderVersion rowTwo = NewPublishedProviderVersion(_ => _.WithFundingStreamId(FundingStreamId)
                .WithProviderId(NewRandomStringWithMaxLength(32))
                .WithFundingPeriodId(FundingPeriodId)
                .WithTotalFunding(NewRandomUnsignedNumber())
                .WithAuthor(NewAuthor(auth => auth.WithName(NewRandomStringWithMaxLength(32))))
                .WithVariationReasons(new List<VariationReason> { VariationReason.CompaniesHouseNumberFieldUpdated, VariationReason.CountryCodeFieldUpdated })
                .WithProvider(NewProvider(prov => 
                    prov.WithStatus(NewRandomStringWithMaxLength(32)))));

            WhenTheRowsAreAdded(rowOne, rowTwo);

            ThenTheDataTableHasColumnsMatching(
                GetDataColumns(sqlExportSource, latestReleasedVersionChannelPopulationEnabled));

            AndTheDataTableHasRowsMatching(
                NewRow(GetDataRow(rowOne, sqlExportSource, latestReleasedVersionChannelPopulationEnabled)),
                NewRow(GetDataRow(rowTwo, sqlExportSource, latestReleasedVersionChannelPopulationEnabled)));

            AndTheTableNameIs($"[dbo].[{FundingStreamId}_{FundingPeriodId}_Funding]");
        }

        private static object[] GetDataRow(
            PublishedProviderVersion publishedProviderVersion,
            SqlExportSource sqlExportSource,
            bool latestReleasedVersionChannelPopulationEnabled)
        {
            List<object> dataRowValues = new()
            {
                publishedProviderVersion.PublishedProviderId,
                publishedProviderVersion.TotalFunding.GetValueOrDefault(),
                publishedProviderVersion.ProviderId,
                publishedProviderVersion.FundingStreamId,
                publishedProviderVersion.FundingPeriodId,
                publishedProviderVersion.MajorVersion.ToString(),
                publishedProviderVersion.MinorVersion.ToString(),
                publishedProviderVersion.Status.ToString()
            };

            dataRowValues.Add(publishedProviderVersion.Date.UtcDateTime);
            dataRowValues.Add(publishedProviderVersion.Author.Name);
            dataRowValues.Add(publishedProviderVersion.IsIndicative);
            dataRowValues.Add(GetVariationReasonAsSemiColonSeparatedString(publishedProviderVersion.VariationReasons));

            if (sqlExportSource == SqlExportSource.CurrentPublishedProviderVersion
                && latestReleasedVersionChannelPopulationEnabled)
            {
                dataRowValues.Add($"{StatementMajorVersion}.0");
                dataRowValues.Add($"{PaymentMajorVersion}.0");
                dataRowValues.Add($"{ContractingMajorVersion}.0");
                dataRowValues.Add($"{ChannelCodeOneMajorVersion}.0");
            }

            return dataRowValues.ToArray();
        }

        private DataColumn[] GetDataColumns(
            SqlExportSource sqlExportSource, 
            bool latestReleasedVersionChannelPopulationEnabled)
        {
            List<DataColumn> dataColumns = new()
            {
                NewDataColumn<string>("PublishedProviderId", 128),
                NewDataColumn<decimal>("TotalFunding"),
                NewDataColumn<string>("ProviderId", 32),
                NewDataColumn<string>("FundingStreamId", 32),
                NewDataColumn<string>("FundingPeriodId", 32),
                NewDataColumn<string>("MajorVersion", 32),
                NewDataColumn<string>("MinorVersion", 32),
                NewDataColumn<string>("Status", 32)
            };

            dataColumns.Add(NewDataColumn<DateTime>("LastUpdated"));
            dataColumns.Add(NewDataColumn<string>("LastUpdatedBy", 256));
            dataColumns.Add(NewDataColumn<bool>("IsIndicative"));
            dataColumns.Add(NewDataColumn<string>("ProviderVariationReasons", 1024));

            if (sqlExportSource == SqlExportSource.CurrentPublishedProviderVersion
                && latestReleasedVersionChannelPopulationEnabled)
            {
                dataColumns.Add(NewDataColumn<string>("LatestStatementReleaseVersion", 8));
                dataColumns.Add(NewDataColumn<string>("LatestPaymentReleaseVersion", 8));
                dataColumns.Add(NewDataColumn<string>("LatestContractReleaseVersion", 8));
                dataColumns.Add(NewDataColumn<string>("LatestChannelCodeOneReleaseVersion", 8));
            }

            return dataColumns.ToArray();
        }

        private static Reference NewAuthor(Action<ReferenceBuilder> setUp = null)
        {
            ReferenceBuilder referenceBuilder = new();

            setUp?.Invoke(referenceBuilder);
            
            return referenceBuilder.Build();
        }

        private static ProviderVersionInChannel NewProviderVersionInChannel(Action<ProviderVersionInChannelBuilder> setUp = null)
        {
            ProviderVersionInChannelBuilder providerVersionInChannelBuilder = new ProviderVersionInChannelBuilder();

            setUp?.Invoke(providerVersionInChannelBuilder);

            return providerVersionInChannelBuilder.Build();
        }

        private static string GetVariationReasonAsSemiColonSeparatedString(IEnumerable<VariationReason> variationReasons)
        {
            return string.Join(";", variationReasons.Select(s => s));
        }
    }
}