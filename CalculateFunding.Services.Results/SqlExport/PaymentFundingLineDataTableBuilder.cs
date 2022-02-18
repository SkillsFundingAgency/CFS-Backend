using System.Collections.Generic;
using System.Data;
using System.Linq;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.SqlExport;
using FundingLine = CalculateFunding.Common.TemplateMetadata.Models.FundingLine;

namespace CalculateFunding.Services.Results.SqlExport
{
    public class PaymentFundingLineDataTableBuilder : DataTableBuilder<ProviderResult>
    {
        private readonly IEnumerable<FundingLine> _fundingLines;
        private readonly ISqlNameGenerator _sqlNameGenerator;
        private readonly string _specificationIdentifier;

        public PaymentFundingLineDataTableBuilder(
            IEnumerable<FundingLine> fundingLines,
            ISqlNameGenerator sqlNameGenerator,
            string specificationIdentifier)
        {
            Guard.ArgumentNotNull(sqlNameGenerator, nameof(sqlNameGenerator));

            _fundingLines = fundingLines;
            _sqlNameGenerator = sqlNameGenerator;
            _specificationIdentifier = specificationIdentifier;
        }

        protected override DataColumn[] GetDataColumns(ProviderResult dto)
        {
            IEnumerable<FundingLine> paymentFundingLines = _fundingLines.Where(_ => _.Type == Common.TemplateMetadata.Enums.FundingLineType.Payment);

            return new[]
            {
                NewDataColumn<string>("ProviderId", 128)
            }
            .Concat(paymentFundingLines.Select(_ => NewDataColumn<decimal>($"Fl_{_.TemplateLineId}_{_sqlNameGenerator.GenerateIdentifier(_.Name)}", allowNull: true)))
            .ToArray();
        }

        protected override void AddDataRowToDataTable(ProviderResult dto)
        {
            IEnumerable<uint> paymentFundingLineTemplateLineIds = _fundingLines.Where(_ => _.Type == Common.TemplateMetadata.Enums.FundingLineType.Payment).Select(_ => _.TemplateLineId);

            IEnumerable<decimal?> fundingLineValues = dto.FundingLineResults.Where(_ => paymentFundingLineTemplateLineIds.Contains(uint.Parse(_.FundingLine.Id)))
                .OrderBy(_ => uint.Parse(_.FundingLine.Id))
                .Select(_ => _.Value);

            DataTable.Rows.Add(
                new[]
                {
                    dto.Provider.Id
                }
                .Concat(fundingLineValues.Select(_ => DbNullSafe(_)))
                .ToArray());
        }

        protected override void EnsureTableNameIsSet(ProviderResult dto)
            => TableName = $"[dbo].[{_specificationIdentifier}_PaymentFundingLines]";
    }
}