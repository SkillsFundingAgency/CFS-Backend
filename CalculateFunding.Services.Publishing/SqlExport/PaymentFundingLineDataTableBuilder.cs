using System.Collections.Generic;
using System.Data;
using System.Linq;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.SqlExport
{
    public class PaymentFundingLineDataTableBuilder : DataTableBuilder<PublishedProviderVersion>
    {
        protected override DataColumn[] GetDataColumns(PublishedProviderVersion dto)
        {
            FundingLine[] paymentFundingLines = dto.FundingLines.Where(_ => _.Type == OrganisationGroupingReason.Payment)
                .OrderBy(_ => _.TemplateLineId)
                .ToArray();

            return new[]
                {
                    NewDataColumn<string>("PublishedProviderId", 128)
                }.Concat(paymentFundingLines
                    .Select(_ => NewDataColumn<decimal>($"Fl_{_.TemplateLineId}_{_.Name.Replace(" ", "")}", allowNull: true)))
                .ToArray();
        }

        protected override void AddDataRowToDataTable(PublishedProviderVersion dto)
        {
            IEnumerable<decimal?> fundingLineValues = dto.FundingLines.Where(_ => _.Type == OrganisationGroupingReason.Payment)
                .OrderBy(_ => _.TemplateLineId)
                .Select(_ => _.Value);

            DataTable.Rows.Add(new[]
            {
                dto.PublishedProviderId
            }.Concat(fundingLineValues.Select(_ => DbNullSafe(_))).ToArray());
        }

        protected override void EnsureTableNameIsSet(PublishedProviderVersion dto)
            => TableName = $"[dbo].[{dto.FundingStreamId}_{dto.FundingPeriodId}_PaymentFundingLines]";
    }
}