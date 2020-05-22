﻿using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Api.External.V3.Interfaces
{
    public interface IFundingStreamService
    {
        Task<IActionResult> GetFundingStreams();
        Task<IActionResult> GetFundingPeriods(string fundingStreamId);

    }
}
