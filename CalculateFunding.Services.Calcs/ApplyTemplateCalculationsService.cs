using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Models;
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

namespace CalculateFunding.Services.Calcs
{
    public class ApplyTemplateCalculationsService : IApplyTemplateCalculationsService
    {
        private readonly IApplyTemplateCalculationsJobTrackerFactory _jobTrackerFactory;
        private readonly ITemplateContentsCalculationQuery _templateContentsCalculationQuery;
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
            ITemplateContentsCalculationQuery templateContentsCalculationQuery,
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
            Guard.ArgumentNotNull(templateContentsCalculationQuery, nameof(templateContentsCalculationQuery));
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
            _templateContentsCalculationQuery = templateContentsCalculationQuery;
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

                ApiResponse<TemplateMetadataContents> templateContentsResponse = await _policiesResiliencePolicy.ExecuteAsync(
                    () => _policiesApiClient.GetFundingTemplateContents(fundingStreamId, templateVersion));

                TemplateMetadataContents templateMetadataContents = templateContentsResponse?.Content;

                if (templateMetadataContents == null)
                {
                    LogAndThrowException(
                        $"Did not locate Template Metadata Contents for funding stream id {fundingStreamId} and template version {templateVersion}");
                }

                TemplateMappingItem[] mappingsWithoutCalculations = templateMapping.TemplateMappingItems.Where(_ => _.CalculationId.IsNullOrWhitespace())
                    .ToArray();

                IEnumerable<FundingLine> flattenedFundingLines = templateMetadataContents.RootFundingLines.Flatten(_ => _.FundingLines)
                                                                 ?? new FundingLine[0];

                IEnumerable<Calculation> flattenedCalculations = flattenedFundingLines.SelectMany(_ => _.Calculations.Flatten(cal => cal.Calculations)) ?? new Calculation[0];

                IEnumerable<Calculation> uniqueflattenedCalculations = flattenedCalculations.GroupBy(x => x.TemplateCalculationId).Select(x => x.FirstOrDefault());

                Func<TemplateMappingItem, Task<bool>> isMissingCalculation = IsMissingCalculation(specificationId,
                        author,
                        correlationId,
                        templateMapping.TemplateMappingItems.Except(templateMapping.TemplateMappingItems.Where(item => flattenedCalculations.Any(calc => item.TemplateId == calc.TemplateCalculationId))));

                templateMapping.TemplateMappingItems.RemoveAll(mappingItem => isMissingCalculation(mappingItem).GetAwaiter().GetResult());

                TemplateMappingItem[] mappingsWithCalculations = templateMapping.TemplateMappingItems.Where(_ => !_.CalculationId.IsNullOrWhitespace())
                    .ToArray();

                int itemCount = uniqueflattenedCalculations?.Count() + uniqueflattenedCalculations?.Sum(
                       cal => (cal.ReferenceData?.Count())
                           .GetValueOrDefault()) ?? 0;

                int startingItemCount = itemCount - mappingsWithoutCalculations.Length - mappingsWithCalculations.Length;

                await jobTracker.NotifyProgress(startingItemCount + 1);

                ApiResponse<SpecificationSummary> specificationApiResponse =
                    await _specificationsApiClientPolicy.ExecuteAsync(() => _specificationsApiClient.GetSpecificationSummaryById(specificationId));

                if (!specificationApiResponse.StatusCode.IsSuccess() || specificationApiResponse.Content == null)
                {
                    LogAndThrowException(
                        $"Did not locate specification : {specificationId}");
                }

                await EnsureAllRequiredCalculationsExist(mappingsWithoutCalculations,
                    templateMetadataContents,
                    fundingStreamId,
                    specificationApiResponse.Content,
                    author,
                    correlationId,
                    startingItemCount,
                    jobTracker,
                    templateMapping);

