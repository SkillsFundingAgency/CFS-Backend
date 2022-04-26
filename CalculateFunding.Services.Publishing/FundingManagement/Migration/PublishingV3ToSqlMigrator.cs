using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Processing;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Polly;
using Serilog;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement
{
    public class PublishingV3ToSqlMigrator : JobProcessingService, IPublishingV3ToSqlMigrator
    {
        private const string MigrationKey = "migration-key";
        private const string MigrationKeyValue = "6695d9f9-079f-4afe-ac13-53ca1dd39e28";
        private const string DeleteAllDataBeforeMigrationKey = "delete-all";
        private readonly IReleaseManagementRepository _repo;
        private readonly ISpecificationsApiClient _specsClient;
        private readonly IPoliciesApiClient _policyClient;
        private readonly AsyncPolicy _specsClientPolicy;
        private readonly AsyncPolicy _policyClientPolicy;
        private Dictionary<string, SqlModels.Channel> _channels;
        private Dictionary<string, SqlModels.FundingPeriod> _fundingPeriods;
        private Dictionary<string, SqlModels.FundingStream> _fundingStreams;
        private Dictionary<string, SqlModels.GroupingReason> _groupingReasons;
        private Dictionary<string, SqlModels.VariationReason> _variationReasons;
        private IChannelsService _channelsService;

        private IPublishedFundingReleaseManagementMigrator _fundingMigrator;
        private Dictionary<string, SqlModels.Specification> _specifications;

        public PublishingV3ToSqlMigrator(
            IReleaseManagementRepository releaseManagementRepository,
            ISpecificationsApiClient specificationsApiClient,
            IPoliciesApiClient policiesApiClient,
            ILogger logger,
            IPublishedFundingReleaseManagementMigrator publishedFundingReleaseManagementMigrator,
            IJobManagement jobManagement,
            IPublishingResiliencePolicies publishingResiliencePolicies,
            IChannelsService channelsService) : base(jobManagement, logger)
        {
            Guard.ArgumentNotNull(releaseManagementRepository, nameof(releaseManagementRepository));
            Guard.ArgumentNotNull(specificationsApiClient, nameof(specificationsApiClient));
            Guard.ArgumentNotNull(policiesApiClient, nameof(policiesApiClient));
            Guard.ArgumentNotNull(publishedFundingReleaseManagementMigrator, nameof(publishedFundingReleaseManagementMigrator));
            Guard.ArgumentNotNull(publishingResiliencePolicies?.PoliciesApiClient, nameof(publishingResiliencePolicies.PoliciesApiClient));
            Guard.ArgumentNotNull(publishingResiliencePolicies?.SpecificationsApiClient, nameof(publishingResiliencePolicies.SpecificationsApiClient));
            Guard.ArgumentNotNull(channelsService, nameof(channelsService));

            _repo = releaseManagementRepository;
            _specsClient = specificationsApiClient;
            _specsClientPolicy = publishingResiliencePolicies.SpecificationsApiClient;
            _policyClient = policiesApiClient;
            _policyClientPolicy = publishingResiliencePolicies.PoliciesApiClient;
            _fundingMigrator = publishedFundingReleaseManagementMigrator;
            _channelsService = channelsService;
        }

        public async Task<IActionResult> QueueReleaseManagementDataMigrationJob(Reference author,
            string correlationId,
            string[] fundingStreamIds = null)
        {
            IEnumerable<JobSummary> jobTypesRunning = await GetJobTypes(new string[] {
                    JobConstants.DefinitionNames.ReleaseManagementDataMigrationJob
            });

            if (jobTypesRunning.AnyWithNullCheck())
            {
                throw new NonRetriableException($"Unable to queue a new release managment data migration job as one is already running job id:{jobTypesRunning.First().JobId}.");
            }

            Job job = await QueueJob(new JobCreateModel
            {
                JobDefinitionId = JobConstants.DefinitionNames.ReleaseManagementDataMigrationJob,
                InvokerUserId = author?.Id,
                InvokerUserDisplayName = author?.Name,
                CorrelationId = correlationId,
                MessageBody = JsonExtensions.AsJson(fundingStreamIds),
                Properties = new Dictionary<string, string>
                {
                    {MigrationKey, MigrationKeyValue}
                },
                Trigger = new Trigger
                {
                    EntityId = MigrationKeyValue,
                    EntityType = MigrationKey
                }
            });

            return new OkObjectResult(new JobCreationResponse
            {
                JobId = job.Id
            });
        }

        public override async Task Process(Message message)
        {
            string[] fundingStreamIds = message.GetPayloadAsInstanceOf<string[]>();

            await PopulateData(fundingStreamIds);
        }

        private async Task PopulateData(string[] fundingStreamIds)
        {
            ApiResponse<IEnumerable<SpecificationSummary>> publishedSpecificationsRequest = await _specsClientPolicy.ExecuteAsync(() => _specsClient.GetSpecificationsSelectedForFunding());

            IEnumerable<SpecificationSummary> publishedSpecifications = publishedSpecificationsRequest?.Content;

            if (fundingStreamIds.AnyWithNullCheck() && publishedSpecifications.AnyWithNullCheck())
            {
                publishedSpecifications = publishedSpecifications.Where(_ => fundingStreamIds.Any(fs => fs == _.FundingStreams.First().Id));
            }

            if (publishedSpecifications.IsNullOrEmpty())
            {
                return;
            }

            await ClearDatabase();

            _groupingReasons = await _channelsService.PopulateGroupingReasons();
            _variationReasons = await _channelsService.PopulateVariationReasons();

            await PopulateChannels();
            await PopulateFundingStreamsAndPeriods(publishedSpecifications);
            await PopulateSpecifications(publishedSpecifications);

            await PopulateFundingAndProviders();
        }

        private async Task ClearDatabase()
        {
            await _repo.ClearDatabase();
        }

        private async Task PopulateSpecifications(IEnumerable<Common.ApiClient.Specifications.Models.SpecificationSummary> specifications)
        {
            IEnumerable<SqlModels.Specification> existingSpecifications = await _repo.GetSpecifications();

            _specifications = new Dictionary<string, SqlModels.Specification>(existingSpecifications.ToDictionary(_ => _.SpecificationId));

            foreach (var specification in specifications)
            {
                if (!_specifications.ContainsKey(specification.Id))
                {
                    SqlModels.Specification createdSpec = await _repo.CreateSpecification(new SqlModels.Specification()
                    {
                        FundingPeriodId = _fundingPeriods[specification.FundingPeriod.Id].FundingPeriodId,
                        FundingStreamId = _fundingStreams[specification.FundingStreams.First().Id].FundingStreamId,
                        SpecificationName = specification.Name,
                        SpecificationId = specification.Id,
                    }
                    );

                    _specifications.Add(specification.Id, createdSpec);
                }
            }
        }

        private async Task PopulateFundingAndProviders()
        {
            await _fundingMigrator.Migrate(_fundingStreams,
                                           _fundingPeriods,
                                           _channels,
                                           _groupingReasons,
                                           _variationReasons,
                                           _specifications);
        }



        private async Task PopulateFundingStreamsAndPeriods(IEnumerable<SpecificationSummary> specifications)
        {

            IEnumerable<SqlModels.FundingPeriod> existingFundingPeriods = await _repo.GetFundingPeriods();
            IEnumerable<SqlModels.FundingStream> existingFundingStreams = await _repo.GetFundingStreams();

            _fundingPeriods = new Dictionary<string, SqlModels.FundingPeriod>(existingFundingPeriods.ToDictionary(_ => _.FundingPeriodCode));
            _fundingStreams = new Dictionary<string, SqlModels.FundingStream>(existingFundingStreams.ToDictionary(_ => _.FundingStreamCode));

            ApiResponse<IEnumerable<FundingStream>> policyFundingStreams = await _policyClientPolicy.ExecuteAsync(() => _policyClient.GetFundingStreams());
            ApiResponse<IEnumerable<FundingPeriod>> policyFundingPeriods = await _policyClientPolicy.ExecuteAsync(() => _policyClient.GetFundingPeriods());

            foreach (var spec in specifications)
            {
                string fundingStreamId = spec.FundingStreams.First().Id;
                if (!_fundingStreams.ContainsKey(fundingStreamId))
                {
                    FundingStream policyFundingStream = policyFundingStreams.Content.Single(_ => _.Id == fundingStreamId);

                    SqlModels.FundingStream fundingStream = new SqlModels.FundingStream()
                    {
                        FundingStreamCode = policyFundingStream.Id,
                        FundingStreamName = policyFundingStream.Name,
                    };

                    SqlModels.FundingStream result = await _repo.CreateFundingStream(fundingStream);
                    _fundingStreams.Add(result.FundingStreamCode, result);
                }

                if (!_fundingPeriods.ContainsKey(spec.FundingPeriod.Id))
                {
                    FundingPeriod policyFundingPeriod = policyFundingPeriods.Content.SingleOrDefault(_ => _.Id == spec.FundingPeriod.Id);

                    if (policyFundingPeriod != null)
                    {
                        SqlModels.FundingPeriod fundingPeriod = new SqlModels.FundingPeriod()
                        {
                            FundingPeriodCode = policyFundingPeriod.Id,
                            FundingPeriodName = policyFundingPeriod.Name,
                        };

                        SqlModels.FundingPeriod result = await _repo.CreateFundingPeriod(fundingPeriod);
                        _fundingPeriods.Add(result.FundingPeriodCode, result);
                    }
                }
            }
        }

        private async Task PopulateChannels()
        {
            IEnumerable<SqlModels.Channel> expectedChannels = GenerateExpectedChannels();

            _channels = (await _repo.GetChannels()).ToDictionary(_ => _.ChannelCode);

            foreach (var channel in expectedChannels)
            {
                if (!_channels.ContainsKey(channel.ChannelCode))
                {
                    await _repo.CreateChannel(channel);
                }
            }
        }

        private IEnumerable<SqlModels.Channel> GenerateExpectedChannels()
        {
            return new List<SqlModels.Channel>()
            {
                new SqlModels.Channel()
                {
                    ChannelCode = "Payment",
                    ChannelName = "Payment",
                    UrlKey = "payments",
                },
                 new SqlModels.Channel()
                {
                    ChannelCode = "Statement",
                    ChannelName = "Statement",
                    UrlKey = "statements",
                },
                  new SqlModels.Channel()
                {
                    ChannelCode = "Contracting",
                    ChannelName = "Contracting",
                    UrlKey = "contracts",
                },
                  new SqlModels.Channel()
                {
                    ChannelCode = "SpecToSpec",
                    ChannelName = "Released data from specifications",
                    UrlKey = "spectospec",
                }
            };
        }
    }
}
