using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Interfaces.Threading;
using CalculateFunding.Services.Publishing.Interfaces;
using Serilog;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement
{
    public class PublishingV3ToSqlMigrator : IPublishingV3ToSqlMigrator
    {
        private readonly IReleaseManagementRepository _repo;
        private readonly ISpecificationsApiClient _specsClient;
        private readonly IPoliciesApiClient _policyClient;
        private readonly IPublishedFundingRepository _cosmosRepo;
        private readonly IProducerConsumerFactory _producerConsumerFactory;
        private readonly ILogger _logger;
        private Dictionary<string, SqlModels.Channel> _channels;
        private Dictionary<string, SqlModels.FundingPeriod> _fundingPeriods;
        private Dictionary<string, SqlModels.FundingStream> _fundingStreams;
        private Dictionary<string, SqlModels.GroupingReason> _groupingReasons;
        private Dictionary<string, SqlModels.VariationReason> _variationReasons;

        private IPublishedFundingReleaseManagementMigrator _fundingMigrator;
        private Dictionary<string, SqlModels.Specification> _specifications;

        public PublishingV3ToSqlMigrator(
            IReleaseManagementRepository releaseManagementRepository,
            ISpecificationsApiClient specificationsApiClient,
            IPoliciesApiClient policiesApiClient,
            IPublishedFundingRepository publishedFundingRepository,
            IProducerConsumerFactory producerConsumerFactory,
            ILogger logger,
            IPublishedFundingReleaseManagementMigrator publishedFundingReleaseManagementMigrator)
        {
            Guard.ArgumentNotNull(releaseManagementRepository, nameof(releaseManagementRepository));

            _repo = releaseManagementRepository;
            _specsClient = specificationsApiClient;
            _policyClient = policiesApiClient;
            _cosmosRepo = publishedFundingRepository;
            _producerConsumerFactory = producerConsumerFactory;
            _logger = logger;
            _fundingMigrator = publishedFundingReleaseManagementMigrator;
        }
        public async Task PopulateReferenceData()
        {
            await PopulateGroupingReasons();
            await PopulateVariationReasons();
            await PopulateChannels();

            var publishedSpecificationsRequest = await _specsClient.GetSpecificationsSelectedForFunding();


            await PopulateFundingStreamsAndPeriods(publishedSpecificationsRequest.Content);
            await PopulateSpecifications(publishedSpecificationsRequest.Content);

            await PopulateFunding();
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

        private async Task PopulateFunding()
        {
            await _fundingMigrator.Migrate(_fundingStreams,
                                           _fundingPeriods,
                                           _channels,
                                           _groupingReasons,
                                           _variationReasons,
                                           _specifications);
        }



        private async Task PopulateFundingStreamsAndPeriods(IEnumerable<Common.ApiClient.Specifications.Models.SpecificationSummary> specifications)
        {

            IEnumerable<SqlModels.FundingPeriod> existingFundingPeriods = await _repo.GetFundingPeriods();
            IEnumerable<SqlModels.FundingStream> existingFundingStreams = await _repo.GetFundingStreams();

            _fundingPeriods = new Dictionary<string, SqlModels.FundingPeriod>(existingFundingPeriods.ToDictionary(_ => _.FundingPeriodCode));
            _fundingStreams = new Dictionary<string, SqlModels.FundingStream>(existingFundingStreams.ToDictionary(_ => _.FundingStreamCode));

            var policyFundingStreams = await _policyClient.GetFundingStreams();
            var policyFundingPeriods = await _policyClient.GetFundingPeriods();

            foreach (var spec in specifications)
            {
                string fundingStreamId = spec.FundingStreams.First().Id;
                if (!_fundingStreams.ContainsKey(fundingStreamId))
                {
                    var policyFundingStream = policyFundingStreams.Content.Single(_ => _.Id == fundingStreamId);

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
                    var policyFundingPeriod = policyFundingPeriods.Content.SingleOrDefault(_ => _.Id == spec.FundingPeriod.Id);

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
                }
            };
        }

        private async Task PopulateVariationReasons()
        {
            IEnumerable<SqlModels.VariationReason> expectedVariationReasons = GenerateExpectedVariationReasons();


            IEnumerable<SqlModels.VariationReason> existingVariationReasons = await _repo.GetVariationReasons();

            _variationReasons = new Dictionary<string, SqlModels.VariationReason>(existingVariationReasons.ToDictionary(_ => _.VariationReasonCode));

            foreach (var reason in expectedVariationReasons)
            {
                if (!_variationReasons.ContainsKey(reason.VariationReasonCode))
                {
                    await _repo.CreateVariationReason(reason);
                }
            }

        }

        private IEnumerable<SqlModels.VariationReason> GenerateExpectedVariationReasons()
        {
            MemberInfo[] memberInfos = typeof(CalculateFunding.Models.Publishing.VariationReason).GetMembers(BindingFlags.Public | BindingFlags.Static);

            List<SqlModels.VariationReason> reasons = new List<SqlModels.VariationReason>();

            foreach (var member in memberInfos)
            {
                int? sqlConstantId = GetSqlId(member);
                if (sqlConstantId.HasValue)
                {
                    string description = GetDescription(member);

                    if (string.IsNullOrWhiteSpace(description))
                    {
                        description = member.Name;
                    }

                    reasons.Add(new SqlModels.VariationReason()
                    {
                        VariationReasonCode = member.Name,
                        VariationReasonId = sqlConstantId.Value,
                        VariationReasonName = description,
                    });
                }
            }

            return reasons;
        }

        private int? GetSqlId(MemberInfo member)
        {
            return member.GetCustomAttribute<SqlConstantIdAttribute>()?.Id;
        }

        private string GetDescription(MemberInfo memberInfo)
        {
            return memberInfo.GetCustomAttribute<DisplayAttribute>()?.Name;
        }

        private async Task PopulateGroupingReasons()
        {
            IEnumerable<SqlModels.GroupingReason> existingGroupingReasons = await _repo.GetGroupingReasons();

            _groupingReasons = new Dictionary<string, SqlModels.GroupingReason>(
                existingGroupingReasons.ToDictionary(_ => _.GroupingReasonCode));

            IEnumerable<SqlModels.GroupingReason> expectedGroupingReasons = GenerateExpectedGroupingReasons();
            foreach (var reason in expectedGroupingReasons)
            {
                if (!_groupingReasons.ContainsKey(reason.GroupingReasonCode))
                {
                    SqlModels.GroupingReason createdGroupingReason = await _repo.CreateGroupingReason(reason);
                    _groupingReasons.Add(createdGroupingReason.GroupingReasonCode, createdGroupingReason);
                }
            }
        }

        private IEnumerable<SqlModels.GroupingReason> GenerateExpectedGroupingReasons()
        {
            return new List<SqlModels.GroupingReason>()
            {
                new SqlModels.GroupingReason()
                {
                    GroupingReasonId = 1,
                    GroupingReasonCode = nameof(GroupingReason.Payment),
                    GroupingReasonName = "Payment",
                },
                new SqlModels.GroupingReason()
                {
                    GroupingReasonId = 2,
                    GroupingReasonCode = nameof(GroupingReason.Information),
                    GroupingReasonName = "Information",
                },
                new SqlModels.GroupingReason()
                {
                    GroupingReasonId = 3,
                    GroupingReasonCode = nameof(GroupingReason.Contracting),
                    GroupingReasonName = "Contracting",
                },
                new SqlModels.GroupingReason()
                {
                    GroupingReasonId = 4,
                    GroupingReasonCode = nameof(GroupingReason.Indicative),
                    GroupingReasonName = "Indicative",
                }
            };
        }
    }
}
