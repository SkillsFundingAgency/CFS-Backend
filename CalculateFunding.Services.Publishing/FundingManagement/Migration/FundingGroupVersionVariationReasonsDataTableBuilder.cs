using System;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.SqlExport;
using System.Data;

namespace CalculateFunding.Services.Publishing.FundingManagement.Migration
{
    public class FundingGroupVersionVariationReasonsDataTableBuilder : DataTableBuilder<FundingGroupVersionVariationReason>
    {
        protected override DataColumn[] GetDataColumns(FundingGroupVersionVariationReason dto) =>
            new[]
            {
                NewDataColumn<Guid>("FundingGroupVersionVariationReasonId"),
                NewDataColumn<Guid>("FundingGroupVersionId"),
                NewDataColumn<int>("VariationReasonId")
            };

        protected override void AddDataRowToDataTable(FundingGroupVersionVariationReason dto)
        {
            DataTable.Rows.Add(dto.FundingGroupVersionVariationReasonId,
                dto.FundingGroupVersionId,
                dto.VariationReasonId);
        }

        protected override void EnsureTableNameIsSet(FundingGroupVersionVariationReason dto)
            => TableName = $"[dbo].[FundingGroupVersionVariationReasons]";
    }
}
