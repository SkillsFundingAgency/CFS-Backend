using System.Collections.Generic;
using System.Data;
using System.Linq;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.SqlExport
{
    public class CalculationDataTableBuilder : DataTableBuilder<PublishedProviderVersion>
    {
        private readonly IDictionary<uint, string> _calculationNames;

        public CalculationDataTableBuilder(IDictionary<uint, string> calculationNames)
        {
            _calculationNames = calculationNames;
        }

        protected override DataColumn[] GetDataColumns(PublishedProviderVersion dto)
        {
            FundingCalculation[] calculations = dto.Calculations
                .OrderBy(_ => _.TemplateCalculationId)
                .ToArray();

            return new[]
                {
                    NewDataColumn<string>("PublishedProviderId", 128)
                }.Concat(calculations
                    .Select(_ => NewDataColumn<decimal>($"Calc_{_.TemplateCalculationId}_{_calculationNames[_.TemplateCalculationId].Replace(" ", "")}", allowNull: true)))
                .ToArray();
        }

        protected override void AddDataRowToDataTable(PublishedProviderVersion dto)
        {
            IEnumerable<object> calculationValues = dto.Calculations
                .OrderBy(_ => _.TemplateCalculationId)
                .Select(_ => _.Value);

            DataTable.Rows.Add(new[]
            {
                dto.PublishedProviderId
            }.Concat(calculationValues.Select(DbNullSafe)).ToArray());
        }

        protected override void EnsureTableNameIsSet(PublishedProviderVersion dto)
            => TableName = $"[dbo].[{dto.FundingStreamId}_{dto.FundingPeriodId}_Calculations]";
    }
}