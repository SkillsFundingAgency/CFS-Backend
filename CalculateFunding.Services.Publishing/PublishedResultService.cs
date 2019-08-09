using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedResultService : IPublishedResultService
    {
        private readonly Polly.Policy _resultsRepositoryPolicy;
        private readonly ICalculationResultsRepository _calculationResultsRepository;

        public PublishedResultService(
            IPublishingResiliencePolicies resiliencePolicies,
            ICalculationResultsRepository calculationResultsRepository)
        {
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            Guard.ArgumentNotNull(calculationResultsRepository, nameof(calculationResultsRepository));

            _resultsRepositoryPolicy = resiliencePolicies.ResultsRepository;
            _calculationResultsRepository = calculationResultsRepository;
        }

        public Task<IEnumerable<ProviderCalculationResult>> GetProviderResultsBySpecificationId(string specificationId)
        {
            Guard.ArgumentNotNull(specificationId, nameof(specificationId));

            return _resultsRepositoryPolicy.ExecuteAsync(() => _calculationResultsRepository.GetCalculationResultsBySpecificationId(specificationId));
        }
    }
}
