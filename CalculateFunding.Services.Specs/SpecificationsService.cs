using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Common.ApiClient.Providers.Models;
using CalculateFunding.Common.ApiClient.Results;
using CalculateFunding.Common.ApiClient.Results.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Messages;
using CalculateFunding.Models.Specs;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.FeatureToggles;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Processing;
using CalculateFunding.Services.Specs.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using Polly;
using Serilog;
using PolicyModels = CalculateFunding.Common.ApiClient.Policies.Models;
using PublishStatus = CalculateFunding.Models.Versioning.PublishStatus;
using SpecificationVersion = CalculateFunding.Models.Specs.SpecificationVersion;

namespace CalculateFunding.Services.Specs
{
    public class SpecificationsService : JobProcessingService, ISpecificationsService, IHealthChecker
    {
        private readonly IMapper _mapper;
        private readonly ISpecificationsRepository _specificationsRepository;
        private readonly IPoliciesApiClient _policiesApiClient;
        private readonly AsyncPolicy _policiesApiClientPolicy;
        private readonly ILogger _logger;
        private readonly IValidator<SpecificationCreateModel> _specificationCreateModelValidator;
        private readonly IMessengerService _messengerService;
        private readonly ISearchRepository<SpecificationIndex> _searchRepository;
        private readonly IValidator<AssignDefinitionRelationshipMessage> _assignDefinitionRelationshipMessageValidator;
        private readonly IValidator<SpecificationEditModel> _specificationEditModelValidator;
        private readonly ICacheProvider _cacheProvider;
        private readonly IResultsRepository _resultsRepository;
        private readonly IVersionRepository<Models.Specs.SpecificationVersion> _specificationVersionRepository;
        private readonly IQueueCreateSpecificationJobActions _queueCreateSpecificationJobAction;
        private readonly IQueueEditSpecificationJobActions _queueEditSpecificationJobActions;
        private readonly IQueueDeleteSpecificationJobActions _queueDeleteSpecificationJobAction;
        private readonly ICalculationsApiClient _calcsApiClient;
        private readonly AsyncPolicy _calcsApiClientPolicy;
        private readonly IFeatureToggle _featureToggle;
        private readonly IProvidersApiClient _providersApiClient;
        private readonly AsyncPolicy _providersApiClientPolicy;
        private readonly ISpecificationIndexer _specificationIndexer;
        private readonly ISpecificationTemplateVersionChangedHandler _templateVersionChangedHandler;
        private readonly IValidator<AssignSpecificationProviderVersionModel> _assignSpecificationProviderVersionModelValidator;
        private readonly IResultsApiClient _results;
        private readonly AsyncPolicy _resultsApiClientPolicy;
        private readonly IJobManagement _jobManagement;

        public SpecificationsService(
            IMapper mapper,
            ISpecificationsRepository specificationsRepository,
            IPoliciesApiClient policiesApiClient,
            ILogger logger,
            IValidator<SpecificationCreateModel> specificationCreateModelValidator,
            IMessengerService messengerService,
            ISearchRepository<SpecificationIndex> searchRepository,
            IValidator<AssignDefinitionRelationshipMessage> assignDefinitionRelationshipMessageValidator,
            ICacheProvider cacheProvider,
            IValidator<SpecificationEditModel> specificationEditModelValidator,
            IResultsRepository resultsRepository,
            IVersionRepository<Models.Specs.SpecificationVersion> specificationVersionRepository,
            ISpecificationsResiliencePolicies resiliencePolicies,
            IQueueCreateSpecificationJobActions queueCreateSpecificationJobAction,
            IQueueEditSpecificationJobActions queueEditSpecificationJobActions,
            IQueueDeleteSpecificationJobActions queueDeleteSpecificationJobAction,
            ICalculationsApiClient calcsApiClient,
            IFeatureToggle featureToggle,
            IProvidersApiClient providersApiClient,
            ISpecificationIndexer specificationIndexer,
            IResultsApiClient results,
            ISpecificationTemplateVersionChangedHandler templateVersionChangedHandler,
            IValidator<AssignSpecificationProviderVersionModel> assignSpecificationProviderVersionModelValidator,
            IJobManagement jobManagement) : base(jobManagement, logger)
        {
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            Guard.ArgumentNotNull(specificationsRepository, nameof(specificationsRepository));
            Guard.ArgumentNotNull(policiesApiClient, nameof(policiesApiClient));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(specificationCreateModelValidator, nameof(specificationCreateModelValidator));
            Guard.ArgumentNotNull(messengerService, nameof(messengerService));
            Guard.ArgumentNotNull(searchRepository, nameof(searchRepository));
            Guard.ArgumentNotNull(assignDefinitionRelationshipMessageValidator,
                nameof(assignDefinitionRelationshipMessageValidator));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(specificationEditModelValidator, nameof(specificationEditModelValidator));
            Guard.ArgumentNotNull(resultsRepository, nameof(resultsRepository));
            Guard.ArgumentNotNull(specificationVersionRepository, nameof(specificationVersionRepository));
            Guard.ArgumentNotNull(resiliencePolicies?.PoliciesApiClient, nameof(resiliencePolicies.PoliciesApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.JobsApiClient, nameof(resiliencePolicies.JobsApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.CalcsApiClient, nameof(resiliencePolicies.CalcsApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.ProvidersApiClient, nameof(resiliencePolicies.ProvidersApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.ResultsApiClient, nameof(resiliencePolicies.ResultsApiClient));
            Guard.ArgumentNotNull(queueCreateSpecificationJobAction, nameof(queueCreateSpecificationJobAction));
            Guard.ArgumentNotNull(queueEditSpecificationJobActions, nameof(queueEditSpecificationJobActions));
            Guard.ArgumentNotNull(queueDeleteSpecificationJobAction, nameof(queueDeleteSpecificationJobAction));
            Guard.ArgumentNotNull(calcsApiClient, nameof(calcsApiClient));
            Guard.ArgumentNotNull(featureToggle, nameof(featureToggle));
            Guard.ArgumentNotNull(providersApiClient, nameof(providersApiClient));
            Guard.ArgumentNotNull(specificationIndexer, nameof(specificationIndexer));
            Guard.ArgumentNotNull(results, nameof(results));
            Guard.ArgumentNotNull(templateVersionChangedHandler, nameof(templateVersionChangedHandler));
            Guard.ArgumentNotNull(assignSpecificationProviderVersionModelValidator, nameof(assignSpecificationProviderVersionModelValidator));
            Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));

            _mapper = mapper;
            _specificationsRepository = specificationsRepository;
            _policiesApiClient = policiesApiClient;
            _policiesApiClientPolicy = resiliencePolicies.PoliciesApiClient;
            _logger = logger;
            _specificationCreateModelValidator = specificationCreateModelValidator;
            _messengerService = messengerService;
            _searchRepository = searchRepository;
            _assignDefinitionRelationshipMessageValidator = assignDefinitionRelationshipMessageValidator;
            _cacheProvider = cacheProvider;
            _specificationEditModelValidator = specificationEditModelValidator;
            _resultsRepository = resultsRepository;
            _specificationVersionRepository = specificationVersionRepository;
            _queueCreateSpecificationJobAction = queueCreateSpecificationJobAction;
            _queueEditSpecificationJobActions = queueEditSpecificationJobActions;
            _queueDeleteSpecificationJobAction = queueDeleteSpecificationJobAction;
            _calcsApiClient = calcsApiClient;
            _featureToggle = featureToggle;
            _calcsApiClientPolicy = resiliencePolicies.CalcsApiClient;
            _providersApiClient = providersApiClient;
            _specificationIndexer = specificationIndexer;
            _results = results;
            _templateVersionChangedHandler = templateVersionChangedHandler;
            _assignSpecificationProviderVersionModelValidator = assignSpecificationProviderVersionModelValidator;
            _providersApiClientPolicy = resiliencePolicies.ProvidersApiClient;
            _resultsApiClientPolicy = resiliencePolicies.ResultsApiClient;
            _jobManagement = jobManagement;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth specRepoHealth = await ((IHealthChecker)_specificationsRepository).IsHealthOk();
            string queueName = ServiceBusConstants.QueueNames.CalculationJobInitialiser;
            (bool Ok, string Message) messengerServiceHealth = await _messengerService.IsHealthOk(queueName);
            (bool Ok, string Message) searchRepoHealth = await _searchRepository.IsHealthOk();
            (bool Ok, string Message) cacheHealth = await _cacheProvider.IsHealthOk();
            ServiceHealth specificationIndexerHealth = await _specificationIndexer.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(SpecificationsService)
            };
            health.Dependencies.AddRange(specRepoHealth.Dependencies);
            health.Dependencies.AddRange(specificationIndexerHealth.Dependencies);
            health.Dependencies.Add(new DependencyHealth
            {
                HealthOk = messengerServiceHealth.Ok,
                DependencyName = $"{_messengerService.GetType().GetFriendlyName()} for queue: {queueName}",
                Message = messengerServiceHealth.Message
            });
            health.Dependencies.Add(new DependencyHealth
            {
                HealthOk = searchRepoHealth.Ok,
                DependencyName = _searchRepository.GetType().GetFriendlyName(),
                Message = searchRepoHealth.Message
            });
            health.Dependencies.Add(new DependencyHealth
            {
                HealthOk = cacheHealth.Ok,
                DependencyName = _cacheProvider.GetType().GetFriendlyName(),
                Message = cacheHealth.Message
            });

            return health;
        }

        public async Task<IActionResult> GetSpecifications()
        {
            IEnumerable<Specification> specifications = await _specificationsRepository.GetSpecifications();

            if (specifications == null)
            {
                _logger.Warning($"No specifications were returned from the repository, result came back null");

                return new NotFoundResult();
            }

            return new OkObjectResult(specifications);
        }

        public async Task<IActionResult> GetSpecificationById(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification Id was provided to GetSpecificationById");

                return new BadRequestObjectResult("Null or empty specification Id provided");
            }

            Specification specification = await _specificationsRepository.GetSpecificationById(specificationId);

            if (specification == null)
            {
                _logger.Warning($"A specification for id {specificationId} could not found");

                return new NotFoundResult();
            }

            return new OkObjectResult(specification);
        }

