using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Datasets.Interfaces;
using Microsoft.Azure.ServiceBus;
using Serilog;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Services.Core.FeatureToggles;
using CalculateFunding.Services.Core.Services;

namespace CalculateFunding.Services.Datasets
{
    public class DatasetDefinitionNameChangeProcessor : ProcessingService, IDatasetDefinitionNameChangeProcessor
    {
        private readonly IDefinitionSpecificationRelationshipService _definitionSpecificationRelationshipService;
        private readonly IDatasetService _datasetService;
        private readonly ILogger _logger;
        private readonly IFeatureToggle _featureToggle;

        public DatasetDefinitionNameChangeProcessor(
            IDefinitionSpecificationRelationshipService definitionSpecificationRelationshipService,
            IDatasetService datasetService,
            ILogger logger,
            IFeatureToggle featureToggle)
        {
            Guard.ArgumentNotNull(definitionSpecificationRelationshipService, nameof(definitionSpecificationRelationshipService));
            Guard.ArgumentNotNull(datasetService, nameof(datasetService));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(featureToggle, nameof(featureToggle));

            _definitionSpecificationRelationshipService = definitionSpecificationRelationshipService;
            _datasetService = datasetService;
            _logger = logger;
            _featureToggle = featureToggle;
        }

        public override async Task Process(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            if (!_featureToggle.IsProcessDatasetDefinitionNameChangesEnabled())
            {
                return;
            }

            DatasetDefinitionChanges datasetDefinitionChanges = message.GetPayloadAsInstanceOf<DatasetDefinitionChanges>();

            _logger.Information("Checking for changes before proceeding");

            if(datasetDefinitionChanges == null)
            {
                throw new NonRetriableException("Message does not contain a valid dataset definition change model");
            }

            if (datasetDefinitionChanges.DefinitionChanges.IsNullOrEmpty())
            {
                _logger.Information($"No dataset definition name change for definition id '{datasetDefinitionChanges.Id}'");

                return;
            }

            Reference reference = new Reference(datasetDefinitionChanges.Id, datasetDefinitionChanges.NewName);

            _logger.Information($"Updating relationships for updated definition name with definition id '{datasetDefinitionChanges.Id}'");

            await _definitionSpecificationRelationshipService.UpdateRelationshipDatasetDefinitionName(reference);

            _logger.Information($"Updating datasets for updated definition name with definition id '{datasetDefinitionChanges.Id}'");

            await _datasetService.UpdateDatasetAndVersionDefinitionName(reference);
        }
    }
}
