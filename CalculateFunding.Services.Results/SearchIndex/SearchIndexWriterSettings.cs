using CalculateFunding.Services.Results.Interfaces;
using Microsoft.Extensions.Configuration;

namespace CalculateFunding.Services.Results.SearchIndex
{
    public class SearchIndexWriterSettings : ISearchIndexWriterSettings
    {
        private readonly IConfigurationSection _config;

        public SearchIndexWriterSettings(IConfigurationSection config)
        {
            _config = config;
        }

        public int ProviderCalculationResultsIndexWriterDegreeOfParallelism => GetValue("providerCalculationResultsIndexWriterDegreeOfParallelism");

        private int GetValue(string key)
        {
            int defaultValue = 45;

            if (_config == null)
            {
                return defaultValue;
            }

            string configValue = _config[key];

            return string.IsNullOrWhiteSpace(configValue)
                ? defaultValue
                : int.TryParse(configValue, out var result)
                    ? result
                    : defaultValue;
        }
    }
}
