using CalculateFunding.Common.ApiClient.Jobs.Models;
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
        private readonly ISpecificationService _specificationService;
        private readonly IPoliciesService _policiesService;
        private readonly IChannelsService _channelService;
        private readonly IPublishedProvidersLoadContext _publishProvidersLoadContext;
        private readonly IReleaseApprovedProvidersService _releaseApprovedProvidersService;
        private readonly ILogger _logger;
        private readonly IPrerequisiteCheckerLocator _prerequisiteCheckerLocator;
        
        public ReleaseProvidersToChannelsService(
            ISpecificationService specificationService,
            IPoliciesService policiesService,
            IChannelsService channelService,
            IPublishedProvidersLoadContext publishProvidersLoadContext,
            IReleaseApprovedProvidersService releaseApprovedProvidersService,
            IJobManagement jobManagement,
            ILogger logger,
            IPrerequisiteCheckerLocator prerequisiteCheckerLocator) : base(jobManagement, logger)
        {
            Guard.ArgumentNotNull(specificationService, nameof(specificationService));
            Guard.ArgumentNotNull(policiesService, nameof(policiesService));
            Guard.ArgumentNotNull(channelService, nameof(channelService));
            Guard.ArgumentNotNull(publishProvidersLoadContext, nameof(publishProvidersLoadContext));
            Guard.ArgumentNotNull(releaseApprovedProvidersService, nameof(releaseApprovedProvidersService));
            Guard.ArgumentNotNull(prerequisiteCheckerLocator, nameof(prerequisiteCheckerLocator));
            
            _specificationService = specificationService;
            _policiesService = policiesService;
            _channelService = channelService;
            _publishProvidersLoadContext = publishProvidersLoadContext;
            _releaseApprovedProvidersService = releaseApprovedProvidersService;
            _logger = logger;
            _prerequisiteCheckerLocator = prerequisiteCheckerLocator;
        }

        public async Task<IActionResult> QueueReleaseProviderVersions(
            string specificationId,
            ReleaseProvidersToChannelRequest releaseProvidersToChannelRequest,
            Reference author,
            string correlationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.ArgumentNotNull(releaseProvidersToChannelRequest, nameof(releaseProvidersToChannelRequest));

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

        public override async Task Process(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            string specificationId = message.GetUserProperty<string>("specification-id");
            ReleaseProvidersToChannelRequest model = message.GetPayloadAsInstanceOf<ReleaseProvidersToChannelRequest>();

            Reference author = message.GetUserDetails();
            string correlationId = message.GetCorrelationId();

            await ReleaseProviderVersions(specificationId, model, author, correlationId);
        }

        public async Task ReleaseProviderVersions(
            string specificationId,
            ReleaseProvidersToChannelRequest releaseProvidersToChannelRequest,
            Reference author,
            string correlationId)
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
                await prerequisiteChecker.PerformChecks(specification, Job.Id, null);
            }
            catch (JobPrereqFailedException ex)
            {
                throw new NonRetriableException(ex.Message, ex);
            }

            _logger.Information("Prerequisites for publish passed");

            string fundingStreamId = specification.FundingStreams.First().Id;

            FundingConfiguration fundingConfiguration = await _policiesService.GetFundingConfiguration(fundingStreamId, specification.FundingPeriod.Id);

            IEnumerable<KeyValuePair<string, SqlModels.Channel>> channels = await _channelService.GetAndVerifyChannels(releaseProvidersToChannelRequest.Channels);

            _publishProvidersLoadContext.SetSpecDetails(fundingStreamId, specification.FundingPeriod.Id);

            await LoadGivenProvidersFromFundingApprovals(releaseProvidersToChannelRequest);

            IEnumerable<string> providerIdsReleased = await _releaseApprovedProvidersService.ReleaseProvidersInApprovedState(author, correlationId, specification);
            
            await RefreshLoadContextWithProvidersApprovedNowReleased(providerIdsReleased);
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
