using System;
using System.Collections.Generic;
using System.Text;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.Core.Options;
using Serilog;

namespace CalculateFunding.Services.Core.Proxies
{
    public class ScenariosApiProxy : ApiClientProxy, IScenariosApiClientProxy
    {
        public ScenariosApiProxy(ApiOptions options, ILogger logger, ICorrelationIdProvider correlationIdProvider) : base(options, logger, correlationIdProvider)
        {
        }
    }
}
