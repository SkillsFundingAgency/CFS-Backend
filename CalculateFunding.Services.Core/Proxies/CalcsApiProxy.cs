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
    public class CalcsApiProxy : ApiClientProxy, ICalcsApiClientProxy
    {
        public CalcsApiProxy(ApiOptions options, ILogger logger, ICorrelationIdProvider correlationIdProvider, IUserProfileProvider userProfileProvider) : base(options, logger, correlationIdProvider, userProfileProvider)
        {
        }
    }
}
