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
        public IEnumerable<dynamic> TransformProviderResultsIntoCsvRows(IEnumerable<ProviderResult> providerResults)
        {
            string[] templateCalculationNames = providerResults
                .SelectMany(_ => _.CalculationResults)
                .Where(_ => _.CalculationType == CalculationType.Template)
                .Select(_ => _.Calculation.Name)
                .Distinct()
                .ToArray();

            foreach (ProviderResult result in providerResults)
            {
                dynamic expandoObject = new ExpandoObject();
                IDictionary<string, object> row = (IDictionary<string, object>) expandoObject;

                ProviderSummary providerSummary = result.Provider;

                row["UKPRN"] = providerSummary.UKPRN;
                row["URN"] = providerSummary.URN;
                row["Estab Number"] = providerSummary.EstablishmentNumber;
                row["Provider Name"] = providerSummary.Name;
                row["LA Code"] = providerSummary.LACode;
                row["LA Name"] = providerSummary.LocalAuthorityName;
                row["Provider Type"] = providerSummary.ProviderType;
                row["Provider SubType"] = providerSummary.ProviderSubType;

                Dictionary<string, decimal?> results = result.CalculationResults
                    .Where(_ => _.CalculationType == CalculationType.Template)
                    .ToDictionary(_ => _.Calculation.Name, _ => _.Value);

                foreach (string calculationName in templateCalculationNames)
                {
                    row[calculationName] = results.TryGetValue(calculationName, out decimal? value) ? value.ToString() : null;
                }

                yield return expandoObject;
            }
        }
    }
}