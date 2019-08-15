using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Extensions;
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
        private readonly Policy _policiesResiliencePolicy;
        private readonly Policy _calculationsRepositoryPolicy;
        private readonly ILogger _logger;

        public ApplyTemplateCalculationsService(ICreateCalculationService createCalculationService,
            IPoliciesApiClient policiesApiClient,
            ICalcsResiliencePolicies calculationsResiliencePolicies,
            ICalculationsRepository calculationsRepository,
            ITemplateContentsCalculationQuery templateContentsCalculationQuery,
            IApplyTemplateCalculationsJobTrackerFactory jobTrackerFactory,
            IInstructionAllocationJobCreation instructionAllocationJobCreation,
            ILogger logger)
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

            _createCalculationService = createCalculationService;
            _calculationsRepository = calculationsRepository;
            _policiesApiClient = policiesApiClient;
            _policiesResiliencePolicy = calculationsResiliencePolicies.PoliciesApiClient;
            _calculationsRepositoryPolicy = calculationsResiliencePolicies.CalculationsRepository;
            _templateContentsCalculationQuery = templateContentsCalculationQuery;
            _logger = logger;
            _instructionAllocationJobCreation = instructionAllocationJobCreation;
            _jobTrackerFactory = jobTrackerFactory;
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
                    LogAndThrowException(
                        $"Did not locate Template Mapping for funding stream id {fundingStreamId} and specification id {specificationId}");

                ApiResponse<TemplateMetadataContents> templateContentsResponse = await _policiesResiliencePolicy.ExecuteAsync(
                    () => _policiesApiClient.GetFundingTemplateContents(fundingStreamId, templateVersion));

                TemplateMetadataContents templateMetadataContents = templateContentsResponse?.Content;

                if (templateMetadataContents == null)
                    LogAndThrowException(
                        $"Did not locate Template Metadata Contents for funding stream id {fundingStreamId} and template version {templateVersion}");

                TemplateMappingItem[] mappingsWithoutCalculations = templateMapping.TemplateMappingItems.Where(_ => _.CalculationId.IsNullOrWhitespace())
                    .Select(_ => _)
                    .ToArray();

                IEnumerable<FundingLine> flattenedFundingLines = templateMetadataContents.RootFundingLines.Flatten(_ => _.FundingLines)
                                                                 ?? new FundingLine[0];

                int itemCount = flattenedFundingLines.Sum(_ =>
                    _.Calculations.Count() + _.Calculations.Sum(
                        cal => (cal.ReferenceData?.Count())
                            .GetValueOrDefault()));

                int startingItemCount = itemCount - mappingsWithoutCalculations.Length;

                await jobTracker.NotifyProgress(startingItemCount);

                await EnsureAllRequiredCalculationsExist(mappingsWithoutCalculations,
                    templateMetadataContents,
                    fundingStreamId,
                    specificationId,
                    author,
                    correlationId,
                    startingItemCount,
                    jobTracker);

                await _calculationsRepositoryPolicy.ExecuteAsync(
                    () => _calculationsRepository.UpdateTemplateMapping(specificationId, fundingStreamId, templateMapping));

                await jobTracker.CompleteTrackingJob("Completed Successfully", itemCount);
                
                await InitiateCalculationRun(specificationId, author, correlationId);
            }
            catch (Exception ex)
            {
                await jobTracker.FailJob(ex.Message);

                throw;
            }
        }

        private async Task EnsureAllRequiredCalculationsExist(TemplateMappingItem[] mappingsWithoutCalculations,
            TemplateMetadataContents templateMetadataContents,
            string fundingStreamId,
            string specificationId,
            Reference author,
            string correlationId,
            int startingItemCount,
            IApplyTemplateCalculationsJobTracker jobTracker)
        {
            if (!mappingsWithoutCalculations.Any()) return;

            for (int calculationCount = 0; calculationCount < mappingsWithoutCalculations.Length; calculationCount++)
            {
                TemplateMappingItem mappingWithCalculation = mappingsWithoutCalculations[calculationCount];

                await CreateDefaultCalculationForTemplateItem(mappingWithCalculation,
                    templateMetadataContents,
                    fundingStreamId,
                    specificationId,
                    author,
                    correlationId);

                if ((calculationCount + 1) % 10 == 0) await jobTracker.NotifyProgress(startingItemCount + calculationCount);
            }
        }

        private async Task InitiateCalculationRun(string specifiationId, Reference user, string correlationId)
        {
            await _instructionAllocationJobCreation.SendInstructAllocationsToJobService(specifiationId, 
                user.Id,
                user.Name,  
                new Trigger
                {
                    Message = "Assigned Template Calculations",
                    EntityId = specifiationId,
                    EntityType = nameof(Specification)
                },  
                correlationId);
        }

        private async Task CreateDefaultCalculationForTemplateItem(TemplateMappingItem templateMapping,
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
    }
}