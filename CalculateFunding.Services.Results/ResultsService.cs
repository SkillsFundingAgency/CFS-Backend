using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Aggregations;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Messages;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.FeatureToggles;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Results.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Search.Models;
using Microsoft.Azure.ServiceBus;
using Serilog;
using SpecModel = CalculateFunding.Common.ApiClient.Specifications.Models;
using CalcModels = CalculateFunding.Common.ApiClient.Calcs.Models;
using ApiModels = CalculateFunding.Common.ApiClient.Models;
using AutoMapper;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Services.Processing;
using CalculateFunding.Common.Storage;
using Microsoft.Azure.Storage.Blob;
using CalculateFunding.Common.ApiClient.Jobs.Models;

namespace CalculateFunding.Services.Results
{
    public class ResultsService : JobProcessingService, IResultsService, IHealthChecker
    {
        private const string CalcsResultsContainerName = "calcresults";
        private const string CalculationResultsReportFilePrefix = "calculation-results";

        private readonly ILogger _logger;
        private readonly ICalculationResultsRepository _resultsRepository;
        private readonly IProviderSourceDatasetRepository _providerSourceDatasetRepository;
        private readonly ISearchRepository<ProviderCalculationResultsIndex> _calculationProviderResultsSearchRepository;
        private readonly Polly.AsyncPolicy _resultsRepositoryPolicy;
        private readonly ISpecificationsApiClient _specificationsApiClient;
        private readonly Polly.AsyncPolicy _resultsSearchRepositoryPolicy;
        private readonly Polly.AsyncPolicy _specificationsApiClientPolicy;
        private readonly ICalculationsRepository _calculationRepository;
        private readonly Polly.AsyncPolicy _calculationsRepositoryPolicy;
        private readonly ICalculationsApiClient _calculationsApiClient;
        private readonly Polly.AsyncPolicy _calculationsApiClientPolicy;
        private readonly Polly.AsyncPolicy _blobClientPolicy;
        private readonly IFeatureToggle _featureToggle;
        private readonly IMapper _mapper;
        private readonly IBlobClient _blobClient;

        public ResultsService(ILogger logger,
            IFeatureToggle featureToggle,
            ICalculationResultsRepository resultsRepository,
            IProviderSourceDatasetRepository providerSourceDatasetRepository,
            ISearchRepository<ProviderCalculationResultsIndex> calculationProviderResultsSearchRepository,
            ISpecificationsApiClient specificationsApiClient,
            ICalculationsApiClient calculationsApiClient,
            IResultsResiliencePolicies resiliencePolicies,
            ICalculationsRepository calculationRepository,
            IMapper mapper,
            IJobManagement jobManagement,
            IBlobClient blobClient) : base(jobManagement, logger)
        {
            Guard.ArgumentNotNull(resultsRepository, nameof(resultsRepository));
            Guard.ArgumentNotNull(providerSourceDatasetRepository, nameof(providerSourceDatasetRepository));
            Guard.ArgumentNotNull(calculationProviderResultsSearchRepository, nameof(calculationProviderResultsSearchRepository));
            Guard.ArgumentNotNull(specificationsApiClient, nameof(specificationsApiClient));
            Guard.ArgumentNotNull(calculationsApiClient, nameof(calculationsApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.ResultsRepository, nameof(resiliencePolicies.ResultsRepository));
            Guard.ArgumentNotNull(resiliencePolicies?.ResultsSearchRepository, nameof(resiliencePolicies.ResultsSearchRepository));
            Guard.ArgumentNotNull(resiliencePolicies?.SpecificationsApiClient, nameof(resiliencePolicies.SpecificationsApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.CalculationsApiClient, nameof(resiliencePolicies.CalculationsApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.CalculationsRepository, nameof(resiliencePolicies.CalculationsRepository));
            Guard.ArgumentNotNull(resiliencePolicies?.BlobClient, nameof(resiliencePolicies.BlobClient));
            Guard.ArgumentNotNull(calculationRepository, nameof(calculationRepository));
            Guard.ArgumentNotNull(featureToggle, nameof(featureToggle));
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));

            _logger = logger;
            _mapper = mapper;
            _resultsRepository = resultsRepository;
            _providerSourceDatasetRepository = providerSourceDatasetRepository;
            _calculationProviderResultsSearchRepository = calculationProviderResultsSearchRepository;
            _resultsRepositoryPolicy = resiliencePolicies.ResultsRepository;
            _specificationsApiClient = specificationsApiClient;
            _calculationsApiClient = calculationsApiClient;
            _resultsSearchRepositoryPolicy = resiliencePolicies.ResultsSearchRepository;
            _specificationsApiClientPolicy = resiliencePolicies.SpecificationsApiClient;
            _calculationsApiClientPolicy = resiliencePolicies.CalculationsApiClient;
            _blobClientPolicy = resiliencePolicies.BlobClient;
            _calculationRepository = calculationRepository;
            _calculationsRepositoryPolicy = resiliencePolicies.CalculationsRepository;
            _featureToggle = featureToggle;
            _blobClient = blobClient;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth datasetsRepoHealth = await ((IHealthChecker)_resultsRepository).IsHealthOk();
            ServiceHealth providerSourceDatasetRepoHealth = await ((IHealthChecker)_providerSourceDatasetRepository).IsHealthOk();
            (bool Ok, string Message) calcSearchRepoHealth = await _calculationProviderResultsSearchRepository.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(ResultsService)
            };
            health.Dependencies.AddRange(datasetsRepoHealth.Dependencies);
            health.Dependencies.AddRange(providerSourceDatasetRepoHealth.Dependencies);
            health.Dependencies.Add(new DependencyHealth { HealthOk = calcSearchRepoHealth.Ok, DependencyName = _calculationProviderResultsSearchRepository.GetType().GetFriendlyName(), Message = calcSearchRepoHealth.Message });

            return health;
        }

