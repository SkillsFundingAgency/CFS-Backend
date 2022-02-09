using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.SqlExport;
using System;
using System.Data;

namespace CalculateFunding.Services.Publishing.FundingManagement.Migration
{
    public class ReleasedProviderVersionChannelDataTableBuilder : DataTableBuilder<ReleasedProviderVersionChannel>
    {
        protected override DataColumn[] GetDataColumns(ReleasedProviderVersionChannel dto) =>
            new[]
            {
                NewDataColumn<int>("ReleasedProviderVersionChannelId"),
                NewDataColumn<int>("ReleasedProviderVersionId"),
                NewDataColumn<int>("ChannelId"),
                NewDataColumn<DateTime>("StatusChangedDate"),
                NewDataColumn<string>("AuthorId", 64),
                NewDataColumn<string>("AuthorName", 512),
            };

        protected override void AddDataRowToDataTable(ReleasedProviderVersionChannel dto)
        {
            DataTable.Rows.Add(dto.ReleasedProviderVersionChannelId,
                dto.ReleasedProviderVersionId,
                dto.ChannelId,
                dto.StatusChangedDate,
                dto.AuthorId,
                dto.AuthorName);
        }

        protected override void EnsureTableNameIsSet(ReleasedProviderVersionChannel dto)
            => TableName = $"[dbo].[ReleasedProviderVersionChannels]";
    }
}
