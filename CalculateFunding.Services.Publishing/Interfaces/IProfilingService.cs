﻿using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IProfilingService
    {
        Task ProfileFundingLines(Dictionary<string, IEnumerable<FundingLine>> fundingLineTotals, string fundingStreamId, string fundingPeriodId);
    }
}