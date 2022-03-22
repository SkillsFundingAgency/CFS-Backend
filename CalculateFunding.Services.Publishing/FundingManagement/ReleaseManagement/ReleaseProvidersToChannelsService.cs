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
        private readonly IPublishedProviderVersionService _publishedProviderVersionService;
        private readonly IPostReleaseJobCreationService _postReleaseJobCreationService;
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
            ISpecificationIdServiceRequestValidator specificationIdValidator,
            IPublishedProviderVersionService publishedProviderVersionService,
            IPostReleaseJobCreationService postReleaseJobCreationService) : base(jobManagement, logger)
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
            Guard.ArgumentNotNull(publishedProviderVersionService, nameof(publishedProviderVersionService));
            Guard.ArgumentNotNull(postReleaseJobCreationService, nameof(postReleaseJobCreationService));

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
            _publishedProviderVersionService = publishedProviderVersionService;
            _postReleaseJobCreationService = postReleaseJobCreationService;
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

            string jobId = Job?.Id;
            Reference author = message.GetUserDetails();
            string correlationId = message.GetCorrelationId();
            string specificationId = message.GetUserProperty<string>("specification-id");
            ReleaseProvidersToChannelRequest model = message.GetPayloadAsInstanceOf<ReleaseProvidersToChannelRequest>();

            _logger.Information("Starting release to channels job for specification '{specificationId}' as part of job '{jobId}'", specificationId, jobId);

            _logger.Information("Getting specification summary for job '{jobId}'", jobId);
            SpecificationSummary specification = await _specificationService.GetSpecificationSummaryById(specificationId);
            if (specification == null)
            {
                throw new InvalidOperationException("Specification not found");
            }

            if (!specification.IsSelectedForFunding)
            {
                throw new InvalidOperationException("Specification is not chosen for funding");
            }


            _logger.Information("Initialising SQL transaction");
            _releaseManagementRepository.InitialiseTransaction();

            try
            {
                await ReleaseProviderVersions(specification,
                    model,
                    jobId,
                    correlationId,
                    author);

                _logger.Information("Committing SQL transaction for release to channels on specification '{specificationId}'", specificationId);
                _releaseManagementRepository.Commit();

                _logger.Information("Queueing post release jobs for release to channels job '{jobId}'", jobId);
                await _postReleaseJobCreationService.QueueJobs(specification, correlationId, author);
                _logger.Information("Post release jobs queued for job ID '{jobId}'", jobId);

            }
            catch (Exception ex)
            {
                _logger.Information("Starting rollback for release to channels job '{jobId}' in specification '{specificationId}'", jobId, specificationId);
                _releaseManagementRepository.RollBack();
                _logger.Information("SQL rollback complete for release to channels job '{jobId}' in specification '{specificationId}'", jobId, specificationId);

                _logger.Information("Queuing published provider search indexer job for release to channels job '{jobId}' in specification '{specificationId}'", jobId, specificationId);
                await _publishedProviderVersionService.CreateReIndexJob(author, correlationId, specificationId, jobId);
                _logger.Information("Queued published provider search indexer in release to channels job '{jobId}' in specification '{specificationId}'", jobId, specificationId);

                _logger.Error(ex, "Error releasing provider versions in specification '{specificationId}'", specificationId);
                throw;
            }

            _logger.Information("Completed release to channels job '{jobId}' on specification '{specificationId}' successfully", jobId,  specificationId);
        }

        public async Task ReleaseProviderVersions(
            SpecificationSummary specification,
            ReleaseProvidersToChannelRequest releaseProvidersToChannelRequest,
            string jobId,
            string correlationId,
            Reference author)
        {
            Guard.ArgumentNotNull(specification, nameof(specification));

            string specificationId = specification.Id;

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

            _logger.Information("Ensuring specification exists for release management");
            await _releaseManagementSpecificationService.EnsureReleaseManagementSpecification(specification);

            string fundingStreamId = specification.FundingStreams.First().Id;

            FundingConfiguration fundingConfiguration = await _policiesService.GetFundingConfiguration(fundingStreamId, specification.FundingPeriod.Id);

            if (fundingConfiguration.ApprovalMode != ApprovalMode.All && releaseProvidersToChannelRequest.ProviderIds.IsNullOrEmpty())
            {
                throw new InvalidOperationException("Providers are required when the specification is configured to be batch mode.");
            }

            if (fundingConfiguration.ApprovalMode == ApprovalMode.All)
            {
                _logger.Information("Loading all providers for release to channels for specification '{specificationId}'", specificationId);
                IEnumerable<string> allModePublishedProviderIds = await _publishedProviderLookupService.GetEligibleProvidersToApproveAndRelease(specification.Id);

                if (allModePublishedProviderIds.IsNullOrEmpty())
                {
                    throw new InvalidOperationException("No providers found to release.");
                }

                _logger.Information("A total of '{Count}' providers are eligable for release for specification '{specificationId}'", allModePublishedProviderIds.Count(), specificationId);

                releaseProvidersToChannelRequest.ProviderIds = allModePublishedProviderIds;
            }

            IEnumerable<string> providerIds = ParseProviderIdsFromPublishedProviderIds(releaseProvidersToChannelRequest.ProviderIds);

            IEnumerable<KeyValuePair<string, SqlModels.Channel>> channels = await _channelService.GetAndVerifyChannels(releaseProvidersToChannelRequest?.Channels);

            _publishProvidersLoadContext.SetSpecDetails(fundingStreamId, specification.FundingPeriod.Id);

            _logger.Information("Loading '{Count}' providers records from cosmos for release to channels", providerIds.Count());
            await LoadGivenProvidersFromFundingApprovals(providerIds);

            _logger.Information("Releasing providers in approved state for specification '{SpecificationId}'", specificationId);
            IEnumerable<string> providerIdsReleased = await _releaseApprovedProvidersService.ReleaseProvidersInApprovedState(specification);
            _logger.Information("A total of '{Count}' approved providers released for specification '{SpecificationId}'", providerIdsReleased.Count(), specificationId);

            _logger.Information("Refreshing context with approved providers ");
            await RefreshLoadContextWithProvidersApprovedNowReleased(providerIdsReleased);

            _logger.Information("Loading existing release providers into context for specification '{SpecificationId}'", specificationId);
            await _existingReleasedProvidersLoadService.LoadExistingReleasedProviders(specificationId, providerIds);

            _logger.Information("Loading existing release provider versions into context for specification '{SpecificationId}'", specificationId);
            await _existingReleasedProviderVersionsLoadService.LoadExistingReleasedProviderVersions(specificationId, providerIds, releaseProvidersToChannelRequest.Channels);

            foreach (KeyValuePair<string, SqlModels.Channel> channel in channels)
            {
                _logger.Information("Releasing providers to channel '{ChannelCode}' for specification '{SpecificationId}'", channel.Value.ChannelCode, specificationId);

                await _channelReleaseService.ReleaseProvidersForChannel(channel.Value,
                                                                        fundingConfiguration,
                                                                        specification,
                                                                        providerIds,
                                                                        author,
                                                                        jobId,
                                                                        correlationId);
            }
        }

        /// <summary>
        /// Parses a published provider ID eg 1619-AS-2122-10000012 to a provider ID 10000012
        /// </summary>
        /// <param name="publishedProviderIds">Published Provider Ids</param>
        /// <returns>List of provider Ids</returns>
        /// <exception cref="InvalidOperationException"></exception>
        private static IEnumerable<string> ParseProviderIdsFromPublishedProviderIds(IEnumerable<string> publishedProviderIds)
        {
            List<string> result = new List<string>(publishedProviderIds.Count());

            foreach (string publishedProviderId in publishedProviderIds)
            {
                int lastHyphen = publishedProviderId.LastIndexOf("-");
                if (lastHyphen < 1)
                {
                    throw new InvalidOperationException($"Unable to parse provider ID from published provider id for value '{publishedProviderId}'");
                }

                result.Add(publishedProviderId.Substring(lastHyphen + 1, publishedProviderId.Length - lastHyphen - 1));
            }

            return result;
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

        private async Task LoadGivenProvidersFromFundingApprovals(IEnumerable<string> providerIds)
        {
            await _publishProvidersLoadContext.LoadProviders(providerIds);
        }
    }
}