        public async Task DeleteCalculationResults(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            string specificationId = message.UserProperties["specification-id"].ToString();
            if (string.IsNullOrEmpty(specificationId))
            {
                string error = "Null or empty specification Id provided for deleting calculation results";
                _logger.Error(error);
                throw new Exception(error);
            }

            string deletionTypeProperty = message.UserProperties["deletion-type"].ToString();
            if (string.IsNullOrEmpty(deletionTypeProperty))
            {
                string error = "Null or empty deletion type provided for deleting calculation results";
                _logger.Error(error);
                throw new Exception(error);
            }

            await _resultsRepository.DeleteCalculationResultsBySpecificationId(specificationId, deletionTypeProperty.ToDeletionType());
        }

        public async Task<IActionResult> GetProviderResults(string providerId, string specificationId)
        {
            if (string.IsNullOrWhiteSpace(providerId))
            {
                _logger.Error("No provider Id was provided to GetProviderResults");
                return new BadRequestObjectResult("Null or empty provider Id provided");
            }

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification Id was provided to GetProviderResults");
                return new BadRequestObjectResult("Null or empty specification Id provided");
            }

            ProviderResult providerResult = await _resultsRepositoryPolicy.ExecuteAsync(() => _resultsRepository.GetProviderResult(providerId, specificationId));

            if (providerResult != null)
            {
                _logger.Information($"A result was found for provider id {providerId}, specification id {specificationId}");

                ApiModels.ApiResponse<IEnumerable<CalcModels.CalculationMetadata>> calculationsResponse = await _calculationsApiClientPolicy.ExecuteAsync(() => _calculationsApiClient.GetCalculationMetadataForSpecification(specificationId));

                if (calculationsResponse?.Content == null)
                {
                    string error = $"Did not locate any calculation metadata for specification {specificationId}.";
                    _logger.Error(error);

                    return new BadRequestObjectResult(error);
                }

                IDictionary<string, CalcModels.CalculationMetadata> calculations = calculationsResponse.Content.ToDictionary(_ => _.CalculationId);

                ProviderResultResponse providerResultResponse = _mapper.Map<ProviderResultResponse>(providerResult);

                foreach(CalculationResultResponse calculationResult in providerResultResponse.CalculationResults)
                {
                    calculationResult.CalculationValueType = _mapper.Map<CalculationValueType>(calculations[calculationResult.Calculation.Id].ValueType);
                }

                return new OkObjectResult(providerResultResponse);
            }

            _logger.Information($"A result was not found for provider id {providerId}, specification id {specificationId}");

            return new NotFoundResult();
        }

        public async Task<IActionResult> GetProviderResultByCalculationType(string providerId, string specificationId, CalculationType calculationType)
        {
            if (string.IsNullOrWhiteSpace(providerId))
            {
                _logger.Error("No provider Id was provided to GetProviderResults");
                return new BadRequestObjectResult("Null or empty provider Id provided");
            }

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification Id was provided to GetProviderResults");
                return new BadRequestObjectResult("Null or empty specification Id provided");
            }

