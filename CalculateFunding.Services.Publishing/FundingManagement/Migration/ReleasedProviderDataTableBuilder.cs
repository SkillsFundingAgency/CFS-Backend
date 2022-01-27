using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.SqlExport;
using System.Data;


namespace CalculateFunding.Services.Publishing.FundingManagement
{
    public class ReleasedProviderDataTableBuilder : DataTableBuilder<ReleasedProvider>
    {
        protected override DataColumn[] GetDataColumns(ReleasedProvider dto) =>
            new[]
            {
                NewDataColumn<int>("ReleasedProviderId"),
                NewDataColumn<string>("SpecificationId", 64),
                NewDataColumn<string>("ProviderId", 32)
            };

        protected override void AddDataRowToDataTable(ReleasedProvider dto)
        {
            DataTable.Rows.Add(dto.ReleasedProviderId,
                dto.SpecificationId,
                dto.ProviderId);
        }

        protected override void EnsureTableNameIsSet(ReleasedProvider dto)
            => TableName = $"[dbo].[ReleasedProviders]";
    }
}