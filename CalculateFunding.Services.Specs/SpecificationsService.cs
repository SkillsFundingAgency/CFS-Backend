using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models;
using CalculateFunding.Models.Exceptions;
using CalculateFunding.Models.Policy;
using CalculateFunding.Models.Specs;
using CalculateFunding.Models.Specs.Messages;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Specs.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Serilog;
using PolicyModels = CalculateFunding.Common.ApiClient.Policies.Models;
using PublishStatus = CalculateFunding.Models.Versioning.PublishStatus;
using Trigger = CalculateFunding.Common.ApiClient.Jobs.Models.Trigger;
using ApiSpecificationSummary = CalculateFunding.Common.ApiClient.Specifications.Models.SpecificationSummary;

namespace CalculateFunding.Services.Specs
{
    public class SpecificationsService : ISpecificationsService, IHealthChecker
    {
        private readonly IMapper _mapper;
        private readonly ISpecificationsRepository _specificationsRepository;
        private readonly IPoliciesApiClient _policiesApiClient;
        private readonly Polly.Policy _policiesApiClientPolicy;
        private readonly ILogger _logger;
        private readonly IValidator<SpecificationCreateModel> _specificationCreateModelvalidator;
        private readonly IValidator<CalculationCreateModel> _calculationCreateModelValidator;
        private readonly IMessengerService _messengerService;
        private readonly ISearchRepository<SpecificationIndex> _searchRepository;
        private readonly IValidator<AssignDefinitionRelationshipMessage> _assignDefinitionRelationshipMessageValidator;
        private readonly IValidator<SpecificationEditModel> _specificationEditModelValidator;
        private readonly ICacheProvider _cacheProvider;
        private readonly IValidator<CalculationEditModel> _calculationEditModelValidator;
        private readonly IResultsRepository _resultsRepository;
        private readonly IVersionRepository<SpecificationVersion> _specificationVersionRepository;
        private readonly IFeatureToggle _featureToggle;
        private readonly Polly.Policy _jobsApiClientPolicy;
        private readonly IJobsApiClient _jobsApiClient;

        // Ctor for use from functions
        public SpecificationsService(
            IMapper mapper,
            ISpecificationsRepository specificationsRepository,
            IPoliciesApiClient policiesApiClient,
            ILogger logger,
            IValidator<SpecificationCreateModel> specificationCreateModelValidator,
            IValidator<CalculationCreateModel> calculationCreateModelValidator,
            IMessengerService messengerService,
            ISearchRepository<SpecificationIndex> searchRepository,
            IValidator<AssignDefinitionRelationshipMessage> assignDefinitionRelationshipMessageValidator,
            ICacheProvider cacheProvider,
            IValidator<SpecificationEditModel> specificationEditModelValidator,
            IValidator<CalculationEditModel> calculationEditModelValidator,
            IResultsRepository resultsRepository,
            IVersionRepository<SpecificationVersion> specificationVersionRepository,
            IFeatureToggle featureToggle,
            ISpecificationsResiliencePolicies resiliencePolicies,
            IJobsApiClient jobsApiClient)
        {
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            Guard.ArgumentNotNull(specificationsRepository, nameof(specificationsRepository));
            Guard.ArgumentNotNull(policiesApiClient, nameof(policiesApiClient));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(specificationCreateModelValidator, nameof(specificationCreateModelValidator));
            Guard.ArgumentNotNull(calculationCreateModelValidator, nameof(calculationCreateModelValidator));
            Guard.ArgumentNotNull(messengerService, nameof(messengerService));
            Guard.ArgumentNotNull(searchRepository, nameof(searchRepository));
            Guard.ArgumentNotNull(assignDefinitionRelationshipMessageValidator, nameof(assignDefinitionRelationshipMessageValidator));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(specificationEditModelValidator, nameof(specificationEditModelValidator));
            Guard.ArgumentNotNull(resultsRepository, nameof(resultsRepository));
            Guard.ArgumentNotNull(specificationVersionRepository, nameof(specificationVersionRepository));
            Guard.ArgumentNotNull(featureToggle, nameof(featureToggle));
            Guard.ArgumentNotNull(jobsApiClient, nameof(jobsApiClient));

            _mapper = mapper;
            _specificationsRepository = specificationsRepository;
            _policiesApiClient = policiesApiClient;
            _policiesApiClientPolicy = resiliencePolicies.PoliciesApiClient;
            _logger = logger;
            _specificationCreateModelvalidator = specificationCreateModelValidator;
            _calculationCreateModelValidator = calculationCreateModelValidator;
            _messengerService = messengerService;
            _searchRepository = searchRepository;
            _assignDefinitionRelationshipMessageValidator = assignDefinitionRelationshipMessageValidator;
            _cacheProvider = cacheProvider;
            _specificationEditModelValidator = specificationEditModelValidator;
            _calculationEditModelValidator = calculationEditModelValidator;
            _resultsRepository = resultsRepository;
            _specificationVersionRepository = specificationVersionRepository;
            _featureToggle = featureToggle;
            _jobsApiClient = jobsApiClient;
            _jobsApiClientPolicy = resiliencePolicies.JobsApiClient;
        }

        // Ctor for use from API
        public SpecificationsService(
            IMapper mapper,
            ISpecificationsRepository specificationsRepository,
            IPoliciesApiClient policiesApiClient,
            ILogger logger,
            IValidator<SpecificationCreateModel> specificationCreateModelValidator,
            IValidator<CalculationCreateModel> calculationCreateModelValidator,
            IMessengerService messengerService,
            ISearchRepository<SpecificationIndex> searchRepository,
            IValidator<AssignDefinitionRelationshipMessage> assignDefinitionRelationshipMessageValidator,
            ICacheProvider cacheProvider,
            IValidator<SpecificationEditModel> specificationEditModelValidator,
            IValidator<CalculationEditModel> calculationEditModelValidator,
            IResultsRepository resultsRepository,
            IVersionRepository<SpecificationVersion> specificationVersionRepository,
            IFeatureToggle featureToggle,
            IJobsApiClient jobsApiClient,
            ISpecificationsResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            Guard.ArgumentNotNull(specificationsRepository, nameof(specificationsRepository));
            Guard.ArgumentNotNull(policiesApiClient, nameof(policiesApiClient));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(specificationCreateModelValidator, nameof(specificationCreateModelValidator));
            Guard.ArgumentNotNull(calculationCreateModelValidator, nameof(calculationCreateModelValidator));
            Guard.ArgumentNotNull(messengerService, nameof(messengerService));
            Guard.ArgumentNotNull(searchRepository, nameof(searchRepository));
            Guard.ArgumentNotNull(assignDefinitionRelationshipMessageValidator, nameof(assignDefinitionRelationshipMessageValidator));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(specificationEditModelValidator, nameof(specificationEditModelValidator));
            Guard.ArgumentNotNull(resultsRepository, nameof(resultsRepository));
            Guard.ArgumentNotNull(specificationVersionRepository, nameof(specificationVersionRepository));
            Guard.ArgumentNotNull(featureToggle, nameof(featureToggle));
            Guard.ArgumentNotNull(jobsApiClient, nameof(jobsApiClient));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));

