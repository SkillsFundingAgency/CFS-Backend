using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.Core.Options;
using Serilog;

namespace CalculateFunding.Services.Core.Proxies
{
    public class ScenariosApiProxy : ApiClientProxy, IScenariosApiClientProxy
    {
        public ScenariosApiProxy(ApiOptions options, ILogger logger) : base(options, logger)
        {
        }
    }
}
