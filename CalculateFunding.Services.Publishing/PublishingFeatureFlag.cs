using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.FeatureManagement;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing
{
    public class PublishingFeatureFlag : IPublishingFeatureFlag
    {
        private readonly IFeatureManagerSnapshot _featureManager;

        public PublishingFeatureFlag(IFeatureManagerSnapshot featureManager)
        {
            _featureManager = featureManager;
        }

        public async Task<bool> IsVariationsEnabled()
        {
            return await _featureManager.IsEnabledAsync("EnableVariations");
        }
    }
}