            _mapper = mapper;
            _specificationsRepository = specificationsRepository;
            _policiesApiClient = policiesApiClient;
            _policiesApiClientPolicy = resiliencePolicies.PoliciesApiClient;
            _logger = logger;
            _specificationCreateModelvalidator = specificationCreateModelValidator;
            _calculationCreateModelValidator = calculationCreateModelValidator;
            _messengerService = messengerService;
            _searchRepository = searchRepository;
            _assignDefinitionRelationshipMessageValidator = assignDefinitionRelationshipMessageValidator;
            _cacheProvider = cacheProvider;
            _specificationEditModelValidator = specificationEditModelValidator;
            _calculationEditModelValidator = calculationEditModelValidator;
            _resultsRepository = resultsRepository;
            _specificationVersionRepository = specificationVersionRepository;
            _featureToggle = featureToggle;
            _jobsApiClient = jobsApiClient;
            _jobsApiClientPolicy = resiliencePolicies.JobsApiClient;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth specRepoHealth = await ((IHealthChecker)_specificationsRepository).IsHealthOk();
            string queueName = ServiceBusConstants.QueueNames.CalculationJobInitialiser;
            (bool Ok, string Message) messengerServiceHealth = await _messengerService.IsHealthOk(queueName);
            (bool Ok, string Message) searchRepoHealth = await _searchRepository.IsHealthOk();
            (bool Ok, string Message) cacheHealth = await _cacheProvider.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(SpecificationsService)
            };
            health.Dependencies.AddRange(specRepoHealth.Dependencies);
            health.Dependencies.Add(new DependencyHealth { HealthOk = messengerServiceHealth.Ok, DependencyName = $"{_messengerService.GetType().GetFriendlyName()} for queue: {queueName}", Message = messengerServiceHealth.Message });
            health.Dependencies.Add(new DependencyHealth { HealthOk = searchRepoHealth.Ok, DependencyName = _searchRepository.GetType().GetFriendlyName(), Message = searchRepoHealth.Message });
            health.Dependencies.Add(new DependencyHealth { HealthOk = cacheHealth.Ok, DependencyName = _cacheProvider.GetType().GetFriendlyName(), Message = cacheHealth.Message });

            return health;
        }

        public async Task<IActionResult> GetSpecifications(HttpRequest request)
        {
            IEnumerable<Specification> specifications = await _specificationsRepository.GetSpecifications();

            if (specifications == null)
            {
                _logger.Warning($"No specifications were returned from the repository, result came back null");

                return new NotFoundResult();
            }

            return new OkObjectResult(specifications);
        }

        public async Task<IActionResult> GetSpecificationById(HttpRequest request)
        {
            request.Query.TryGetValue("specificationId", out StringValues specId);

            string specificationId = specId.FirstOrDefault();

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

        public async Task<IActionResult> GetSpecificationSummaryById(HttpRequest request)
        {
            request.Query.TryGetValue("specificationId", out StringValues specId);

            string specificationId = specId.FirstOrDefault();

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

        public async Task<IActionResult> GetSpecificationSummariesByIds(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            if (string.IsNullOrWhiteSpace(json))
            {
                _logger.Warning("No specification ids was provided to GetSpecificationSummariesByIds");
                return new BadRequestObjectResult("Null or empty specification ids provided");
            }

            string[] specificationIds = JsonConvert.DeserializeObject<IEnumerable<string>>(json).ToArray();

            List<SpecificationSummary> result = new List<SpecificationSummary>();

            if (specificationIds.Any())
            {
                IEnumerable<Specification> specifications = await _specificationsRepository.GetSpecificationsByQuery(c => specificationIds.Contains(c.Id));

                foreach (Specification specification in specifications)
                {
                    result.Add(_mapper.Map<SpecificationSummary>(specification));
                }
            }

            return new OkObjectResult(result);
        }

        public async Task<IActionResult> GetSpecificationSummaries(HttpRequest request)
        {
            IEnumerable<Specification> specifications = await _specificationsRepository.GetSpecifications();

            if (specifications == null)
            {
                _logger.Warning($"No specifications were returned from the repository, result came back null");

                return new NotFoundResult();
            }

            List<SpecificationSummary> result = await _cacheProvider.GetAsync<List<SpecificationSummary>>(CacheKeys.SpecificationSummaries);
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

        public async Task<IActionResult> GetCurrentSpecificationById(HttpRequest request)
        {
            request.Query.TryGetValue("specificationId", out StringValues specId);

            string specificationId = specId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification Id was provided to GetSpecificationById");

                return new BadRequestObjectResult("Null or empty specification Id provided");
            }

            string cacheKey = $"{CacheKeys.SpecificationCurrentVersionById}{specificationId}";

            SpecificationCurrentVersion result = await _cacheProvider.GetAsync<SpecificationCurrentVersion>(cacheKey);
            if (result == null)
            {
                DocumentEntity<Specification> specification = await _specificationsRepository.GetSpecificationDocumentEntityById(specificationId);

                if (specification == null)
                {
                    _logger.Warning($"A specification for id {specificationId} could not found");

                    return new NotFoundResult();
                }

                if (specification.Content == null)
                {
                    return new InternalServerErrorResult("Specification content is null");
                }

                if (specification.Content.Current == null)
                {
                    return new InternalServerErrorResult("Specification current is null");
                }

                List<FundingStream> fundingStreams = new List<FundingStream>();
                if (!specification.Content.Current.FundingStreams.IsNullOrEmpty())
                {
                    string[] fundingStreamIds = specification.Content.Current.FundingStreams.Select(p => p.Id).ToArray();
                    Common.ApiClient.Models.ApiResponse<PolicyModels.FundingStream> fundingStreamResultResponse = await _policiesApiClientPolicy.ExecuteAsync(() => _policiesApiClient.GetFundingStreamById(fundingStreamIds.FirstOrDefault()));
                    FundingStream fundingStreamResult = _mapper.Map<FundingStream>(fundingStreamResultResponse?.Content);
                    fundingStreams.Add(fundingStreamResult);
                }

                result = ConvertSpecificationToCurrentVersion(specification, fundingStreams);

                await _cacheProvider.SetAsync(cacheKey, result, TimeSpan.FromDays(1), true);
            }

            return new OkObjectResult(result);
        }

        public async Task<IActionResult> GetSpecificationsByFundingPeriodId(HttpRequest request)
        {
            request.Query.TryGetValue("fundingPeriodId", out StringValues yearId);

            string fundingPeriodId = yearId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(fundingPeriodId))
            {
                _logger.Error("No funding period Id was provided to GetSpecificationByFundingPeriodId");

                return new BadRequestObjectResult("Null or empty fundingPeriodId provided");
            }

            string cacheKey = $"{CacheKeys.SpecificationSummariesByFundingPeriodId}{fundingPeriodId}";

            List<SpecificationSummary> result = await _cacheProvider.GetAsync<List<SpecificationSummary>>(cacheKey);
            if (result.IsNullOrEmpty())
            {
                IEnumerable<Specification> specifications = await _specificationsRepository.GetSpecificationsByQuery(m => m.Current.FundingPeriod.Id == fundingPeriodId);

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

        public async Task<IActionResult> GetCurrentSpecificationsByFundingPeriodIdAndFundingStreamId(HttpRequest request)
        {
            request.Query.TryGetValue("fundingPeriodId", out StringValues yearId);

            string fundingPeriodId = yearId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(fundingPeriodId))
            {
                _logger.Error("No funding period Id was provided to GetSpecificationsByFundingPeriodIdAndFundingPeriodId");

                return new BadRequestObjectResult("Null or empty fundingPeriodId provided");
            }

            request.Query.TryGetValue("fundingStreamId", out StringValues fundingStream);

            string fundingStreamId = fundingStream.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(fundingStreamId))
            {
                _logger.Error("No funding stream Id was provided to GetSpecificationsByFundingPeriodIdAndFundingPeriodId");

                return new BadRequestObjectResult("Null or empty fundingstreamId provided");
            }

            IEnumerable<Specification> specifications = await _specificationsRepository.GetApprovedOrUpdatedSpecificationsByFundingPeriodAndFundingStream(fundingPeriodId, fundingStreamId);

            ConcurrentBag<SpecificationSummary> mappedSpecifications = new ConcurrentBag<SpecificationSummary>();

            IList<Task> checkForResulstsTasks = new List<Task>();

            foreach (Specification specification in specifications)
            {
                Task task = Task.Run(async () =>
                {
                    bool hasProviderResults = await _resultsRepository.SpecificationHasResults(specification.Id);

                    if (hasProviderResults)
                    {
                        mappedSpecifications.Add(_mapper.Map<SpecificationSummary>(specification));
                    }
                });

                checkForResulstsTasks.Add(task);
            }

            await TaskHelper.WhenAllAndThrow(checkForResulstsTasks.ToArray());

            return new OkObjectResult(mappedSpecifications);
        }

        public async Task<IActionResult> GetSpecificationsSelectedForFunding(HttpRequest request)
        {
            IEnumerable<SpecificationSummary> specifications = (
                await _specificationsRepository.GetSpecificationsByQuery(c => c.IsSelectedForFunding)
                    ).Select(s => _mapper.Map<SpecificationSummary>(s));

            return new OkObjectResult(specifications);
        }

        public async Task<IActionResult> GetSpecificationsSelectedForFundingByPeriod(HttpRequest request)
        {
            request.Query.TryGetValue("fundingPeriodId", out StringValues yearId);

            string fundingPeriodId = yearId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(fundingPeriodId))
            {
                _logger.Error("No funding period was provided to GetSpecificationsSelectedForFundingPeriod");

                return new BadRequestObjectResult("Null or empty funding period provided");
            }

            IEnumerable<SpecificationSummary> specifications = (
                    await _specificationsRepository.GetSpecificationsByQuery(c => c.IsSelectedForFunding && c.Current.FundingPeriod.Id == fundingPeriodId)
                    ).Select(s => _mapper.Map<SpecificationSummary>(s));


            if (!specifications.Any())
            {
                _logger.Information($"Specification was not found for funding period: {fundingPeriodId}");

                return new NotFoundResult();
            }

            return new OkObjectResult(specifications);
        }


        public async Task<IActionResult> GetFundingStreamsSelectedForFundingBySpecification(HttpRequest request)
        {
            request.Query.TryGetValue("specificationId", out StringValues specId);

            string specificationId = specId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification id was provided to GetFundingStreamsSelectedForFundingBySpecification");

                return new BadRequestObjectResult("Null or empty specification id was provided");
            }

            IEnumerable<FundingStream> fundingStreams = (
                    await _specificationsRepository.GetSpecificationsByQuery(c => c.IsSelectedForFunding && c.Id == specificationId)
                    ).Select(s => _mapper.Map<FundingStream>(s));

            return new OkObjectResult(fundingStreams);
        }

        public async Task<IActionResult> UpdateCalculationLastUpdatedDate(HttpRequest request)
        {
            request.Query.TryGetValue("specificationId", out StringValues specId);

            string specificationId = specId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification id was provided to UpdateCalculationLastUpdatedDate");

                return new BadRequestObjectResult("Null or empty specification id was provided");
            }

            Specification specification = await _specificationsRepository.GetSpecificationById(specificationId);

            if (specification == null)
            {
                _logger.Warning($"A specification for id {specificationId} could not found");

                return new NotFoundResult();
            }

            specification.LastCalculationUpdatedAt = DateTimeOffset.Now.ToLocalTime();

            HttpStatusCode result = await _specificationsRepository.UpdateSpecification(specification);

            if (!result.IsSuccess())
            {
                string message = $"Failed to update calculation last updated date on specification {specificationId}";

                _logger.Error(message);

                return new InternalServerErrorResult(message);
            }

            await _cacheProvider.RemoveAsync<SpecificationSummary>($"{CacheKeys.SpecificationSummaryById}{specification.Id}");
            await _cacheProvider.RemoveAsync<SpecificationCurrentVersion>($"{CacheKeys.SpecificationCurrentVersionById}{specification.Id}");

            return new NoContentResult();
        }


        public async Task<IActionResult> GetSpecificationByName(HttpRequest request)
        {
            request.Query.TryGetValue("specificationName", out StringValues specName);

            string specificationName = specName.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(specName))
            {
                _logger.Error("No specification name was provided to GetSpecificationByName");

                return new BadRequestObjectResult("Null or empty specification name provided");
            }

            IEnumerable<Specification> specifications = await _specificationsRepository.GetSpecificationsByQuery(m => m.Name.ToLower() == specificationName.ToLower());

            if (!specifications.Any())
            {
                _logger.Information($"Specification was not found for name: {specificationName}");

                return new NotFoundResult();
            }

            _logger.Information($"Specification found for name: {specificationName}");

            return new OkObjectResult(specifications.FirstOrDefault());
        }

        public async Task<IActionResult> GetCalculationByName(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            CalculationGetModel model = JsonConvert.DeserializeObject<CalculationGetModel>(json);

            if (string.IsNullOrWhiteSpace(model.SpecificationId))
            {
                _logger.Error("No specification id was provided to GetCalculationByName");
                return new BadRequestObjectResult("Null or empty specification id provided");
            }

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                _logger.Error("No calculation name was provided to GetCalculationByName");
                return new BadRequestObjectResult("Null or empty calculation name provided");
            }

            Calculation calculation = await _specificationsRepository.GetCalculationBySpecificationIdAndCalculationName(model.SpecificationId, model.Name);

            if (calculation != null)
            {
                _logger.Information($"A calculation was found for specification id {model.SpecificationId} and name {model.Name}");

                return new OkObjectResult(calculation);
            }

            _logger.Information($"A calculation was not found for specification id {model.SpecificationId} and name {model.Name}");

            return new NotFoundResult();
        }

        public async Task<IActionResult> GetBaselineCalculations(string specificationId, HttpRequest request)
        {
            Guard.ArgumentNotNull(specificationId, nameof(specificationId));

            Specification specification = await _specificationsRepository.GetSpecificationById(specificationId);
            if (specification == null)
            {
                return new PreconditionFailedResult("Specification not found");
            }

            List<CalculationCurrentVersion> baselineCalculations = new List<CalculationCurrentVersion>();

            if (specification.Current != null)
            {
               
                foreach (Calculation calculation in specification.Current.Calculations)
                {
                    if (calculation.CalculationType == CalculationType.Baseline)
                    {

                        baselineCalculations.Add(GenerateCalculationCurrentVersion(specificationId, calculation));
                    }
                }
            }

            return new OkObjectResult(baselineCalculations);
        }

        public async Task<IActionResult> AssociateTemplateIdWithSpecification(string specificationId, string templateId, string fundingStreamId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.IsNullOrWhiteSpace(templateId, nameof(templateId));
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));

            Specification specification = await _specificationsRepository.GetSpecificationById(specificationId);
            if (specification == null)
            {
                string message = $"No specification ID {specificationId} were returned from the repository, result came back null";
                _logger.Error(message);

                return new PreconditionFailedResult(message);
            }

            bool specificationContainsGivenFundingStream = specification.Current.FundingStreams.Any(x => x.Id == fundingStreamId);
            if (!specificationContainsGivenFundingStream)
            {
                string message = $"Specification ID {specificationId} does not have contain given funding stream with ID {fundingStreamId}";
                _logger.Error(message);

                return new PreconditionFailedResult(message);
            }

            Common.ApiClient.Models.ApiResponse<PolicyModels.FundingTemplateContents> fundingTemplateContents = await _policiesApiClientPolicy.ExecuteAsync(() => _policiesApiClient.GetFundingTemplate(fundingStreamId, templateId));
            if (fundingTemplateContents.StatusCode != HttpStatusCode.OK)
            {
                string message = $"Retrieve funding template with fundingStreamId: {fundingStreamId} and templateId: {templateId} did not return OK.";
                _logger.Error(message);

                return new PreconditionFailedResult(message);
            }

            TemplateMapping fundingTemplateMapping = fundingTemplateContents.Content != null ? GetTemplateMappingFromTemplate(fundingTemplateContents.Content.Metadata) : new TemplateMapping();
            TemplateMapping specificationTemplateMapping = await _specificationsRepository.GetTemplateMappingForSpecificationId(specificationId);

            List<Calculation> currentCalculations = ApplyTemplateChanges(specification, fundingTemplateMapping, specificationTemplateMapping);

            SpecificationVersion previousSpecificationVersion = specification.Current;
            SpecificationVersion specificationVersion = specification.Current.Clone() as SpecificationVersion;
            specificationVersion.TemplateId = templateId;
            specificationVersion.Calculations = currentCalculations;
            HttpStatusCode updateSpecificationResult = await UpdateSpecification(specification, specificationVersion, previousSpecificationVersion);

            return new OkObjectResult(updateSpecificationResult);
        }

        private static List<Calculation> ApplyTemplateChanges(Specification specification, TemplateMapping fundingTemplateMapping, TemplateMapping specificationTemplateMapping)
        {
            IEnumerable<string> templateCalculationNames = fundingTemplateMapping
                .TemplateMappingItems
                .Where(x => x.EntityType == TemplateMappingEntityType.Calculation)
                .Select(x => x.Name);
            IEnumerable<string> specificationTemplateCalculationNames = specificationTemplateMapping
                .TemplateMappingItems
                .Where(x => x.EntityType == TemplateMappingEntityType.Calculation)
                .Select(x => x.Name);

            IEnumerable<string> calculationNamesRemovedFromTemplate = specificationTemplateCalculationNames.Except(templateCalculationNames);
            List<Calculation> currentCalculations = specification.Current.Calculations.ToList();

            foreach (var calculationNameRemovedFromTemplate in calculationNamesRemovedFromTemplate)
            {
                currentCalculations.Remove(currentCalculations.FirstOrDefault(x => x.Name == calculationNameRemovedFromTemplate));
            }

            return currentCalculations;
        }

        private static TemplateMapping GetTemplateMappingFromTemplate(Common.TemplateMetadata.Models.TemplateMetadataContents templateMetadataContents)
        {
            TemplateMapping templateMapping = new TemplateMapping
            {
                TemplateMappingItems = new List<TemplateMappingItem>()
            };

            List<Common.TemplateMetadata.Models.FundingLine> allFundingLines = templateMetadataContents.RootFundingLines.ToList();
            allFundingLines.AddRange(templateMetadataContents.RootFundingLines.SelectMany(x => x.FundingLines));

            templateMapping.TemplateMappingItems.AddRange(
                allFundingLines
                    .Select(fundingLine => new TemplateMappingItem
                    {
                        EntityType = TemplateMappingEntityType.FundingLine,
                        Name = fundingLine.Name,
                        TemplateId = fundingLine.ReferenceId //TODO: Naming convention for the other fields are Template{EntityName}Id. This one can be renamed to TemplateFundingLineId instead of ReferenceId, to match it
                    }));

            templateMapping.TemplateMappingItems.AddRange(
                allFundingLines
                    .SelectMany(x => x.Calculations)
                    .Select(calculation => new TemplateMappingItem
                    {
                        EntityType = TemplateMappingEntityType.Calculation,
                        Name = calculation.Name,
                        TemplateId = calculation.TemplateCalculationId,
                    }));

            templateMapping.TemplateMappingItems.AddRange(
                allFundingLines
                    .SelectMany(x => x.Calculations)
                    .SelectMany(x => x.ReferenceData)
                    .Select(referenceData => new TemplateMappingItem
                    {
                        EntityType = TemplateMappingEntityType.ReferenceData,
                        Name = referenceData.Name,
                        TemplateId = referenceData.TemplateReferenceId
                    }));

            return templateMapping;
        }

        public async Task<IActionResult> GetCalculationBySpecificationIdAndCalculationId(HttpRequest request)
        {
            request.Query.TryGetValue("specificationId", out StringValues specId);

            string specificationId = specId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification Id was provided to GetCalculationBySpecificationIdAndCalculationId");

                return new BadRequestObjectResult("Null or empty specification Id provided");
            }

            request.Query.TryGetValue("calculationId", out StringValues calcId);

            string calculationId = calcId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(calculationId))
            {
                _logger.Error("No calculation Id was provided to GetCalculationBySpecificationIdAndCalculationId");

                return new BadRequestObjectResult("Null or empty specification Id provided");
            }

            Specification specification = await _specificationsRepository.GetSpecificationById(specificationId);
            if (specification == null)
            {
                return new PreconditionFailedResult("Specification not found");
            }

            if (specification.Current != null)
            {
                if (specification.Current != null)
                {
                    foreach (Calculation calculation in specification.Current.Calculations)
                    {
                        if (calculation.Id == calculationId)
                        {
                            return GenerateCalculationCurrentVersionActionResult(specificationId, calculation);
                        }
                    }
                }
            }

            _logger.Information($"A calculation was not found for specification id {specificationId} and calculation id {calculationId}");

            return new NotFoundObjectResult("Calculation not found");
        }

        private CalculationCurrentVersion GenerateCalculationCurrentVersion(string specificationId, Calculation calculation)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.ArgumentNotNull(calculation, nameof(calculation));

            CalculationCurrentVersion calculationCurrentVersion = _mapper.Map<CalculationCurrentVersion>(calculation);

            _logger.Information($"A calculation was found for specification id {specificationId} and calculation id {calculation.Id}");

            return calculationCurrentVersion;
        }

        private IActionResult GenerateCalculationCurrentVersionActionResult(string specificationId, Calculation calculation)
        {
            return new OkObjectResult(GenerateCalculationCurrentVersion(specificationId, calculation));
        }

        public async Task<IActionResult> GetCalculationsBySpecificationId(HttpRequest request)
        {
            request.Query.TryGetValue("specificationId", out StringValues specId);

            string specificationId = specId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification Id was provided to GetCalculationsBySpecificationId");

                return new BadRequestObjectResult("Null or empty specification Id provided");
            }

            IEnumerable<Calculation> calculations = await _specificationsRepository.GetCalculationsBySpecificationId(specificationId);

            if (calculations == null)
            {
                _logger.Error("No calculations could be retrieved found for specification id {specificationId}", specificationId);

                return new NotFoundObjectResult("No calculations could be retrieved");
            }

            _logger.Verbose("Calculations were found for specification id {specificationId}", specificationId);

            return new OkObjectResult(calculations);
        }

        public async Task<IActionResult> GetFundingStreamsForSpecificationById(HttpRequest request)
        {
            request.Query.TryGetValue("specificationId", out StringValues specificationIdParse);

            string specificationId = specificationIdParse.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specificationId was provided to GetFundingStreamsForSpecificationById");

                return new BadRequestObjectResult("Null or empty specificationId provided");
            }

            Specification specification = await _specificationsRepository.GetSpecificationById(specificationId);
            if (specification == null)
            {
                return new PreconditionFailedResult("Specification not found");
            }

            if (!specification.Current.FundingStreams.Any())
            {
                return new InternalServerErrorResult("Specification contains no funding streams");
            }

            string[] fundingSteamIds = specification.Current.FundingStreams.Select(s => s.Id).ToArray();

            Common.ApiClient.Models.ApiResponse<PolicyModels.FundingStream> fundingStreamResponse = await _policiesApiClientPolicy.ExecuteAsync(() => _policiesApiClient.GetFundingStreamById(fundingSteamIds.FirstOrDefault()));
            if (fundingStreamResponse?.Content == null)
            {
                _logger.Error("No funding streams were returned");

                return new InternalServerErrorResult("No funding stream were returned");
            }

            FundingStream fundingStream = _mapper.Map<FundingStream>(fundingStreamResponse?.Content);

            return new OkObjectResult(new List<FundingStream> { fundingStream });
        }

        public async Task<IActionResult> CreateSpecification(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            SpecificationCreateModel createModel = JsonConvert.DeserializeObject<SpecificationCreateModel>(json);

            if (createModel == null)
            {
                return new BadRequestObjectResult("Null policy create model provided");
            }

            BadRequestObjectResult validationResult = (await _specificationCreateModelvalidator.ValidateAsync(createModel)).PopulateModelState();

            if (validationResult != null)
            {
                return validationResult;
            }

            Common.ApiClient.Models.ApiResponse<PolicyModels.Period> fundingPeriodResponse = await _policiesApiClientPolicy.ExecuteAsync(() => _policiesApiClient.GetFundingPeriodById(createModel.FundingPeriodId));
            Period fundingPeriod = _mapper.Map<Period>(fundingPeriodResponse?.Content);

            Reference user = request.GetUser();

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
                Date = DateTimeOffset.Now.ToLocalTime()
            };

            List<Reference> fundingStreams = new List<Reference>();
            List<FundingStream> fundingStreamObjects = new List<FundingStream>();

            foreach (string fundingStreamId in createModel.FundingStreamIds)
            {
                Common.ApiClient.Models.ApiResponse<PolicyModels.FundingStream> fundingStreamResponse = await _policiesApiClientPolicy.ExecuteAsync(() => _policiesApiClient.GetFundingStreamById(fundingStreamId));
                FundingStream fundingStream = _mapper.Map<FundingStream>(fundingStreamResponse?.Content);
                if (fundingStream == null)
                {
                    return new PreconditionFailedResult($"Unable to find funding stream with ID '{fundingStreamId}'.");
                }

                fundingStreams.Add(new Reference(fundingStream.Id, fundingStream.Name));
                fundingStreamObjects.Add(fundingStream);
            }

            if (!fundingStreams.Any())
            {
                return new InternalServerErrorResult("No funding streams were retrieved to add to the Specification");
            }

            specificationVersion.FundingStreams = fundingStreams;

            specification.Current = specificationVersion;

            DocumentEntity<Specification> repositoryCreateResult = await _specificationsRepository.CreateSpecification(specification);

            if (repositoryCreateResult == null)
            {
                return new InternalServerErrorResult("Error creating specification in repository");
            }

            await _searchRepository.Index(new List<SpecificationIndex>
            {
                new SpecificationIndex
                {
                    Id = specification.Id,
                    Name = specification.Name,
                    FundingStreamIds = specificationVersion.FundingStreams.Select(s=>s.Id).ToArray(),
                    FundingStreamNames = specificationVersion.FundingStreams.Select(s=>s.Name).ToArray(),
                    FundingPeriodId = specificationVersion.FundingPeriod.Id,
                    FundingPeriodName = specificationVersion.FundingPeriod.Name,
                    LastUpdatedDate = repositoryCreateResult.CreatedAt,
                    Description= specificationVersion.Description,
                    Status = Enum.GetName(typeof(PublishStatus), specificationVersion.PublishStatus),
                }
            });

            specificationVersion = await _specificationVersionRepository.CreateVersion(specificationVersion);

            await _specificationVersionRepository.SaveVersion(specificationVersion);

            await ClearSpecificationCacheItems(specificationVersion.FundingPeriod.Id);

            SpecificationCurrentVersion result = ConvertSpecificationToCurrentVersion(repositoryCreateResult, fundingStreamObjects);

            return new OkObjectResult(result);
        }

        public async Task<IActionResult> EditSpecification(HttpRequest request)
        {
            request.Query.TryGetValue("specificationId", out StringValues specId);
            string specificationId = specId.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification Id was provided to EditSpecification");
                return new BadRequestObjectResult("Null or empty specification Id provided");
            }
            string json = await request.GetRawBodyStringAsync();
            SpecificationEditModel editModel = JsonConvert.DeserializeObject<SpecificationEditModel>(json);
            if (editModel == null)
            {
                _logger.Error("No edit modeld was provided to EditSpecification");
                return new BadRequestObjectResult("Null edit specification model provided");
            }

            editModel.SpecificationId = specificationId;

            BadRequestObjectResult validationResult = (await _specificationEditModelValidator.ValidateAsync(editModel)).PopulateModelState();
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

            Reference user = request.GetUser();

            SpecificationVersion previousSpecificationVersion = specification.Current;

            SpecificationVersion specificationVersion = specification.Current.Clone() as SpecificationVersion;

            specificationVersion.ProviderVersionId = editModel.ProviderVersionId;
            specificationVersion.Name = editModel.Name;
            specificationVersion.Description = editModel.Description;
            specificationVersion.Author = user;
            specificationVersion.SpecificationId = specificationId;

            specification.Name = editModel.Name;

            string previousFundingPeriodId = specificationVersion.FundingPeriod.Id;

            if (editModel.FundingPeriodId != specificationVersion.FundingPeriod.Id)
            {
                Common.ApiClient.Models.ApiResponse<PolicyModels.Period> fundingPeriodResponse = await _policiesApiClientPolicy.ExecuteAsync(() => _policiesApiClient.GetFundingPeriodById(editModel.FundingPeriodId));
                if (fundingPeriodResponse?.Content == null)
                {
                    return new PreconditionFailedResult($"Unable to find funding period with ID '{editModel.FundingPeriodId}'.");
                }

                Period fundingPeriod = _mapper.Map<Period>(fundingPeriodResponse?.Content);
                specificationVersion.FundingPeriod = new Reference { Id = fundingPeriod.Id, Name = fundingPeriod.Name };
            }

            IEnumerable<string> existingFundingStreamIds = specificationVersion.FundingStreams?.Select(m => m.Id);

            bool fundingStreamsChanged = !existingFundingStreamIds.SequenceEqual(editModel.FundingStreamIds);

            if (fundingStreamsChanged)
            {
                foreach (string fundingStreamId in editModel.FundingStreamIds)
                {
                    Common.ApiClient.Models.ApiResponse<PolicyModels.FundingStream> fundingStreamResponse = await _policiesApiClientPolicy.ExecuteAsync(() => _policiesApiClient.GetFundingStreamById(fundingStreamId));

                    if (fundingStreamResponse?.Content == null)
                    {
                        return new InternalServerErrorResult("No funding streams were retrieved to add to the Specification");
                    }

                    FundingStream fundingStream = _mapper.Map<FundingStream>(fundingStreamResponse?.Content);

                    List<Reference> fundingStreamReferences = new List<Reference>();

                    Dictionary<string, bool> allocationLines = new Dictionary<string, bool>();

                    fundingStreamReferences.Add(new Reference(fundingStream.Id, fundingStream.Name));
                    foreach (AllocationLine allocationLine in fundingStream.AllocationLines)
                    {
                        allocationLines.Add(allocationLine.Id, true);
                    }

                    specificationVersion.FundingStreams = fundingStreamReferences;

                    RemoveMissingAllocationLineAssociations(allocationLines, specificationVersion.Calculations);
                }
            }

            HttpStatusCode statusCode = await UpdateSpecification(specification, specificationVersion, previousSpecificationVersion);
            if (!statusCode.IsSuccess())
            {
                return new StatusCodeResult((int)statusCode);
            }

            await TaskHelper.WhenAllAndThrow(ReindexSpecification(specification), ClearSpecificationCacheItems(specificationVersion.FundingPeriod.Id));

            if (previousFundingPeriodId != specificationVersion.FundingPeriod.Id)
            {
                await _cacheProvider.RemoveAsync<List<SpecificationSummary>>($"{CacheKeys.SpecificationSummariesByFundingPeriodId}{previousFundingPeriodId}");

            }

            await SendSpecificationComparisonModelMessageToTopic(specificationId, ServiceBusConstants.TopicNames.EditSpecification, specification.Current, previousSpecificationVersion, request);

            return new OkObjectResult(specification);
        }

        private async Task ReindexSpecification(Specification specification)
        {
            IEnumerable<IndexError> specificationIndexingErrors = await _searchRepository.Index(new[]
            {
                CreateSpecificationIndex(specification)
            });

            List<IndexError> specificationIndexingErrorsAsList = specificationIndexingErrors.ToList();
            if (!specificationIndexingErrorsAsList.IsNullOrEmpty())
            {
                string specificationIndexingErrorsConcatted = string.Join(". ", specificationIndexingErrorsAsList.Select(e => e.ErrorMessage));
                string formattedErrorMessage =
                    $"Could not index specification {specification.Current.Id} because: {specificationIndexingErrorsConcatted}";
                _logger.Error(formattedErrorMessage);
                throw new ApplicationException(formattedErrorMessage);
            }
        }

        public async Task<IActionResult> EditSpecificationStatus(HttpRequest request)
        {
            request.Query.TryGetValue("specificationId", out StringValues specId);
            string specificationId = specId.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification Id was provided to EditSpecification");
                return new BadRequestObjectResult("Null or empty specification Id provided");
            }
            string json = await request.GetRawBodyStringAsync();

            EditStatusModel editStatusModel = null;

            try
            {
                editStatusModel = JsonConvert.DeserializeObject<EditStatusModel>(json);

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

            if ((specification.Current.PublishStatus == PublishStatus.Approved || specification.Current.PublishStatus == PublishStatus.Updated) && editStatusModel.PublishStatus == PublishStatus.Draft)
            {
                return new BadRequestObjectResult("Publish status can't be changed to Draft from Updated or Approved");
            }

            Reference user = request.GetUser();

            SpecificationVersion previousSpecificationVersion = specification.Current;

            SpecificationVersion specificationVersion = specification.Current.Clone() as SpecificationVersion;

            HttpStatusCode statusCode;

            if (editStatusModel.PublishStatus == PublishStatus.Approved)
            {
                specificationVersion.PublishStatus = PublishStatus.Approved;

                statusCode = await PublishSpecification(specification, specificationVersion, previousSpecificationVersion);
            }
            else
            {

                specificationVersion.PublishStatus = editStatusModel.PublishStatus;

                statusCode = await UpdateSpecification(specification, specificationVersion, previousSpecificationVersion);
            }

            if (!statusCode.IsSuccess())
            {
                return new StatusCodeResult((int)statusCode);
            }

            await ReindexSpecification(specification);

            await TaskHelper.WhenAllAndThrow(
                 ClearSpecificationCacheItems(specificationVersion.FundingPeriod.Id),
                _cacheProvider.RemoveAsync<SpecificationSummary>($"{CacheKeys.SpecificationSummaryById}{specification.Id}")
                );

            PublishStatusResultModel result = new PublishStatusResultModel()
            {
                PublishStatus = specification.Current.PublishStatus,
            };

            return new OkObjectResult(result);
        }

        public async Task<IActionResult> UpdatePublishedRefreshedDate(HttpRequest request)
        {
            request.Query.TryGetValue("specificationId", out StringValues specId);
            string specificationId = specId.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification Id was provided to UpdatePublishedRefreshedDate");
                return new BadRequestObjectResult("Null or empty specification Id provided");
            }
            string json = await request.GetRawBodyStringAsync();

            UpdatePublishedRefreshedDateModel updateRefreshDateModel = null;

            try
            {
                updateRefreshDateModel = JsonConvert.DeserializeObject<UpdatePublishedRefreshedDateModel>(json);

                if (updateRefreshDateModel == null || updateRefreshDateModel.PublishedResultsRefreshedAt == DateTimeOffset.MinValue)
                {
                    _logger.Error("A null refresh date model was provided");
                    return new BadRequestObjectResult("Null refresh date model provided");
                }
            }
            catch (JsonReaderException jre)
            {
                _logger.Error(jre, $"An invalid refresh date was provided for specification: {specificationId}");

                return new BadRequestObjectResult("An invalid refresh date was provided");
            }

            Specification specification = await _specificationsRepository.GetSpecificationById(specificationId);
            if (specification == null)
            {
                _logger.Warning($"Failed to find specification for id: {specificationId}");
                return new NotFoundObjectResult("Specification not found");
            }

            specification.PublishedResultsRefreshedAt = updateRefreshDateModel.PublishedResultsRefreshedAt;
            HttpStatusCode statusCode = await _specificationsRepository.UpdateSpecification(specification);

            if (!statusCode.IsSuccess())
            {
                string error = $"Failed to set PublishedResultsRefreshedAt on specification for id: {specificationId} to value: {updateRefreshDateModel.PublishedResultsRefreshedAt.ToString()}";
                _logger.Error(error);
                return new InternalServerErrorResult(error);
            }

            await _cacheProvider.RemoveAsync<SpecificationSummary>($"{CacheKeys.SpecificationSummaryById}{specification.Id}");
            await _cacheProvider.RemoveAsync<SpecificationCurrentVersion>($"{CacheKeys.SpecificationCurrentVersionById}{specification.Id}");

            SpecificationIndex specificationIndex = CreateSpecificationIndex(specification);

            IEnumerable<IndexError> errors = await _searchRepository.Index(new List<SpecificationIndex> { specificationIndex });

            if (errors.Any())
            {
                string error = $"Failed to index search for specification {specificationId} with the following errors: {string.Join(";", errors.Select(m => m.ErrorMessage))}";
                _logger.Error(error);
                return new InternalServerErrorResult(error);
            }

            return new OkResult();
        }

        private async Task SendSpecificationComparisonModelMessageToTopic(string specificationId, string topicName, SpecificationVersion current, SpecificationVersion previous, HttpRequest request)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.ArgumentNotNull(current, nameof(current));
            Guard.ArgumentNotNull(previous, nameof(previous));
            Guard.ArgumentNotNull(request, nameof(request));

            IDictionary<string, string> properties = CreateMessageProperties(request);

            SpecificationVersionComparisonModel comparisonModel = new SpecificationVersionComparisonModel
            {
                Id = specificationId,
                Current = current,
                Previous = previous
            };

            await _messengerService.SendToTopic(topicName, comparisonModel, properties, true);
        }

        private async Task SendCalculationComparisonModelMessageToTopic(string specificationId, string calculationId, string topicName, Calculation current, Calculation previous, HttpRequest request)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.ArgumentNotNull(current, nameof(current));
            Guard.ArgumentNotNull(previous, nameof(previous));
            Guard.ArgumentNotNull(request, nameof(request));

            IDictionary<string, string> properties = CreateMessageProperties(request);

            CalculationVersionComparisonModel comparisonModel = new CalculationVersionComparisonModel
            {
                SpecificationId = specificationId,
                CalculationId = calculationId,
                Current = current,
                Previous = previous,
            };

            await _messengerService.SendToTopic(topicName, comparisonModel, properties);
        }

        /// <summary>
        /// Remove Missing Allocation Line Associations
        /// </summary>
        /// <param name="allocationLines">Valid allocation lines</param>
        /// <param name="policies">Policies</param>
        private static void RemoveMissingAllocationLineAssociations(Dictionary<string, bool> allocationLines, IEnumerable<Calculation> calculations)
        {
            if (calculations.IsNullOrEmpty() || allocationLines.IsNullOrEmpty())
            {
                return;
            }

            foreach (Calculation calculation in calculations)
            {
                if (calculation.AllocationLine != null && !allocationLines.ContainsKey(calculation.AllocationLine.Id))
                {
                    calculation.AllocationLine = null;
                }
            }
        }

        public async Task<IActionResult> CreateCalculation(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();
            CalculationCreateModel createModel = JsonConvert.DeserializeObject<CalculationCreateModel>(json);
            if (createModel == null)
            {
                _logger.Error("Null calculation create model provided to CreateCalculation");
                return new BadRequestObjectResult("Null calculation create model provided");
            }
            BadRequestObjectResult validationResult = (await _calculationCreateModelValidator.ValidateAsync(createModel)).PopulateModelState();
            if (validationResult != null)
            {
                _logger.Error("Invalid data was provided for CreateCalculation");
                return validationResult;
            }
            Specification specification = await _specificationsRepository.GetSpecificationById(createModel.SpecificationId);
            if (specification == null)
            {
                _logger.Warning($"Specification not found for specification id {createModel.SpecificationId}");
                return new PreconditionFailedResult($"Specification not found for specification id {createModel.SpecificationId}");
            }

            SpecificationVersion previousSpecificationVersion = specification.Current;

            SpecificationVersion specificationVersion = specification.Current.Clone() as SpecificationVersion;

            Calculation calculation = _mapper.Map<Calculation>(createModel);
            calculation.LastUpdated = DateTimeOffset.Now;

            FundingStream currentFundingStream = null;

            if (!string.IsNullOrWhiteSpace(createModel.AllocationLineId))
            {
                string[] fundingSteamIds = specificationVersion.FundingStreams.Select(s => s.Id).ToArray();
                Common.ApiClient.Models.ApiResponse<IEnumerable<PolicyModels.FundingStream>> fundingStreamsResponse = await _policiesApiClientPolicy.ExecuteAsync(() => _policiesApiClient.GetFundingStreams());
                IEnumerable<FundingStream> fundingStreams = _mapper.Map<IEnumerable<FundingStream>>(fundingStreamsResponse?.Content);

                if (fundingStreams.IsNullOrEmpty() || !fundingStreams.Any(x => fundingSteamIds.Contains(x.Id)))
                {
                    _logger.Error("No funding streams were returned");

                    return new InternalServerErrorResult("No funding stream were returned");
                }

                foreach (FundingStream fundingStream in fundingStreams)
                {
                    AllocationLine allocationLine = fundingStream.AllocationLines.FirstOrDefault(m => m.Id == createModel.AllocationLineId);
                    if (allocationLine != null)
                    {
                        calculation.AllocationLine = allocationLine;
                        currentFundingStream = fundingStream;
                        break;
                    }
                }
                if (currentFundingStream == null)
                {
                    return new PreconditionFailedResult($"A funding stream was not found for specification with id: {specification.Id} for allocation ID {createModel.AllocationLineId}");
                }
            }

            specificationVersion.Calculations = (specificationVersion.Calculations == null
                ? new[] { calculation }
                : specificationVersion.Calculations.Concat(new[] { calculation }));

            HttpStatusCode statusCode = await UpdateSpecification(specification, specificationVersion, previousSpecificationVersion);
            if (statusCode != HttpStatusCode.OK)
            {
                _logger.Error($"Failed to update specification when creating a calc with status {statusCode}");
                return new StatusCodeResult((int)statusCode);
            }

            await ReindexSpecification(specification);

            IDictionary<string, string> properties = request.BuildMessageProperties();

            await _messengerService.SendToQueue(ServiceBusConstants.QueueNames.CreateDraftCalculation,
                new Models.Calcs.Calculation
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = calculation.Name,
                    CalculationSpecification = new Reference(calculation.Id, calculation.Name),
                    AllocationLine = calculation.AllocationLine,
                    CalculationType = (Models.Calcs.CalculationType)calculation.CalculationType,
                    SpecificationId = specification.Id,
                    FundingPeriod = specificationVersion.FundingPeriod,
                    FundingStream = currentFundingStream,
                },
                properties);

            return new OkObjectResult(calculation);
        }

        public async Task<IActionResult> EditCalculation(HttpRequest request)
        {
            Guard.ArgumentNotNull(request, nameof(request));

            request.Query.TryGetValue("specificationId", out StringValues specId);

            string specificationId = specId.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification Id was provided to EditCalculation");
                return new BadRequestObjectResult("Null or empty specification Id provided");
            }

            request.Query.TryGetValue("calculationId", out StringValues calcId);

            string calculationId = calcId.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(calculationId))
            {
                _logger.Error("No calculation Id was provided to EditCalculation");
                return new BadRequestObjectResult("Null or empty calculation Id provided");
            }

            string json = await request.GetRawBodyStringAsync();
            CalculationEditModel editModel = JsonConvert.DeserializeObject<CalculationEditModel>(json);

            if (editModel == null)
            {
                _logger.Error("Null calculation edit model provided to EditCalculation");
                return new BadRequestObjectResult("Null calculation edit model provided");
            }

            editModel.CalculationId = calculationId;
            editModel.SpecificationId = specificationId;

            BadRequestObjectResult validationResult = (await _calculationEditModelValidator.ValidateAsync(editModel)).PopulateModelState();
            if (validationResult != null)
            {
                _logger.Error("Invalid data was provided for EdditCalculation");
                return validationResult;
            }

            Specification specification = await _specificationsRepository.GetSpecificationById(specificationId);
            if (specification == null)
            {
                _logger.Warning($"Specification not found for specification id {specificationId}");
                return new PreconditionFailedResult($"Specification not found for specification id {specificationId}");
            }

            SpecificationVersion previousSpecificationVersion = specification.Current;

            SpecificationVersion specificationVersion = specification.Current.Clone() as SpecificationVersion;

            Calculation calculation = specificationVersion.Calculations?.FirstOrDefault(m => m.Id == calculationId);

            if (calculation == null)
            {
                _logger.Warning($"Calculation not found for calculation id '{calculationId}'");
                return new NotFoundObjectResult($"Calculation not found for calculation id '{calculationId}'");
            }

            Calculation previousCalculation = calculation.Clone();

            calculation.Name = editModel.Name;
            calculation.Description = editModel.Description;
            calculation.LastUpdated = DateTimeOffset.Now;
            calculation.CalculationType = editModel.CalculationType;

            FundingStream currentFundingStream = null;
            if (!string.IsNullOrWhiteSpace(editModel.AllocationLineId))
            {
                string[] fundingSteamIds = specificationVersion.FundingStreams.Select(s => s.Id).ToArray();
                Common.ApiClient.Models.ApiResponse<PolicyModels.FundingStream> fundingStreamResponse = await _policiesApiClientPolicy.ExecuteAsync(() => _policiesApiClient.GetFundingStreamById(fundingSteamIds.FirstOrDefault()));
                if (fundingStreamResponse?.Content == null)
                {
                    _logger.Error("No funding streams were returned");

                    return new InternalServerErrorResult("No funding stream were returned");
                }

                FundingStream fundingStream = _mapper.Map<FundingStream>(fundingStreamResponse?.Content);


                AllocationLine allocationLine = fundingStream.AllocationLines.FirstOrDefault(m => m.Id == editModel.AllocationLineId);
                if (allocationLine != null)
                {
                    calculation.AllocationLine = allocationLine;
                    currentFundingStream = fundingStream;
                }

                if (currentFundingStream == null)
                {
                    return new PreconditionFailedResult($"A funding stream was not found for specification with id: {specification.Id} for allocation ID {editModel.AllocationLineId}");
                }
            }

            HttpStatusCode statusCode = await UpdateSpecification(specification, specificationVersion, previousSpecificationVersion);
            if (statusCode != HttpStatusCode.OK)
            {
                _logger.Error($"Failed to update specification when creating a calc with status {statusCode}");
                return new StatusCodeResult((int)statusCode);
            }

            await ReindexSpecification(specification);

            await SendCalculationComparisonModelMessageToTopic(specificationId, calculationId, ServiceBusConstants.TopicNames.EditCalculation, calculation, previousCalculation, request);

            return new OkObjectResult(calculation);
        }

        public async Task AssignDataDefinitionRelationship(Message message)
        {
            AssignDefinitionRelationshipMessage relationshipMessage = message.GetPayloadAsInstanceOf<AssignDefinitionRelationshipMessage>();

            if (relationshipMessage == null)
            {
                _logger.Error("A null relationship message was provided to AssignDataDefinitionRelationship");

                throw new ArgumentNullException(nameof(relationshipMessage));
            }
            else
            {
                FluentValidation.Results.ValidationResult validationResult = await _assignDefinitionRelationshipMessageValidator.ValidateAsync(relationshipMessage);

                if (!validationResult.IsValid)
                {
                    throw new InvalidModelException(GetType().ToString(), validationResult.Errors.Select(m => m.ErrorMessage).ToArraySafe());
                }

                string specificationId = relationshipMessage.SpecificationId;

                string relationshipId = relationshipMessage.RelationshipId;

                Specification specification = await _specificationsRepository.GetSpecificationById(specificationId);
                if (specification == null)
                {
                    throw new InvalidModelException(relationshipMessage.GetType().ToString(), new[] { $"Specification could not be found for id {specificationId}" });
                }

                SpecificationVersion previousSpecificationVersion = specification.Current;

                SpecificationVersion specificationVersion = specification.Current.Clone() as SpecificationVersion;

                if (specificationVersion.DataDefinitionRelationshipIds.IsNullOrEmpty())
                {
                    specificationVersion.DataDefinitionRelationshipIds = new string[0];
                }

                if (!specificationVersion.DataDefinitionRelationshipIds.Contains(relationshipId))
                {
                    specificationVersion.DataDefinitionRelationshipIds = specificationVersion.DataDefinitionRelationshipIds.Concat(new[] { relationshipId });
                }

                HttpStatusCode status = await UpdateSpecification(specification, specificationVersion, previousSpecificationVersion);

                if (!status.IsSuccess())
                {
                    _logger.Error($"Failed to update specification for id: {specificationId} with dataset definition relationship id {relationshipId}");

                    throw new Exception($"Failed to update specification for id: {specificationId} with dataset definition relationship id {relationshipId}");
                }

                SpecificationIndex specIndex = CreateSpecificationIndex(specification);

                IEnumerable<IndexError> errors = await _searchRepository.Index(new List<SpecificationIndex> { specIndex });

                if (errors.Any())
                {
                    _logger.Error($"failed to index search with the following errors: {string.Join(";", errors.Select(m => m.ErrorMessage))}");

                    throw new FailedToIndexSearchException(errors);
                }

                _logger.Information($"Successfully assigned relationship id: {relationshipId} to specification with id: {specificationId}");
            }
        }

        public async Task<IActionResult> ReIndex()
        {
            try
            {
                await _searchRepository.DeleteIndex();

                SqlQuerySpec sqlQuerySpec = new SqlQuerySpec
                {
                    QueryText = @"
SELECT  s.id, 
        s.content.current.name, 
        s.content.current.fundingStreams,
        s.content.current.fundingPeriod,
        s.content.current.publishStatus, 
        s.content.current.description, 
        s.content.current.dataDefinitionRelationshipIds, 
        s.content.publishedResultsRefreshedAt, 
        s.updatedAt
FROM    specs s 
WHERE   s.documentType = @DocumentType",
                    Parameters = new SqlParameterCollection
                    {
                        new SqlParameter("@DocumentType", "Specification")
                    }
                };

                IEnumerable<SpecificationSearchModel> specifications = (await _specificationsRepository.GetSpecificationsByRawQuery<SpecificationSearchModel>(sqlQuerySpec)).ToArraySafe();

                List<SpecificationIndex> specDocuments = new List<SpecificationIndex>();

                specDocuments = specifications.Select(specification => new SpecificationIndex
                {
                    Id = specification.Id,
                    Name = specification.Name,
                    FundingStreamIds = specification.FundingStreams?.Select(s => s.Id).ToArray(),
                    FundingStreamNames = specification.FundingStreams?.Select(s => s.Name).ToArray(),
                    FundingPeriodId = specification.FundingPeriod.Id,
                    FundingPeriodName = specification.FundingPeriod.Name,
                    LastUpdatedDate = specification.UpdatedAt,
                    Status = specification.PublishStatus,
                    Description = specification.Description,
                    IsSelectedForFunding = specification.IsSelectedForFunding,
                    DataDefinitionRelationshipIds = specification.DataDefinitionRelationshipIds.IsNullOrEmpty()
                        ? new string[0]
                        : specification.DataDefinitionRelationshipIds,
                    PublishedResultsRefreshedAt = specification.PublishedResultsRefreshedAt
                }).ToList();

                if (!specDocuments.IsNullOrEmpty())
                {
                    await _searchRepository.Index(specDocuments);
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

        public async Task<IActionResult> RefreshPublishedResults(HttpRequest request)
        {
            request.Query.TryGetValue("specificationId", out StringValues specId);

            string specificationId = specId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Warning("No specification Id was provided to SelectSpecificationForFunding");
                return new BadRequestObjectResult("Null or empty specification Id provided");
            }

            Specification specification = await _specificationsRepository.GetSpecificationById(specificationId);

            if (specification == null)
            {
                return new BadRequestObjectResult($"Specification {specificationId} - was not found");
            }

            try
            {
                await PublishProviderResults(request, specificationId, "Refreshing published provider results for specification");

                return new NoContentResult();
            }
            catch (Exception e)
            {
                return new InternalServerErrorResult(e.Message);
            }
        }

        public async Task<IActionResult> SelectSpecificationForFunding(HttpRequest request)
        {
            request.Query.TryGetValue("specificationId", out StringValues specId);

            var specificationId = specId.FirstOrDefault();

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
                    $"Attempt to mark specification with id: {specificationId} selected when alreday selected");

                return new NoContentResult();
            }

            if (await SharesFundingStreamWithAnyOtherSpecificationSelectedForFundingInTheSamePeriod(specification))
            {
                return new ConflictResult();
            }

            specification.IsSelectedForFunding = true;

            SpecificationIndex specificationIndex = null;

            try
            {
                HttpStatusCode statusCode = await _specificationsRepository.UpdateSpecification(specification);

                if (!statusCode.IsSuccess())
                {
                    var error =
                        $"Failed to set IsSelectedForFunding on specification for id: {specificationId} with status code: {statusCode.ToString()}";
                    _logger.Error(error);
                    return new InternalServerErrorResult(error);
                }

                specificationIndex = CreateSpecificationIndex(specification);

                var errors = await _searchRepository.Index(new List<SpecificationIndex> {specificationIndex});

                if (errors.Any())
                {
                    var error =
                        $"Failed to index search for specification {specificationId} with the following errors: {string.Join(";", errors.Select(m => m.ErrorMessage))}";
                    _logger.Error(error);
                    throw new Exception(error);
                }

                await _cacheProvider.RemoveAsync<SpecificationSummary>(
                    $"{CacheKeys.SpecificationSummaryById}{specification.Id}");
                await _cacheProvider.RemoveAsync<SpecificationCurrentVersion>(
                    $"{CacheKeys.SpecificationCurrentVersionById}{specification.Id}");

                await PublishProviderResults(request, specificationId, "Selecting specification for funding");
            }
            catch (Exception ex)
            {
                specification.IsSelectedForFunding = false;

                specificationIndex = CreateSpecificationIndex(specification);

                await TaskHelper.WhenAllAndThrow(
                    _specificationsRepository.UpdateSpecification(specification),
                    _searchRepository.Index(new[] {specificationIndex})
                );

                await _cacheProvider.RemoveAsync<SpecificationSummary>(
                    $"{CacheKeys.SpecificationSummaryById}{specification.Id}");
                await _cacheProvider.RemoveAsync<SpecificationCurrentVersion>(
                    $"{CacheKeys.SpecificationCurrentVersionById}{specification.Id}");

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
                _logger.Warning($"Specifications selected for publishing in funding period {fundingPeriodId} returned null");

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
                fundingStreams.Intersect(_.Current?.FundingStreams?.Select(fs => fs.Id)).Any());
        }

        private async Task PublishProviderResults(HttpRequest request, string specificationId, string triggerMessage)
        {
            try
            {
                Reference user = request.GetUser();

                Trigger trigger = new Trigger
                {
                    EntityId = specificationId,
                    EntityType = nameof(Specification),
                    Message = triggerMessage
                };

                string correlationId = request.GetCorrelationId();

                JobCreateModel job = new JobCreateModel
                {
                    InvokerUserDisplayName = user.Name,
                    InvokerUserId = user.Id,
                    JobDefinitionId = JobConstants.DefinitionNames.PublishProviderResultsJob,
                    Properties = new Dictionary<string, string>
                    {
                        { "specification-id", specificationId }
                    },
                    SpecificationId = specificationId,
                    Trigger = trigger,
                    CorrelationId = correlationId
                };

                UpdateCacheWithCalculationStarted(specificationId);
                await _jobsApiClientPolicy.ExecuteAsync(() => _jobsApiClient.CreateJob(job));
            }
            catch (Exception ex)
            {
                string error = $"Failed to queue publishing of provider results for specification id: {specificationId}";
                UpdateCacheWithCalculationError(specificationId, error);
                _logger.Error(error);
                throw new Exception(error);
            }
        }

        public async Task<IActionResult> CheckPublishResultStatus(HttpRequest request)
        {
            if (request == null)
            {
                _logger.Error("The http request is null");
                return new BadRequestObjectResult("The request is null");
            }

            if (request.Query == null)
            {
                _logger.Error("The http request query is empty or null");
                return new BadRequestObjectResult("the request query is empty or null");
            }

            request.Query.TryGetValue("specificationId", out StringValues specificationId);

            try
            {
                SpecificationCalculationExecutionStatus specProgress = await _cacheProvider.GetAsync<SpecificationCalculationExecutionStatus>($"{CacheKeys.CalculationProgress}{specificationId}");
                if (specProgress == null)
                {
                    _logger.Error("Cache returned null, couldn't find specification - {specificationId}", specificationId);
                    return new BadRequestObjectResult($"Couldn't find progress statement for specification - {specificationId}");
                }
                return new OkObjectResult(specProgress);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to retrieve calculation progress from cache - {specificationId}", specificationId);
                return new InternalServerErrorResult(ex.Message);
            }
        }

        private async Task<HttpStatusCode> UpdateSpecification(Specification specification, SpecificationVersion specificationVersion, SpecificationVersion previousVersion)
        {
            specificationVersion = await _specificationVersionRepository.CreateVersion(specificationVersion, previousVersion);

            specification.Current = specificationVersion;

            HttpStatusCode result = await _specificationsRepository.UpdateSpecification(specification);

            if (result == HttpStatusCode.OK)
            {
                await _specificationVersionRepository.SaveVersion(specificationVersion);

                await _cacheProvider.RemoveAsync<SpecificationSummary>($"{CacheKeys.SpecificationSummaryById}{specification.Id}");
                await _cacheProvider.RemoveAsync<SpecificationCurrentVersion>($"{CacheKeys.SpecificationCurrentVersionById}{specification.Id}");
            }

            return result;
        }

        private async Task<HttpStatusCode> PublishSpecification(Specification specification, SpecificationVersion specificationVersion, SpecificationVersion previousVersion)
        {
            specificationVersion = await _specificationVersionRepository.CreateVersion(specificationVersion, previousVersion);

            specification.Current = specificationVersion;

            HttpStatusCode result = await _specificationsRepository.UpdateSpecification(specification);
            if (result == HttpStatusCode.OK)
            {
                await _specificationVersionRepository.SaveVersion(specificationVersion);

                await _cacheProvider.RemoveAsync<SpecificationSummary>($"{CacheKeys.SpecificationSummaryById}{specification.Id}");
                await _cacheProvider.RemoveAsync<SpecificationCurrentVersion>($"{CacheKeys.SpecificationCurrentVersionById}{specification.Id}");
            }

            return result;
        }

        private void UpdateCacheWithCalculationStarted(string specificationId)
        {
            SpecificationCalculationExecutionStatus statusToCache = new SpecificationCalculationExecutionStatus(specificationId, 0, CalculationProgressStatus.NotStarted);
            CacheHelper.UpdateCacheForItem($"{CacheKeys.CalculationProgress}{specificationId}", statusToCache, _cacheProvider);
        }

        private void UpdateCacheWithCalculationError(string specificationId, string errorMessage)
        {
            SpecificationCalculationExecutionStatus statusToCache =
                new SpecificationCalculationExecutionStatus(specificationId, 0, CalculationProgressStatus.Error)
                {
                    ErrorMessage = errorMessage
                };
            CacheHelper.UpdateCacheForItem($"{CacheKeys.CalculationProgress}{specificationId}", statusToCache, _cacheProvider);
        }

        private async Task ClearSpecificationCacheItems(string fundingPeriodId)
        {
            await TaskHelper.WhenAllAndThrow(
                _cacheProvider.RemoveAsync<List<SpecificationSummary>>(CacheKeys.SpecificationSummaries),
                _cacheProvider.RemoveAsync<List<SpecificationSummary>>($"{CacheKeys.SpecificationSummariesByFundingPeriodId}{fundingPeriodId}")
                );
        }

        private IDictionary<string, string> CreateMessageProperties(HttpRequest request)
        {
            Reference user = request.GetUser();

            IDictionary<string, string> properties = new Dictionary<string, string>
            {
                { "sfa-correlationId", request.GetCorrelationId() }
            };

            if (user != null)
            {
                properties.Add("user-id", user.Id);
                properties.Add("user-name", user.Name);
            }

            return properties;
        }

        private static SpecificationCurrentVersion ConvertSpecificationToCurrentVersion(DocumentEntity<Specification> specification, IEnumerable<FundingStream> fundingStreams)
        {
            return new SpecificationCurrentVersion()
            {
                DataDefinitionRelationshipIds = specification.Content.Current.DataDefinitionRelationshipIds,
                Description = specification.Content.Current.Description,
                FundingPeriod = specification.Content.Current.FundingPeriod,
                Id = specification.Content.Id,
                LastUpdatedDate = specification.UpdatedAt,
                Name = specification.Content.Name,
                Calculations = specification.Content.Current.Calculations,
                ProviderVersionId = specification.Content.Current.ProviderVersionId,
                FundingStreams = fundingStreams,
                PublishStatus = specification.Content.Current.PublishStatus,
                IsSelectedForFunding = specification.Content.IsSelectedForFunding,
                PublishedResultsRefreshedAt = specification.Content.PublishedResultsRefreshedAt,
                LastCalculationUpdatedAt = specification.Content.LastCalculationUpdatedAt,
                VariationDate = specification.Content.Current.VariationDate,
                TemplateId = specification.Content.Current.TemplateId
            };
        }

        private SpecificationIndex CreateSpecificationIndex(Specification specification)
        {
            SpecificationVersion specificationVersion = specification.Current.Clone() as SpecificationVersion;

            return new SpecificationIndex
            {
                Id = specification.Id,
                Name = specification.Name,
                IsSelectedForFunding = specification.IsSelectedForFunding,
                Status = Enum.GetName(typeof(PublishStatus), specificationVersion.PublishStatus),
                Description = specificationVersion.Description,
                FundingStreamIds = specificationVersion.FundingStreams.Select(s => s.Id).ToArray(),
                FundingStreamNames = specificationVersion.FundingStreams.Select(s => s.Name).ToArray(),
                FundingPeriodId = specificationVersion.FundingPeriod.Id,
                FundingPeriodName = specificationVersion.FundingPeriod.Name,
                LastUpdatedDate = DateTimeOffset.Now,
                DataDefinitionRelationshipIds = specificationVersion.DataDefinitionRelationshipIds.ToArraySafe(),
                PublishedResultsRefreshedAt = specification.PublishedResultsRefreshedAt
            };
        }
    }
}
