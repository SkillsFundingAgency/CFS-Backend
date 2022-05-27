using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.SqlExport;

namespace CalculateFunding.Services.Publishing.SqlExport
{
    public class PublishedProviderVersionDataTableBuilder : DataTableBuilder<PublishedProviderVersion>
    {
        private readonly IReleaseCandidateService _releaseCandidateService;
        private readonly IEnumerable<ProviderVersionInChannel> _allProviderVersionInChannels;
        private readonly SqlExportSource _sqlExportSource;
        private readonly bool _latestReleasedVersionChannelPopulationEnabled;

        public PublishedProviderVersionDataTableBuilder(
            IReleaseCandidateService releaseCandidateService,
            IEnumerable<ProviderVersionInChannel> providerVersionInChannels,
            SqlExportSource sqlExportSource,
            bool latestReleasedVersionChannelPopulationEnabled)
        {
            _releaseCandidateService = releaseCandidateService;
            _allProviderVersionInChannels = providerVersionInChannels;
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

            dataColumns.Add(NewDataColumn<DateTime>("LastUpdated"));
            dataColumns.Add(NewDataColumn<string>("LastUpdatedBy", 256));
            dataColumns.Add(NewDataColumn<bool>("IsIndicative", defaultValue: false));
            dataColumns.Add(NewDataColumn<string>("ProviderVariationReasons", 1024));

            if (_sqlExportSource == SqlExportSource.CurrentPublishedProviderVersion
                && _latestReleasedVersionChannelPopulationEnabled)
            {
                foreach (string channelCode in Enumerable.Distinct(_allProviderVersionInChannels.Select(_ => _.ChannelCode)))
                {
                    dataColumns.Add(NewDataColumn<string>($"Latest{channelCode}ReleaseVersion", maxLength: 8, allowNull: true));
                }

                dataColumns.Add(NewDataColumn<bool>("ReleaseCandidate", defaultValue: false));
            }

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

            dataRowValues.Add(dto.Date.UtcDateTime);
            dataRowValues.Add(dto.Author.Name);
            
            dataRowValues.Add(dto.IsIndicative);
            dataRowValues.Add(string.Join(";", dto.VariationReasons.Select(s => s)));

            if (_sqlExportSource == SqlExportSource.CurrentPublishedProviderVersion
                && _latestReleasedVersionChannelPopulationEnabled)
            {
                List<ReleaseChannel> releaseChannels = new List<ReleaseChannel>();

                IEnumerable<string> distinctChannelCodes = Enumerable.Distinct(_allProviderVersionInChannels.Select(_ => _.ChannelCode));

                foreach (var distinctChannelCode in distinctChannelCodes)
                {
                    ProviderVersionInChannel providerVersionInChannel = _allProviderVersionInChannels.SingleOrDefault(p => p.ProviderId == dto.ProviderId && p.ChannelCode == distinctChannelCode);

                    releaseChannels.Add(new ReleaseChannel { ChannelCode = distinctChannelCode, MajorVersion = providerVersionInChannel?.MajorVersion ?? 0 });

                    if (providerVersionInChannel != null)
                    {
                        dataRowValues.Add($"{providerVersionInChannel?.MajorVersion}.0");
                    }
                    else
                    {
                        dataRowValues.Add(DbNullSafe(null));
                    }
                }

                dataRowValues.Add(_releaseCandidateService.IsReleaseCandidate(dto.MajorVersion, releaseChannels));
            }

            DataTable.Rows.Add(dataRowValues.ToArray());
        }

        protected override void EnsureTableNameIsSet(PublishedProviderVersion dto)
            => TableName = $"[dbo].[{dto.FundingStreamId}_{dto.FundingPeriodId}_Funding]";
    }
}