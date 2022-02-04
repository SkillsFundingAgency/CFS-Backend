using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.SqlExport;

namespace CalculateFunding.Services.Publishing.SqlExport
{
    public class PublishedProviderVersionDataTableBuilder : DataTableBuilder<PublishedProviderVersion>
    {
        private readonly IEnumerable<ProviderVersionInChannel> _providerVersionInChannels;
        private readonly SqlExportSource _sqlExportSource;
        private readonly bool _latestReleasedVersionChannelPopulationEnabled;

        public PublishedProviderVersionDataTableBuilder(
            IEnumerable<ProviderVersionInChannel> providerVersionInChannels,
            SqlExportSource sqlExportSource,
            bool latestReleasedVersionChannelPopulationEnabled)
        {
            _providerVersionInChannels = providerVersionInChannels;
            _sqlExportSource = sqlExportSource;
            _latestReleasedVersionChannelPopulationEnabled = latestReleasedVersionChannelPopulationEnabled;
        }

        protected override DataColumn[] GetDataColumns(PublishedProviderVersion dto)
        {
            List<DataColumn> dataColumns = new List<DataColumn>
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

            if (_sqlExportSource == SqlExportSource.CurrentPublishedProviderVersion
                && _latestReleasedVersionChannelPopulationEnabled)
            {
                dataColumns.Add(NewDataColumn<string>("LatestStatementReleaseVersion", 8));
                dataColumns.Add(NewDataColumn<string>("LatestPaymentReleaseVersion", 8));
                dataColumns.Add(NewDataColumn<string>("LatestContractReleaseVersion", 8));
            }

            dataColumns.Add(NewDataColumn<DateTime>("LastUpdated"));
            dataColumns.Add(NewDataColumn<string>("LastUpdatedBy", 256));
            dataColumns.Add(NewDataColumn<bool>("IsIndicative", defaultValue: false));
            dataColumns.Add(NewDataColumn<string>("ProviderVariationReasons", 1024));

            return dataColumns.ToArray();
        }

        protected override void AddDataRowToDataTable(PublishedProviderVersion dto)
        {
            List<object> dataRowValues = new List<object>
            {
                dto.PublishedProviderId,
                  dto.TotalFunding.GetValueOrDefault(),
                  dto.ProviderId,
                  dto.FundingStreamId,
                  dto.FundingPeriodId,
                  dto.MajorVersion.ToString(),
                  dto.MinorVersion.ToString(),
                  dto.Status.ToString()
            };

            if (_sqlExportSource == SqlExportSource.CurrentPublishedProviderVersion
                && _latestReleasedVersionChannelPopulationEnabled)
            {
                dataRowValues.Add($"{_providerVersionInChannels.SingleOrDefault(_ => _.ChannelCode == "Statement")?.MajorVersion}.0");
                dataRowValues.Add($"{_providerVersionInChannels.SingleOrDefault(_ => _.ChannelCode == "Payment")?.MajorVersion}.0");
                dataRowValues.Add($"{_providerVersionInChannels.SingleOrDefault(_ => _.ChannelCode == "Contracting")?.MajorVersion}.0");
            }

            dataRowValues.Add(dto.Date.UtcDateTime);
            dataRowValues.Add(dto.Author.Name);
            dataRowValues.Add(dto.IsIndicative);
            dataRowValues.Add(string.Join(";", dto.VariationReasons.Select(s => s)));

            DataTable.Rows.Add(dataRowValues.ToArray());
        }

        protected override void EnsureTableNameIsSet(PublishedProviderVersion dto)
            => TableName = $"[dbo].[{dto.FundingStreamId}_{dto.FundingPeriodId}_Funding]";
    }
}