        public async Task<IActionResult> GetSpecificationSummaryById(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification Id was provided to GetSpecificationSummaryById");

                return new BadRequestObjectResult("Null or empty specification Id provided");
            }

            string cacheKey = $"{CacheKeys.SpecificationSummaryById}{specificationId}";

            SpecificationSummary summary = await _cacheProvider.GetAsync<SpecificationSummary>(cacheKey);
            if (summary == null)
            {
                Specification specification = await _specificationsRepository.GetSpecificationById(specificationId);

                if (specification == null)
                {
                    _logger.Information($"A specification for id '{specificationId}' could not found");

                    return new NotFoundResult();
                }

                summary = _mapper.Map<SpecificationSummary>(specification);

                await _cacheProvider.SetAsync(cacheKey, summary, TimeSpan.FromDays(1), true);
            }

            return new OkObjectResult(summary);
        }

        public async Task<IActionResult> GetSpecificationSummariesByIds(string[] specificationIds)
        {
            List<SpecificationSummary> result = new List<SpecificationSummary>();

            if (specificationIds?.Any() == false)
            {
                _logger.Warning("No specification ids was provided to GetSpecificationSummariesByIds");
                return new BadRequestObjectResult("Null or empty specification ids provided");
            }

            IEnumerable<Specification> specifications =
                await _specificationsRepository.GetSpecificationsByQuery(c => specificationIds.Contains(c.Id));

            foreach (Specification specification in specifications)
            {
                result.Add(_mapper.Map<SpecificationSummary>(specification));
            }

            return new OkObjectResult(result);
        }

        public async Task<IActionResult> GetSpecificationSummaries()
        {
            IEnumerable<Specification> specifications = await _specificationsRepository.GetSpecifications();

            if (specifications == null)
            {
                _logger.Warning($"No specifications were returned from the repository, result came back null");

                return new NotFoundResult();
            }

            List<SpecificationSummary> result =
                await _cacheProvider.GetAsync<List<SpecificationSummary>>(CacheKeys.SpecificationSummaries);
            if (result.IsNullOrEmpty())
            {
                result = new List<SpecificationSummary>();

                if (specifications.Any())
                {
                    foreach (Specification specification in specifications)
                    {
                        result.Add(_mapper.Map<SpecificationSummary>(specification));
                    }
                }

                await _cacheProvider.SetAsync(CacheKeys.SpecificationSummaries, result);
            }

            return new OkObjectResult(result);
        }

        public async Task<IActionResult> GetSpecificationsByFundingPeriodId(string fundingPeriodId)
        {
            if (string.IsNullOrWhiteSpace(fundingPeriodId))
            {
                _logger.Error("No funding period Id was provided to GetSpecificationByFundingPeriodId");

                return new BadRequestObjectResult("Null or empty fundingPeriodId provided");
            }

            string cacheKey = $"{CacheKeys.SpecificationSummariesByFundingPeriodId}{fundingPeriodId}";

            List<SpecificationSummary> result = await _cacheProvider.GetAsync<List<SpecificationSummary>>(cacheKey);
            if (result.IsNullOrEmpty())
            {
                IEnumerable<Specification> specifications =
                    await _specificationsRepository.GetSpecificationsByQuery(m =>
                        m.Content.Current.FundingPeriod.Id == fundingPeriodId);

                result = new List<SpecificationSummary>();

                if (!specifications.IsNullOrEmpty())
                {
                    foreach (Specification specification in specifications)
                    {
                        result.Add(_mapper.Map<SpecificationSummary>(specification));
                    }

                    await _cacheProvider.SetAsync(cacheKey, result, TimeSpan.FromHours(1), true);
                }
            }

            _logger.Information($"Found {result.Count} specifications for academic year with id {fundingPeriodId}");


            return new OkObjectResult(result);
        }

