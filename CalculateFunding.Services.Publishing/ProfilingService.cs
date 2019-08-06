using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Profiling;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing
{
    public class ProfilingService : IProfilingService
    {
        private readonly IProfilingApiClient _profilingApiClient;

        public ProfilingService(IProfilingApiClient profilingApiClient)
        {
            Guard.ArgumentNotNull(profilingApiClient, nameof(profilingApiClient));

            _profilingApiClient = profilingApiClient;
        }

        /// <summary>
        /// Profile funding lines
        /// </summary>
        /// <param name="fundingLineTotals">Funding lines for a specification</param>
        /// <param name="fundingStreamId">Funding Stream ID</param>
        /// <param name="fundingPeriodId">Funding Period ID</param>
        /// <returns></returns>
        public Task ProfileFundingLines(Dictionary<string, IEnumerable<FundingLine>> fundingLineTotals, string fundingStreamId, string fundingPeriodId)
        {
            // Filter only on funding lines where Type=Payment

            // Set the profiling results directly on the funding lines for each of the providers

            throw new NotImplementedException();
        }
    }
}
