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
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Processing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Polly;
using Serilog;
using Calculation = CalculateFunding.Common.TemplateMetadata.Models.Calculation;
using CalculationType = CalculateFunding.Models.Calcs.CalculationType;
using FundingLine = CalculateFunding.Common.TemplateMetadata.Models.FundingLine;

namespace CalculateFunding.Services.Calcs
{
    public class ApplyTemplateCalculationsService : JobProcessingService, IApplyTemplateCalculationsService
    {
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
        private readonly ICodeContextCache _codeContextCache;
        private readonly ISpecificationsApiClient _specificationsApiClient;
        private readonly AsyncPolicy _specificationsApiClientPolicy;

        public ApplyTemplateCalculationsService(
            ICreateCalculationService createCalculationService,
            IPoliciesApiClient policiesApiClient,
            ICalcsResiliencePolicies calculationsResiliencePolicies,
            ICalculationsRepository calculationsRepository,
            IJobManagement jobManagement,
            IInstructionAllocationJobCreation instructionAllocationJobCreation,
            ILogger logger,
            ICalculationService calculationService,
            ICacheProvider cacheProvider,
            ISpecificationsApiClient specificationsApiClient,
            IGraphRepository graphRepository,
            ICodeContextCache codeContextCache) : base(jobManagement, logger)
        {
            Guard.ArgumentNotNull(instructionAllocationJobCreation, nameof(instructionAllocationJobCreation));
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
            _calculationService = calculationService;
            _cachePolicy = calculationsResiliencePolicies.CacheProviderPolicy;
            _cacheProvider = cacheProvider;
            _specificationsApiClient = specificationsApiClient;
            _codeContextCache = codeContextCache;
            _specificationsApiClientPolicy = calculationsResiliencePolicies.SpecificationsApiClient;
        }

        public override async Task Process(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

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

            TemplateMappingItem[] mappingsWithCalculations = templateMapping.TemplateMappingItems.Where(_ => !_.CalculationId.IsNullOrWhitespace())
                .ToArray();

            int startingItemCount = 1;

            await NotifyProgress(startingItemCount);

            await EnsureAllExistingCalculationsModified(mappingsWithCalculations,
                specificationSummary,
                correlationId,
                author,
                uniqueTemplateCalculations,
                startingItemCount);

            TemplateMappingItem[] newMappingsWithCalculations = await EnsureAllRequiredCalculationsExist(mappingsWithoutCalculations,
                mappingsWithCalculations,
                fundingStreamId,
                specificationSummary,
                author,
                correlationId,
                startingItemCount += mappingsWithCalculations.Length,
                uniqueTemplateCalculations);

            // refresh template mapping
            await RefreshTemplateMapping(specificationId, fundingStreamId, templateMapping);

            await InitiateCalculationRun(specificationId, author, correlationId);

            await QueueUpdateCodeContextJob(specificationId);
        }

        private async Task QueueUpdateCodeContextJob(string specificationId)
        {
            await _codeContextCache.QueueCodeContextCacheUpdate(specificationId);
        }

        private async Task EnsureAllExistingCalculationsModified(TemplateMappingItem[] mappingsWithCalculations,
            SpecificationSummary specification,
            string correlationId,
            Reference author,
            IDictionary<uint, Calculation> uniqueTemplateCalculations,
            int startingItemCount)
        {
            if (!mappingsWithCalculations.Any()) return;

            bool madeChanges = false;

            IEnumerable<Models.Calcs.Calculation> existingCalculations = await _calculationsRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.GetCalculationsBySpecificationId(specification.Id));
            IDictionary<string, Models.Calcs.Calculation> existingCalculationsById = existingCalculations.ToDictionary(_ => _.Id);

            // filter out unchanged calculations
            (Calculation TemplateCalculation, Models.Calcs.Calculation Calculation)[] AllCalculation = GetAllCalculations(mappingsWithCalculations, uniqueTemplateCalculations, existingCalculationsById).ToArray();
            List<CalculationVersionComparisonModel> calcNameChanges = new List<CalculationVersionComparisonModel>();

            for (int calculationCount = 0; calculationCount < AllCalculation.Count(); calculationCount++)
            {
                Calculation templateCalculation = AllCalculation[calculationCount].TemplateCalculation;
                Models.Calcs.Calculation existingCalculation = AllCalculation[calculationCount].Calculation;

                if ((calculationCount + 1) % 10 == 0) await NotifyProgress(startingItemCount + calculationCount + 1);

                if (existingCalculation.Current.Name != templateCalculation.Name)
                {
                    calcNameChanges.Add(new CalculationVersionComparisonModel
                    {
                        CalculationId = existingCalculation.Id,
                        CurrentName = templateCalculation.Name,
                        PreviousName = existingCalculation.Current.Name,
                        SpecificationId = specification.Id,
                        CalculationDataType = existingCalculation.Current.DataType,
                        Namespace = existingCalculation.Namespace
                    });
                }

                CalculationEditModel calculationEditModel = new CalculationEditModel
                {
                    Description = existingCalculation.Current.Description,
                    SourceCode = existingCalculation.Current.SourceCode,
                    Name = templateCalculation.Name,
                    ValueType = templateCalculation.ValueFormat.AsMatchingEnum<CalculationValueType>(),
                    AllowedEnumTypeValues = templateCalculation.AllowedEnumTypeValues
                };

                IActionResult editCalculationResult = await _calculationService.EditCalculation(specification.Id,
                    existingCalculation.Current.CalculationId,
                    calculationEditModel,
                    author,
                    correlationId,
                    setAdditional: false,
                    skipInstruct: true,
                    skipValidation: true,
                    updateBuildProject: false,
                    setTemplate: true,
                    calculationEditMode: CalculationEditMode.System,
                    existingCalculation: existingCalculation);

                madeChanges = true;

                if (!(editCalculationResult is OkObjectResult))
                {
                    LogAndThrowException("Unable to edit template calculation for template mapping");
                }
            }

