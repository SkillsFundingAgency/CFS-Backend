using System;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.SqlExport;
using System.Data;

namespace CalculateFunding.Services.Publishing.FundingManagement.Migration
{
    public class FundingGroupProviderDataTableBuilder : DataTableBuilder<FundingGroupProvider>
    {
        protected override DataColumn[] GetDataColumns(FundingGroupProvider dto) =>
            new[]
            {
                NewDataColumn<Guid>("FundingGroupProviderId"),
                NewDataColumn<Guid>("FundingGroupVersionId"),
                NewDataColumn<Guid>("ReleasedProviderVersionChannelId")
            };

        protected override void AddDataRowToDataTable(FundingGroupProvider dto)
        {
            DataTable.Rows.Add(dto.FundingGroupProviderId,
                dto.FundingGroupVersionId,
                dto.ReleasedProviderVersionChannelId);
        }

        protected override void EnsureTableNameIsSet(FundingGroupProvider dto)
            => TableName = $"[dbo].[FundingGroupProviders]";
    }
}
