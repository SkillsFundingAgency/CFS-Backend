using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.TemplateMetadata.Enums;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Polly;
using Serilog;
using Calculation = CalculateFunding.Common.TemplateMetadata.Models.Calculation;
using CalculationType = CalculateFunding.Models.Calcs.CalculationType;
using FundingLine = CalculateFunding.Common.TemplateMetadata.Models.FundingLine;

namespace CalculateFunding.Services.Calcs
{
    public class ApplyTemplateCalculationsService : IApplyTemplateCalculationsService
    {
        private readonly IApplyTemplateCalculationsJobTrackerFactory _jobTrackerFactory;
        private readonly ICreateCalculationService _createCalculationService;
        private readonly ICalculationsRepository _calculationsRepository;
        private readonly IPoliciesApiClient _policiesApiClient;
        private readonly IInstructionAllocationJobCreation _instructionAllocationJobCreation;
        private readonly AsyncPolicy _policiesResiliencePolicy;
        private readonly AsyncPolicy _calculationsRepositoryPolicy;
        private readonly ILogger _logger;
        private readonly ICalculationService _calculationService;
        private readonly AsyncPolicy _cachePolicy;
        private readonly ICacheProvider _cacheProvider;
        private readonly ISpecificationsApiClient _specificationsApiClient;
        private readonly AsyncPolicy _specificationsApiClientPolicy;

