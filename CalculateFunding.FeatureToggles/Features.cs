using System;
using Microsoft.Extensions.Configuration;

namespace CalculateFunding.FeatureToggles
{
    public class Features : IFeatureToggle
    {
        private readonly IConfigurationSection _config;

        public Features(IConfigurationSection config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public bool IsAllocationLineMajorMinorVersioningEnabled()
        {
            return CheckSetting("allocationLineMajorMinorVersioningEnabled");
        }

        public bool IsProviderProfilingServiceEnabled()
        {
            return CheckSetting("providerProfilingServiceEnabled");
        }

        public bool IsAggregateSupportInCalculationsEnabled()
        {
            return CheckSetting("aggregateSupportInCalculationsEnabled");
        }

        private bool CheckSetting(string featureName)
        {
            string value = _config[featureName];

            if (string.IsNullOrEmpty(value))
            {
                return false;
            }
            else
            {
                if (bool.TryParse(value, out var result))
                {
                    return result;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
