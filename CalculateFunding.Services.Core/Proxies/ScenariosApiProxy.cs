using System;
using System.Collections.Generic;
using System.Text;
using CalculateFunding.Models.Users;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.Core.Options;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace CalculateFunding.Services.Core.Proxies
{
    public class ScenariosApiProxy : ApiClientProxy, IScenariosApiClientProxy
    {
        public ScenariosApiProxy(ApiOptions options, ILogger logger, ICorrelationIdProvider correlationIdProvider, UserProfile userProfile) : base(options, logger, correlationIdProvider, userProfile)
        {
        }
    }
}
