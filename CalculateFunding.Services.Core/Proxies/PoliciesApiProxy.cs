using System;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.Core.Options;
using Serilog;

namespace CalculateFunding.Services.Core.Proxies
{
    [Obsolete]
    public class PoliciesApiProxy : ApiClientProxy, IPoliciesApiClientProxy
    {
        public PoliciesApiProxy(ApiOptions options, ILogger logger) : base(options, logger)
        {
        }
    }
}