        public async Task<IActionResult> GetSpecificationWithResultsByFundingPeriodIdAndFundingStreamId(
            string fundingPeriodId, string fundingStreamId)
        {
            if (string.IsNullOrWhiteSpace(fundingPeriodId))
            {
                _logger.Error("No funding period Id was provided to GetSpecificationWithResultsByFundingPeriodIdAndFundingStreamId");

                return new BadRequestObjectResult("Null or empty fundingPeriodId provided");
            }
            if (string.IsNullOrWhiteSpace(fundingStreamId))
            {
                _logger.Error("No funding stream Id was provided to GetSpecificationWithResultsByFundingPeriodIdAndFundingStreamId");

                return new BadRequestObjectResult("Null or empty fundingStreamId provided");
            }

            SemaphoreSlim throttler = new SemaphoreSlim(initialCount: 15);

            IEnumerable<Specification> specifications =
                await _specificationsRepository.GetSpecificationsByFundingPeriodAndFundingStream(
                    fundingPeriodId, fundingStreamId);

            ConcurrentBag<SpecificationSummary> mappedSpecifications = new ConcurrentBag<SpecificationSummary>();
            IList<Task> checkForResulstsTasks = new List<Task>();

            foreach (Specification specification in specifications)
            {
                await throttler.WaitAsync();

                Task task = Task.Run(async () =>
                {
                    try
                    {
                        bool hasProviderResults = await _resultsRepository.SpecificationHasResults(specification.Id);

                        if (hasProviderResults)
                        {
                            mappedSpecifications.Add(_mapper.Map<SpecificationSummary>(specification));
                        }
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                    finally
                    {
                        throttler.Release();
                    }
                });

                checkForResulstsTasks.Add(task);
            }

            await Core.Helpers.TaskHelper.WhenAllAndThrow(checkForResulstsTasks.ToArray());

            IEnumerable<SpecificationSummary> specificationSummaries =
                _mapper.Map<IEnumerable<SpecificationSummary>>(mappedSpecifications);

            return new OkObjectResult(specificationSummaries);
        }

        public async Task<IActionResult> GetApprovedSpecificationsByFundingPeriodIdAndFundingStreamId(
    string fundingPeriodId, string fundingStreamId)
        {
            if (string.IsNullOrWhiteSpace(fundingPeriodId))
            {
                _logger.Error("No funding period Id was provided to GetSpecificationWithResultsByFundingPeriodIdAndFundingStreamId");

                return new BadRequestObjectResult("Null or empty fundingPeriodId provided");
            }
            if (string.IsNullOrWhiteSpace(fundingStreamId))
            {
                _logger.Error("No funding stream Id was provided to GetSpecificationWithResultsByFundingPeriodIdAndFundingStreamId");

                return new BadRequestObjectResult("Null or empty fundingStreamId provided");
            }

            IEnumerable<Specification> specifications =
                await _specificationsRepository.GetApprovedOrUpdatedSpecificationsByFundingPeriodAndFundingStream(
                    fundingPeriodId, fundingStreamId);

            IEnumerable<SpecificationSummary> specificationSummaries =
                _mapper.Map<IEnumerable<SpecificationSummary>>(specifications);

            return new OkObjectResult(specificationSummaries);
        }

        public async Task<IActionResult> GetSelectedSpecificationsByFundingPeriodIdAndFundingStreamId(string fundingPeriodId,
            string fundingStreamId)
        {
            if (string.IsNullOrWhiteSpace(fundingPeriodId))
            {
                _logger.Error("No funding period Id was provided to GetSpecificationWithResultsByFundingPeriodIdAndFundingStreamId");

                return new BadRequestObjectResult("Null or empty fundingPeriodId provided");
            }

            if (string.IsNullOrWhiteSpace(fundingStreamId))
            {
                _logger.Error("No funding stream Id was provided to GetSpecificationWithResultsByFundingPeriodIdAndFundingStreamId");

                return new BadRequestObjectResult("Null or empty fundingStreamId provided");
            }

            IEnumerable<Specification> specifications =
                await _specificationsRepository.GetSpecificationsSelectedForFundingByPeriodAndFundingStream(
                    fundingPeriodId,
                    fundingStreamId);

            IEnumerable<SpecificationSummary> specificationSummaries =
                _mapper.Map<IEnumerable<SpecificationSummary>>(specifications);

            return new OkObjectResult(specificationSummaries);
        }

        public async Task<IActionResult> GetCurrentSpecificationsByFundingPeriodIdAndFundingStreamId(
            string fundingPeriodId, string fundingStreamId)
        {
            if (string.IsNullOrWhiteSpace(fundingPeriodId))
            {
                _logger.Error("No funding period Id was provided to GetCurrentSpecificationsByFundingPeriodIdAndFundingStreamId");

                return new BadRequestObjectResult("Null or empty fundingPeriodId provided");
            }
            if (string.IsNullOrWhiteSpace(fundingStreamId))
            {
                _logger.Error("No funding stream Id was provided to GetCurrentSpecificationsByFundingPeriodIdAndFundingStreamId");

                return new BadRequestObjectResult("Null or empty fundingStreamId provided");
            }

            IEnumerable<Specification> specifications =
                await _specificationsRepository.GetSpecificationsByFundingPeriodAndFundingStream(
                    fundingPeriodId, fundingStreamId);

            IEnumerable<SpecificationSummary> specificationSummaries =
                _mapper.Map<IEnumerable<SpecificationSummary>>(specifications);

            return new OkObjectResult(specificationSummaries);
        }

        public async Task<IActionResult> GetSpecificationsSelectedForFunding()
        {
            IEnumerable<SpecificationSummary> specifications =
                (await _specificationsRepository.GetSpecificationsByQuery(c => c.Content.IsSelectedForFunding))
                .Select(s => _mapper.Map<SpecificationSummary>(s));

            return new OkObjectResult(specifications);
        }

        public async Task<IActionResult> GetSpecificationsSelectedForFundingByPeriod(string fundingPeriodId)
        {
            if (string.IsNullOrWhiteSpace(fundingPeriodId))
            {
                _logger.Error("No funding period was provided to GetSpecificationsSelectedForFundingPeriod");

                return new BadRequestObjectResult("Null or empty funding period provided");
            }

            IEnumerable<SpecificationSummary> specifications = (
                await _specificationsRepository.GetSpecificationsByQuery(c =>
                    c.Content.IsSelectedForFunding && c.Content.Current.FundingPeriod.Id == fundingPeriodId)
            ).Select(s => _mapper.Map<SpecificationSummary>(s));


            if (!specifications.Any())
            {
                _logger.Information($"Specification was not found for funding period: {fundingPeriodId}");

                return new NotFoundResult();
            }

            return new OkObjectResult(specifications);
        }

        public async Task<IActionResult> GetFundingStreamsSelectedForFundingBySpecification(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification id was provided to GetFundingStreamsSelectedForFundingBySpecification");

                return new BadRequestObjectResult("Null or empty specification id was provided");
            }

            IEnumerable<Reference> fundingStreams = (
                await _specificationsRepository.GetSpecificationsByQuery(c =>
                    c.Content.IsSelectedForFunding && c.Id == specificationId)
            ).Select(s => _mapper.Map<Reference>(s));

            return new OkObjectResult(fundingStreams);
        }

        public async Task<IActionResult> GetSpecificationByName(string specificationName)
        {
            if (string.IsNullOrWhiteSpace(specificationName))
            {
                _logger.Error("No specification name was provided to GetSpecificationByName");

                return new BadRequestObjectResult("Null or empty specification name provided");
            }

            IEnumerable<Specification> specifications =
                await _specificationsRepository.GetSpecificationsByQuery(m =>
                    m.Content.Name.ToLower() == specificationName.ToLower());

            if (!specifications.Any())
            {
                _logger.Information($"Specification was not found for name: {specificationName}");

                return new NotFoundResult();
            }

            _logger.Information($"Specification found for name: {specificationName}");

            return new OkObjectResult(specifications.FirstOrDefault());
        }

