using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.Core.Options;
using Serilog;

namespace CalculateFunding.Services.Core.Proxies
{
    public class SpecificationsApiProxy : ApiClientProxy, ISpecificationsApiClientProxy
    {
        public SpecificationsApiProxy(ApiOptions options, ILogger logger) : base(options, logger)
        {
        }
    }
}
