using CalculateFunding.Models.Specs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using AutoMapper;
using CalculateFunding.Services.Specs.Interfaces;
using CalculateFunding.Models;
using System.Linq;
using System.Net;
using FluentValidation;
using CalculateFunding.Services.Core.Extensions;
using Serilog;
using CalculateFunding.Services.Core.Options;
using System;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Models.Specs.Messages;
using CalculateFunding.Models.Exceptions;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using Microsoft.Azure.ServiceBus;
using CalculateFunding.Services.Core.Constants;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Core.Helpers;

namespace CalculateFunding.Services.Specs
{
    public class SpecificationsService : ISpecificationsService
    {
        private readonly IMapper _mapper;
        private readonly ISpecificationsRepository _specificationsRepository;
        private readonly ILogger _logger;
        private readonly IValidator<PolicyCreateModel> _policyCreateModelValidator;
        private readonly IValidator<SpecificationCreateModel> _specificationCreateModelvalidator;
        private readonly IValidator<CalculationCreateModel> _calculationCreateModelValidator;
        private readonly IMessengerService _messengerService;
        private readonly ServiceBusSettings _eventHubSettings;
        private readonly ISearchRepository<SpecificationIndex> _searchRepository;
        private readonly IValidator<AssignDefinitionRelationshipMessage> _assignDefinitionRelationshipMessageValidator;
        private readonly IValidator<SpecificationEditModel> _specificationEditModelValidator;
        private readonly ICacheProvider _cacheProvider;

        public SpecificationsService(
            IMapper mapper,
            ISpecificationsRepository specificationsRepository,
            ILogger logger,
            IValidator<PolicyCreateModel> policyCreateModelValidator,
            IValidator<SpecificationCreateModel> specificationCreateModelvalidator,
            IValidator<CalculationCreateModel> calculationCreateModelValidator,
            IMessengerService messengerService, ServiceBusSettings eventHubSettings,
            ISearchRepository<SpecificationIndex> searchRepository,
            IValidator<AssignDefinitionRelationshipMessage> assignDefinitionRelationshipMessageValidator,
            ICacheProvider cacheProvider,
            IValidator<SpecificationEditModel> specificationEditModelValidator)
        {
            _mapper = mapper;
            _specificationsRepository = specificationsRepository;
            _logger = logger;
            _policyCreateModelValidator = policyCreateModelValidator;
            _specificationCreateModelvalidator = specificationCreateModelvalidator;
            _calculationCreateModelValidator = calculationCreateModelValidator;
            _messengerService = messengerService;
            _eventHubSettings = eventHubSettings;
            _searchRepository = searchRepository;
            _assignDefinitionRelationshipMessageValidator = assignDefinitionRelationshipMessageValidator;
            _cacheProvider = cacheProvider;
            _specificationEditModelValidator = specificationEditModelValidator;
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
            request.Query.TryGetValue("specificationId", out var specId);

            var specificationId = specId.FirstOrDefault();

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
            request.Query.TryGetValue("specificationId", out var specId);

            var specificationId = specId.FirstOrDefault();

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
            request.Query.TryGetValue("specificationId", out var specId);

            var specificationId = specId.FirstOrDefault();

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

                result = new SpecificationCurrentVersion()
                {
                    DataDefinitionRelationshipIds = specification.Content.Current.DataDefinitionRelationshipIds,
                    Description = specification.Content.Current.Description,
                    FundingPeriod = specification.Content.Current.FundingPeriod,
                    FundingStreams = specification.Content.Current.FundingStreams,
                    Id = specification.Content.Id,
                    LastUpdatedDate = specification.UpdatedAt,
                    Name = specification.Content.Name,
                    Policies = specification.Content.Current.Policies,
                };

                await _cacheProvider.SetAsync(cacheKey, result, TimeSpan.FromDays(1), true);
            }

            return new OkObjectResult(result);
        }

