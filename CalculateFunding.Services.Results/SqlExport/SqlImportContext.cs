using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.SqlExport;
using System.Collections.Generic;

namespace CalculateFunding.Services.Results.SqlExport
{
    public class SqlImportContext : ISqlImportContext
    {
        public ICosmosDbFeedIterator Documents { get; set; }
        public IDataTableBuilder<ProviderResult> CalculationRuns { get; set; }
        public IDataTableBuilder<ProviderResult> ProviderSummaries { get; set; }
        public IDataTableBuilder<ProviderResult> PaymentFundingLines { get; set; }
        public IDataTableBuilder<ProviderResult> InformationFundingLines { get; set; }
        public IDataTableBuilder<ProviderResult> TemplateCalculations { get; set; }
        public IDataTableBuilder<ProviderResult> AdditionalCalculations { get; set; }
        public HashSet<string> Providers { get; set; }

        public void AddRows(ProviderResult dto)
        {
            CalculationRuns.AddRows(dto);
            ProviderSummaries.AddRows(dto);
            TemplateCalculations.AddRows(dto);
            AdditionalCalculations.AddRows(dto);
            PaymentFundingLines.AddRows(dto);
            InformationFundingLines.AddRows(dto);
        }
    }
}
