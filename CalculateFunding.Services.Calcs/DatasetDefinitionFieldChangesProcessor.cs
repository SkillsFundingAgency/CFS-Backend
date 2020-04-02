using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.ViewModels;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.CodeGeneration.VisualBasic;
using CalculateFunding.Services.Compiler;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.FeatureToggles;
using Microsoft.Azure.ServiceBus;
using Serilog;

namespace CalculateFunding.Services.Calcs
{
    public class DatasetDefinitionFieldChangesProcessor : IDatasetDefinitionFieldChangesProcessor
    {
        private readonly IFeatureToggle _featureToggle;
        private readonly IDatasetRepository _datasetRepository;
        private readonly Polly.AsyncPolicy _datasetRepositoryPolicy;
        private readonly Polly.AsyncPolicy _calculationsRepositoryPolicy;
        private readonly ILogger _logger;
        private readonly ICalculationService _calculationService;
        private readonly ICalculationsRepository _calculationsRepository;

        public DatasetDefinitionFieldChangesProcessor(
            IFeatureToggle featureToggle,
            IDatasetRepository datasetRepository,
            ICalcsResiliencePolicies resiliencePolicies,
            ILogger logger,
            ICalculationService calculationService,
            ICalculationsRepository calculationsRepository)
        {
            Guard.ArgumentNotNull(featureToggle, nameof(featureToggle));
            Guard.ArgumentNotNull(datasetRepository, nameof(datasetRepository));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(calculationService, nameof(calculationService));
            Guard.ArgumentNotNull(calculationsRepository, nameof(calculationsRepository));
            Guard.ArgumentNotNull(resiliencePolicies?.CalculationsRepository, nameof(resiliencePolicies.CalculationsRepository));
            Guard.ArgumentNotNull(resiliencePolicies?.DatasetsRepository, nameof(resiliencePolicies.DatasetsRepository));

            _featureToggle = featureToggle;
            _datasetRepository = datasetRepository;
            _datasetRepositoryPolicy = resiliencePolicies.DatasetsRepository;
            _logger = logger;
            _calculationService = calculationService;
            _calculationsRepository = calculationsRepository;
            _calculationsRepositoryPolicy = resiliencePolicies.CalculationsRepository;
        }

        public async Task ProcessChanges(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            if (!_featureToggle.IsProcessDatasetDefinitionFieldChangesEnabled())
            {
                return;
            }

            DatasetDefinitionChanges datasetDefinitionChanges = message.GetPayloadAsInstanceOf<DatasetDefinitionChanges>();

            _logger.Information("Checking for dataset definition changes before proceeding");

            if (datasetDefinitionChanges == null)
            {
                throw new NonRetriableException("Message does not contain a valid dataset definition change model");
            }

            if (!datasetDefinitionChanges.HasChanges)
            {
                _logger.Information($"No dataset definition field changes for definition id '{datasetDefinitionChanges.Id}'");

                return;
            }

            IEnumerable<FieldDefinitionChanges> fieldChanges = datasetDefinitionChanges.TableDefinitionChanges.SelectMany(m => m.FieldChanges);

            if (fieldChanges.IsNullOrEmpty())
            {
                _logger.Information($"No dataset definition field changes for definition id '{datasetDefinitionChanges.Id}'");

                return;
            }

            IEnumerable<string> relationshipSpecificationIds = await _datasetRepositoryPolicy.ExecuteAsync(() => _datasetRepository.GetRelationshipSpecificationIdsByDatasetDefinitionId(datasetDefinitionChanges.Id));

            if (relationshipSpecificationIds.IsNullOrEmpty())
            {
                _logger.Information($"No dataset definition specification relationships exists for definition id '{datasetDefinitionChanges.Id}'");

                return;
            }

            await ProcessFieldChanges(datasetDefinitionChanges.Id, fieldChanges, relationshipSpecificationIds);
        }

        private async Task ProcessFieldChanges(string datasetDefinitionId, IEnumerable<FieldDefinitionChanges> fieldChanges, IEnumerable<string> relationshipSpecificationIds)
        {
            Guard.IsNullOrWhiteSpace(datasetDefinitionId, nameof(datasetDefinitionId));
            Guard.ArgumentNotNull(fieldChanges, nameof(fieldChanges));
            Guard.ArgumentNotNull(relationshipSpecificationIds, nameof(relationshipSpecificationIds));

            IEnumerable<IGrouping<string, FieldDefinitionChanges>> groupedFieldChanges = fieldChanges.GroupBy(f => f.FieldDefinition.Id);

            IList<FieldDefinitionChanges> fieldDefinitionChanges = new List<FieldDefinitionChanges>();

            bool shouldResetCalculation = false;

            foreach (IGrouping<string, FieldDefinitionChanges> grouping in groupedFieldChanges)
            {
                FieldDefinitionChanges fieldDefinitionChange = grouping.FirstOrDefault(m => m.ChangeTypes.Any(
                   c => c == FieldDefinitionChangeType.FieldName) || m.RequiresRemap);

                if (fieldDefinitionChange != null)
                {
                    fieldDefinitionChanges.Add(fieldDefinitionChange);
                }

                shouldResetCalculation = true;
            }

            if (!shouldResetCalculation)
            {
                return;
            }

            foreach (string specificationId in relationshipSpecificationIds)
            {
                IEnumerable<DatasetSpecificationRelationshipViewModel> relationships = await _datasetRepositoryPolicy.ExecuteAsync(() => _datasetRepository.GetCurrentRelationshipsBySpecificationIdAndDatasetDefinitionId(specificationId, datasetDefinitionId));

                if (relationships.IsNullOrEmpty())
                {
                    throw new RetriableException($"No relationships found for specificationId '{specificationId}' and dataset definition id '{datasetDefinitionId}'");
                }

                IEnumerable<Calculation> calculations = (await _calculationsRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.GetCalculationsBySpecificationId(specificationId))).ToList();

                IEnumerable<string> aggregateParameters = calculations.SelectMany(m => SourceCodeHelpers.GetDatasetAggregateFunctionParameters(m.Current.SourceCode));

                HashSet<string> fieldNames = new HashSet<string>();

                foreach (FieldDefinitionChanges changes in fieldDefinitionChanges)
                {
                    //Check if only aggregable changes
                    if (!changes.ChangeTypes.Contains(FieldDefinitionChangeType.FieldType) && !changes.ChangeTypes.Contains(FieldDefinitionChangeType.FieldName))
                    {
                        foreach (DatasetSpecificationRelationshipViewModel datasetSpecificationRelationshipViewModel in relationships)
                        {
                            if (aggregateParameters.Contains($"Datasets.{VisualBasicTypeGenerator.GenerateIdentifier(datasetSpecificationRelationshipViewModel.Name)}.{VisualBasicTypeGenerator.GenerateIdentifier(changes.ExistingFieldDefinition.Name)}"))
                            {
                                fieldNames.Add(changes.ExistingFieldDefinition.Name);
                            }
                        }
                    }
                    else
                    {
                        fieldNames.Add(changes.ExistingFieldDefinition.Name);
                    }
                }

                if (fieldNames.Any())
                {
                    await _calculationService.ResetCalculationForFieldDefinitionChanges(relationships, specificationId, fieldNames);
                }
            }
        }
    }
}
