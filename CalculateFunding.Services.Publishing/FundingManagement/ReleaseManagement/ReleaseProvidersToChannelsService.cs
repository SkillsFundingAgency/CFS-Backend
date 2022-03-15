using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Models.Publishing.FundingManagement;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Processing;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    public class ReleaseProvidersToChannelsService : JobProcessingService, IReleaseProvidersToChannelsService
    {
        private readonly ISpecificationIdServiceRequestValidator _specificationIdValidator;
        private readonly ISpecificationService _specificationService;
        private readonly IPoliciesService _policiesService;
        private readonly IChannelsService _channelService;
        private readonly IPublishedProvidersLoadContext _publishProvidersLoadContext;
        private readonly IReleaseApprovedProvidersService _releaseApprovedProvidersService;
        private readonly ILogger _logger;
        private readonly IPrerequisiteCheckerLocator _prerequisiteCheckerLocator;
        private readonly IReleaseManagementRepository _releaseManagementRepository;
        private readonly IReleaseManagementSpecificationService _releaseManagementSpecificationService;
        private readonly IChannelReleaseService _channelReleaseService;
        private readonly IReleaseToChannelSqlMappingContext _releaseToChannelSqlMappingContext;
        private readonly IExistingReleasedProvidersLoadService _existingReleasedProvidersLoadService;
        private readonly IExistingReleasedProviderVersionsLoadService _existingReleasedProviderVersionsLoadService;
        private readonly IPublishedProviderLookupService _publishedProviderLookupService;

        public ReleaseProvidersToChannelsService(
            ISpecificationService specificationService,
            IPoliciesService policiesService,
            IChannelsService channelService,
            IPublishedProvidersLoadContext publishProvidersLoadContext,
            IReleaseApprovedProvidersService releaseApprovedProvidersService,
            IJobManagement jobManagement,
            ILogger logger,
            IPrerequisiteCheckerLocator prerequisiteCheckerLocator,
            IReleaseManagementRepository releaseManagementRepository,
            IReleaseManagementSpecificationService releaseManagementSpecificationService,
            IChannelReleaseService channelReleaseService,
            IReleaseToChannelSqlMappingContext releaseToChannelSqlMappingContext,
            IExistingReleasedProvidersLoadService existingReleasedProvidersLoadService,
            IExistingReleasedProviderVersionsLoadService existingReleasedProviderVersionsLoadService,
            IPublishedProviderLookupService publishedProviderLookupService,
            ISpecificationIdServiceRequestValidator specificationIdValidator) : base(jobManagement, logger)
        {
            Guard.ArgumentNotNull(specificationService, nameof(specificationService));
            Guard.ArgumentNotNull(policiesService, nameof(policiesService));
            Guard.ArgumentNotNull(channelService, nameof(channelService));
            Guard.ArgumentNotNull(publishProvidersLoadContext, nameof(publishProvidersLoadContext));
            Guard.ArgumentNotNull(releaseApprovedProvidersService, nameof(releaseApprovedProvidersService));
            Guard.ArgumentNotNull(prerequisiteCheckerLocator, nameof(prerequisiteCheckerLocator));
            Guard.ArgumentNotNull(releaseManagementRepository, nameof(releaseManagementRepository));
            Guard.ArgumentNotNull(releaseManagementSpecificationService, nameof(releaseManagementSpecificationService));
            Guard.ArgumentNotNull(channelReleaseService, nameof(channelReleaseService));
            Guard.ArgumentNotNull(releaseToChannelSqlMappingContext, nameof(releaseToChannelSqlMappingContext));
            Guard.ArgumentNotNull(existingReleasedProvidersLoadService, nameof(existingReleasedProvidersLoadService));
            Guard.ArgumentNotNull(existingReleasedProviderVersionsLoadService, nameof(existingReleasedProviderVersionsLoadService));
            Guard.ArgumentNotNull(publishedProviderLookupService, nameof(publishedProviderLookupService));
            Guard.ArgumentNotNull(specificationIdValidator, nameof(specificationIdValidator));

            _specificationService = specificationService;
            _policiesService = policiesService;
            _channelService = channelService;
            _publishProvidersLoadContext = publishProvidersLoadContext;
            _releaseApprovedProvidersService = releaseApprovedProvidersService;
            _logger = logger;
            _prerequisiteCheckerLocator = prerequisiteCheckerLocator;
            _releaseManagementRepository = releaseManagementRepository;
            _releaseManagementSpecificationService = releaseManagementSpecificationService;
            _channelReleaseService = channelReleaseService;
            _releaseToChannelSqlMappingContext = releaseToChannelSqlMappingContext;
            _existingReleasedProvidersLoadService = existingReleasedProvidersLoadService;
            _existingReleasedProviderVersionsLoadService = existingReleasedProviderVersionsLoadService;
            _publishedProviderLookupService = publishedProviderLookupService;
            _specificationIdValidator = specificationIdValidator;
        }

        public async Task<IActionResult> QueueRelease(string specificationId,
            ReleaseProvidersToChannelRequest releaseProvidersToChannelRequest,
            Reference author,
            string correlationId)
        {
            ValidationResult validationResult = _specificationIdValidator.Validate(specificationId);

            if (!validationResult.IsValid)
            {
                return validationResult.AsBadRequest();
            }

            IActionResult actionResult = await IsSpecificationReadyForPublish(specificationId, releaseProvidersToChannelRequest);

            if (!actionResult.IsOk())
            {
                return actionResult;
            }

            Job job = await QueueJob(new JobCreateModel
            {
                JobDefinitionId = JobConstants.DefinitionNames.ReleaseProvidersToChannelsJob,
                SpecificationId = specificationId,
                InvokerUserId = author?.Id,
                InvokerUserDisplayName = author?.Name,
                CorrelationId = correlationId,
                MessageBody = releaseProvidersToChannelRequest.AsJson(),
                Properties = new Dictionary<string, string>
                {
                    {"specification-id", specificationId}
                },
                Trigger = new Trigger
                {
                    EntityId = specificationId,
                    EntityType = "Specification"
                }
            });

            return new OkObjectResult(new JobCreationResponse
            {
                JobId = job.Id
            });
        }

        public async Task<IActionResult> QueueReleaseProviderVersions(
            string specificationId,
            ReleaseProvidersToChannelRequest releaseProvidersToChannelRequest,
            Reference author,
            string correlationId)
        {
            return await QueueRelease(specificationId,
                releaseProvidersToChannelRequest,
                author,
                correlationId);
        }

        public override async Task Process(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            _releaseManagementRepository.InitialiseTransaction();

            string jobId = Job?.Id;
            Reference author = message.GetUserDetails();
            string correlationId = message.GetCorrelationId();
            string specificationId = message.GetUserProperty<string>("specification-id");

            ReleaseProvidersToChannelRequest model = message.GetPayloadAsInstanceOf<ReleaseProvidersToChannelRequest>();

            try
            {
                await ReleaseProviderVersions(specificationId,
                    model,
                    jobId,
                    correlationId,
                    author);

                _releaseManagementRepository.Commit();
            }
            catch
            {
                _releaseManagementRepository.RollBack();
            }
        }

        public async Task ReleaseProviderVersions(
            string specificationId,
            ReleaseProvidersToChannelRequest releaseProvidersToChannelRequest,
            string jobId,
            string correlationId,
            Reference author)
        {
            SpecificationSummary specification = await _specificationService.GetSpecificationSummaryById(specificationId);
            if (specification == null)
            {
                throw new InvalidOperationException("Specification not found");
            }

            if (!specification.IsSelectedForFunding)
            {
                throw new InvalidOperationException("Specification is not chosen for funding");
            }

            _logger.Information($"Verifying prerequisites for release providers to channels");

            IPrerequisiteChecker prerequisiteChecker = _prerequisiteCheckerLocator.GetPreReqChecker(PrerequisiteCheckerType.ReleaseProvidersToChannels);

            try
            {
                await prerequisiteChecker.PerformChecks(specification, Job?.Id, null);
            }
            catch (JobPrereqFailedException ex)
            {
                throw new NonRetriableException(ex.Message, ex);
            }

            _logger.Information("Prerequisites for publish passed");

            _releaseToChannelSqlMappingContext.JobId = jobId;
            _releaseToChannelSqlMappingContext.Author = author;
            _releaseToChannelSqlMappingContext.CorrelationId = correlationId;

            await _releaseManagementSpecificationService.EnsureReleaseManagementSpecification(specification);

            string fundingStreamId = specification.FundingStreams.First().Id;

            FundingConfiguration fundingConfiguration = await _policiesService.GetFundingConfiguration(fundingStreamId, specification.FundingPeriod.Id);

            if (fundingConfiguration.ApprovalMode != ApprovalMode.All && releaseProvidersToChannelRequest.ProviderIds.IsNullOrEmpty())
            {
                throw new InvalidOperationException("Providers are required when the specification is configured to be batch mode.");
            }

            if (fundingConfiguration.ApprovalMode == ApprovalMode.All)
            {
                IEnumerable<PublishedProviderFundingSummary> publishedProviderFundingSummaries = await _publishedProviderLookupService.GetPublishedProviderFundingSummaries(
                  specification,
                  new[] { PublishedProviderStatus.Approved, PublishedProviderStatus.Released },
                  releaseProvidersToChannelRequest.ProviderIds);

                if (publishedProviderFundingSummaries.IsNullOrEmpty())
                {
                    throw new InvalidOperationException("No providers found to release.");
                }

                releaseProvidersToChannelRequest.ProviderIds = publishedProviderFundingSummaries.Select(_ => _.Provider.UKPRN);
            }

            IEnumerable<KeyValuePair<string, SqlModels.Channel>> channels = await _channelService.GetAndVerifyChannels(releaseProvidersToChannelRequest?.Channels);

            _publishProvidersLoadContext.SetSpecDetails(fundingStreamId, specification.FundingPeriod.Id);

            await LoadGivenProvidersFromFundingApprovals(releaseProvidersToChannelRequest);

            IEnumerable<string> providerIdsReleased = await _releaseApprovedProvidersService.ReleaseProvidersInApprovedState(specification);

            await RefreshLoadContextWithProvidersApprovedNowReleased(providerIdsReleased);

            await _existingReleasedProvidersLoadService.LoadExistingReleasedProviders(specificationId, releaseProvidersToChannelRequest.ProviderIds);
            await _existingReleasedProviderVersionsLoadService.LoadExistingReleasedProviderVersions(specificationId, releaseProvidersToChannelRequest.ProviderIds, releaseProvidersToChannelRequest.Channels);

            foreach (KeyValuePair<string, SqlModels.Channel> channel in channels)
            {
                await _channelReleaseService.ReleaseProvidersForChannel(channel.Value,
                                                                        fundingConfiguration,
                                                                        specification,
                                                                        releaseProvidersToChannelRequest.ProviderIds,
                                                                        author,
                                                                        jobId,
                                                                        correlationId);
            }
        }

        private async Task<IActionResult> IsSpecificationReadyForPublish(string specificationId, ReleaseProvidersToChannelRequest releaseProvidersToChannelRequest)
        {
            SpecificationSummary specificationSummary = await _specificationService.GetSpecificationSummaryById(specificationId);

            if (specificationSummary == null)
            {
                return new NotFoundResult();
            }

            if (!specificationSummary.IsSelectedForFunding)
            {
                return new PreconditionFailedResult($"Specification with id : {specificationId} has not been selected for funding");
            }

            if (releaseProvidersToChannelRequest == null || releaseProvidersToChannelRequest.Channels.IsNullOrEmpty())
            {
                return new PreconditionFailedResult($"You must select one or more channels to publish to for specification with id : {specificationId}.");
            }

            FundingConfiguration fundingConfiguration = await _policiesService.GetFundingConfiguration(
            specificationSummary.FundingStreams.First().Id, specificationSummary.FundingPeriod.Id);

            if (fundingConfiguration.ApprovalMode != ApprovalMode.All && releaseProvidersToChannelRequest.ProviderIds.IsNullOrEmpty())
            {
                return new PreconditionFailedResult($"Providers are required for specification with id : {specificationId} as it is configured to use batch mode.");
            }

            return new OkObjectResult(null);
        }


        private async Task RefreshLoadContextWithProvidersApprovedNowReleased(IEnumerable<string> providerIdsReleased)
        {
            await _publishProvidersLoadContext.LoadProviders(providerIdsReleased);
        }

        private async Task LoadGivenProvidersFromFundingApprovals(ReleaseProvidersToChannelRequest releaseProvidersToChannelRequest)
        {
            await _publishProvidersLoadContext.LoadProviders(releaseProvidersToChannelRequest.ProviderIds);
        }
    }
}