        public async Task<IActionResult> CreateSpecification(SpecificationCreateModel createModel, Reference user, string correlationId)
        {
            if (createModel == null)
            {
                return new BadRequestObjectResult("Null policy create model provided");
            }

            createModel.Name = createModel.Name?.Trim();

            BadRequestObjectResult validationResult =
                (await _specificationCreateModelValidator.ValidateAsync(createModel)).PopulateModelState();

            if (validationResult != null)
            {
                return validationResult;
            }

            ApiResponse<PolicyModels.FundingPeriod> fundingPeriodResponse =
                await _policiesApiClientPolicy.ExecuteAsync(() =>
                    _policiesApiClient.GetFundingPeriodById(createModel.FundingPeriodId));
            PolicyModels.FundingPeriod fundingPeriod = fundingPeriodResponse.Content;

            Specification specification = new Specification()
            {
                Name = createModel.Name,
                Id = Guid.NewGuid().ToString(),
            };

            SpecificationVersion specificationVersion = new SpecificationVersion
            {
                Name = createModel.Name,
                ProviderVersionId = createModel.ProviderVersionId,
                FundingPeriod = new Reference(fundingPeriod.Id, fundingPeriod.Name),
                Description = createModel.Description,
                DataDefinitionRelationshipIds = new List<string>(),
                Author = user,
                SpecificationId = specification.Id,
                Version = 1,
                Date = DateTimeOffset.Now.ToLocalTime(),
                CoreProviderVersionUpdates = createModel.CoreProviderVersionUpdates
            };

            List<Reference> fundingStreams = new List<Reference>();

            foreach (string fundingStreamId in createModel.FundingStreamIds)
            {
                ApiResponse<PolicyModels.FundingStream> fundingStreamResponse =
                    await _policiesApiClientPolicy.ExecuteAsync(() =>
                        _policiesApiClient.GetFundingStreamById(fundingStreamId));
                Reference fundingStream = _mapper.Map<Reference>(fundingStreamResponse?.Content);
                if (fundingStream == null)
                {
                    return new PreconditionFailedResult($"Unable to find funding stream with ID '{fundingStreamId}'.");
                }

                fundingStreams.Add(new Reference(fundingStream.Id, fundingStream.Name));
            }

            if (!fundingStreams.Any())
            {
                return new InternalServerErrorResult("No funding streams were retrieved to add to the Specification");
            }

            specificationVersion.FundingStreams = fundingStreams;

            specification.Current = specificationVersion;

            IDictionary<string, string> templateVersions = new Dictionary<string, string>();

            foreach (string fundingStreamId in specification.Current.FundingStreams.Select(m => m.Id))
            {
                ApiResponse<PolicyModels.FundingConfig.FundingConfiguration> fundingConfigResponse =
                    await _policiesApiClientPolicy.ExecuteAsync(() =>
                        _policiesApiClient.GetFundingConfiguration(fundingStreamId,
                            fundingPeriod.Id));

                if (!fundingConfigResponse.StatusCode.IsSuccess())
                {
                    return new InternalServerErrorResult(
                        $"No funding configuration returned for funding stream id '{fundingStreamId}' and funding period id '{specification.Current.FundingPeriod.Id}'");
                }

                PolicyModels.FundingConfig.FundingConfiguration fundingConfiguration = fundingConfigResponse.Content;

                if (createModel.AssignedTemplateIds?.ContainsKey(fundingStreamId) ?? false)
                {
                    ApiResponse<PolicyModels.FundingTemplateContents> publishedFundingTemplateResponse =
                        await _policiesApiClientPolicy.ExecuteAsync(() =>
                            _policiesApiClient.GetFundingTemplate(fundingStreamId,
                                fundingPeriod.Id, createModel.AssignedTemplateIds[fundingStreamId]));

                    if (!publishedFundingTemplateResponse.StatusCode.IsSuccess())
                    {
                        return new InternalServerErrorResult(
                            $"No published funding template returned for funding stream id '{fundingStreamId}' and funding period id " +
                            $"'{specification.Current.FundingPeriod.Id}' and template version '{createModel.AssignedTemplateIds[fundingStreamId]}'");
                    }

                    templateVersions.Add(fundingStreamId, createModel.AssignedTemplateIds[fundingStreamId]);
                }
                else
                {
                    if (!string.IsNullOrEmpty(fundingConfiguration?.DefaultTemplateVersion))
                    {
                        templateVersions.Add(fundingStreamId, fundingConfiguration.DefaultTemplateVersion);
                    }
                    else
                    {
                        ApiResponse<IEnumerable<PolicyModels.PublishedFundingTemplate>> publishedFundingTemplatesResponse =
                            await _policiesApiClientPolicy.ExecuteAsync(() =>
                                _policiesApiClient.GetFundingTemplates(fundingStreamId,
                                    specification.Current.FundingPeriod.Id));

                        if (!publishedFundingTemplatesResponse.StatusCode.IsSuccess())
                        {
                            return new InternalServerErrorResult(
                                $"No published funding template returned for funding stream id '{fundingStreamId}' and funding period id '{specification.Current.FundingPeriod.Id}'");
                        }

                        if (!publishedFundingTemplatesResponse.Content.Any())
                        {
                            return new InternalServerErrorResult(
                                $"Default Template Version is empty for funding stream id '{fundingStreamId}' and funding period id '{specification.Current.FundingPeriod.Id}'");
                        }

                        IEnumerable<string> templateVersionStrings = publishedFundingTemplatesResponse.Content.Select(_ => _.TemplateVersion);

                        List<decimal> templateVersionValues = new List<decimal>();
                        foreach (string templateVersionString in templateVersionStrings)
                        {
                            bool parsed = decimal.TryParse(templateVersionString, out var parsedTemplateVersion);

                            if (parsed)
                            {
                                templateVersionValues.Add(parsedTemplateVersion);
                            }
                        }

                        decimal templateVersion = templateVersionValues.Max();
                        templateVersions.Add(fundingStreamId, templateVersion.ToString());
                    }
                }

                if (specification.Current.FundingStreams.FirstOrDefault()?.Id == fundingStreamId)
                {
                    specificationVersion.ProviderSource = fundingConfiguration.ProviderSource switch
                    {
                        ProviderSource.CFS => Models.Providers.ProviderSource.CFS,
                        ProviderSource.FDZ => Models.Providers.ProviderSource.FDZ,
                        _ => throw new ArgumentOutOfRangeException(
                            nameof(fundingConfiguration.ProviderSource),
                            $"Provider Source of Funding config: {fundingConfiguration.ProviderSource} is not allowed")
                    };

                    if (fundingConfiguration.ProviderSource == ProviderSource.FDZ)
                    {
                        if (createModel.CoreProviderVersionUpdates == CoreProviderVersionUpdates.Manual && !createModel.ProviderSnapshotId.HasValue)
                        {
                            return new PreconditionFailedResult(
                                $"{nameof(createModel.ProviderSnapshotId)} not set when provider source is FDZ for " +
                                $"funding stream ID '{fundingStreamId}' and funding period ID '{createModel.FundingPeriodId}'.");
                        }

                        specificationVersion.ProviderSnapshotId = createModel.ProviderSnapshotId;
                    }
                }
            }

            specification.Current = specificationVersion = await _specificationVersionRepository.CreateVersion(specificationVersion);

            List<Task> tasks = new List<Task>();

            foreach (string fundingStreamId in specification.Current.FundingStreams.Select(m => m.Id))
            {
                if (templateVersions.ContainsKey(fundingStreamId))
                {
                    specificationVersion.AddOrUpdateTemplateId(fundingStreamId, templateVersions[fundingStreamId]);
                    tasks.Add(_calcsApiClientPolicy.ExecuteAsync(() =>
                        _calcsApiClient.ProcessTemplateMappings(specification.Id,
                            templateVersions[fundingStreamId], fundingStreamId)));
                }
            }

            DocumentEntity<Specification> repositoryCreateResult =
                await _specificationsRepository.CreateSpecification(specification);

            if (repositoryCreateResult == null)
            {
                return new InternalServerErrorResult("Error creating specification in repository");
            }

            await _specificationVersionRepository.SaveVersion(specificationVersion);

            await TaskHelper.WhenAllAndThrow(tasks.ToArraySafe());

            await _specificationIndexer.Index(specification);

            await ClearSpecificationCacheItems(specificationVersion.FundingPeriod.Id);

            await _queueCreateSpecificationJobAction.Run(specificationVersion, user, correlationId);

            SpecificationSummary specificationSummary = _mapper.Map<SpecificationSummary>(specification);

            return new OkObjectResult(specificationSummary);
        }

        public async Task<IActionResult> EditSpecification(
            string specificationId,
            SpecificationEditModel editModel,
            Reference user,
            string correlationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification Id was provided to EditSpecification");
                return new BadRequestObjectResult("Null or empty specification Id provided");
            }

            if (editModel == null)
            {
                _logger.Error("No edit modeld was provided to EditSpecification");
                return new BadRequestObjectResult("Null edit specification model provided");
            }

            if (await IsAnySpecificationJobRunning(specificationId))
            {
                return new BadRequestObjectResult("Specification cannot be edited while specification jobs are running");
            }

            editModel.Name = editModel.Name?.Trim();
            editModel.SpecificationId = specificationId;

            BadRequestObjectResult validationResult =
                (await _specificationEditModelValidator.ValidateAsync(editModel)).PopulateModelState();
            if (validationResult != null)
            {
                return validationResult;
            }

            Specification specification = await _specificationsRepository.GetSpecificationById(specificationId);
            if (specification == null)
            {
                _logger.Warning($"Failed to find specification for id: {specificationId}");
                return new NotFoundObjectResult("Specification not found");
            }

            SpecificationVersion previousSpecificationVersion = specification.Current;

            if (previousSpecificationVersion.ProviderVersionId != editModel.ProviderVersionId)
            {
                ApiResponse<bool> refreshCacheFromApi = await _providersApiClientPolicy.ExecuteAsync(() =>
                                 _providersApiClient.RegenerateProviderSummariesForSpecification(specificationId, true));

                if (!refreshCacheFromApi.StatusCode.IsSuccess())
                {
                    string errorMessage = $"Unable to re-generate scoped providers while editing specification '{specificationId}' with status code: {refreshCacheFromApi.StatusCode}";

                    _logger.Information(errorMessage);

                    throw new RetriableException(errorMessage);
                }
            }

