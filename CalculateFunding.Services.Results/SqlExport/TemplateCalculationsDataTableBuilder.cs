using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.SqlExport;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using CalcsApiCalculation = CalculateFunding.Common.ApiClient.Calcs.Models.Calculation;
using TemplateCalculationType = CalculateFunding.Common.TemplateMetadata.Enums.CalculationType;
using TemplateMetadataCalculation = CalculateFunding.Common.TemplateMetadata.Models.Calculation;

namespace CalculateFunding.Services.Results.SqlExport
{
    public class TemplateCalculationsDataTableBuilder : DataTableBuilder<ProviderResult>
    {
        private readonly IEnumerable<CalcsApiCalculation> _calculations;
        private readonly ISqlNameGenerator _sqlNameGenerator;
        private readonly IEnumerable<TemplateMetadataCalculation> _templateMetadataCalculation;
        private readonly string _specificationIdentifier;

        public TemplateCalculationsDataTableBuilder(
            IEnumerable<CalcsApiCalculation> calculations,
            ISqlNameGenerator sqlNameGenerator,
            IEnumerable<TemplateMetadataCalculation> templateMetadataCalculations,
            string specificationIdentifier)
        {
            Guard.ArgumentNotNull(sqlNameGenerator, nameof(sqlNameGenerator));

            _calculations = calculations;
            _sqlNameGenerator = sqlNameGenerator;
            _templateMetadataCalculation = templateMetadataCalculations;
            _specificationIdentifier = specificationIdentifier;
        }

        protected override DataColumn[] GetDataColumns(ProviderResult dto)
        {
            return new[]
            {
                NewDataColumn<string>("ProviderId", 128)
            }
            .Concat(_templateMetadataCalculation.OrderBy(_ => _.TemplateCalculationId).Select(_ => 
                NewDataColumn(_)))
            .ToArray();
        }

        protected override void AddDataRowToDataTable(ProviderResult dto)
        {
            IEnumerable<CalcsApiCalculation> templateCalculations = _calculations.Where(_ => _.CalculationType == Common.ApiClient.Calcs.Models.CalculationType.Template);

            IEnumerable<object> templateCalculationValues = _templateMetadataCalculation
                .Select<TemplateMetadataCalculation, (uint TemplateCalculationId, CalculationResult CalculationResult)>(_ => 
                (_.TemplateCalculationId, dto.CalculationResults.SingleOrDefault(cr => cr.Calculation.Name == _.Name)))
                .OrderBy(_ => _.TemplateCalculationId)
                .Select(_ => _.CalculationResult?.Value);

            DataTable.Rows.Add(
                new[]
                {
                    dto.Provider.Id
                }
                .Concat(templateCalculationValues.Select(_ => DbNullSafe(_)))
                .ToArray());
        }

        private DataColumn NewDataColumn(
            TemplateMetadataCalculation templateCalculation)
            => templateCalculation.Type switch
            {
                var format when format == TemplateCalculationType.Cash ||
                                format == TemplateCalculationType.Number ||
                                format == TemplateCalculationType.Adjustment ||
                                format == TemplateCalculationType.LumpSum ||
                                format == TemplateCalculationType.PerPupilFunding ||
                                format == TemplateCalculationType.ProviderLedFunding ||
                                format == TemplateCalculationType.PupilNumber ||
                                format == TemplateCalculationType.Rate ||
                                format == TemplateCalculationType.Weighting
                => NewDataColumn<decimal>(GetColumnName(templateCalculation, templateCalculation.TemplateCalculationId), allowNull: true),
                TemplateCalculationType.Boolean => NewDataColumn<bool>(GetColumnName(templateCalculation, templateCalculation.TemplateCalculationId), allowNull: true),
                _ => NewDataColumn<string>(GetColumnName(templateCalculation, templateCalculation.TemplateCalculationId), allowNull: true),
            };

        private string GetColumnName(
            TemplateMetadataCalculation templateCalculation,
            uint templateCalculationId)
            => $"Calc_{templateCalculationId}_{_sqlNameGenerator.GenerateIdentifier(templateCalculation.Name)}";

        protected override void EnsureTableNameIsSet(ProviderResult dto)
            => TableName = $"[dbo].[{_specificationIdentifier}_TemplateCalculations]";
    }
}
