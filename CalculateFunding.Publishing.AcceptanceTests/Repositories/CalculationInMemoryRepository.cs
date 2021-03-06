﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Publishing.AcceptanceTests.Repositories
{
    public class CalculationInMemoryRepository : ICalculationResultsRepository
    {
        public IDictionary<string, IEnumerable<CalculationResult>> ProviderResults { get; } =
            new Dictionary<string, IEnumerable<CalculationResult>>();

        public IEnumerable<CalculationResult> Results { get; private set; } = new CalculationResult[0];

        public Task<IEnumerable<ProviderCalculationResult>> GetCalculationResultsBySpecificationAndProvider(string specificationId, string providerId)
        {
            return Task.FromResult(
                new List<ProviderCalculationResult> 
                {
                    new ProviderCalculationResult 
                    { 
                        ProviderId = providerId,  
                        Results = ProviderResults.ContainsKey(providerId) ? ProviderResults[providerId] : Results 
                    }
                }.AsEnumerable());
        }

        public void SetCalculationResults(IEnumerable<CalculationResult> calculationResults)
        {
            Results = calculationResults.ToArray();
        }

        public void AddCalculationResults(IEnumerable<CalculationResult> calculationResults)
        {
            Results = Results.Concat(calculationResults);
        }

        public void AddProviderResults(string providerId, IEnumerable<CalculationResult> providerResults)
        {
            ProviderResults[providerId] = providerResults;
        }
    }
}
