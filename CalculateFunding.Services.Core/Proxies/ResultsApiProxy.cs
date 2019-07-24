using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.Core.Options;
using Serilog;

namespace CalculateFunding.Services.Core.Proxies
{
    public class ResultsApiProxy : ApiClientProxy, IResultsApiClientProxy
    {
        public ResultsApiProxy(ApiOptions options, ILogger logger) : base(options, logger)
        {
        }
    }
}
