using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Results.Search;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Results.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results
{
    public class ProviderCalculationResultsReIndexerService : IProviderCalculationResultsReIndexerService
    {
        private readonly ILogger _logger;
        private readonly ISearchRepository<ProviderCalculationResultsIndex> _providerCalculationResultsSearchRepository;
        private readonly ISpecificationsRepository _specificationsRepository;
        private readonly ICalculationResultsRepository _resultsRepository;
        private readonly Polly.Policy _resultsRepositoryPolicy;
        private readonly Polly.Policy _specificationsRepositoryPolicy;
        private readonly Polly.Policy _resultsSearchRepositoryPolicy;
        private readonly IFeatureToggle _featureToggle;

        private const int batchSize = 200;
        private readonly IMessengerService _messengerService;

        public ProviderCalculationResultsReIndexerService(
            ILogger logger, 
            ISearchRepository<ProviderCalculationResultsIndex> providerCalculationResultsSearchRepository,
            ISpecificationsRepository specificationsRepository,
            ICalculationResultsRepository resultsRepository,
            IResultsResiliencePolicies resiliencePolicies,
            IFeatureToggle featureToggle,
            IMessengerService messengerService)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(providerCalculationResultsSearchRepository, nameof(providerCalculationResultsSearchRepository));
            Guard.ArgumentNotNull(specificationsRepository, nameof(specificationsRepository));
            Guard.ArgumentNotNull(resultsRepository, nameof(resultsRepository));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            Guard.ArgumentNotNull(featureToggle, nameof(featureToggle));
            Guard.ArgumentNotNull(messengerService, nameof(messengerService));

            _logger = logger;
            _providerCalculationResultsSearchRepository = providerCalculationResultsSearchRepository;
            _specificationsRepository = specificationsRepository;
            _resultsRepository = resultsRepository;
            _resultsRepositoryPolicy = resiliencePolicies.ResultsRepository;
            _specificationsRepositoryPolicy = resiliencePolicies.SpecificationsRepository;
            _resultsSearchRepositoryPolicy = resiliencePolicies.ResultsSearchRepository;
            _featureToggle = featureToggle;
            _messengerService = messengerService;
        }

        public async Task<IActionResult> ReIndexCalculationResults(HttpRequest httpRequest)
        {
            Guard.ArgumentNotNull(httpRequest, nameof(httpRequest));

            IDictionary<string, string> properties = httpRequest.BuildMessageProperties();

            await _messengerService.SendToQueue(ServiceBusConstants.QueueNames.ReIndexCalculationResultsIndex, "", properties);

            return new NoContentResult();
        }

        public async Task ReIndexCalculationResults(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            Reference user = message.GetUserDetails();

            _logger.Information($"{nameof(ReIndexCalculationResults)} initiated by: '{user.Name}'");

            IEnumerable<DocumentEntity<ProviderResult>> providerResults = await _resultsRepositoryPolicy.ExecuteAsync(() => _resultsRepository.GetAllProviderResults());

            Dictionary<string, SpecificationSummary> specifications = new Dictionary<string, SpecificationSummary>();

            IEnumerable<string> specificationIds = providerResults.Select(m => m.Content.SpecificationId).Distinct();

            foreach (string specificationId in specificationIds)
            {
                SpecificationSummary specificationSummary = await _specificationsRepositoryPolicy.ExecuteAsync(() => _specificationsRepository.GetSpecificationSummaryById(specificationId));

                specifications.Add(specificationId, specificationSummary);
            }

            IList<ProviderCalculationResultsIndex> results = new List<ProviderCalculationResultsIndex>();

            foreach (DocumentEntity<ProviderResult> providerResultEntity in providerResults)
            {
                ProviderResult providerResult = providerResultEntity.Content;

                if (!providerResult.CalculationResults.IsNullOrEmpty())
                {
                    SpecificationSummary specification = specifications[providerResult.SpecificationId];

                    ProviderCalculationResultsIndex calculationResult = new ProviderCalculationResultsIndex
                    {
                        SpecificationId = providerResult.SpecificationId,
                        SpecificationName = specification?.Name,
                        ProviderId = providerResult.Provider?.Id,
                        ProviderName = providerResult.Provider?.Name,
                        ProviderType = providerResult.Provider?.ProviderType,
                        ProviderSubType = providerResult.Provider?.ProviderSubType,
                        LocalAuthority = providerResult.Provider?.Authority,
                        LastUpdatedDate = providerResultEntity.CreatedAt,
                        UKPRN = providerResult.Provider?.UKPRN,
                        URN = providerResult.Provider?.URN,
                        UPIN = providerResult.Provider?.UPIN,
                        EstablishmentNumber = providerResult.Provider?.EstablishmentNumber,
                        OpenDate = providerResult.Provider?.DateOpened,
                        CalculationId = providerResult.CalculationResults.Select(m => m.Calculation.Id).ToArraySafe(),
                        CalculationName = providerResult.CalculationResults.Select(m => m.Calculation.Name).ToArraySafe(),
                        CalculationResult = providerResult.CalculationResults.Select(m => m.Value.HasValue ? m.Value.ToString() : "null").ToArraySafe()
                    };

                    if (_featureToggle.IsExceptionMessagesEnabled())
                    {
                        calculationResult.CalculationException = providerResult.CalculationResults
                            .Select(m => !string.IsNullOrWhiteSpace(m.ExceptionType) ? "true" : "false")
                            .ToArraySafe();

                        calculationResult.CalculationExceptionType = providerResult.CalculationResults
                            .Select(m => m.ExceptionType ?? string.Empty)
                            .ToArraySafe();

                        calculationResult.CalculationExceptionMessage = providerResult.CalculationResults
                            .Select(m => m.ExceptionMessage ?? string.Empty)
                            .ToArraySafe();
                    }

                    results.Add(calculationResult);
                }
            }

            for (int i = 0; i < results.Count; i += batchSize)
            {
                IEnumerable<ProviderCalculationResultsIndex> partitionedResults = results.Skip(i).Take(batchSize);

                IEnumerable<IndexError> errors = await _resultsSearchRepositoryPolicy.ExecuteAsync(() => _providerCalculationResultsSearchRepository.Index(partitionedResults));

                if (errors.Any())
                {
                    string errorMessage = $"Failed to index calculation provider result documents with errors: { string.Join(";", errors.Select(m => m.ErrorMessage)) }";

                    _logger.Error(errorMessage);

                    throw new RetriableException(errorMessage);
                }
            }
        }
    }
}
