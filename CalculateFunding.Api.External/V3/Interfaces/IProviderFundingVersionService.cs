using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Api.External.V3.Interfaces
{
    public interface IProviderFundingVersionService
    {
        Task<IActionResult> GetFunding(string providerFundingVersion);
    }
}
