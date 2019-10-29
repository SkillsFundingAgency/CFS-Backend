using System.Buffers;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Results.Interfaces;

namespace CalculateFunding.Services.Results
{
    public class ProverResultsToCsvRowsTransformation : IProverResultsToCsvRowsTransformation
    {
        private readonly ArrayPool<ExpandoObject> _expandoObjectsPool 
            = ArrayPool<ExpandoObject>.Create(ProviderResultsCsvGeneratorService.BatchSize, 4);
        
        public IEnumerable<ExpandoObject> TransformProviderResultsIntoCsvRows(IEnumerable<ProviderResult> providerResults)
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
                row["LA Name"] = providerSummary.LocalAuthorityName;
                row["Provider Type"] = providerSummary.ProviderType;
                row["Provider SubType"] = providerSummary.ProviderSubType;

                //all of the provider results inside a single specification id will share the same 
                //lists of template calculations so we don't really need to handle missing calc results
                //from provider result to provider result
                foreach (CalculationResult templateCalculationResult in result.CalculationResults.Where(_ => 
                    _.CalculationType == CalculationType.Template)
                    .OrderBy(_ => _.Calculation.Name))
                {
                    row[templateCalculationResult.Calculation.Name] = templateCalculationResult.Value?.ToString();
                }

                yield return (ExpandoObject) row;
            }
            
            _expandoObjectsPool.Return(resultsBatch);
        }
    }
}