        public ApplyTemplateCalculationsService(ICreateCalculationService createCalculationService,
            IPoliciesApiClient policiesApiClient,
            ICalcsResiliencePolicies calculationsResiliencePolicies,
            ICalculationsRepository calculationsRepository,
            IApplyTemplateCalculationsJobTrackerFactory jobTrackerFactory,
            IInstructionAllocationJobCreation instructionAllocationJobCreation,
            ILogger logger,
            ICalculationService calculationService,
            ICacheProvider cacheProvider,
            ISpecificationsApiClient specificationsApiClient,
            IGraphRepository graphRepository)
        {
            Guard.ArgumentNotNull(instructionAllocationJobCreation, nameof(instructionAllocationJobCreation));
            Guard.ArgumentNotNull(jobTrackerFactory, nameof(jobTrackerFactory));
            Guard.ArgumentNotNull(createCalculationService, nameof(createCalculationService));
            Guard.ArgumentNotNull(policiesApiClient, nameof(policiesApiClient));
            Guard.ArgumentNotNull(calculationsRepository, nameof(calculationsRepository));
            Guard.ArgumentNotNull(calculationsResiliencePolicies?.PoliciesApiClient, nameof(calculationsResiliencePolicies.PoliciesApiClient));
            Guard.ArgumentNotNull(calculationsResiliencePolicies?.CalculationsRepository, nameof(calculationsResiliencePolicies.CalculationsRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(calculationService, nameof(calculationService));
            Guard.ArgumentNotNull(calculationsResiliencePolicies?.CacheProviderPolicy, nameof(calculationsResiliencePolicies.CacheProviderPolicy));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(graphRepository, nameof(graphRepository));
            Guard.ArgumentNotNull(specificationsApiClient, nameof(specificationsApiClient));

            _createCalculationService = createCalculationService;
            _calculationsRepository = calculationsRepository;
            _policiesApiClient = policiesApiClient;
            _policiesResiliencePolicy = calculationsResiliencePolicies.PoliciesApiClient;
            _calculationsRepositoryPolicy = calculationsResiliencePolicies.CalculationsRepository;
            _logger = logger;
            _instructionAllocationJobCreation = instructionAllocationJobCreation;
            _jobTrackerFactory = jobTrackerFactory;
            _calculationService = calculationService;
            _cachePolicy = calculationsResiliencePolicies.CacheProviderPolicy;
            _cacheProvider = cacheProvider;
            _specificationsApiClient = specificationsApiClient;
            _specificationsApiClientPolicy = calculationsResiliencePolicies.SpecificationsApiClient;
        }

        public async Task ApplyTemplateCalculation(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            IApplyTemplateCalculationsJobTracker jobTracker = _jobTrackerFactory.CreateJobTracker(message);

            try
            {
                bool canRunJob = await jobTracker.TryStartTrackingJob();

                if (!canRunJob) return;

                string specificationId = UserPropertyFrom(message, "specification-id");
                string fundingStreamId = UserPropertyFrom(message, "fundingstream-id");
                string templateVersion = UserPropertyFrom(message, "template-version");

                string correlationId = message.GetCorrelationId();
                Reference author = message.GetUserDetails();

                TemplateMapping templateMapping = await _calculationsRepositoryPolicy.ExecuteAsync(
                    () => _calculationsRepository.GetTemplateMapping(specificationId, fundingStreamId));

                if (templateMapping == null)
                {
                    LogAndThrowException(
                        $"Did not locate Template Mapping for funding stream id {fundingStreamId} and specification id {specificationId}");
                }

                ApiResponse<SpecificationSummary> specificationApiResponse =
                    await _specificationsApiClientPolicy.ExecuteAsync(() => _specificationsApiClient.GetSpecificationSummaryById(specificationId));

                if (!specificationApiResponse.StatusCode.IsSuccess() || specificationApiResponse.Content == null)
                {
                    LogAndThrowException(
                        $"Did not locate specification : {specificationId}");
                }

                SpecificationSummary specificationSummary = specificationApiResponse.Content;

                ApiResponse<TemplateMetadataContents> templateContentsResponse = await _policiesResiliencePolicy.ExecuteAsync(
                    () => _policiesApiClient.GetFundingTemplateContents(fundingStreamId, specificationSummary.FundingPeriod.Id, templateVersion));

                TemplateMetadataContents templateMetadataContents = templateContentsResponse?.Content;

                if (templateMetadataContents == null)
                {
                    LogAndThrowException(
                        $"Did not locate Template Metadata Contents for funding stream id {fundingStreamId}, funding period id {specificationSummary.FundingPeriod.Id} and template version {templateVersion}");
                }

                TemplateMappingItem[] mappingsWithoutCalculations = templateMapping.TemplateMappingItems.Where(_ => _.CalculationId.IsNullOrWhitespace())
                    .ToArray();

                FundingLine[] flattenedFundingLines = templateMetadataContents.RootFundingLines.Flatten(_ => _.FundingLines).ToArray();

                IDictionary<uint, Calculation> uniqueTemplateCalculations = flattenedFundingLines
                    .SelectMany(_ => _.Calculations.Flatten(cal => cal.Calculations))
                    .GroupBy(x => x.TemplateCalculationId)
                    .Select(x => x.FirstOrDefault())
                    .ToDictionary(_ => _.TemplateCalculationId);

                TemplateMappingItem[] missingMappings = templateMapping.TemplateMappingItems.Except(templateMapping.TemplateMappingItems.Where(item => uniqueTemplateCalculations.ContainsKey(item.TemplateId))).ToArray();
                
                Func<TemplateMappingItem, Task<bool>> isMissingCalculation = IsMissingCalculation(specificationId,
                        author,
                        correlationId,
                        missingMappings);

                templateMapping.TemplateMappingItems.RemoveAll(mappingItem => isMissingCalculation(mappingItem).GetAwaiter().GetResult());

                TemplateMappingItem[] mappingsWithCalculations = templateMapping.TemplateMappingItems.Where(_ => !_.CalculationId.IsNullOrWhitespace())
                    .ToArray();

                int itemCount = uniqueTemplateCalculations.Count;

                int startingItemCount = itemCount - mappingsWithoutCalculations.Length - mappingsWithCalculations.Length;

                await jobTracker.NotifyProgress(startingItemCount + 1);

                await EnsureAllRequiredCalculationsExist(mappingsWithoutCalculations,
                    fundingStreamId,
                    specificationSummary,
                    author,
                    correlationId,
                    startingItemCount,
                    jobTracker,
                    templateMapping,
                    uniqueTemplateCalculations);

                await EnsureAllExistingCalculationsModified(mappingsWithCalculations,
                    specificationSummary,
                    correlationId,
                    author,
                    uniqueTemplateCalculations,
                    jobTracker,
                    startingItemCount + mappingsWithoutCalculations.Length);

                await RefreshTemplateMapping(specificationId, fundingStreamId, templateMapping);

                await jobTracker.CompleteTrackingJob("Completed Successfully", itemCount);

                await InitiateCalculationRun(specificationId, author, correlationId);
            }
            catch (Exception ex)
            {
                await jobTracker.FailJob(ex.Message);

                throw;
            }
        }

        private async Task EnsureAllExistingCalculationsModified(TemplateMappingItem[] mappingsWithCalculations,
            SpecificationSummary specification,
            string correlationId,
            Reference author,
            IDictionary<uint, Calculation> uniqueTemplateCalculations,
            IApplyTemplateCalculationsJobTracker jobTracker,
            int startingItemCount)
        {
            if (!mappingsWithCalculations.Any()) return;

            IEnumerable<Models.Calcs.Calculation> existingCalculations = await _calculationsRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.GetCalculationsBySpecificationId(specification.Id));
            IDictionary<string, Models.Calcs.Calculation> existingCalculationsById = existingCalculations.ToDictionary(_ => _.Id);

            for (int calculationCount = 0; calculationCount < mappingsWithCalculations.Length; calculationCount++)
            {
                TemplateMappingItem mappingWithCalculations = mappingsWithCalculations[calculationCount];

                uniqueTemplateCalculations.TryGetValue(mappingWithCalculations.TemplateId, out Calculation templateCalculation);
                existingCalculationsById.TryGetValue(mappingWithCalculations.CalculationId, out Models.Calcs.Calculation existingCalculation);
                
                if ((calculationCount + 1) % 10 == 0) await jobTracker.NotifyProgress(startingItemCount + calculationCount + 1);

                if (templateCalculation?.Name == existingCalculation?.Current.Name && templateCalculation?.ValueFormat.AsMatchingEnum<CalculationValueType>() == existingCalculation?.Current.ValueType)
                {
                    continue;
                }

                CalculationEditModel calculationEditModel = new CalculationEditModel
                {
                    Description = existingCalculation.Current.Description,
                    SourceCode = existingCalculation.Current.SourceCode,
                    Name = templateCalculation.Name,
                    ValueType = templateCalculation.ValueFormat.AsMatchingEnum<CalculationValueType>()
                };

                IActionResult editCalculationResult = await _calculationService.EditCalculation(specification.Id,
                    mappingWithCalculations.CalculationId,
                    calculationEditModel,
                    author,
                    correlationId,
                    false,
                    false,
                    true,
                    existingCalculation);

                if (!(editCalculationResult is OkObjectResult))
                {
                    LogAndThrowException("Unable to edit template calculation for template mapping");
                }
            }
        }

        private async Task EnsureAllRequiredCalculationsExist(TemplateMappingItem[] mappingsWithoutCalculations,
            string fundingStreamId,
            SpecificationSummary specification,
            Reference author,
            string correlationId,
            int startingItemCount,
            IApplyTemplateCalculationsJobTracker jobTracker,
            TemplateMapping templateMapping,
            IDictionary<uint, Calculation> uniqueTemplateCalculations)
        {
            if (!mappingsWithoutCalculations.Any()) return;

            for (int calculationCount = 0; calculationCount < mappingsWithoutCalculations.Length; calculationCount++)
            {
                TemplateMappingItem mappingWithCalculation = mappingsWithoutCalculations[calculationCount];

                await CreateDefaultCalculationForTemplateItem(mappingWithCalculation,
                    fundingStreamId,
                    specification.Id,
                    author,
                    correlationId,
                    uniqueTemplateCalculations);

                await RefreshTemplateMapping(specification.Id, fundingStreamId, templateMapping);

                if ((calculationCount + 1) % 10 == 0) await jobTracker.NotifyProgress(startingItemCount + calculationCount + 1);
            }
        }

        private Func<TemplateMappingItem, Task<bool>> IsMissingCalculation(string specificationId, Reference author, string correlationId, IEnumerable<TemplateMappingItem> missingMappings)
        {
            return async (mapping) =>
            {
                if (!missingMappings.IsNullOrEmpty() && mapping.CalculationId != null && missingMappings.Contains(mapping))
                {
                    Models.Calcs.Calculation existingCalculation = await _calculationsRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.GetCalculationById(mapping.CalculationId));

                    if (existingCalculation != null)
                    {
                        CalculationEditModel calculationEditModel = new CalculationEditModel
                        {
                            Description = existingCalculation.Current.Description,
                            SourceCode = existingCalculation.Current.SourceCode,
                            Name = existingCalculation.Current.Name,
                            ValueType = existingCalculation.Current.ValueType,
                        };

                        IActionResult editCalculationResult = await _calculationService.EditCalculation(specificationId, 
                            mapping.CalculationId, 
                            calculationEditModel, 
                            author, 
                            correlationId, 
                            true,
                            false,
                            true,
                            existingCalculation);

                        if (!(editCalculationResult is OkObjectResult))
                        {
                            LogAndThrowException("Unable to edit template calculation for template mapping");
                        }
                    }

                    return true;
                }

                return false;
            };
        }

