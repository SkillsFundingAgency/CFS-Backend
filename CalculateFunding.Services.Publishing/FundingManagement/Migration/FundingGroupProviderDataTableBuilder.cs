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
                NewDataColumn<int>("FundingGroupProviderId"),
                NewDataColumn<int>("FundingGroupVersionId"),
                NewDataColumn<int>("ReleasedProviderVersionChannelId")
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
