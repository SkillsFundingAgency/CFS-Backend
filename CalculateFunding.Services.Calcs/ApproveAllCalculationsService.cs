using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Results;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Helpers;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Processing;
using Microsoft.Azure.ServiceBus;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Calculation = CalculateFunding.Models.Calcs.Calculation;
using SpecModel = CalculateFunding.Common.ApiClient.Specifications.Models;

namespace CalculateFunding.Services.Calcs
{
    public class ApproveAllCalculationsService : JobProcessingService, IApproveAllCalculationsService
    {
        private readonly ICalculationsRepository _calculationsRepository;
        private readonly ILogger _logger;
        private readonly Polly.AsyncPolicy _calculationRepositoryPolicy;
        private readonly ISpecificationsApiClient _specificationsApiClient;
        private readonly Polly.AsyncPolicy _specificationsApiClientPolicy;
        private readonly IResultsApiClient _resultsApiClient;
        private readonly Polly.AsyncPolicy _resultsApiClientPolicy;
        private readonly ISearchRepository<CalculationIndex> _searchRepository;
        private readonly ICacheProvider _cacheProvider;
        private readonly Polly.AsyncPolicy _cachePolicy;

        public ApproveAllCalculationsService(
            ICalculationsRepository calculationsRepository,
            ICalcsResiliencePolicies resiliencePolicies,
            ISpecificationsApiClient specificationsApiClient,
            IResultsApiClient resultsApiClient,
            ISearchRepository<CalculationIndex> searchRepository,
            ICacheProvider cacheProvider,
            ILogger logger,
            IJobManagement jobManagement) : base(jobManagement, logger)
        {
            Guard.ArgumentNotNull(calculationsRepository, nameof(calculationsRepository));
            Guard.ArgumentNotNull(specificationsApiClient, nameof(specificationsApiClient));
            Guard.ArgumentNotNull(resultsApiClient, nameof(resultsApiClient));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(searchRepository, nameof(searchRepository));
            Guard.ArgumentNotNull(resiliencePolicies?.SpecificationsApiClient, nameof(resiliencePolicies.SpecificationsApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.CacheProviderPolicy, nameof(resiliencePolicies.CacheProviderPolicy));
            Guard.ArgumentNotNull(resiliencePolicies?.ResultsApiClient, nameof(resiliencePolicies.ResultsApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.CalculationsRepository, nameof(resiliencePolicies.CalculationsRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _calculationsRepository = calculationsRepository;
            _logger = logger;
            _calculationRepositoryPolicy = resiliencePolicies.CalculationsRepository;
            _specificationsApiClient = specificationsApiClient;
            _specificationsApiClientPolicy = resiliencePolicies.SpecificationsApiClient;
            _resultsApiClient = resultsApiClient;
            _resultsApiClientPolicy = resiliencePolicies.ResultsApiClient;
            _searchRepository = searchRepository;
            _cacheProvider = cacheProvider;
            _cachePolicy = resiliencePolicies.CacheProviderPolicy;
        }

        public override async Task Process(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            string specificationId = UserPropertyFrom(message, "specification-id");

            IEnumerable<Calculation> calculations =
                (await _calculationRepositoryPolicy.ExecuteAsync(() =>
                    _calculationsRepository.GetCalculationsBySpecificationId(specificationId)))
                .ToArraySafe();

            if (calculations.IsNullOrEmpty())
            {
                string calcNotFoundMessage = $"No calculations found for specification id: {specificationId}";

                _logger.Information(calcNotFoundMessage);

                return;
            }

            foreach (Calculation calculation in calculations)
            {
                calculation.Current.PublishStatus = Models.Versioning.PublishStatus.Approved;
            }

            ApiResponse<SpecModel.SpecificationSummary> specificationApiResponse =
                    await _specificationsApiClientPolicy.ExecuteAsync(() => _specificationsApiClient.GetSpecificationSummaryById(specificationId));
            
            if (!specificationApiResponse.StatusCode.IsSuccess() || specificationApiResponse.Content == null)
            {
                throw new NonRetriableException("Specification not found");
            }

            SpecModel.SpecificationSummary specificationSummary = specificationApiResponse.Content;

            await _calculationRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.UpdateCalculations(calculations));

            Reference fundingStream = specificationSummary.FundingStreams.SingleOrDefault(_ => _.Id == calculations.FirstOrDefault().FundingStreamId);

            await UpdateSearch(calculations, specificationSummary.Name, fundingStream.Name);

            IEnumerable<Task> tasks = calculations.Select(_ => UpdateCalculationInCache(_.ToResponseModel()));

            await TaskHelper.WhenAllAndThrow(tasks.ToArraySafe());

            await UpdateCalculationsInCache(specificationId, specificationSummary.FundingPeriod?.Id, fundingStream.Id);

        }

        private async Task UpdateCalculationInCache(CalculationResponseModel currentVersion)
        {
            // Set current version in cache
            await _cachePolicy.ExecuteAsync(() => _cacheProvider.SetAsync($"{CacheKeys.CurrentCalculation}{currentVersion.Id}", currentVersion, TimeSpan.FromDays(7), true));
        }

        private async Task UpdateCalculationsInCache(string specificationId,
            string fundingPeriodId,
            string fundingStreamId)
        {
            // Invalidate cached calculations for this specification
            await _cachePolicy.ExecuteAsync(() => _cacheProvider.KeyDeleteAsync<List<CalculationSummaryModel>>($"{CacheKeys.CalculationsSummariesForSpecification}{specificationId}"));
            await _cachePolicy.ExecuteAsync(() => _cacheProvider.KeyDeleteAsync<List<CalculationResponseModel>>($"{CacheKeys.CurrentCalculationsForSpecification}{specificationId}"));
            await _cachePolicy.ExecuteAsync(() => _cacheProvider.KeyDeleteAsync<List<CalculationResponseModel>>($"{CacheKeys.CalculationsMetadataForSpecification}{specificationId}"));
            
            //invalidate funding structure lastModified for this calcs specification
            await _resultsApiClientPolicy.ExecuteAsync(() => _resultsApiClient.UpdateFundingStructureLastModified(
                new Common.ApiClient.Results.Models.UpdateFundingStructureLastModifiedRequest
                {
                    LastModified = DateTimeOffset.UtcNow,
                    SpecificationId = specificationId,
                    FundingPeriodId = fundingPeriodId,//add to call stack
                    FundingStreamId = fundingStreamId
                }));
        }

        private async Task UpdateSearch(IEnumerable<Calculation> calculations, string specificationName, string fundingStreamName)
        {
            await _searchRepository.Index(calculations.Select(_ =>
            {
                return CreateCalculationIndexItem(_, specificationName, fundingStreamName);
            }));
        }

        private CalculationIndex CreateCalculationIndexItem(Calculation calculation,
            string specificationName,
            string fundingStreamName)
        {
            return new CalculationIndex
            {
                Id = calculation.Id,
                SpecificationId = calculation.SpecificationId,
                SpecificationName = specificationName,
                Name = calculation.Current.Name,
                ValueType = calculation.Current.ValueType.ToString(),
                FundingStreamId = calculation.FundingStreamId ?? "N/A",
                FundingStreamName = fundingStreamName ?? "N/A",
                Namespace = calculation.Current.Namespace.ToString(),
                CalculationType = calculation.Current.CalculationType.ToString(),
                Description = calculation.Current.Description,
                WasTemplateCalculation = calculation.Current.WasTemplateCalculation,
                Status = calculation.Current.PublishStatus.ToString(),
                LastUpdatedDate = DateTimeOffset.Now
            };
        }

        private string UserPropertyFrom(Message message, string key)
        {
            string userProperty = message.GetUserProperty<string>(key);

            Guard.IsNullOrWhiteSpace(userProperty, key);

            return userProperty;
        }
    }
}
