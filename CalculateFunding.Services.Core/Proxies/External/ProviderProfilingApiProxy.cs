using CalculateFunding.Services.Core.Interfaces.Proxies.External;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Core.Options;
using Serilog;

namespace CalculateFunding.Services.Core.Proxies.External
{
    public class ProviderProfilingApiProxy : ApiClientProxy, IProviderProfilingApiProxy
    {
        public ProviderProfilingApiProxy(ExternalApiOptions options, IBearerTokenProvider bearerTokenProvider, ILogger logger) : base (options, bearerTokenProvider, logger)
        { }
    }
}