            List<Models.Calcs.Calculation> updatedCalculations = new List<Models.Calcs.Calculation>();

            // now all the calc names have been updated we need to update all the references in the calc source
            foreach (CalculationVersionComparisonModel calculationVersionComparisonModel in calcNameChanges)
            {
                updatedCalculations.AddRange(await _calculationService.UpdateCalculationCodeOnCalculationChange(calculationVersionComparisonModel,
                author));
            }

            if (madeChanges || updatedCalculations.Any())
            {
                await _calculationService.UpdateBuildProject(specification);
            }
        }

        private IEnumerable<(Calculation TemplateCalculation, Models.Calcs.Calculation Calculation)> GetAllCalculations(TemplateMappingItem[] templateMappings,
            IDictionary<uint, Calculation> uniqueTemplateCalculations,
            IDictionary<string, Models.Calcs.Calculation> existingCalculationsById)
        {

            foreach (TemplateMappingItem templateMapping in templateMappings)
            {
                if (!uniqueTemplateCalculations.TryGetValue(templateMapping.TemplateId, out Calculation templateCalculation))
                {
                    continue;
                }

                if (!existingCalculationsById.TryGetValue(templateMapping.CalculationId, out Models.Calcs.Calculation existingCalculation))
                {
                    continue;
                }

                if (existingCalculation.Current.CalculationType != CalculationType.Additional && templateCalculation.Name == existingCalculation.Current.Name && templateCalculation.ValueFormat.AsMatchingEnum<CalculationValueType>() == existingCalculation.Current.ValueType)
                {
                    if (existingCalculation.Current.DataType != CalculationDataType.Enum)
                    {
                        continue;
                    }

                    if (existingCalculation.Current.AllowedEnumTypeValues == null && templateCalculation.AllowedEnumTypeValues == null)
                    {
                        continue;
                    }

                    IEnumerable<string> existingEnumValues = existingCalculation.Current.AllowedEnumTypeValues ?? new string[0];
                    IEnumerable<string> targetEnumValues = templateCalculation.AllowedEnumTypeValues ?? new string[0];

                    if (existingEnumValues.All(_ => targetEnumValues.Contains(_))
                       && existingEnumValues.Count() == targetEnumValues.Count())
                    {
                        continue;
                    }
                }

                yield return (templateCalculation, existingCalculation);
            }
        }

        private async Task<TemplateMappingItem[]> EnsureAllRequiredCalculationsExist(TemplateMappingItem[] mappingsWithoutCalculations,
            TemplateMappingItem[] mappingsWithCalculations,
            string fundingStreamId,
            SpecificationSummary specification,
            Reference author,
            string correlationId,
            int startingItemCount,
            IDictionary<uint, Calculation> uniqueTemplateCalculations)
        {
            if (!mappingsWithoutCalculations.Any()) return mappingsWithCalculations;

            bool createdNewCalculations = false;

            TemplateMappingItem[] newMappingsWithCalculations = new TemplateMappingItem[0];

            for (int calculationCount = 0; calculationCount < mappingsWithoutCalculations.Length; calculationCount++)
            {
                TemplateMappingItem newMappingWithCalculation = mappingsWithoutCalculations[calculationCount];

                Models.Calcs.Calculation calculation = await _calculationsRepository.GetCalculationsBySpecificationIdAndCalculationName(specification.Id, newMappingWithCalculation.Name);

                // if this was originally a template calculation then we need to resurrect it
                if (calculation != null && (calculation.Current.WasTemplateCalculation || calculation.Current.CalculationType == CalculationType.Template))
                {
                    newMappingWithCalculation.CalculationId = calculation.Id;
                    newMappingsWithCalculations = newMappingsWithCalculations.Concat(new[] { newMappingWithCalculation }).ToArray();
                }
                else
                {
                    await CreateDefaultCalculationForTemplateItem(newMappingWithCalculation,
                        fundingStreamId,
                        specification.Id,
                        author,
                        correlationId,
                        uniqueTemplateCalculations);

                    createdNewCalculations = true;
                }

                if ((calculationCount + 1) % 10 == 0) await NotifyProgress(startingItemCount + calculationCount + 1);
            }

            if (createdNewCalculations)
            {
                await _calculationService.UpdateBuildProject(specification);
            }

            return newMappingsWithCalculations;
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
            TemplateCalculationType templateCalculationType = templateCalculation.Type.AsMatchingEnum<TemplateCalculationType>();

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