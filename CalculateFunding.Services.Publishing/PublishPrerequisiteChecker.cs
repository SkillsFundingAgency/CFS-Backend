using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using Serilog;

namespace CalculateFunding.Services.Publishing
{
    public class PublishPrerequisiteChecker : IPublishPrerequisiteChecker
    {
        private readonly ISpecificationFundingStatusService _specificationFundingStatusService;
        private readonly ILogger _logger;

        public PublishPrerequisiteChecker(
            ISpecificationFundingStatusService specificationFundingStatusService,
            ILogger logger)
        {
            Guard.ArgumentNotNull(specificationFundingStatusService, nameof(specificationFundingStatusService));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _specificationFundingStatusService = specificationFundingStatusService;
            _logger = logger;
        }

        public async Task<IEnumerable<string>> PerformPrerequisiteChecks(SpecificationSummary specification, IEnumerable<PublishedProvider> publishedProviders)
        {
            Guard.ArgumentNotNull(specification, nameof(specification));
            Guard.ArgumentNotNull(publishedProviders, nameof(publishedProviders));

            SpecificationFundingStatus specificationFundingStatus = await _specificationFundingStatusService.CheckChooseForFundingStatus(specification);

            if (specificationFundingStatus != SpecificationFundingStatus.AlreadyChosen)
            {
                string errorMessage = $"Specification with id: '{specification.Id}' is not chosen for funding";

                _logger.Error(errorMessage);
                return new string[] { errorMessage };
            }

            if (publishedProviders?.Any(_ => _.Current.Status != PublishedProviderStatus.Approved) ?? false)
            {
                IEnumerable<string> results = publishedProviders.Where(_ => _.Current.Status != PublishedProviderStatus.Approved).Select(_ => $"Provider with id:{_.Id} has current status:{_.Current.Status} so cannot be published.");
                _logger.Error(string.Join(Environment.NewLine, results));
                return results;
            }

            return null;
        }
    }
}
