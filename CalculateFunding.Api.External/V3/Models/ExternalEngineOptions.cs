using CalculateFunding.Common.Utility;
using Microsoft.Extensions.Configuration;
using System.Runtime.CompilerServices;

namespace CalculateFunding.Api.External.V3.Models
{
    public class ExternalEngineOptions : IExternalEngineOptions
    {
        private const int DefaultInt = 15;

        private readonly IConfiguration _configuration;

        public ExternalEngineOptions(IConfiguration configuration)
        {
            Guard.ArgumentNotNull(configuration, nameof(configuration));

            _configuration = configuration;
        }

        public int BlobLookupConcurrencyCount => GetExternalEngineOptionsConfigurationInteger(overrideDefaultValue: 50);

        private int GetExternalEngineOptionsConfigurationInteger(
            [CallerMemberName] string key = null,
            int? overrideDefaultValue = null) => int.TryParse(_configuration[$"externalEngineOptions:{key}"],
            out int intValue)
            ? intValue
            : overrideDefaultValue ?? DefaultInt;
    }
}