                await EnsureAllExistingCalculationsModified(mappingsWithCalculations,
                    specificationApiResponse.Content,
                    correlationId,
                    author,
                    flattenedFundingLines,
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
            IEnumerable<FundingLine> flattenedFundingLines,
            IApplyTemplateCalculationsJobTracker jobTracker,
            int startingItemCount)
        {
            if (!mappingsWithCalculations.Any()) return;

            IEnumerable<Calculation> flattenedCalculations = flattenedFundingLines
                .Where(_ => _.Calculations != null)
                .SelectMany(_ => _.Calculations)
                .Flatten(_ => _?.Calculations)
                ?? new Calculation[0];

            IEnumerable<Models.Calcs.Calculation> existingCalculations = await _calculationsRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.GetCalculationsBySpecificationId(specification.Id));

            for (int calculationCount = 0; calculationCount < mappingsWithCalculations.Length; calculationCount++)
            {
                TemplateMappingItem mappingWithCalculations = mappingsWithCalculations[calculationCount];
                Calculation templateCalculation = flattenedCalculations.FirstOrDefault(_ => _.TemplateCalculationId == mappingWithCalculations.TemplateId);
                Models.Calcs.Calculation existingCalculation = existingCalculations.SingleOrDefault(_ => _.Id == mappingWithCalculations.CalculationId);

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
                    ValueType = templateCalculation.ValueFormat.AsMatchingEnum<CalculationValueType>(),
                };

                IActionResult editCalculationResult = await _calculationService.EditCalculation(specification.Id, mappingWithCalculations.CalculationId, calculationEditModel, author, correlationId);

                if (!(editCalculationResult is OkObjectResult))
                {
                    LogAndThrowException("Unable to edit template calculation for template mapping");
                }
            }
        }

        private async Task EnsureAllRequiredCalculationsExist(TemplateMappingItem[] mappingsWithoutCalculations,
            TemplateMetadataContents templateMetadataContents,
            string fundingStreamId,
            SpecificationSummary specification,
            Reference author,
            string correlationId,
            int startingItemCount,
            IApplyTemplateCalculationsJobTracker jobTracker,
            TemplateMapping templateMapping)
        {
            if (!mappingsWithoutCalculations.Any()) return;

            List<Models.Calcs.Calculation> calculations = new List<Models.Calcs.Calculation>();

            for (int calculationCount = 0; calculationCount < mappingsWithoutCalculations.Length; calculationCount++)
            {
                TemplateMappingItem mappingWithCalculation = mappingsWithoutCalculations[calculationCount];

                calculations.Add(await CreateDefaultCalculationForTemplateItem(mappingWithCalculation,
                    templateMetadataContents,
                    fundingStreamId,
                    specification.Id,
                    author,
                    correlationId));

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
                            Name = existingCalculation?.Current.Name,
                            ValueType = existingCalculation?.Current.ValueType,
                        };

                        IActionResult editCalculationResult = await _calculationService.EditCalculation(specificationId, mapping.CalculationId, calculationEditModel, author, correlationId, true);

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

        private async Task<Models.Calcs.Calculation> CreateDefaultCalculationForTemplateItem(TemplateMappingItem templateMapping,
            TemplateMetadataContents templateMetadataContents,
            string fundingStreamId,
            string specificationId,
            Reference author,
            string correlationId)
        {
            Calculation templateCalculation = _templateContentsCalculationQuery.GetTemplateContentsForMappingItem(templateMapping, templateMetadataContents);

            if (templateCalculation == null)
                LogAndThrowException($"Unable to locate template contents for template calculation id {templateMapping.TemplateId}");

            CalculationCreateModel calculationCreateModel = new CalculationCreateModel
            {
                FundingStreamId = fundingStreamId,
                SpecificationId = specificationId,
                ValueType = templateCalculation.ValueFormat.AsMatchingEnum<CalculationValueType>(),
                Name = templateCalculation.Name,
                SourceCode = "return 0"
            };

            CreateCalculationResponse createCalculationResponse = await _createCalculationService.CreateCalculation(specificationId,
                calculationCreateModel,
                CalculationNamespace.Template,
                CalculationType.Template,
                author,
                correlationId,
                initiateCalcRun: false);

            if (!(createCalculationResponse?.Succeeded).GetValueOrDefault())
                LogAndThrowException("Unable to create new default template calculation for template mapping");

            templateMapping.CalculationId = createCalculationResponse.Calculation.Id;

            return createCalculationResponse.Calculation;
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