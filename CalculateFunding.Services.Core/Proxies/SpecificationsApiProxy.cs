using System;
using System.Collections.Generic;
using System.Text;
using CalculateFunding.Models.Users;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Core.Options;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace CalculateFunding.Services.Core.Proxies
{
    public class SpecificationsApiProxy : ApiClientProxy, ISpecificationsApiClientProxy
    {
        public SpecificationsApiProxy(ApiOptions options, ILogger logger, ICorrelationIdProvider correlationIdProvider) : base(options, logger, correlationIdProvider)
        {
        }
    }
}
