using System;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.SqlExport;
using System.Data;

namespace CalculateFunding.Services.Publishing.FundingManagement
{
    public class ReleasedProviderVersionDataTableBuilder : DataTableBuilder<ReleasedProviderVersion>
    {
        protected override DataColumn[] GetDataColumns(ReleasedProviderVersion dto) =>
            new[]
            {
                NewDataColumn<Guid>("ReleasedProviderVersionId"),
                NewDataColumn<Guid>("ReleasedProviderId"),
                NewDataColumn<int>("MajorVersion"),
                NewDataColumn<int>("MinorVersion"),
                NewDataColumn<string>("FundingId", 128),
                NewDataColumn<decimal>("TotalFunding"),
                NewDataColumn<string>("CoreProviderVersionId", 128)

            };

        protected override void AddDataRowToDataTable(ReleasedProviderVersion dto)
        {
            DataTable.Rows.Add(dto.ReleasedProviderVersionId,
                dto.ReleasedProviderId,
                dto.MajorVersion,
                dto.MinorVersion,
                dto.FundingId,
                dto.TotalFunding,
                dto.CoreProviderVersionId);
        }

        protected override void EnsureTableNameIsSet(ReleasedProviderVersion dto)
            => TableName = $"[dbo].[ReleasedProviderVersions]";
    }
}