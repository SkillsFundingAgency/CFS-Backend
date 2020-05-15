using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.DataSets;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.ViewModels;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.FeatureToggles;
using CalculateFunding.Services.Scenarios.Interfaces;
using Microsoft.Azure.ServiceBus;
using Serilog;

namespace CalculateFunding.Services.Scenarios
{
    public class DatasetDefinitionFieldChangesProcessor : IDatasetDefinitionFieldChangesProcessor
    {
        private readonly IFeatureToggle _featureToggle;
        private readonly ILogger _logger;
        private readonly IDatasetsApiClient _datasetsApiClient;
        private readonly Polly.AsyncPolicy _datasetsApiClientPolicy;
        private readonly IScenariosService _scenariosService;
        private readonly IMapper _mapper;

        public DatasetDefinitionFieldChangesProcessor(
            IFeatureToggle featureToggle,
            ILogger logger,
            IDatasetsApiClient datasetsApiClient,
            IScenariosResiliencePolicies scenariosResiliencePolicies,
            IScenariosService scenariosService,
            IMapper mapper)
        {
            Guard.ArgumentNotNull(featureToggle, nameof(featureToggle));
            Guard.ArgumentNotNull(datasetsApiClient, nameof(datasetsApiClient));
            Guard.ArgumentNotNull(scenariosResiliencePolicies?.DatasetsApiClient, nameof(scenariosResiliencePolicies.DatasetsApiClient));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(scenariosService, nameof(scenariosService));
            Guard.ArgumentNotNull(mapper, nameof(mapper));

            _featureToggle = featureToggle;
            _logger = logger;
            _datasetsApiClient = datasetsApiClient;
            _datasetsApiClientPolicy = scenariosResiliencePolicies.DatasetsApiClient;
            _scenariosService = scenariosService;
            _mapper = mapper;
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

            ApiResponse<IEnumerable<string>> datasetsApiClientResponse = await _datasetsApiClientPolicy.ExecuteAsync(() => _datasetsApiClient.GetSpecificationIdsForRelationshipDefinitionId(datasetDefinitionChanges.Id));

            if (!datasetsApiClientResponse.StatusCode.IsSuccess())
            {
                string errorMessage = $"No Specification ids for relationship definition id {datasetDefinitionChanges.Id} were returned from the repository, result came back null";
                _logger.Error(errorMessage);

                throw new RetriableException(errorMessage);
            }

            IEnumerable<string> relationshipSpecificationIds = datasetsApiClientResponse.Content;

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
                ApiResponse<IEnumerable<Common.ApiClient.DataSets.Models.DatasetSpecificationRelationshipViewModel>> datasetsApiClientResponse 
                    = await _datasetsApiClientPolicy.ExecuteAsync(() => _datasetsApiClient.GetCurrentRelationshipsBySpecificationIdAndDatasetDefinitionId(specificationId, datasetDefinitionId));

                if (!datasetsApiClientResponse.StatusCode.IsSuccess() || datasetsApiClientResponse.Content.IsNullOrEmpty())
                {
                    throw new RetriableException($"No relationships found for specificationId '{specificationId}' and dataset definition id '{datasetDefinitionId}'");
                }

                IEnumerable<DatasetSpecificationRelationshipViewModel> relationships = 
                    _mapper.Map<IEnumerable<DatasetSpecificationRelationshipViewModel>>(datasetsApiClientResponse.Content);

                if (relationships.IsNullOrEmpty())
                {
                    throw new RetriableException($"No relationships found for specificationId '{specificationId}' and dataset definition id '{datasetDefinitionId}'");
                }

                await _scenariosService.ResetScenarioForFieldDefinitionChanges(relationships, specificationId, fieldChanges.Select(m => m.ExistingFieldDefinition.Name));
            }
        }
    }
}