            SpecificationVersion specificationVersion = specification.Current.DeepCopy(useCamelCase: false);

            specificationVersion.ProviderVersionId = editModel.ProviderVersionId;
            specificationVersion.Name = editModel.Name;
            specificationVersion.Description = editModel.Description;
            specificationVersion.Author = user;
            specificationVersion.SpecificationId = specificationId;
            specificationVersion.CoreProviderVersionUpdates = editModel.CoreProviderVersionUpdates;

            specification.Name = editModel.Name;

            string previousFundingPeriodId = specificationVersion.FundingPeriod.Id;

            if (editModel.FundingPeriodId != specificationVersion.FundingPeriod.Id)
            {
                ApiResponse<PolicyModels.FundingPeriod> fundingPeriodResponse =
                    await _policiesApiClientPolicy.ExecuteAsync(() =>
                        _policiesApiClient.GetFundingPeriodById(editModel.FundingPeriodId));
                PolicyModels.FundingPeriod fundingPeriod = fundingPeriodResponse?.Content;
                if (fundingPeriod == null)
                {
                    return new PreconditionFailedResult(
                        $"Unable to find funding period with ID '{editModel.FundingPeriodId}'.");
                }

                specificationVersion.FundingPeriod = new Reference { Id = fundingPeriod.Id, Name = fundingPeriod.Period };
            }

            string fundingStreamId = specification.Current.FundingStreams.FirstOrDefault().Id;

            ApiResponse<PolicyModels.FundingConfig.FundingConfiguration> fundingConfigResponse =
                await _policiesApiClientPolicy.ExecuteAsync(() =>
                    _policiesApiClient.GetFundingConfiguration(fundingStreamId,
                        editModel.FundingPeriodId));

            if (!fundingConfigResponse.StatusCode.IsSuccess())
            {
                return new InternalServerErrorResult(
                    $"No funding configuration returned for funding stream id " +
                    $"'{fundingStreamId}' and funding period id '{specification.Current.FundingPeriod.Id}'");
            }

            PolicyModels.FundingConfig.FundingConfiguration fundingConfiguration = fundingConfigResponse.Content;

            specificationVersion.ProviderSource = fundingConfiguration.ProviderSource switch
            {
                ProviderSource.CFS => Models.Providers.ProviderSource.CFS,
                ProviderSource.FDZ => Models.Providers.ProviderSource.FDZ,
                _ => throw new ArgumentOutOfRangeException(
                            nameof(fundingConfiguration.ProviderSource),
                            $"Provider Source of Funding config: {fundingConfiguration.ProviderSource} is not allowed")
            };

            if (fundingConfiguration.ProviderSource == ProviderSource.FDZ)
            {
                if (editModel.CoreProviderVersionUpdates == CoreProviderVersionUpdates.Manual && !editModel.ProviderSnapshotId.HasValue)
                {
                    return new PreconditionFailedResult(
                        $"{nameof(editModel.ProviderSnapshotId)} not set when provider source is FDZ for " +
                        $"funding stream ID '{fundingStreamId}' and fundint period ID '{editModel.FundingPeriodId}'.");
                }

                specificationVersion.ProviderSnapshotId = editModel.ProviderSnapshotId;
            }

            bool triggerProviderSnapshotDataLoadJob = false;
            if (specification.Current.CoreProviderVersionUpdates == CoreProviderVersionUpdates.Manual
                    && editModel.CoreProviderVersionUpdates == CoreProviderVersionUpdates.UseLatest)
            {
                ApiResponse<CurrentProviderVersionMetadata> providerMetadataResponse = await _providersApiClientPolicy.ExecuteAsync(
                    () => _providersApiClient.GetCurrentProviderMetadataForFundingStream(fundingStreamId));

                if (providerMetadataResponse.StatusCode.IsSuccess() && providerMetadataResponse.Content != null)
                {
                    specificationVersion.ProviderSnapshotId = providerMetadataResponse.Content.ProviderSnapshotId;
                    triggerProviderSnapshotDataLoadJob = true;
                }
                else
                {
                    return new InternalServerErrorResult($"No current provider metadata returned for funding stream id '{fundingStreamId}'.");
                }
            }

            await _templateVersionChangedHandler.HandleTemplateVersionChanged(
                previousSpecificationVersion,
                specificationVersion,
                editModel.AssignedTemplateIds,
                user,
                correlationId);

            HttpStatusCode statusCode =
                await UpdateSpecification(specification, specificationVersion, previousSpecificationVersion);

            if (!statusCode.IsSuccess())
            {
                return new StatusCodeResult((int)statusCode);
            }

            await TaskHelper.WhenAllAndThrow(ReindexSpecification(specification),
                ClearSpecificationCacheItems(specificationVersion.FundingPeriod.Id));

            if (previousFundingPeriodId != specificationVersion.FundingPeriod.Id)
            {
                await _cacheProvider.RemoveAsync<List<SpecificationSummary>>(
                    $"{CacheKeys.SpecificationSummariesByFundingPeriodId}{previousFundingPeriodId}");
            }

            await SendSpecificationComparisonModelMessageToTopic(specificationId,
                ServiceBusConstants.TopicNames.EditSpecification, specification.Current, previousSpecificationVersion,
                user, correlationId);

            bool triggerCalculationEngineRunJob = fundingConfiguration.RunCalculationEngineAfterCoreProviderUpdate;

            await _queueEditSpecificationJobActions.Run(
                specificationVersion, 
                user, 
                correlationId, 
                triggerProviderSnapshotDataLoadJob, 
                triggerCalculationEngineRunJob);

