using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.Core.Options;
using Serilog;

namespace CalculateFunding.Services.Core.Proxies
{
    public class PoliciesApiProxy : ApiClientProxy, IPoliciesApiClientProxy
    {
        public PoliciesApiProxy(ApiOptions options, ILogger logger, ICorrelationIdProvider correlationIdProvider) : base(options, logger, correlationIdProvider)
        {
        }
    }
}
