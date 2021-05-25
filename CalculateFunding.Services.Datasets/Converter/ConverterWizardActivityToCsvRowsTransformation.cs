using System.Buffers;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using CalculateFunding.Models.Datasets.Converter;
using CalculateFunding.Models.Datasets.ViewModels;
using CalculateFunding.Services.Datasets.Interfaces;

namespace CalculateFunding.Services.Datasets.Converter
{
    public class ConverterWizardActivityToCsvRowsTransformation : IConverterWizardActivityToCsvRowsTransformation
    {
        private const string NotPresentInLogs = "Not present in logs";
        private const string NotEligible = "Not eligible";

        private readonly ArrayPool<ExpandoObject> _expandoObjectsPool 
            = ArrayPool<ExpandoObject>.Create(ConverterWizardActivityCsvGenerationGeneratorService.BatchSize, 4);
        
        public IEnumerable<ExpandoObject> TransformConvertWizardActivityIntoCsvRows(IEnumerable<ProviderConverterDetail> eligibleConverters, IEnumerable<ConverterDataMergeLog> converterDataMergeLogs, IEnumerable<DatasetSpecificationRelationshipViewModel> datasetSpecificationRelationshipViewModels)
        {
            int resultsCount = eligibleConverters.Count();
            
            ExpandoObject[] results = _expandoObjectsPool.Rent(resultsCount);

            Dictionary<string, ConverterDataMergeLog> datasetRelationshipLogs = converterDataMergeLogs.ToDictionary(_ => _.Request.DatasetRelationshipId);

            for (int resultCount = 0; resultCount < resultsCount; resultCount++)
            {
                ProviderConverterDetail result = eligibleConverters.ElementAt(resultCount);
                IDictionary<string, object> row = results[resultCount] ?? (results[resultCount] = new ExpandoObject());

                row["Target UKPRN"] = result.TargetProviderId;
                row["Target Provider Name"] = result.TargetProviderName;
                row["Target Provider Status"] = result.TargetStatus;
                row["Target Opening Date"] = result.TargetOpeningDate;
                row["Target Provider Ineligible"] = result.ProviderInEligible;
                row["Source Provider UKPRN"] = result.PreviousProviderIdentifier;

                foreach (DatasetSpecificationRelationshipViewModel datasetSpecificationRelationshipViewModel in datasetSpecificationRelationshipViewModels)
                {
                    string outcome = NotPresentInLogs;

                    if (datasetRelationshipLogs.ContainsKey(datasetSpecificationRelationshipViewModel.Id))
                        outcome = result.IsEligible ?
                            datasetRelationshipLogs[datasetSpecificationRelationshipViewModel.Id].Results
                                .Where(_ => _.EligibleConverter.TargetProviderId == result.TargetProviderId)
                                .FirstOrDefault()?.Outcome.ToString() :
                            NotEligible;

                    row[datasetSpecificationRelationshipViewModel.Name] = outcome;
                }

                yield return (ExpandoObject) row;
            }
            
            _expandoObjectsPool.Return(results);
        }
    }
}