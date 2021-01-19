using System.Runtime.CompilerServices;
using CalculateFunding.Common.Utility;
using Microsoft.Extensions.Configuration;

namespace CalculateFunding.Services.Publishing
{
    public class BatchProfilingOptions : IBatchProfilingOptions
    {
        private readonly IConfiguration _configuration;

        public BatchProfilingOptions(IConfiguration configuration)
        {
            Guard.ArgumentNotNull(configuration, nameof(configuration));
            
            _configuration = configuration;
        }

        public int BatchSize => GetConfiguredIntegerValue(50);

        public int ConsumerCount => GetConfiguredIntegerValue(10);
        
        private int GetConfiguredIntegerValue(int defaultIfMissing,
            [CallerMemberName] string key = null) 
            => int.TryParse(_configuration[$"batchProfilingOptions:{key}"],
                out int intValue)
                ? intValue
                : defaultIfMissing;
    }
}