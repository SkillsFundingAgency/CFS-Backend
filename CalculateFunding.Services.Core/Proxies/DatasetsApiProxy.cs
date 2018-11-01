using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.Core.Options;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Core.Proxies
{
    public class DatasetsApiProxy : ApiClientProxy, IDatasetsApiClientProxy
    {
        public DatasetsApiProxy(ApiOptions options, ILogger logger, ICorrelationIdProvider correlationIdProvider) : base(options, logger, correlationIdProvider)
        {

        }
    }
}
