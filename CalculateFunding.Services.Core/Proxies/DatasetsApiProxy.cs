using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.Core.Options;
using Serilog;

namespace CalculateFunding.Services.Core.Proxies
{
    public class DatasetsApiProxy : ApiClientProxy, IDatasetsApiClientProxy
    {
        public DatasetsApiProxy(ApiOptions options, ILogger logger) : base(options, logger)
        {

        }
    }
}
