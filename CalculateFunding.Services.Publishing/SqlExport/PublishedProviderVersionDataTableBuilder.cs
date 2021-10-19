using System;
using System.Data;
using System.Linq;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.SqlExport
{
    public class PublishedProviderVersionDataTableBuilder : DataTableBuilder<PublishedProviderVersion>
    {
        protected override DataColumn[] GetDataColumns(PublishedProviderVersion dto)
            => new[]
            {
                NewDataColumn<string>("PublishedProviderId", 128),
                NewDataColumn<decimal>("TotalFunding"),
                NewDataColumn<string>("ProviderId", 32),
                NewDataColumn<string>("FundingStreamId", 32),
                NewDataColumn<string>("FundingPeriodId", 32),
                NewDataColumn<string>("MajorVersion", 32),
                NewDataColumn<string>("MinorVersion", 32),
                NewDataColumn<string>("Status", 32),
                NewDataColumn<DateTime>("LastUpdated"),
                NewDataColumn<string>("LastUpdatedBy", 256),
                NewDataColumn<bool>("IsIndicative", defaultValue: false),
                NewDataColumn<string>("ProviderVariationReasons", 1024)
            };

        protected override void AddDataRowToDataTable(PublishedProviderVersion dto)
            => DataTable.Rows.Add(dto.PublishedProviderId,
                dto.TotalFunding.GetValueOrDefault(),
                dto.ProviderId,
                dto.FundingStreamId,
                dto.FundingPeriodId,
                dto.MajorVersion.ToString(),
                dto.MinorVersion.ToString(),
                dto.Status.ToString(),
                dto.Date.UtcDateTime,
                dto.Author.Name,
                dto.IsIndicative,
                string.Join(";", dto.VariationReasons.Select(s => s)));

        protected override void EnsureTableNameIsSet(PublishedProviderVersion dto)
            => TableName = $"[dbo].[{dto.FundingStreamId}_{dto.FundingPeriodId}_Funding]";
    }
}