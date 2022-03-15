using System;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.SqlExport;
using System.Data;

namespace CalculateFunding.Services.Publishing.FundingManagement.Migration
{
    public class ReleasedProviderChannelVariationReasonDataTableBuilder : DataTableBuilder<ReleasedProviderChannelVariationReason>
    {
        protected override DataColumn[] GetDataColumns(ReleasedProviderChannelVariationReason dto) =>
            new[]
            {
                NewDataColumn<Guid>("ReleasedProviderChannelVariationReasonId"),
                NewDataColumn<int>("VariationReasonId"),
                NewDataColumn<Guid>("ReleasedProviderVersionChannelId")
            };

        protected override void AddDataRowToDataTable(ReleasedProviderChannelVariationReason dto)
        {
            DataTable.Rows.Add(dto.ReleasedProviderChannelVariationReasonId,
                dto.VariationReasonId,
                dto.ReleasedProviderVersionChannelId);
        }

        protected override void EnsureTableNameIsSet(ReleasedProviderChannelVariationReason dto)
            => TableName = $"[dbo].[ReleasedProviderChannelVariationReasons]";
    }
}