        public async Task<IActionResult> GetSpecificationsByFundingPeriodId(HttpRequest request)
        {
            request.Query.TryGetValue("fundingPeriodId", out var yearId);

            var fundingPeriodId = yearId.FirstOrDefault();

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

        public async Task<IActionResult> GetSpecificationByName(HttpRequest request)
        {
            request.Query.TryGetValue("specificationName", out var specName);

            var specificationName = specName.FirstOrDefault();

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

        public async Task<IActionResult> GetPolicyByName(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            PolicyGetModel model = JsonConvert.DeserializeObject<PolicyGetModel>(json);

            if (string.IsNullOrWhiteSpace(model.SpecificationId))
            {
                _logger.Error("No specification id was provided to GetPolicyByName");
                return new BadRequestObjectResult("Null or empty specification id provided");
            }

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                _logger.Error("No policy name was provided to GetPolicyByName");
                return new BadRequestObjectResult("Null or empty policy name provided");
            }

            Specification specification = await _specificationsRepository.GetSpecificationById(model.SpecificationId);

            if (specification == null)
            {
                _logger.Error($"No specification was found for specification id {model.SpecificationId}");
                return new StatusCodeResult(412);
            }

            Policy policy = specification.Current.GetPolicyByName(model.Name);

            if (policy != null)
            {
                _logger.Information($"A policy was found for specification id {model.SpecificationId} and name {model.Name}");

                return new OkObjectResult(policy);
            }

            _logger.Information($"A policy was not found for specification id {model.SpecificationId} and name {model.Name}");

            return new NotFoundResult();
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

        public async Task<IActionResult> GetCalculationBySpecificationIdAndCalculationId(HttpRequest request)
        {
            request.Query.TryGetValue("specificationId", out var specId);

            var specificationId = specId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification Id was provided to GetCalculationBySpecificationIdAndCalculationId");

                return new BadRequestObjectResult("Null or empty specification Id provided");
            }

            request.Query.TryGetValue("calculationId", out var calcId);

            var calculationId = calcId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(calculationId))
            {
                _logger.Error("No calculation Id was provided to GetCalculationBySpecificationIdAndCalculationId");

                return new BadRequestObjectResult("Null or empty specification Id provided");
            }

            Calculation calculation = await _specificationsRepository.GetCalculationBySpecificationIdAndCalculationId(specificationId, calculationId);

            if (calculation != null)
            {
                _logger.Information($"A calculation was found for specification id {specificationId} and calculation id {calculationId}");

                return new OkObjectResult(calculation);
            }

            _logger.Information($"A calculation was not found for specification id {specificationId} and calculation id {calculationId}");

            return new NotFoundResult();
        }

        public async Task<IActionResult> GetCalculationsBySpecificationId(HttpRequest request)
        {
            request.Query.TryGetValue("specificationId", out var specId);

            var specificationId = specId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification Id was provided to GetCalculationsBySpecificationId");

                return new BadRequestObjectResult("Null or empty specification Id provided");
            }

            IEnumerable<Calculation> calculations = await _specificationsRepository.GetCalculationsBySpecificationId(specificationId);

            if (calculations != null)
            {
                _logger.Verbose("Calculations were found for specification id {specificationId}", specificationId);

                return new OkObjectResult(calculations);
            }

            _logger.Error("No calculations could be retrieved found for specification id {specificationId}", specificationId);