        private async Task InitiateCalculationRun(string specificationId, Reference user, string correlationId)
        {
            await _instructionAllocationJobCreation.SendInstructAllocationsToJobService(specificationId,
                user.Id,
                user.Name,
                new Trigger
                {
                    Message = "Assigned Template Calculations",
                    EntityId = specificationId,
                    EntityType = "Specification"
                },
                correlationId);
        }

        private async Task CreateDefaultCalculationForTemplateItem(TemplateMappingItem templateMapping,
            string fundingStreamId,
            string specificationId,
            Reference author,
            string correlationId,
            IDictionary<uint, Calculation> uniqueTemplatesCalculations)
        {
            if (!uniqueTemplatesCalculations.TryGetValue(templateMapping.TemplateId, out Calculation templateCalculation))
                LogAndThrowException($"Unable to locate template contents for template calculation id {templateMapping.TemplateId}");

            CalculationValueType calculationValueType = templateCalculation.ValueFormat.AsMatchingEnum<CalculationValueType>();
            TemplateCalculationType templateCalculationType = templateCalculation.Type.AsMatchingEnum <TemplateCalculationType>();

            CalculationCreateModel calculationCreateModel = new CalculationCreateModel
            {
                FundingStreamId = fundingStreamId,
                SpecificationId = specificationId,
                ValueType = calculationValueType,
                Name = templateCalculation.Name,
                SourceCode = calculationValueType.GetDefaultSourceCode(),
            };

            CreateCalculationResponse createCalculationResponse = await _createCalculationService.CreateCalculation(specificationId,
                calculationCreateModel,
                CalculationNamespace.Template,
                CalculationType.Template,
                author,
                correlationId,
                initiateCalcRun: false,
                templateCalculationType,
                templateCalculation.AllowedEnumTypeValues);

            if (!(createCalculationResponse?.Succeeded).GetValueOrDefault())
                 LogAndThrowException("Unable to create new default template calculation for template mapping");

            templateMapping.CalculationId = createCalculationResponse.Calculation.Id;
        }

        private string UserPropertyFrom(Message message, string key)
        {
            string userProperty = message.GetUserProperty<string>(key);

            Guard.IsNullOrWhiteSpace(userProperty, key);

            return userProperty;
        }

        private void LogAndThrowException(string message)
        {
            _logger.Error(message);

            throw new Exception(message);
        }

        private async Task RefreshTemplateMapping(string specificationId, string fundingStreamId, TemplateMapping templateMapping)
        {
            await _calculationsRepositoryPolicy.ExecuteAsync(
                    () => _calculationsRepository.UpdateTemplateMapping(specificationId, fundingStreamId, templateMapping));

            string cacheKey = $"{CacheKeys.TemplateMapping}{specificationId}-{fundingStreamId}";
            await _cachePolicy.ExecuteAsync(() => _cacheProvider.RemoveAsync<TemplateMapping>(cacheKey));
        }
    }
}