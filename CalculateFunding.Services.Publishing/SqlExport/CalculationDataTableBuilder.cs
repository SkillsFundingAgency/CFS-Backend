using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using CalculateFunding.Common.TemplateMetadata.Enums;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.SqlExport;

namespace CalculateFunding.Services.Publishing.SqlExport
{
    public class CalculationDataTableBuilder : DataTableBuilder<PublishedProviderVersion>
    {
        private readonly IDictionary<uint, Calculation> _calculations;

        public CalculationDataTableBuilder(IEnumerable<Calculation> calculations)
        {
            _calculations = calculations.ToDictionary(_ => _.TemplateCalculationId);
        }

        protected override DataColumn[] GetDataColumns(PublishedProviderVersion dto)
        {
            Calculation[] calculations = _calculations.Values
                .OrderBy(_ => _.TemplateCalculationId)
                .ToArray();

            return new[]
                {
                    NewDataColumn<string>("PublishedProviderId", 128)
                }.Concat(calculations
                    .Select(NewCalculationDataColumn))
                .ToArray();
        }

        private DataColumn NewCalculationDataColumn(Calculation templateCalculation)
        {
            string columnName = $"Calc_{templateCalculation.TemplateCalculationId}_{templateCalculation.Name.Replace(" ", "")}";

            return templateCalculation.ValueFormat switch
            {
                var format when format == CalculationValueFormat.Currency ||
                                format == CalculationValueFormat.Number ||
                                format == CalculationValueFormat.Percentage
                    => NewDataColumn<decimal>(columnName, allowNull: true),
                CalculationValueFormat.Boolean => NewDataColumn<bool>(columnName, allowNull: true),
                CalculationValueFormat.String => NewDataColumn<string>(columnName, 128, true),
                _ => throw new InvalidOperationException("Unknown value format")
            };
        }

        private Calculation GetTemplateCalculation(uint templateCalculationId)
            => _calculations.TryGetValue(templateCalculationId, out Calculation calculation) ? calculation : throw new ArgumentOutOfRangeException(nameof(templateCalculationId));

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