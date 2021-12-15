using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.SqlExport;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using CalcsApiCalculation = CalculateFunding.Common.ApiClient.Calcs.Models.Calculation;
using CalcsApiCalculationValueType = CalculateFunding.Common.ApiClient.Calcs.Models.CalculationValueType;

namespace CalculateFunding.Services.Results.SqlExport
{
    public class TemplateCalculationsDataTableBuilder : DataTableBuilder<ProviderResult>
    {
        private readonly IEnumerable<CalcsApiCalculation> _calculations;
        private readonly ISqlNameGenerator _sqlNameGenerator;

        public TemplateCalculationsDataTableBuilder(
            IEnumerable<CalcsApiCalculation> calculations,
            ISqlNameGenerator sqlNameGenerator)
        {
            Guard.ArgumentNotNull(sqlNameGenerator, nameof(sqlNameGenerator));

            _calculations = calculations;
            _sqlNameGenerator = sqlNameGenerator;
        }

        protected override DataColumn[] GetDataColumns(ProviderResult dto)
        {
            IEnumerable<CalcsApiCalculation> templateCalculations = _calculations.Where(_ => _.CalculationType == Common.ApiClient.Calcs.Models.CalculationType.Template);

            CalculationResult[] templateCalculationResults = dto.CalculationResults.Where(_ => templateCalculations.Select(_ => _.Id).Contains(_.Calculation.Id))
                .OrderBy(_ => _.Calculation.Id)
                .ToArray();

            return new[]
            {
                NewDataColumn<string>("ProviderId", 128)
            }
            .Concat(templateCalculationResults.Select(_ => NewDataColumn(_, templateCalculations.SingleOrDefault(t => t.Id == _.Calculation.Id))))
            .ToArray();
        }

        protected override void AddDataRowToDataTable(ProviderResult dto)
        {
            IEnumerable<CalcsApiCalculation> templateCalculations = _calculations.Where(_ => _.CalculationType == Common.ApiClient.Calcs.Models.CalculationType.Template);

            IEnumerable<object> templateCalculationValues = dto.CalculationResults.Where(_ => templateCalculations.Select(_ => _.Id).Contains(_.Calculation.Id))
                .OrderBy(_ => _.Calculation.Id)
                .Select(_ => _.Value);

            DataTable.Rows.Add(
                new[]
                {
                    dto.Provider.Id
                }
                .Concat(templateCalculationValues.Select(_ => DbNullSafe(_)))
                .ToArray());
        }

        private DataColumn NewDataColumn(
            CalculationResult calculationResult,
            CalcsApiCalculation calcsApiCalculation)
            => calcsApiCalculation.ValueType switch
            {
                var format when format == CalcsApiCalculationValueType.Currency ||
                                format == CalcsApiCalculationValueType.Number ||
                                format == CalcsApiCalculationValueType.Percentage
                => NewDataColumn<decimal>(GetColumnName(calculationResult), allowNull: true),
                CalcsApiCalculationValueType.Boolean => NewDataColumn<bool>(GetColumnName(calculationResult), allowNull: true),
                CalcsApiCalculationValueType.String => NewDataColumn<string>(GetColumnName(calculationResult), allowNull: true),
                _ => throw new InvalidOperationException("Unknown value type")
            };

        private string GetColumnName(CalculationResult calculationResult)
            => $"Calc_{calculationResult.Calculation.Id}_{_sqlNameGenerator.GenerateIdentifier(calculationResult.Calculation.Name)}";

        protected override void EnsureTableNameIsSet(ProviderResult dto)
            => TableName = $"[dbo].[{dto.SpecificationId}_TemplateCalculations]";
    }
}
