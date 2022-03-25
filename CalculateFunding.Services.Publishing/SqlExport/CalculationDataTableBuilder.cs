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

            return templateCalculation.Type switch
            {
                var format when format == CalculationType.Cash ||
                                format == CalculationType.Number ||
                                format == CalculationType.Adjustment ||
                                format == CalculationType.LumpSum ||
                                format == CalculationType.PerPupilFunding ||
                                format == CalculationType.ProviderLedFunding ||
                                format == CalculationType.PupilNumber ||
                                format == CalculationType.Rate ||
                                format == CalculationType.Weighting
                => NewDataColumn<decimal>(columnName, allowNull: true),
                CalculationType.Boolean => NewDataColumn<bool>(columnName, allowNull: true),
                _ => NewDataColumn<string>(columnName, allowNull: true),
            };
        }

        protected override void AddDataRowToDataTable(PublishedProviderVersion dto)
        {
            IEnumerable<object> calculationValues = _calculations.Values
                .Select<Calculation, (uint TemplateCalculationId, FundingCalculation CalculationResult)>(_ =>
                (_.TemplateCalculationId, dto.Calculations.SingleOrDefault(cr => cr.TemplateCalculationId == _.TemplateCalculationId)))
                .OrderBy(_ => _.TemplateCalculationId)
                .Select(_ => _.CalculationResult?.Value);

            DataTable.Rows.Add(new[]
            {
                dto.PublishedProviderId
            }.Concat(calculationValues.Select(DbNullSafe)).ToArray());
        }

        protected override void EnsureTableNameIsSet(PublishedProviderVersion dto)
            => TableName = $"[dbo].[{dto.FundingStreamId}_{dto.FundingPeriodId}_Calculations]";
    }
}