using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Services;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Publishing.AcceptanceTests.Repositories
{
    public class CalculationInMemoryRepository : ICalculationResultsRepository
    {
        private readonly IEnumerable<CalculationResult> _results;

        public CalculationInMemoryRepository(IEnumerable<CalculationResult> results)
        {
            _results = results;
        }

        public Task<ProviderCalculationResult> GetCalculationResultsBySpecificationAndProvider(string specificationId, string providerId)
        {
            return Task.FromResult( new ProviderCalculationResult { ProviderId = providerId, Results = _results });
        }
    }
}
