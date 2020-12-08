using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing
{
    public class CalculationResultsService : ICalculationResultsService
    {
        private readonly Polly.AsyncPolicy _resultsRepositoryPolicy;

        private readonly ICalculationResultsRepository _calculationResultsRepository;

        public CalculationResultsService(
            IPublishingResiliencePolicies resiliencePolicies,
            ICalculationResultsRepository calculationResultsRepository)
        {
            Guard.ArgumentNotNull(calculationResultsRepository, nameof(calculationResultsRepository));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            Guard.ArgumentNotNull(resiliencePolicies.CalculationResultsRepository, nameof(resiliencePolicies.CalculationResultsRepository));

            _calculationResultsRepository = calculationResultsRepository;
            _resultsRepositoryPolicy = resiliencePolicies.CalculationResultsRepository;
        }

        public async Task<IDictionary<string, ProviderCalculationResult>> GetCalculationResultsBySpecificationId(string specificationId, IEnumerable<string> scopedProviderIds)
        {
            if (!scopedProviderIds.AnyWithNullCheck())
            {
                return new Dictionary<string, ProviderCalculationResult>(); ;
            }

            List<string> providerIds = new List<string>(scopedProviderIds.Distinct());
            List<Task<IEnumerable<ProviderCalculationResult>>> allTasks = new List<Task<IEnumerable<ProviderCalculationResult>>>();

            foreach (var providerId in providerIds)
            {
                allTasks.Add(_resultsRepositoryPolicy.ExecuteAsync(() =>
                    _calculationResultsRepository.GetCalculationResultsBySpecificationAndProvider(specificationId, providerId)));
            }

            await Task.WhenAll(allTasks);

            IEnumerable<ProviderCalculationResult> providerCalculationResults = allTasks.SelectMany(_ => _.Result);

            return providerCalculationResults.ToDictionary(_ => _.ProviderId);
        }
    }
}
