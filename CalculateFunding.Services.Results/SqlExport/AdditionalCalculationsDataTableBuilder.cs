using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.SqlExport;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using CalcsApiCalculation = CalculateFunding.Common.ApiClient.Calcs.Models.Calculation;
using CalcsApiCalculationValueType = CalculateFunding.Common.ApiClient.Calcs.Models.CalculationValueType;
using CalcsApiCalculationType = CalculateFunding.Common.ApiClient.Calcs.Models.CalculationType;

namespace CalculateFunding.Services.Results.SqlExport
{
    public class AdditionalCalculationsDataTableBuilder : DataTableBuilder<ProviderResult>
    {
        private readonly IEnumerable<CalcsApiCalculation> _calculations;
        private readonly ISqlNameGenerator _sqlNameGenerator;
        private readonly string _specificationIdentifier;

        public AdditionalCalculationsDataTableBuilder(
            IEnumerable<CalcsApiCalculation> calculations,
            ISqlNameGenerator sqlNameGenerator,
            string specificationIdentifier)
        {
            Guard.ArgumentNotNull(sqlNameGenerator, nameof(sqlNameGenerator));

            _calculations = calculations;
            _sqlNameGenerator = sqlNameGenerator;
            _specificationIdentifier = specificationIdentifier;
        }

        protected override DataColumn[] GetDataColumns(ProviderResult dto)
        {
            IEnumerable<CalcsApiCalculation> templateCalculations = _calculations.Where(_ => _.CalculationType == CalcsApiCalculationType.Additional);

            CalculationResult[] templateCalculationResults = dto.CalculationResults.Where(_ => templateCalculations.Select(_ => _.Id).Contains(_.Calculation.Id))
                .OrderBy(_ => _.Calculation.Id)
                .ToArray();

            return new[]
                {
                    NewDataColumn<string>("ProviderId", 128)
                }.Concat(templateCalculationResults
                    .Select(_ => NewDataColumn(_, templateCalculations.SingleOrDefault(t => t.Id == _.Calculation.Id))))
                .ToArray();
        }

        protected override void AddDataRowToDataTable(ProviderResult dto)
        {
            IEnumerable<CalcsApiCalculation> templateCalculations = _calculations.Where(_ => _.CalculationType == CalcsApiCalculationType.Additional);

            IEnumerable<object> templateCalculationValues = dto.CalculationResults.Where(_ => templateCalculations.Select(_ => _.Id).Contains(_.Calculation.Id))
                .OrderBy(_ => _.Calculation.Id)
                .Select(_ => _.Value);

            DataTable.Rows.Add(new[]
            {
                dto.Provider.Id
            }.Concat(templateCalculationValues.Select(_ => DbNullSafe(_))).ToArray());
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
            => $"Calc_{_sqlNameGenerator.GenerateIdentifier(calculationResult.Calculation.Name)}";

        protected override void EnsureTableNameIsSet(ProviderResult dto)
            =>  TableName = _calculations.Any(_ => _.CalculationType == CalcsApiCalculationType.Additional) ? $"[dbo].[{_specificationIdentifier}_AdditionalCalculations]" : string.Empty;
    }
}