            ProviderResult providerResult = await _resultsRepositoryPolicy.ExecuteAsync(() => _resultsRepository.GetProviderResultByCalculationType(providerId, specificationId, calculationType));

            if (providerResult != null)
            {
                _logger.Information($"A result was found for provider id {providerId}, specification id {specificationId}");

                return new OkObjectResult(providerResult);
            }

            _logger.Information($"A result was not found for provider id {providerId}, specification id {specificationId}");

            return new NotFoundResult();
        }

        public async Task<IActionResult> ProviderHasResultsBySpecificationId(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification Id was provided");
                return new BadRequestObjectResult("Null or empty specification Id provided");
            }

            bool hasResults = await _resultsRepositoryPolicy.ExecuteAsync(() => _resultsRepository.ProviderHasResultsBySpecificationId(specificationId));

            return new OkObjectResult(hasResults);
        }

        #region "GetProviderResultsBySpecificationId"
        public async Task<IActionResult> GetProviderResultsBySpecificationId(string specificationId, string top)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification Id was provided to GetProviderResults");
                return new BadRequestObjectResult("Null or empty specification Id provided");
            }

            return new OkObjectResult(await ProviderResultsBySpecificationId(specificationId, top));
        }

        private async Task<IEnumerable<ProviderResult>> ProviderResultsBySpecificationId(string specificationId, string top)
        {
            IEnumerable<ProviderResult> providerResults;

            if (!string.IsNullOrWhiteSpace(top))
            {
                if (int.TryParse(top, out int maxResults))
                {
                    providerResults = await ProviderResultsBySpecificationId(specificationId, maxResults);

                    return providerResults;
                }
            }

            providerResults = await ProviderResultsBySpecificationId(specificationId);

            return providerResults;
        }

        //TODO; change this all to work in a batch basis

        private async Task<IEnumerable<ProviderResult>> ProviderResultsBySpecificationId(string specificationId, int maxResults = -1)
        {
            return await _resultsRepositoryPolicy.ExecuteAsync(() => _resultsRepository.GetProviderResultsBySpecificationId(specificationId, maxResults));
        }
        #endregion

        #region "GetProviderSpecifications"
        /// <summary>
        /// Returns distinct specificationIds where there are results for this provider
        /// </summary>
        /// <param name="providerId"></param>
        /// <returns></returns>
        public async Task<IActionResult> GetProviderSpecifications(string providerId)
        {
            List<string> result = new List<string>();

            if (string.IsNullOrWhiteSpace(providerId))
            {
                _logger.Error("No provider Id was provided to GetProviderSpecifications");
                return new BadRequestObjectResult("Null or empty provider Id provided");
            }

            IEnumerable<ProviderResult> providerResults = (await _resultsRepositoryPolicy.ExecuteAsync(() => _resultsRepository.GetSpecificationResults(providerId))).ToList();

            if (!providerResults.IsNullOrEmpty())
            {
                _logger.Information($"Results was found for provider id '{providerId}'");

                result.AddRange(providerResults
                    .Where(m => !string.IsNullOrWhiteSpace(m.SpecificationId))
                    .Select(s => s.SpecificationId)
                    .Distinct());
            }
            else
            {
                _logger.Information($"Results were not found for provider id '{providerId}'");
            }

            return new OkObjectResult(result);
        }
        #endregion

        public async Task<IActionResult> GetFundingCalculationResultsForSpecifications(SpecificationListModel specifications)
        {
            if (specifications == null)
            {
                _logger.Error("Null specification model provided");

                return new BadRequestObjectResult("Null specifications model provided");
            }

            if (specifications.SpecificationIds.IsNullOrEmpty())
            {
                _logger.Error("Null or empty specification ids provided");

                return new BadRequestObjectResult("Null or empty specification ids provided");
            }

            ConcurrentBag<FundingCalculationResultsTotals> totalsModels = new ConcurrentBag<FundingCalculationResultsTotals>();

            IList<Task> totalsTasks = new List<Task>();

            foreach (string specificationId in specifications.SpecificationIds)
            {
                totalsTasks.Add(Task.Run(async () =>
                {
                    decimal totalResult = await _resultsRepository.GetCalculationResultTotalForSpecificationId(specificationId);

                    totalsModels.Add(new FundingCalculationResultsTotals
                    {
                        SpecificationId = specificationId,
                        TotalResult = totalResult
                    });

                }));
            }

            try
            {
                await TaskHelper.WhenAllAndThrow(totalsTasks.ToArray());
            }
            catch (Exception ex)
            {
                return new InternalServerErrorResult($"An error occurred when obtaining calculation totals with the following message: \n {ex.Message}");
            }

            return new OkObjectResult(totalsModels);
        }

        public async Task<IActionResult> GetProviderSourceDatasetsByProviderIdAndSpecificationId(string specificationId, string providerId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification Id was provided to GetProviderResultsBySpecificationId");
                return new BadRequestObjectResult("Null or empty specification Id provided");
            }

            if (string.IsNullOrWhiteSpace(providerId))
            {
                _logger.Error("No provider Id was provided to GetProviderResultsBySpecificationId");
                return new BadRequestObjectResult("Null or empty provider Id provided");
            }

            IEnumerable<ProviderSourceDataset> providerResults = await _resultsRepositoryPolicy.ExecuteAsync(() => _providerSourceDatasetRepository.GetProviderSourceDatasets(providerId, specificationId));

            return new OkObjectResult(providerResults);
        }

        public async Task<IActionResult> GetScopedProviderIdsBySpecificationId(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification Id was provided to GetProviderResultsBySpecificationId");
                return new BadRequestObjectResult("Null or empty specification Id provided");
            }

            IEnumerable<string> providerResults = (await _resultsRepositoryPolicy.ExecuteAsync(() => _providerSourceDatasetRepository.GetAllScopedProviderIdsForSpecificationId(specificationId))).ToList();

            return new OkObjectResult(providerResults);
        }

        public async Task<IActionResult> ReIndexCalculationProviderResults()
        {
            IEnumerable<DocumentEntity<ProviderResult>> providerResults = await _resultsRepositoryPolicy.ExecuteAsync(() => _resultsRepository.GetAllProviderResults());

            IList<ProviderCalculationResultsIndex> searchItems = new List<ProviderCalculationResultsIndex>();

            Dictionary<string, SpecModel.SpecificationSummary> specifications = new Dictionary<string, SpecModel.SpecificationSummary>();

            foreach (DocumentEntity<ProviderResult> documentEntity in providerResults)
            {
                ProviderResult providerResult = documentEntity.Content;

                foreach (CalculationResult calculationResult in providerResult.CalculationResults)
                {
                    SpecModel.SpecificationSummary specificationSummary = null;
                    if (!specifications.ContainsKey(providerResult.SpecificationId))
                    {
                        Common.ApiClient.Models.ApiResponse<SpecModel.SpecificationSummary> specificationApiResponse =
                            await _specificationsApiClientPolicy.ExecuteAsync(() => _specificationsApiClient.GetSpecificationSummaryById(providerResult.SpecificationId));

                        if (!specificationApiResponse.StatusCode.IsSuccess() || specificationApiResponse.Content == null)
                        {
                            throw new InvalidOperationException($"Specification Summary returned null for specification ID '{providerResult.SpecificationId}'");
                        }

                        specificationSummary = specificationApiResponse.Content;

                        specifications.Add(providerResult.SpecificationId, specificationSummary);
                    }
                    else
                    {
                        specificationSummary = specifications[providerResult.SpecificationId];
                    }

                    ProviderCalculationResultsIndex searchItem = new ProviderCalculationResultsIndex
                    {
                        SpecificationId = providerResult.SpecificationId,
                        SpecificationName = specificationSummary?.Name,
                        CalculationName = providerResult.CalculationResults.Select(x => x.Calculation.Name).ToArraySafe(),
                        CalculationId = providerResult.CalculationResults.Select(x => x.Calculation.Id).ToArraySafe(),
                        ProviderId = providerResult.Provider.Id,
                        ProviderName = providerResult.Provider.Name,
                        ProviderType = providerResult.Provider.ProviderType,
                        ProviderSubType = providerResult.Provider.ProviderSubType,
                        LocalAuthority = providerResult.Provider.Authority,
                        LastUpdatedDate = documentEntity.UpdatedAt,
                        UKPRN = providerResult.Provider.UKPRN,
                        URN = providerResult.Provider.URN,
                        UPIN = providerResult.Provider.UPIN,
                        EstablishmentNumber = providerResult.Provider.EstablishmentNumber,
                        OpenDate = providerResult.Provider.DateOpened,
                        CalculationResult = providerResult.CalculationResults.Select(m => !string.IsNullOrEmpty(m.Value?.ToString()) ? m.Value.ToString() : "null").ToArraySafe()
                    };

                    if (_featureToggle.IsExceptionMessagesEnabled())
                    {
                        searchItem.CalculationException = providerResult.CalculationResults
                            .Where(m => !string.IsNullOrWhiteSpace(m.ExceptionType))
                            .Select(e => e.Calculation.Id)
                            .ToArraySafe();

                        searchItem.CalculationExceptionType = providerResult.CalculationResults
                            .Select(m => m.ExceptionType ?? string.Empty)
                            .ToArraySafe();

                        searchItem.CalculationExceptionMessage = providerResult.CalculationResults
                            .Select(m => m.ExceptionMessage ?? string.Empty)
                            .ToArraySafe();
                    }

                    searchItems.Add(searchItem);
                }
            }

            const int partitionSize = 500;
            for (int i = 0; i < searchItems.Count; i += partitionSize)
            {
                IEnumerable<ProviderCalculationResultsIndex> partitionedResults = searchItems
                    .Skip(i)
                    .Take(partitionSize);

                IEnumerable<IndexError> errors = await _resultsSearchRepositoryPolicy.ExecuteAsync(() => _calculationProviderResultsSearchRepository.Index(partitionedResults));

                if (errors.Any())
                {
                    _logger.Error($"Failed to index calculation provider result documents with errors: { string.Join(";", errors.Select(m => m.ErrorMessage)) }");

                    return new InternalServerErrorResult(null);
                }
            }

            return new NoContentResult();
        }

        public override async Task Process(Message message)
        {
            string specificationId = message.UserProperties["specificationId"].ToString();

            SpecificationProviders specificationProviders = message.GetPayloadAsInstanceOf<SpecificationProviders>();

            IEnumerable<ProviderResult> providerResults = await _resultsRepositoryPolicy
                .ExecuteAsync(() => _resultsRepository.GetProviderResultsBySpecificationIdAndProviders(specificationProviders.Providers, specificationId)
            );

            if (providerResults.Any())
            {
                _logger.Information($"Removing {specificationProviders.Providers.Count()} from calculation results for specification {specificationId}");

                await _resultsRepositoryPolicy
                    .ExecuteAsync(() => _resultsRepository.DeleteCurrentProviderResults(providerResults)
                );

                SearchResults<ProviderCalculationResultsIndex> indexItems = await _resultsSearchRepositoryPolicy
                    .ExecuteAsync(() => _calculationProviderResultsSearchRepository.Search(string.Empty,
                            new SearchParameters
                            {
                                Top = providerResults.Count(),
                                SearchMode = SearchMode.Any,
                                Filter = $"specificationId eq '{specificationId}' and (" + string.Join(" or ", providerResults.Select(m => $"providerId eq '{m.Provider.Id}'")) + ")",
                                QueryType = QueryType.Full
                            }
                        )
                    );

                await _resultsSearchRepositoryPolicy.ExecuteAsync(() => _calculationProviderResultsSearchRepository.Remove(indexItems?.Results.Select(m => m.Result)));
            }
        }

        public async Task<IActionResult> HasCalculationResults(string calculationId)
        {
            Guard.IsNullOrWhiteSpace(calculationId, nameof(calculationId));

            Common.ApiClient.Calcs.Models.Calculation calculation = await _calculationsRepositoryPolicy.ExecuteAsync(() => _calculationRepository.GetCalculationById(calculationId));

            if (calculation == null)
            {
                _logger.Error($"Calculation could not be found for calculation id '{calculationId}'");

                return new NotFoundObjectResult($"Calculation could not be found for calculation id '{calculationId}'");
            }

            ProviderResult providerResult = await _resultsRepositoryPolicy.ExecuteAsync(() => _resultsRepository.GetSingleProviderResultBySpecificationId(calculation.SpecificationId));

            CalculationResult calculationResult = providerResult?
                .CalculationResults?
                .FirstOrDefault(m => 
                    string.Equals(m.Calculation.Id, 
                        calculationId, 
                        StringComparison.InvariantCultureIgnoreCase));

            if (calculationResult != null)
            {
                return new OkObjectResult(true);
            }

            return new OkObjectResult(false);
        }

        public async Task QueueCsvGenerationMessages()
        {
            Common.ApiClient.Models.ApiResponse<IEnumerable<SpecModel.SpecificationSummary>> specificationApiResponse =
                            await _specificationsApiClientPolicy.ExecuteAsync(() => _specificationsApiClient.GetSpecificationSummaries());

            if (!specificationApiResponse.StatusCode.IsSuccess() || specificationApiResponse.Content.IsNullOrEmpty())
            {
                string errorMessage = "No specification summaries found to generate calculation results csv.";

                _logger.Error(errorMessage);

                throw new RetriableException(errorMessage);
            }

            IEnumerable<SpecModel.SpecificationSummary> specificationSummaries = specificationApiResponse.Content;

            Task<Job>[] queueCsvJobTasks = specificationSummaries.Select(_ => 
                QueueCsvGenerationMessageIfNewCalculationResults(_.Id, _.Name))
                .ToArray();

            await TaskHelper.WhenAllAndThrow(queueCsvJobTasks);
        }

        public async Task<IActionResult> QueueCsvGeneration(string specificationId)
        {
            Common.ApiClient.Models.ApiResponse<SpecModel.SpecificationSummary> specificationApiResponse =
                await _specificationsApiClientPolicy.ExecuteAsync(() => _specificationsApiClient.GetSpecificationSummaryById(specificationId));

            if (!specificationApiResponse.StatusCode.IsSuccess() || specificationApiResponse.Content == null)
            {
                string errorMessage = "No specification summaries found to generate calculation results csv.";

                _logger.Error(errorMessage);

                throw new RetriableException(errorMessage);
            }

            Job job = await QueueCsvGenerationMessage(specificationId, specificationApiResponse.Content.Name);

            return new OkObjectResult(job);
        }

        public async Task<IActionResult> GetSpecificationCalculationResultsMetadata(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification Id was provided to get calculation results metadata");
                return new BadRequestObjectResult("Null or empty specification Id provided");
            }

            DateTime? lastUpdatedDate = await _resultsRepositoryPolicy.ExecuteAsync(() => _resultsRepository.GetSpecificationCalculationResultsLastUpdated(specificationId));

            return lastUpdatedDate.HasValue ?
                new OkObjectResult(new SpecificationCalculationResultsMetadata() { SpecificationId = specificationId, LastUpdated = lastUpdatedDate.Value })
                : (IActionResult)new NotFoundResult();
        }

        public async Task<Job> QueueCsvGenerationMessageIfNewCalculationResults(string specificationId, string specificationName)
        {
            bool hasNewResults;

            bool blobExists = await _blobClientPolicy.ExecuteAsync(() => 
                _blobClient.DoesBlobExistAsync($"{CalculationResultsReportFilePrefix}-{specificationId}", CalcsResultsContainerName));

            if (blobExists)
            {
                ICloudBlob cloudBlob = await _blobClientPolicy.ExecuteAsync(() => 
                    _blobClient.GetBlobReferenceFromServerAsync(
                        $"{CalculationResultsReportFilePrefix}-{specificationId}",
                        CalcsResultsContainerName));

                DateTimeOffset? lastModified = cloudBlob.Properties?.LastModified;

                hasNewResults = await _resultsRepositoryPolicy.ExecuteAsync(
                    () => _resultsRepository.CheckHasNewResultsForSpecificationIdAndTime(specificationId,
                        lastModified.GetValueOrDefault()));
            }
            else
            {
                // only queue the csv report job if there are calculation results for the specification
                hasNewResults = await _resultsRepositoryPolicy.ExecuteAsync(
                    () => _resultsRepository.ProviderHasResultsBySpecificationId(specificationId));
            }

            if (!hasNewResults)
            {
                _logger.Information(
                    $"No new calculation results for specification id '{specificationId}'. Not queueing report job");
                
                return null;
            }

            _logger.Information($"Found new calculation results for specification id '{specificationId}'");

            return await QueueCsvGenerationMessage(specificationId, specificationName);
        }

        private async Task<Job> QueueCsvGenerationMessage(string specificationId, string specificationName)
        {
            JobCreateModel jobCreateModel = new JobCreateModel
            {
                Trigger = new Trigger
                {
                    EntityId = specificationId,
                    EntityType = "Specification",
                    Message = "Generate Calc Csv Results Timer Job"
                },
                JobDefinitionId = JobConstants.DefinitionNames.GenerateCalcCsvResultsJob,
                SpecificationId = specificationId,
                Properties = new Dictionary<string, string>
                {
                    { "specification-id", specificationId },
                    { "specification-name", specificationName }
                }
            };

            return await QueueJob(jobCreateModel);
        }
    }
}