            return new NotFoundObjectResult("No calculations could be retrieved");
        }

        public async Task<IActionResult> GetFundingPeriods(HttpRequest request)
        {
            IEnumerable<FundingPeriod> fundingPeriods = await _cacheProvider.GetAsync<FundingPeriod[]>(CacheKeys.FundingPeriods);

            if (fundingPeriods.IsNullOrEmpty())
            {
                fundingPeriods = await _specificationsRepository.GetFundingPeriods();

                if (!fundingPeriods.IsNullOrEmpty())
                {
                    await _cacheProvider.SetAsync<FundingPeriod[]>(CacheKeys.FundingPeriods, fundingPeriods.ToArraySafe(), TimeSpan.FromDays(100), true);
                }
                else
                {
                    return new InternalServerErrorResult("Failed to find any funding periods");
                }
            }

            return new OkObjectResult(fundingPeriods);
        }

        public async Task<IActionResult> GetFundingStreams(HttpRequest request)
        {
            IEnumerable<FundingStream> fundingStreams = await _specificationsRepository.GetFundingStreams();

            if (fundingStreams.IsNullOrEmpty())
            {
                _logger.Error("No funding streams were returned");

                fundingStreams = new FundingStream[0];
            }

            return new OkObjectResult(fundingStreams);
        }

        public async Task<IActionResult> GetFundingStreamsForSpecificationById(HttpRequest request)
        {
            request.Query.TryGetValue("specificationId", out var specificationIdParse);

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

            IEnumerable<FundingStream> fundingStreams = await _specificationsRepository.GetFundingStreams(f => fundingSteamIds.Contains(f.Id));

            if (fundingStreams.IsNullOrEmpty())
            {
                _logger.Error("No funding streams were returned");

                return new InternalServerErrorResult("No funding stream were returned");
            }

            return new OkObjectResult(fundingStreams);
        }

        public async Task<IActionResult> GetFundingStreamById(HttpRequest request)
        {
            request.Query.TryGetValue("fundingStreamId", out var funStreamId);

            string fundingStreamId = funStreamId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(fundingStreamId))
            {
                _logger.Error("No funding stream Id was provided to GetFundingStreamById");

                return new BadRequestObjectResult("Null or empty funding stream Id provided");
            }

            FundingStream fundingStream = await _specificationsRepository.GetFundingStreamById(fundingStreamId);

            if (fundingStream == null)
            {
                _logger.Error($"No funding stream was found for funding stream id : {fundingStreamId}");

                return new NotFoundResult();
            }

            return new OkObjectResult(fundingStream);
        }

        public async Task<IActionResult> CreatePolicy(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            PolicyCreateModel createModel = JsonConvert.DeserializeObject<PolicyCreateModel>(json);

            if (createModel == null)
                return new BadRequestObjectResult("Null policy create model provided");

            var validationResult = (await _policyCreateModelValidator.ValidateAsync(createModel)).PopulateModelState();

            if (validationResult != null)
                return validationResult;

            Specification specification = await _specificationsRepository.GetSpecificationById(createModel.SpecificationId);

            if (specification == null)
                return new NotFoundResult();

            Policy policy = _mapper.Map<Policy>(createModel);

            SpecificationVersion specificationVersion = specification.Current.Clone() as SpecificationVersion;

            if (!string.IsNullOrWhiteSpace(createModel.ParentPolicyId))
            {
                Policy parentPolicy = specificationVersion.GetPolicy(createModel.ParentPolicyId);

                parentPolicy.SubPolicies = (parentPolicy.SubPolicies == null
                    ? new[] { policy }
                    : parentPolicy.SubPolicies.Concat(new[] { policy }));
            }
            else
            {
                specificationVersion.Policies = (specificationVersion.Policies == null
                   ? new[] { policy }
                   : specificationVersion.Policies.Concat(new[] { policy }));
            }

            HttpStatusCode statusCode = await UpdateSpecification(specification, specificationVersion);

            if (statusCode != HttpStatusCode.OK)
                return new StatusCodeResult((int)statusCode);

            return new OkObjectResult(policy);
        }

        public async Task<IActionResult> CreateSpecification(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            SpecificationCreateModel createModel = JsonConvert.DeserializeObject<SpecificationCreateModel>(json);

            if (createModel == null)
                return new BadRequestObjectResult("Null policy create model provided");

            var validationResult = (await _specificationCreateModelvalidator.ValidateAsync(createModel)).PopulateModelState();

            if (validationResult != null)
                return validationResult;

            FundingPeriod fundingPeriod = await _specificationsRepository.GetFundingPeriodById(createModel.FundingPeriodId);

            Reference user = request.GetUser();

            Specification specification = new Specification()
            {
                Name = createModel.Name,
                Id = Guid.NewGuid().ToString(),
            };
            specification.Init();

            SpecificationVersion specificationVersion = new SpecificationVersion
            {
                Name = createModel.Name,
                FundingPeriod = new Reference(fundingPeriod.Id, fundingPeriod.Name),
                Description = createModel.Description,
                Policies = new List<Policy>(),
                DataDefinitionRelationshipIds = new List<string>(),
                Author = user,
            };

            List<Reference> fundingStreams = new List<Reference>();
            foreach (string fundingStreamId in createModel.FundingStreamIds)
            {
                FundingStream fundingStream = await _specificationsRepository.GetFundingStreamById(fundingStreamId);
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

            specification.Save(specificationVersion);

            HttpStatusCode statusCode = await _specificationsRepository.CreateSpecification(specification);

            if (!statusCode.IsSuccess())
                return new StatusCodeResult((int)statusCode);

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
                    LastUpdatedDate = DateTimeOffset.Now
                }
            });

            await ClearSpecificationCacheItems(specificationVersion.FundingPeriod.Id);

            return new OkObjectResult(specification);
        }

        public async Task<IActionResult> EditSpecification(HttpRequest request)
        {
            request.Query.TryGetValue("specificationId", out var specId);
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

            var validationResult = (await _specificationEditModelValidator.ValidateAsync(editModel)).PopulateModelState();
            if (validationResult != null)
                return validationResult;

            Specification specification = await _specificationsRepository.GetSpecificationById(specificationId);
            if (specification == null)
            {
                _logger.Warning($"Failed to find specification for id: {specificationId}");
                return new NotFoundObjectResult("Specification not found");
            }

            Reference user = request.GetUser();

            SpecificationVersion previousSpecificationVersion = specification.Current;

            SpecificationVersion specificationVersion = specification.Current.Clone() as SpecificationVersion;

            specificationVersion.Name = editModel.Name;
            specificationVersion.Description = editModel.Description;
            specificationVersion.Author = user;

            specification.Name = editModel.Name;

            string previousFundingPeriod = specificationVersion.FundingPeriod.Id;

            if (editModel.FundingPeriodId != specificationVersion.FundingPeriod.Id)
            {
                FundingPeriod fundingPeriod = await _specificationsRepository.GetFundingPeriodById(editModel.FundingPeriodId);
                if (fundingPeriod == null)
                {
                    return new PreconditionFailedResult($"Unable to find funding period with ID '{editModel.FundingPeriodId}'.");
                }
                specificationVersion.FundingPeriod = new Reference { Id = fundingPeriod.Id, Name = fundingPeriod.Name };
            }

            IEnumerable<string> existingFundingStreamIds = specificationVersion.FundingStreams.Select(m => m.Id);

            bool fundingStreamsChanged = !existingFundingStreamIds.EqualTo(editModel.FundingStreamIds);
            if (fundingStreamsChanged)
            {
                string[] fundingStreamIds = editModel.FundingStreamIds.ToArray();

                IEnumerable<FundingStream> fundingStreams = await _specificationsRepository.GetFundingStreams(f => fundingStreamIds.Contains(f.Id));

                if (!fundingStreams.Any())
                {
                    return new InternalServerErrorResult("No funding streams were retrieved to add to the Specification");
                }

                List<Reference> fundingStreamReferences = new List<Reference>();

                Dictionary<string, bool> allocationLines = new Dictionary<string, bool>();

                foreach (FundingStream fundingStream in fundingStreams)
                {
                    fundingStreamReferences.Add(new Reference(fundingStream.Id, fundingStream.Name));
                    foreach (AllocationLine allocationLine in fundingStream.AllocationLines)
                    {
                        allocationLines.Add(allocationLine.Id, true);
                    }
                }

                specificationVersion.FundingStreams = fundingStreamReferences;

                RemoveMissingAllocationLineAssociations(allocationLines, specificationVersion.Policies);
            }

            HttpStatusCode statusCode = await UpdateSpecification(specification, specificationVersion);
            if (!statusCode.IsSuccess())
                return new StatusCodeResult((int)statusCode);

            await _searchRepository.Index(new List<SpecificationIndex>
            {
                new SpecificationIndex
                {
                    Id = specification.Id,
                    Name = specificationVersion.Name,
                    FundingStreamIds = specificationVersion.FundingStreams.Select(s=>s.Id).ToArray(),
                    FundingStreamNames = specificationVersion.FundingStreams.Select(s=>s.Name).ToArray(),
                    FundingPeriodId = specificationVersion.FundingPeriod.Id,
                    FundingPeriodName = specificationVersion.FundingPeriod.Name,
                    LastUpdatedDate = DateTimeOffset.Now
                }
            });

            await TaskHelper.WhenAllAndThrow(
                ClearSpecificationCacheItems(specificationVersion.FundingPeriod.Id),
                _cacheProvider.RemoveAsync<SpecificationSummary>($"{CacheKeys.SpecificationSummaryById}{specification.Id}")
                );

            if (previousFundingPeriod != specificationVersion.FundingPeriod.Id)
            {
                await _cacheProvider.RemoveAsync<List<SpecificationSummary>>($"{CacheKeys.SpecificationSummariesByFundingPeriodId}{previousFundingPeriod}");

            }

            await SendMessageToTopic(specificationId, specification.Current, previousSpecificationVersion, request);

            return new OkObjectResult(specification);
        }

        Task SendMessageToTopic(string specificationId, SpecificationVersion current, SpecificationVersion previous, HttpRequest request)
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

            return _messengerService.SendToTopic(ServiceBusConstants.TopicNames.EditSpecification, comparisonModel, properties);
        }

        /// <summary>
        /// Remove Missing Allocation Line Associations
        /// </summary>
        /// <param name="allocationLines">Valid allocation lines</param>
        /// <param name="policies">Policies</param>
        private static void RemoveMissingAllocationLineAssociations(Dictionary<string, bool> allocationLines, IEnumerable<Policy> policies)
        {
            if (policies == null)
            {
                return;
            }

            if (allocationLines.IsNullOrEmpty())
            {
                return;
            }

            foreach (Policy policy in policies)
            {
                if (policy.Calculations != null)
                {
                    foreach (Calculation calculation in policy.Calculations)
                    {
                        if (calculation.AllocationLine != null && !allocationLines.ContainsKey(calculation.AllocationLine.Id))
                        {
                            calculation.AllocationLine = null;
                        }
                    }
                }

                RemoveMissingAllocationLineAssociations(allocationLines, policy.SubPolicies);
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
            var validationResult = (await _calculationCreateModelValidator.ValidateAsync(createModel)).PopulateModelState();
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
            SpecificationVersion specificationVersion = specification.Current.Clone() as SpecificationVersion;
            Policy policy = specificationVersion.GetPolicy(createModel.PolicyId);
            if (policy == null)
            {
                _logger.Warning($"Policy not found for policy id '{createModel.PolicyId}'");
                return new PreconditionFailedResult($"Policy not found for policy id '{createModel.PolicyId}'");
            }
            Calculation calculation = _mapper.Map<Calculation>(createModel);
            FundingStream currentFundingStream = null;
            if (!string.IsNullOrWhiteSpace(createModel.AllocationLineId))
            {
                string[] fundingSteamIds = specificationVersion.FundingStreams.Select(s => s.Id).ToArray();
                IEnumerable<FundingStream> fundingStreams = await _specificationsRepository.GetFundingStreams(f => fundingSteamIds.Contains(f.Id));
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
            policy.Calculations = (policy.Calculations == null
                ? new[] { calculation }
                : policy.Calculations.Concat(new[] { calculation }));
            HttpStatusCode statusCode = await UpdateSpecification(specification, specificationVersion);
            if (statusCode != HttpStatusCode.OK)
            {
                _logger.Error($"Failed to update specification when creating a calc with status {statusCode}");
                return new StatusCodeResult((int)statusCode);
            }
            IDictionary<string, string> properties = CreateMessageProperties(request);
            await _messengerService.SendToQueue(ServiceBusConstants.QueueNames.CreateDraftCalculation,
                new Models.Calcs.Calculation
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = calculation.Name,
                    CalculationSpecification = new Reference(calculation.Id, calculation.Name),
                    AllocationLine = calculation.AllocationLine,
                    CalculationType = calculation.CalculationType,
                    Policies = new List<Reference>
                    {
                        new Reference( policy.Id, policy.Name )
                    },
                    SpecificationId = specification.Id,
                    FundingPeriod = specificationVersion.FundingPeriod,
                    FundingStream = currentFundingStream,
                },
                properties);
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
                var validationResult = await _assignDefinitionRelationshipMessageValidator.ValidateAsync(relationshipMessage);

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

                SpecificationVersion specificationVersion = specification.Current.Clone() as SpecificationVersion;

                if (specificationVersion.DataDefinitionRelationshipIds.IsNullOrEmpty())
                    specificationVersion.DataDefinitionRelationshipIds = new string[0];

                if (!specificationVersion.DataDefinitionRelationshipIds.Contains(relationshipId))
                    specificationVersion.DataDefinitionRelationshipIds = specificationVersion.DataDefinitionRelationshipIds.Concat(new[] { relationshipId });

                HttpStatusCode status = await UpdateSpecification(specification, specificationVersion);

                if (!status.IsSuccess())
                {
                    _logger.Error($"Failed to update specification for id: {specificationId} with dataset definition relationship id {relationshipId}");

                    throw new Exception($"Failed to update specification for id: {specificationId} with dataset definition relationship id {relationshipId}");
                }

                SpecificationIndex specIndex = await _searchRepository.SearchById(specificationId);

                if (specIndex == null)
                {
                    specIndex = new SpecificationIndex
                    {
                        Id = specification.Id,
                        Name = specification.Name,
                        FundingStreamIds = specificationVersion.FundingStreams.Select(s => s.Id).ToArray(),
                        FundingStreamNames = specificationVersion.FundingStreams.Select(s => s.Name).ToArray(),
                        FundingPeriodId = specificationVersion.FundingPeriod.Id,
                        FundingPeriodName = specificationVersion.FundingPeriod.Name,
                        LastUpdatedDate = DateTimeOffset.Now
                    };
                }

                specIndex.DataDefinitionRelationshipIds = specificationVersion.DataDefinitionRelationshipIds.ToArraySafe();

                IEnumerable<IndexError> errors = await _searchRepository.Index(new List<SpecificationIndex> { specIndex });

                if (errors.Any())
                {
                    _logger.Error($"failed to index search with the following errors: {string.Join(";", errors.Select(m => m.ErrorMessage))}");

                    throw new FailedToIndexSearchException(errors);
                }

                _logger.Information($"Succeffuly assigned relationship id: {relationshipId} to specification with id: {specificationId}");
            }
        }

        public async Task<IActionResult> ReIndex()
        {
            try
            {
                await _searchRepository.DeleteIndex();

                const string sql = "select s.id, s.content.name, s.content.current.fundingStream, s.content.current.fundingPeriod, s.content.current.dataDefinitionRelationshipIds, s.updatedAt from specs s";

                IEnumerable<SpecificationSearchModel> specifications = (await _specificationsRepository.GetSpecificationsByRawQuery<SpecificationSearchModel>(sql)).ToArraySafe();

                List<SpecificationIndex> specDocuments = new List<SpecificationIndex>();

                foreach (SpecificationSearchModel specification in specifications)
                {
                    specDocuments.Add(new SpecificationIndex
                    {
                        Id = specification.Id,
                        Name = specification.Name,
                        FundingStreamIds = specification.FundingStreams.Select(s => s.Id).ToArray(),
                        FundingStreamNames = specification.FundingStreams.Select(s => s.Name).ToArray(),
                        FundingPeriodId = specification.FundingPeriod.Id,
                        FundingPeriodName = specification.FundingPeriod.Name,
                        LastUpdatedDate = specification.UpdatedAt,
                        DataDefinitionRelationshipIds = specification.DataDefinitionRelationshipIds.IsNullOrEmpty() ? new string[0] : specification.DataDefinitionRelationshipIds
                    });
                }

                if (!specDocuments.IsNullOrEmpty())
                {
                    await _searchRepository.Index(specDocuments);
                    _logger.Information($"Succesfully re-indexed {specifications.Count()} documents");
                }
                else
                    _logger.Warning("No specification documents were returned from cosmos db");

                return new NoContentResult();
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Failed re-indexing specifications");

                return new StatusCodeResult(500);
            }
        }

        async public Task<IActionResult> SaveFundingStream(HttpRequest request)
        {
            string yaml = await request.GetRawBodyStringAsync();

            string yamlFilename = request.GetYamlFileNameFromRequest();

            if (string.IsNullOrEmpty(yaml))
            {
                _logger.Error($"Null or empty yaml provided for file: {yamlFilename}");
                return new BadRequestObjectResult($"Invalid yaml was provided for file: {yamlFilename}");
            }

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .Build();

            FundingStream fundingStream = null;

            try
            {
                fundingStream = deserializer.Deserialize<FundingStream>(yaml);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"Invalid yaml was provided for file: {yamlFilename}");
                return new BadRequestObjectResult($"Invalid yaml was provided for file: {yamlFilename}");
            }

            try
            {
                HttpStatusCode result = await _specificationsRepository.SaveFundingStream(fundingStream);

                if (!result.IsSuccess())
                {
                    int statusCode = (int)result;

                    _logger.Error($"Failed to save yaml file: {yamlFilename} to cosmos db with status {statusCode}");

                    return new StatusCodeResult(statusCode);
                }
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"Exception occurred writing to yaml file: {yamlFilename} to cosmos db");

                return new StatusCodeResult(500);
            }

            _logger.Information($"Successfully saved file: {yamlFilename} to cosmos db");

            return new OkResult();
        }

        async public Task<IActionResult> SaveFundingPeriods(HttpRequest request)
        {
            string yaml = await request.GetRawBodyStringAsync();

            string yamlFilename = request.GetYamlFileNameFromRequest();

            if (string.IsNullOrEmpty(yaml))
            {
                _logger.Error($"Null or empty yaml provided for file: {yamlFilename}");
                return new BadRequestObjectResult($"Invalid yaml was provided for file: {yamlFilename}");
            }

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .Build();

            FundingPeriodsYamlModel fundingPeriodsYamlModel = null;

            try
            {
                fundingPeriodsYamlModel = deserializer.Deserialize<FundingPeriodsYamlModel>(yaml);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"Invalid yaml was provided for file: {yamlFilename}");
                return new BadRequestObjectResult($"Invalid yaml was provided for file: {yamlFilename}");
            }

            try
            {
                if (!fundingPeriodsYamlModel.FundingPeriods.IsNullOrEmpty())
                {
                    await _specificationsRepository.SaveFundingPeriods(fundingPeriodsYamlModel.FundingPeriods);

                    await _cacheProvider.SetAsync<FundingPeriod[]>(CacheKeys.FundingPeriods, fundingPeriodsYamlModel.FundingPeriods, TimeSpan.FromDays(100), true);

                    _logger.Information($"Upserted {fundingPeriodsYamlModel.FundingPeriods.Length} funding periods into cosomos");
                }
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"Exception occurred writing to yaml file: {yamlFilename} to cosmos db");

                return new StatusCodeResult(500);
            }

            _logger.Information($"Successfully saved file: {yamlFilename} to cosmos db");

            return new OkResult();
        }

        private async Task<HttpStatusCode> UpdateSpecification(Specification specification, SpecificationVersion specificationVersion)
        {
            specification.Save(specificationVersion);
            HttpStatusCode result = await _specificationsRepository.UpdateSpecification(specification);
            if (result == HttpStatusCode.OK)
            {
                await _cacheProvider.RemoveAsync<SpecificationCurrentVersion>($"{CacheKeys.SpecificationCurrentVersionById}{specification.Id}");
            }

            return result;
        }

        private async Task ClearSpecificationCacheItems(string fundingPeriodId)
        {
            await TaskHelper.WhenAllAndThrow(
                _cacheProvider.KeyDeleteAsync<List<SpecificationSummary>>(CacheKeys.SpecificationSummaries),
                _cacheProvider.KeyDeleteAsync<List<SpecificationSummary>>($"{CacheKeys.SpecificationSummariesByFundingPeriodId}{fundingPeriodId}")
                );
        }

        IDictionary<string, string> CreateMessageProperties(HttpRequest request)
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
    }
}
