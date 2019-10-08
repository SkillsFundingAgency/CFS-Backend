using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.ViewModels;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Scenarios.Interfaces;
using Microsoft.Azure.ServiceBus;
using Serilog;

namespace CalculateFunding.Services.Scenarios
{
    public class DatasetDefinitionFieldChangesProcessor : IDatasetDefinitionFieldChangesProcessor
    {
        private readonly IFeatureToggle _featureToggle;
        private readonly ILogger _logger;
        private readonly IDatasetRepository _datasetRepository;
        private readonly Polly.Policy _datasetRepositoryPolicy;
        private readonly IScenariosService _scenariosService;

        public DatasetDefinitionFieldChangesProcessor(
            IFeatureToggle featureToggle,
            ILogger logger,
            IDatasetRepository datasetRepository,
            IScenariosResiliencePolicies scenariosResiliencePolicies,
            IScenariosService scenariosService)
        {
            Guard.ArgumentNotNull(featureToggle, nameof(featureToggle));
            Guard.ArgumentNotNull(datasetRepository, nameof(datasetRepository));
            Guard.ArgumentNotNull(scenariosResiliencePolicies?.DatasetRepository, nameof(scenariosResiliencePolicies.DatasetRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(scenariosService, nameof(scenariosService));

            _featureToggle = featureToggle;
            _logger = logger;
            _datasetRepository = datasetRepository;
            _datasetRepositoryPolicy = scenariosResiliencePolicies.DatasetRepository;
            _scenariosService = scenariosService;
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

                await _scenariosService.ResetScenarioForFieldDefinitionChanges(relationships, specificationId, fieldChanges.Select(m => m.ExistingFieldDefinition.Name));
            }
        }
    }
}
