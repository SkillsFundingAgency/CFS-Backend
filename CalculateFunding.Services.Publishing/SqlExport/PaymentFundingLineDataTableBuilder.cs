using System.Collections.Generic;
using System.Data;
using System.Linq;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.SqlExport;
using CommonModels = CalculateFunding.Common.TemplateMetadata.Models;

namespace CalculateFunding.Services.Publishing.SqlExport
{
    public class PaymentFundingLineDataTableBuilder : DataTableBuilder<PublishedProviderVersion>
    {
        private readonly IDictionary<uint, CommonModels.FundingLine> _paymentFundingLines;

        public PaymentFundingLineDataTableBuilder(IEnumerable<CommonModels.FundingLine> paymentFundingLines)
        {
            _paymentFundingLines = paymentFundingLines.ToDictionary(_ => _.TemplateLineId);
        }

        protected override DataColumn[] GetDataColumns(PublishedProviderVersion dto)
        {
            CommonModels.FundingLine[] paymentFundingLines = _paymentFundingLines.Values
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
            IEnumerable<decimal?> paymentFundingLineValues = _paymentFundingLines.Values
                .Select<CommonModels.FundingLine, (uint TemplateLineId, FundingLine FundingLineValue)>(_ =>
                (_.TemplateLineId, dto.FundingLines.SingleOrDefault(flv => flv.TemplateLineId == _.TemplateLineId && flv.Type == FundingLineType.Payment)))
                .OrderBy(_ => _.TemplateLineId)
                .Select(_ => _.FundingLineValue?.Value);

            DataTable.Rows.Add(new[]
            {
                dto.PublishedProviderId
            }.Concat(paymentFundingLineValues.Select(_ => DbNullSafe(_))).ToArray());
        }

        protected override void EnsureTableNameIsSet(PublishedProviderVersion dto)
            => TableName = $"[dbo].[{dto.FundingStreamId}_{dto.FundingPeriodId}_PaymentFundingLines]";
    }
}