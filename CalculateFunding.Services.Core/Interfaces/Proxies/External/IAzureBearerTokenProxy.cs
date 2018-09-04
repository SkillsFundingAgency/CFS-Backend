using CalculateFunding.Models;
using CalculateFunding.Services.Core.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Core.Interfaces.Proxies.External
{
    public interface IAzureBearerTokenProxy
    {
        Task<AzureBearerToken> FetchToken(AzureBearerTokenOptions azureBearerTokenOptions);
    }
}
