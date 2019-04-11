using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Scenarios.Interfaces;
using Microsoft.Azure.ServiceBus;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Scenarios
{
    public class DatasetDefinitionFieldChangesProcessor : IDatasetDefinitionFieldChangesProcessor
    {
        private readonly IFeatureToggle _featureToggle;

        public DatasetDefinitionFieldChangesProcessor(IFeatureToggle featureToggle)
        {
            _featureToggle = featureToggle;
        }

        public async Task ProcessChanges(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            if (!_featureToggle.IsProcessDatasetDefinitionFieldChangesEnabled())
            {
                return;
            }

            return;
        }
    }
}
