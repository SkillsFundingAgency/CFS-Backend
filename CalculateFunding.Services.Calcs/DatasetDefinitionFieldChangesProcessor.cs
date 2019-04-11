using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Helpers;
using Microsoft.Azure.ServiceBus;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs
{
    public class DatasetDefinitionFieldChangesProcessor : IDatasetDefinitionFieldChangesProcessor
    {
        private readonly IFeatureToggle _featureToggle;

        public DatasetDefinitionFieldChangesProcessor(IFeatureToggle featureToggle)
        {
            Guard.ArgumentNotNull(featureToggle, nameof(featureToggle));

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
