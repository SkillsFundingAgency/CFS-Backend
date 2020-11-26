using System.Buffers;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using CalculateFunding.Models.Calcs;
using TemplateMappingItem = CalculateFunding.Common.ApiClient.Calcs.Models.TemplateMappingItem;
using CalculateFunding.Models.ProviderLegacy;
using CalculateFunding.Services.Results.Interfaces;

namespace CalculateFunding.Services.Results
{
    public class ProviderResultsToCsvRowsTransformation : IProviderResultsToCsvRowsTransformation
    {
        private readonly ArrayPool<ExpandoObject> _expandoObjectsPool 
            = ArrayPool<ExpandoObject>.Create(ProviderResultsCsvGeneratorService.BatchSize, 4);
        
        public IEnumerable<ExpandoObject> TransformProviderResultsIntoCsvRows(IEnumerable<ProviderResult> providerResults, IDictionary<string, TemplateMappingItem> allTemplateMappings)
        {
            int resultsCount = providerResults.Count();
            
            ExpandoObject[] resultsBatch = _expandoObjectsPool.Rent(resultsCount);

            for (int resultCount = 0; resultCount < resultsCount; resultCount++)
            {
                ProviderResult result = providerResults.ElementAt(resultCount);
                IDictionary<string, object> row = resultsBatch[resultCount] ?? (resultsBatch[resultCount] = new ExpandoObject());

                ProviderSummary providerSummary = result.Provider;

                row["UKPRN"] = providerSummary.UKPRN;
                row["URN"] = providerSummary.URN;
                row["Estab Number"] = providerSummary.EstablishmentNumber;
                row["Provider Name"] = providerSummary.Name;
                row["LA Code"] = providerSummary.LACode;
                row["LA Name"] = providerSummary.Authority;
                row["Provider Type"] = providerSummary.ProviderType;
                row["Provider SubType"] = providerSummary.ProviderSubType;

                if (result.FundingLineResults != null)
                {
                    foreach (FundingLineResult fundingLineResult in result.FundingLineResults)
                    {
                        row[$"FUN: {fundingLineResult.FundingLine.Name} ({fundingLineResult.FundingLine.Id})"] = fundingLineResult.Value?.ToString();
                    }
                }

                //all of the provider results inside a single specification id will share the same 
                //lists of template calculations so we don't really need to handle missing calc results
                //from provider result to provider result
                foreach (CalculationResult templateCalculationResult in result.CalculationResults.Where(_ => 
                    _.Calculation != null && _.CalculationType == CalculationType.Template)
                    .OrderBy(_ => _.Calculation.Name))
                {
                    row[$"CAL: {templateCalculationResult.Calculation.Name} ({allTemplateMappings[templateCalculationResult.Calculation.Id].TemplateId})"] = templateCalculationResult.Value?.ToString();
                }

                foreach (CalculationResult templateCalculationResult in result.CalculationResults.Where(_ =>
                    _.Calculation != null && _.CalculationType == CalculationType.Additional)
                    .OrderBy(_ => _.Calculation.Name))
                {
                    row[$"ADD: {templateCalculationResult.Calculation.Name}"] = templateCalculationResult.Value?.ToString();
                }


                yield return (ExpandoObject) row;
            }
            
            _expandoObjectsPool.Return(resultsBatch);
        }
    }
}