            return new OkObjectResult(specification);
        }

        private async Task<bool> IsAnySpecificationJobRunning(string specificationId)
        {
            List<string> specificationJobTypes = new List<string>
            {
                JobConstants.DefinitionNames.RefreshFundingJob,
                JobConstants.DefinitionNames.ApproveAllProviderFundingJob,
                JobConstants.DefinitionNames.ApproveBatchProviderFundingJob,
                JobConstants.DefinitionNames.PublishAllProviderFundingJob,
                JobConstants.DefinitionNames.PublishBatchProviderFundingJob
            };

            IEnumerable<JobSummary> latestSpecificationJobs =
                await _jobManagement.GetLatestJobsForSpecification(specificationId, specificationJobTypes);

            return latestSpecificationJobs?.Any(job => job != null && job.RunningStatus == RunningStatus.InProgress) == true;
        }

        private async Task ReindexSpecification(Specification specification)
        {
            await _specificationIndexer.Index(specification);
        }

        public async Task<IActionResult> EditSpecificationStatus(string specificationId, EditStatusModel editStatusModel, Reference user)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification Id was provided to EditSpecification");
                return new BadRequestObjectResult("Null or empty specification Id provided");
            }

            try
            {
                if (editStatusModel == null)
                {
                    _logger.Error("A null status model was provided");
                    return new BadRequestObjectResult("Null status model provided");
                }
            }
            catch (JsonSerializationException jse)
            {
                _logger.Error(jse, $"An invalid status was provided for specification: {specificationId}");

                return new BadRequestObjectResult("An invalid status was provided");
            }

            Specification specification = await _specificationsRepository.GetSpecificationById(specificationId);
            if (specification == null)
            {
                _logger.Warning($"Failed to find specification for id: {specificationId}");
                return new NotFoundObjectResult("Specification not found");
            }

            if (specification.Current.PublishStatus == editStatusModel.PublishStatus)
            {
                return new OkObjectResult(specification);
            }

            if ((specification.Current.PublishStatus == PublishStatus.Approved ||
                 specification.Current.PublishStatus == PublishStatus.Updated) &&
                editStatusModel.PublishStatus == PublishStatus.Draft)
            {
                return new BadRequestObjectResult("Publish status can't be changed to Draft from Updated or Approved");
            }

            Models.Specs.SpecificationVersion previousSpecificationVersion = specification.Current;

            Models.Specs.SpecificationVersion specificationVersion =
                specification.Current.Clone() as Models.Specs.SpecificationVersion;

            HttpStatusCode statusCode;

            if (editStatusModel.PublishStatus == PublishStatus.Approved)
            {
                specificationVersion.PublishStatus = PublishStatus.Approved;

                statusCode =
                    await PublishSpecification(specification, specificationVersion, previousSpecificationVersion);
            }
            else
            {
                specificationVersion.PublishStatus = editStatusModel.PublishStatus;

                statusCode =
                    await UpdateSpecification(specification, specificationVersion, previousSpecificationVersion);
            }

            if (!statusCode.IsSuccess())
            {
                return new StatusCodeResult((int)statusCode);
            }

            await ReindexSpecification(specification);

            await TaskHelper.WhenAllAndThrow(
                ClearSpecificationCacheItems(specificationVersion.FundingPeriod.Id),
                _cacheProvider.RemoveAsync<SpecificationSummary>(
                    $"{CacheKeys.SpecificationSummaryById}{specification.Id}")
            );

            PublishStatusResultModel result = new PublishStatusResultModel()
            {
                PublishStatus = specification.Current.PublishStatus,
            };

            return new OkObjectResult(result);
        }

        private async Task SendSpecificationComparisonModelMessageToTopic(string specificationId, string topicName,
            SpecificationVersion current, SpecificationVersion previous, Reference user, string correlationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.ArgumentNotNull(current, nameof(current));
            Guard.ArgumentNotNull(previous, nameof(previous));

            IDictionary<string, string> properties = MessageExtensions.BuildMessageProperties(correlationId, user);

            Models.Messages.SpecificationVersion currentSpecVersion =
                _mapper.Map<Models.Messages.SpecificationVersion>(current);
            Models.Messages.SpecificationVersion previousSpecVersion =
                _mapper.Map<Models.Messages.SpecificationVersion>(previous);

            SpecificationVersionComparisonModel comparisonModel = new SpecificationVersionComparisonModel
            {
                Id = specificationId,
                Current = currentSpecVersion,
                Previous = previousSpecVersion
            };

            await _messengerService.SendToTopic(topicName, comparisonModel, properties, true);
        }

        public override async Task Process(Message message)
        {
            AssignDefinitionRelationshipMessage relationshipMessage =
                message.GetPayloadAsInstanceOf<AssignDefinitionRelationshipMessage>();

            if (relationshipMessage == null)
            {
                _logger.Error("A null relationship message was provided to AssignDataDefinitionRelationship");

                throw new ArgumentNullException(nameof(relationshipMessage));
            }
            else
            {
                FluentValidation.Results.ValidationResult validationResult =
                    await _assignDefinitionRelationshipMessageValidator.ValidateAsync(relationshipMessage);

                if (!validationResult.IsValid)
                {
                    throw new InvalidModelException(GetType().ToString(),
                        validationResult.Errors.Select(m => m.ErrorMessage).ToArraySafe());
                }

                string specificationId = relationshipMessage.SpecificationId;

                string relationshipId = relationshipMessage.RelationshipId;

                Specification specification = await _specificationsRepository.GetSpecificationById(specificationId);
                if (specification == null)
                {
                    throw new InvalidModelException(relationshipMessage.GetType().ToString(),
                        new[] { $"Specification could not be found for id {specificationId}" });
                }

                Models.Specs.SpecificationVersion previousSpecificationVersion = specification.Current;

                Models.Specs.SpecificationVersion specificationVersion =
                    specification.Current.Clone() as Models.Specs.SpecificationVersion;

                if (specificationVersion.DataDefinitionRelationshipIds.IsNullOrEmpty())
                {
                    specificationVersion.DataDefinitionRelationshipIds = new string[0];
                }

                if (!specificationVersion.DataDefinitionRelationshipIds.Contains(relationshipId))
                {
                    specificationVersion.DataDefinitionRelationshipIds =
                        specificationVersion.DataDefinitionRelationshipIds.Concat(new[] { relationshipId });
                }

                HttpStatusCode status =
                    await UpdateSpecification(specification, specificationVersion, previousSpecificationVersion);

                if (!status.IsSuccess())
                {
                    _logger.Error(
                        $"Failed to update specification for id: {specificationId} with dataset definition relationship id {relationshipId}");

                    throw new Exception(
                        $"Failed to update specification for id: {specificationId} with dataset definition relationship id {relationshipId}");
                }

                await _specificationIndexer.Index(specification);

                _logger.Information(
                    $"Successfully assigned relationship id: {relationshipId} to specification with id: {specificationId}");
            }
        }

        public async Task<IActionResult> ReIndex()
        {
            try
            {
                CosmosDbQuery cosmosDbQuery = new CosmosDbQuery
                {
                    QueryText = @"
SELECT  s.id, 
        s.content.current.name, 
        s.content.current.fundingStreams,
        s.content.current.fundingPeriod,
        s.content.current.publishStatus, 
        s.content.current.description, 
        s.content.current.dataDefinitionRelationshipIds,
        s.content.isSelectedForFunding,
        s.content.publishedResultsRefreshedAt, 
        s.updatedAt
FROM    specs s 
WHERE   s.documentType = @DocumentType",
                    Parameters = new[]
                    {
                        new CosmosDbQueryParameter("@DocumentType", "Specification")
                    }
                };

                IEnumerable<SpecificationSearchModel> specifications =
                    (await _specificationsRepository.GetSpecificationsByRawQuery<SpecificationSearchModel>(
                        cosmosDbQuery)).ToArraySafe();

                if (specifications.Any())
                {
                    await _specificationIndexer.Index(specifications);

                    _logger.Information($"Successfully re-indexed {specifications.Count()} documents");
                }
                else
                {
                    _logger.Warning("No specification documents were returned from cosmos db");
                }

                return new NoContentResult();
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Failed re-indexing specifications");

                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> DeselectSpecificationForFunding(string specificationId)
        {
            if (_featureToggle.IsDeletePublishedProviderForbidden())
            {
                return new ForbidResult();
            }

            if (specificationId.IsNullOrEmpty())
            {
                _logger.Warning("No specification Id was provided to DeselectSpecificationForFunding");

                return new BadRequestObjectResult("Null or empty specification Id provided");
            }

            Specification specification = await _specificationsRepository.GetSpecificationById(specificationId);

            if (specification == null)
            {
                _logger.Warning($"Specification not found for id: {specificationId}");

                return new NotFoundObjectResult($"Specification not found for id: {specificationId}");
            }

            if (specification.IsSelectedForFunding == false)
            {
                _logger.Warning(
                    $"Attempt to deselect specification with id: {specificationId} selected when not yet selected");

                return new NoContentResult();
            }

            specification.IsSelectedForFunding = false;

            return await UpdateSpecification(specification, specificationId);
        }

        public async Task<IActionResult> SelectSpecificationForFunding(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Warning("No specification Id was provided to SelectSpecificationForFunding");
                return new BadRequestObjectResult("Null or empty specification Id provided");
            }

            Specification specification = await _specificationsRepository.GetSpecificationById(specificationId);

            if (specification == null)
            {
                _logger.Warning($"Specification not found for id: {specificationId}");

                return new NotFoundObjectResult($"Specification not found for id: {specificationId}");
            }

            if (specification.IsSelectedForFunding)
            {
                _logger.Warning(
                    $"Attempt to mark specification with id: {specificationId} selected when already selected");

                return new NoContentResult();
            }

            if (await SharesFundingStreamWithAnyOtherSpecificationSelectedForFundingInTheSamePeriod(specification))
            {
                return new ConflictResult();
            }

            specification.IsSelectedForFunding = true;

            return await UpdateSpecification(specification, specificationId);
        }

        private async Task<IActionResult> UpdateSpecification(Specification specification, string specificationId)
        {
            try
            {
                HttpStatusCode statusCode = await _specificationsRepository.UpdateSpecification(specification);

                if (!statusCode.IsSuccess())
                {
                    string error =
                        $"Failed to set IsSelectedForFunding on specification for id: {specificationId} with status code: {statusCode}";
                    _logger.Error(error);
                    return new InternalServerErrorResult(error);
                }

                await _specificationIndexer.Index(specification);

                await _cacheProvider.RemoveAsync<SpecificationSummary>(
                    $"{CacheKeys.SpecificationSummaryById}{specification.Id}");
            }
            catch (Exception ex)
            {
                specification.IsSelectedForFunding = false;

                await TaskHelper.WhenAllAndThrow(
                    _specificationsRepository.UpdateSpecification(specification),
                     _specificationIndexer.Index(specification)
                );

                await _cacheProvider.RemoveAsync<SpecificationSummary>(
                    $"{CacheKeys.SpecificationSummaryById}{specification.Id}");

                _logger.Error(ex, ex.Message);

                return new InternalServerErrorResult(ex.Message);
            }

            return new NoContentResult();
        }

        private async Task<bool> SharesFundingStreamWithAnyOtherSpecificationSelectedForFundingInTheSamePeriod(
            Specification specification)
        {
            string fundingPeriodId = specification.Current?.FundingPeriod?.Id;

            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId),
                $"Specification {specification.Id} has no funding period id");

            IEnumerable<Specification> specificationsInFundingPeriod =
                await _specificationsRepository.GetSpecificationsSelectedForFundingByPeriod(fundingPeriodId);

            if (specificationsInFundingPeriod == null)
            {
                _logger.Warning(
                    $"Specifications selected for publishing in funding period {fundingPeriodId} returned null");

                return false;
            }

            return AnySpecificationsInThisPeriodShareFundingStreams(specificationsInFundingPeriod,
                specification.Current.FundingStreams?.Select(_ => _.Id) ?? new string[0]);
        }

        private static bool AnySpecificationsInThisPeriodShareFundingStreams(
            IEnumerable<Specification> specificationsInFundingPeriod,
            IEnumerable<string> fundingStreams)
        {
            return specificationsInFundingPeriod.Any(_ =>
                fundingStreams.Intersect(_.Current?.FundingStreams?.Select(fs => fs.Id) ?? ArraySegment<string>.Empty).Any());
        }

        private async Task<HttpStatusCode> UpdateSpecification(Specification specification,
            Models.Specs.SpecificationVersion specificationVersion, Models.Specs.SpecificationVersion previousVersion)
        {
            specificationVersion =
                await _specificationVersionRepository.CreateVersion(specificationVersion, previousVersion);

            specification.Current = specificationVersion;

            HttpStatusCode result = await _specificationsRepository.UpdateSpecification(specification);

            if (result == HttpStatusCode.OK)
            {
                await _specificationVersionRepository.SaveVersion(specificationVersion);

                await _cacheProvider.RemoveAsync<SpecificationSummary>(
                    $"{CacheKeys.SpecificationSummaryById}{specification.Id}");
            }

            return result;
        }

        private async Task<HttpStatusCode> PublishSpecification(Specification specification,
            Models.Specs.SpecificationVersion specificationVersion, Models.Specs.SpecificationVersion previousVersion)
        {
            specificationVersion =
                await _specificationVersionRepository.CreateVersion(specificationVersion, previousVersion);

            specification.Current = specificationVersion;

            HttpStatusCode result = await _specificationsRepository.UpdateSpecification(specification);
            if (result == HttpStatusCode.OK)
            {
                await _specificationVersionRepository.SaveVersion(specificationVersion);

                await _cacheProvider.RemoveAsync<SpecificationSummary>(
                    $"{CacheKeys.SpecificationSummaryById}{specification.Id}");
            }

            return result;
        }

        private async Task ClearSpecificationCacheItems(string fundingPeriodId)
        {
            await TaskHelper.WhenAllAndThrow(
                _cacheProvider.RemoveAsync<List<SpecificationSummary>>(CacheKeys.SpecificationSummaries),
                _cacheProvider.RemoveAsync<List<SpecificationSummary>>(
                    $"{CacheKeys.SpecificationSummariesByFundingPeriodId}{fundingPeriodId}")
            );
        }

        public async Task<IActionResult> GetPublishDates(string specificationId)
        {
            Guard.ArgumentNotNull(specificationId, nameof(specificationId));

            Specification specification = await _specificationsRepository.GetSpecificationById(specificationId);

            if (specification == null)
            {
                string message =
                    $"No specification ID {specificationId} were returned from the repository, result came back null";
                _logger.Error(message);
                return new PreconditionFailedResult(message);
            }

            Models.Specs.SpecificationVersion specificationVersion = specification.Current;

            if (specificationVersion == null)
            {
                string message =
                    $"Specification ID {specificationId} does not contains current for given specification";
                _logger.Error(message);
                return new PreconditionFailedResult(message);
            }

            return new OkObjectResult(new SpecificationPublishDateModel()
            {
                ExternalPublicationDate = specificationVersion.ExternalPublicationDate,
                EarliestPaymentAvailableDate = specificationVersion.EarliestPaymentAvailableDate
            });
        }

        public async Task<IActionResult> GetProfileVariationPointers(string specificationId)
        {
            Guard.ArgumentNotNull(specificationId, nameof(specificationId));

            Specification specification = await _specificationsRepository.GetSpecificationById(specificationId);

            if (specification == null)
            {
                string message = $"No specification ID {specificationId} were returned from the repository, result came back null";
                _logger.Error(message);
                return new PreconditionFailedResult(message);
            }

            Models.Specs.SpecificationVersion specificationVersion = specification.Current;

            if (specificationVersion == null)
            {
                string message = $"Specification ID {specificationId} does not contains current for given specification";
                _logger.Error(message);
                return new PreconditionFailedResult(message);
            }

            return new OkObjectResult(specificationVersion.ProfileVariationPointers?.Select(_ => new SpecificationProfileVariationPointerModel
            {
                FundingLineId = _.FundingLineId,
                FundingStreamId = _.FundingStreamId,
                Occurrence = _.Occurrence,
                PeriodType = _.PeriodType,
                TypeValue = _.TypeValue,
                Year = _.Year
            }));
        }

        public async Task<IActionResult> SetPublishDates(string specificationId,
            SpecificationPublishDateModel specificationPublishDateModel)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.ArgumentNotNull(specificationPublishDateModel, nameof(specificationPublishDateModel));

            Specification specification = await _specificationsRepository.GetSpecificationById(specificationId);

            if (specification == null)
            {
                string message =
                    $"No specification ID {specificationId} were returned from the repository, result came back null";
                _logger.Error(message);
                return new PreconditionFailedResult(message);
            }

            Models.Specs.SpecificationVersion currentSpecificationVersion = specification.Current;
            Models.Specs.SpecificationVersion newSpecificationVersion =
                specification.Current.Clone() as Models.Specs.SpecificationVersion;

            newSpecificationVersion.Version++;
            newSpecificationVersion.ExternalPublicationDate = specificationPublishDateModel.ExternalPublicationDate.TrimToTheMinute();
            newSpecificationVersion.EarliestPaymentAvailableDate =
                specificationPublishDateModel.EarliestPaymentAvailableDate.TrimToTheMinute();

            HttpStatusCode updateSpecificationResult =
                await UpdateSpecification(specification, newSpecificationVersion, currentSpecificationVersion);

            if (!updateSpecificationResult.IsSuccess())
            {
                string message =
                    $"Failed to update specification for id: {specificationId} with ExternalPublishDate {specificationPublishDateModel.ExternalPublicationDate} " +
                    $"and EarliestPaymentAvailableDate {specificationPublishDateModel.EarliestPaymentAvailableDate}";
                _logger.Error(message);

                return new InternalServerErrorResult(message);
            }

            return new OkObjectResult(updateSpecificationResult);
        }

        public async Task<IActionResult> SetProfileVariationPointers(string specificationId,
            IEnumerable<SpecificationProfileVariationPointerModel> specificationProfileVariationPointerModels,
            bool merge = false)
        {
            Guard.ArgumentNotNull(specificationId, nameof(specificationId));
            Guard.ArgumentNotNull(specificationProfileVariationPointerModels, nameof(specificationProfileVariationPointerModels));

            Specification specification = await _specificationsRepository.GetSpecificationById(specificationId);

            if (specification == null)
            {
                string message = $"No specification ID {specificationId} were returned from the repository, result came back null";
                _logger.Error(message);
                return new PreconditionFailedResult(message);
            }

            Models.Specs.SpecificationVersion currentSpecificationVersion = specification.Current;
            Models.Specs.SpecificationVersion newSpecificationVersion = specification.Current.Clone() as Models.Specs.SpecificationVersion;

            IEnumerable<ProfileVariationPointer> profileVariationPointers = specificationProfileVariationPointerModels.Select(_ => new ProfileVariationPointer
            {
                FundingLineId = _.FundingLineId,
                FundingStreamId = _.FundingStreamId,
                Occurrence = _.Occurrence,
                PeriodType = _.PeriodType,
                TypeValue = _.TypeValue,
                Year = _.Year
            });

            if (merge && !newSpecificationVersion.ProfileVariationPointers.IsNullOrEmpty())
            {
                profileVariationPointers = newSpecificationVersion.ProfileVariationPointers
                    .Concat(
                        profileVariationPointers
                            .Where(_ =>
                                !newSpecificationVersion.ProfileVariationPointers.Any(pvp => pvp.FundingLineId == _.FundingLineId && pvp.FundingStreamId == _.FundingStreamId))).ToList();
            }

            newSpecificationVersion.Version++;
            newSpecificationVersion.ProfileVariationPointers = profileVariationPointers;

            HttpStatusCode updateSpecificationResult = await UpdateSpecification(specification, newSpecificationVersion, currentSpecificationVersion);

            if (!updateSpecificationResult.IsSuccess())
            {
                string message = $"Failed to update specification for id: {specificationId} with ProfileVariationPointers {profileVariationPointers?.AsJson()}";
                _logger.Error(message);

                return new InternalServerErrorResult(message);
            }

            return new OkObjectResult(updateSpecificationResult);
        }

        public async Task<IActionResult> SetProfileVariationPointer(string specificationId,
            SpecificationProfileVariationPointerModel specificationProfileVariationPointerModel)
        {
            return await SetProfileVariationPointers(specificationId, new[] { specificationProfileVariationPointerModel }, true);
        }

        public async Task<IActionResult> GetFundingStreamIdsForSelectedFundingSpecifications()
        {
            IEnumerable<Specification> specificationsByQuery =
                await _specificationsRepository.GetSpecificationsByQuery(c => c.Content.IsSelectedForFunding);

            IEnumerable<string> fundingStreamIds = specificationsByQuery
                .Where(specification => specification.IsSelectedForFunding)
                .SelectMany(specification => specification.Current.FundingStreams
                    .Select(fundingStream => fundingStream.Id))
                .Distinct();

            return new OkObjectResult(fundingStreamIds);
        }

        public async Task<IActionResult> GetFundingPeriodsByFundingStreamIdsForSelectedSpecifications(
            string fundingStreamId)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));

            // Get Specification which are selected for funding, and filter by FundingStreamId
            IEnumerable<Specification> specificationsByQuery =
                await _specificationsRepository.GetSpecificationsByQuery(c =>
                    c.Content.IsSelectedForFunding &&
                    c.Content.Current.FundingStreams.Any(f => f.Id == fundingStreamId));

            IEnumerable<string> specificationsFundingPeriodIds = specificationsByQuery
                .Select(specificationSummary => specificationSummary.Current.FundingPeriod.Id)
                .Distinct()
                .ToArray();

            // Get all FundingPeriods from policy service to get the latest name
            ApiResponse<IEnumerable<PolicyModels.FundingPeriod>> fundingPeriodResponse =
                await _policiesApiClient.GetFundingPeriods();
            IEnumerable<PolicyModels.FundingPeriod> fundingPeriods = fundingPeriodResponse.Content;

            // Only return periods where there are matching specifications
            IEnumerable<Reference> filteredFundingPeriodsByIdAndPeriod = fundingPeriods
                .Where(c => specificationsFundingPeriodIds.Contains(c.Id))
                .Select(fundingPeriod => new Reference
                {
                    Id = fundingPeriod.Id,
                    Name = fundingPeriod.Name
                })
                .OrderBy(f => f.Name);

            return new OkObjectResult(filteredFundingPeriodsByIdAndPeriod);
        }

        public async Task<IActionResult> SoftDeleteSpecificationById(string specificationId, Reference user, string correlationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification id was provided to soft delete specification");

                return new BadRequestObjectResult("Null or empty specification id provided");
            }

            await _queueDeleteSpecificationJobAction.Run(specificationId, user, correlationId,
                DeletionType.SoftDelete);

            return new OkObjectResult(true);
        }

        public async Task<IActionResult> PermanentDeleteSpecificationById(string specificationId, Reference user, string correlationId, bool allowDelete = false)
        {
            if (!allowDelete)
            {
                return new BadRequestObjectResult("Requested endpoint cannot be executed in the current environment");
            }

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification id was provided to soft delete specification");

                return new BadRequestObjectResult("Null or empty specification id provided");
            }

            await _queueDeleteSpecificationJobAction.Run(specificationId, user, correlationId,
                DeletionType.PermanentDelete);

            return new OkObjectResult(true);
        }

        public async Task DeleteSpecification(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            string specificationId = message.UserProperties["specification-id"].ToString();
            if (string.IsNullOrEmpty(specificationId))
            {
                string error = "Null or empty specification Id provided";
                _logger.Error(error);
                throw new Exception(error);
            }

            string deletionTypeProperty = message.UserProperties["deletion-type"].ToString();
            if (string.IsNullOrEmpty(deletionTypeProperty))
            {
                string error = "Null or empty deletion type provided";
                _logger.Error(error);
                throw new Exception(error);
            }

            DeletionType deletionType = deletionTypeProperty.ToDeletionType();

            await _specificationsRepository.DeleteSpecifications(specificationId, deletionType);
        }

        private async Task TrackJobFailed(string jobId, Exception exception)
        {
            await AddJobTracking(jobId, new JobLogUpdateModel
            {
                CompletedSuccessfully = false,
                Outcome = exception.ToString()
            });
        }

        private async Task AddJobTracking(string jobId, JobLogUpdateModel tracking)
        {
            await _jobManagement.AddJobLog(jobId, tracking);
        }


        public async Task<IActionResult> GetDistinctFundingStreamsForSpecifications()
        {
            IEnumerable<string> fundingStreamIds =
                await _specificationsRepository.GetDistinctFundingStreamsForSpecifications();

            return new OkObjectResult(fundingStreamIds);
        }

        public async Task<IActionResult> SetProviderVersion(AssignSpecificationProviderVersionModel assignSpecificationProviderVersionModel, Reference user)
        {
            ValidationResult validationResult = await _assignSpecificationProviderVersionModelValidator.ValidateAsync(assignSpecificationProviderVersionModel);

            if (!validationResult.IsValid)
            {
                if (validationResult.Errors.Any(x => x.PropertyName == "SpecificationId" && x.ErrorMessage.Contains("Specification not found for SpecificationId")))
                {
                    return new NotFoundObjectResult("Specification not found");
                }

                return validationResult.PopulateModelState();
            }

            Specification specification = await _specificationsRepository.GetSpecificationById(assignSpecificationProviderVersionModel.SpecificationId);
            SpecificationVersion previousSpecificationVersion = specification.Current;
            SpecificationVersion specificationVersion = specification.Current.DeepCopy(useCamelCase: false);

            specificationVersion.ProviderVersionId = assignSpecificationProviderVersionModel.ProviderVersionId;
            specificationVersion.Author = user;

            HttpStatusCode statusCode = await UpdateSpecification(specification, specificationVersion, previousSpecificationVersion);

            return statusCode.IsSuccess() ? new OkResult() : new StatusCodeResult((int)statusCode);
        }

        public async Task<IActionResult> GetSpecificationsWithProviderVersionUpdatesAsUseLatest()
        {
            IEnumerable<SpecificationSummary> specifications = (
                await _specificationsRepository.GetSpecificationsByQuery(c => c.Content.Current.CoreProviderVersionUpdates == CoreProviderVersionUpdates.UseLatest))
                .Select(s => _mapper.Map<SpecificationSummary>(s));


            if (!specifications.Any())
            {
                _logger.Information($"Specifications was not found with CoreProviderVersionUpdates as UseLatest");

                return new NotFoundResult();
            }

            return new OkObjectResult(specifications);
        }
    }
}
