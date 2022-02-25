using CalculateFunding.Publishing.AcceptanceTests.Contexts;
using CalculateFunding.Publishing.AcceptanceTests.Models;
using CalculateFunding.Publishing.AcceptanceTests.Repositories;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;
using GroupingReasonEnum = CalculateFunding.Services.Publishing.GroupingReason;
using VariationReasonSql = CalculateFunding.Services.Publishing.FundingManagement.SqlModels.VariationReason;

namespace CalculateFunding.Publishing.AcceptanceTests.StepDefinitions
{
    [Binding]
    public class ReleaseManagementRepositoryStepDefinitions : StepDefinitionBase
    {
        private readonly IReleaseManagementRepository _repo;
        private readonly InMemoryReleaseManagementRepository _repoInMemory;
        private readonly ICurrentJobStepContext _currentJobStepContext;

        public ReleaseManagementRepositoryStepDefinitions(IReleaseManagementRepository releaseManagementRepository,
            ICurrentJobStepContext currentJobStepContext)
        {
            _repo = releaseManagementRepository;
            _repoInMemory = (InMemoryReleaseManagementRepository)releaseManagementRepository;
            _currentJobStepContext = currentJobStepContext;
        }

        [Given(@"release management repo has prereq data populated")]
        public async Task GivenReleaseManagementRepoHasPrereqDataPopulated()
        {
            await _repoInMemory.CreateFundingPeriod(new FundingPeriod()
            {
                FundingPeriodCode = "AY-2122",
                FundingPeriodId = 1,
                FundingPeriodName = "Academic Year 21/22",
            });

            await _repoInMemory.CreateFundingStream(new FundingStream()
            {
                FundingStreamCode = "PSG",
                FundingStreamId = 1,
                FundingStreamName = "PE and Sport Grant",
            });

            await _repoInMemory.CreateChannel(new Channel()
            {
                ChannelCode = "Contracting",
                ChannelName = "Contract",
                UrlKey = "contracts",
                ChannelId = 1,
            });

            await _repoInMemory.CreateChannel(new Channel()
            {
                ChannelCode = "Payment",
                ChannelName = "Payment",
                UrlKey = "payments",
                ChannelId = 2,
            });

            await _repoInMemory.CreateChannel(new Channel()
            {
                ChannelCode = "Statement",
                ChannelName = "Statement",
                UrlKey = "statements",
                ChannelId = 3,
            });

            AddGroupingReasons();
            await AddVariationReasons();
        }

        private async Task AddVariationReasons()
        {
            MemberInfo[] memberInfos = typeof(CalculateFunding.Models.Publishing.VariationReason).GetMembers(BindingFlags.Public | BindingFlags.Static);

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

                    await _repoInMemory.CreateVariationReason(new VariationReasonSql()
                    {
                        VariationReasonCode = member.Name,
                        VariationReasonId = sqlConstantId.Value,
                        VariationReasonName = description,
                    });
                }
            }
        }

        private int? GetSqlId(MemberInfo member)
        {
            return member.GetCustomAttribute<CalculateFunding.Models.Publishing.SqlConstantIdAttribute>()?.Id;
        }

        private string GetDescription(MemberInfo memberInfo)
        {
            return memberInfo.GetCustomAttribute<DisplayAttribute>()?.Name;
        }

        private void AddGroupingReasons()
        {
            List<GroupingReason> groupingReasons = new(4)
            {
                new GroupingReason()
                {
                    GroupingReasonId = 1,
                    GroupingReasonCode = nameof(GroupingReasonEnum.Payment),
                    GroupingReasonName = "Payment",
                },
                new GroupingReason()
                {
                    GroupingReasonId = 2,
                    GroupingReasonCode = nameof(GroupingReasonEnum.Information),
                    GroupingReasonName = "Information",
                },
                new GroupingReason()
                {
                    GroupingReasonId = 3,
                    GroupingReasonCode = nameof(GroupingReasonEnum.Contracting),
                    GroupingReasonName = "Contracting",
                },
                new GroupingReason()
                {
                    GroupingReasonId = 4,
                    GroupingReasonCode = nameof(GroupingReasonEnum.Indicative),
                    GroupingReasonName = "Indicative",
                }
            };

            foreach (GroupingReason groupingReason in groupingReasons)
            {
                _repoInMemory.CreateGroupingReason(groupingReason);
            }
        }


        [Given(@"the following specification exists in release management")]
        public async Task GivenTheFollowingSpecificationExistsInReleaseManagement(Table table)
        {
            SpecificationCreateRequest request = table.CreateInstance<SpecificationCreateRequest>();

            Specification specification = new Specification()
            {
                SpecificationId = request.SpecificationId,
                SpecificationName = request.SpecificationName,
            };

            if (string.IsNullOrWhiteSpace(request.FundingPeriodId))
            {
                throw new InvalidOperationException("Missing funding period Id");
            }

            FundingPeriod fundingPeriod = await _repoInMemory.GetFundingPeriodByCode(request.FundingPeriodId);
            if (fundingPeriod == null)
            {
                throw new InvalidOperationException($"Unable to funding period {request.FundingPeriodId}");
            }

            specification.FundingPeriodId = fundingPeriod.FundingPeriodId;

            FundingStream fundingStream = await _repoInMemory.GetFundingStreamByCode(request.FundingStreamId);
            if (fundingStream == null)
            {
                throw new InvalidOperationException($"Unable to find funding stream Id '{request.FundingStreamId}'");
            }

            specification.FundingStreamId = fundingStream.FundingStreamId;

            await _repoInMemory.CreateSpecification(specification);
        }

        [Given(@"there is a released provider record in the release management repository")]
        public async Task GivenThereIsAReleasedProviderRecordInTheReleaseManagementRepository(Table table)
        {
            ReleasedProvider provider = table.CreateInstance<ReleasedProvider>();

            await _repoInMemory.CreateReleasedProvider(provider);
        }

        [Given(@"a released provider version exists in the release management repository")]
        public async Task GivenAReleasedProviderVersionExistsInTheReleaseManagementRepository(Table table)
        {
            ReleasedProviderVersion providerVersion = table.CreateInstance<ReleasedProviderVersion>();

            await _repoInMemory.CreateReleasedProviderVersion(providerVersion);
        }

        [Given(@"a released provider version channel record exists in the release management repository")]
        public async Task GivenAReleasedProviderVersionChannelRecordExistsInTheReleaseManagementRepository(Table table)
        {
            ReleasedProviderVersionChannel item = await CreateReleasedProviderVersionChannelFromTable(table);

            await _repoInMemory.CreateReleasedProviderVersionChannel(item);
        }

        [Given(@"a funding group record exists in the release management repository")]
        public async Task GivenAFundingGroupRecordExistsInTheReleaseManagementRepository(Table table)
        {
            FundingGroup fundingGroup = await GenerateFundingGroupFromTable(table);

            await _repoInMemory.CreateFundingGroup(fundingGroup);
        }

        [Given(@"a funding group version exists in the release management repository")]
        public async Task GivenAFundingGroupVersionExistsInTheReleaseManagementRepository(Table table)
        {
            FundingGroupVersion fundingGroupVersion = await GenerateFundingGroupVersionFromTable(table);

            await _repoInMemory.CreateFundingGroupVersion(fundingGroupVersion);
        }

        [Given(@"the provider versions associated with the funding group versions exist in the release management repository")]
        public async Task GivenTheProviderVersionsAssociatedWithTheFundingGroupVersionsExistInTheReleaseManagementRepository(Table table)
        {
            IEnumerable<FundingGroupProvider> fundingGroupProvider = table.CreateSet<FundingGroupProvider>();

            foreach (var provider in fundingGroupProvider)
            {
                await _repoInMemory.CreateFundingGroupProviderUsingAmbientTransaction(provider);
            }

        }


        [Then(@"there is a released provider record in the release management repository")]
        public void ThenThereIsAReleasedProviderVersionInTheReleaseManagementRepository(Table table)
        {
            ReleasedProvider expectedProvider = table.CreateInstance<ReleasedProvider>();

            ReleasedProvider actualProvider = _repoInMemory.GetReleasedProviderById(expectedProvider.ReleasedProviderId);

            actualProvider.Should().BeEquivalentTo(expectedProvider);
        }

        [Then(@"there is a total of '([^']*)' released provider records in the release management repoistory")]
        public void ThenThereIsATotalOfReleasedProviderRecordsInTheReleaseManagementRepoistory(int count)
        {
            _repoInMemory.ReleasedProviderCount.Should().Be(count);
        }


        [Then(@"there is a released provider version record created in the release management repository")]
        public async Task ThenThereIsAReleasedProviderVersionRecordCreatedInTheReleaseManagementRepository(Table table)
        {
            ReleasedProviderVersion expectedProvider = table.CreateInstance<ReleasedProviderVersion>();

            ReleasedProviderVersion actualProvider = await _repoInMemory.GetReleasedProviderVersionById(expectedProvider.ReleasedProviderVersionId);

            actualProvider.Should().BeEquivalentTo(expectedProvider);
        }

        [Then(@"there is a total of '([^']*)' released provider version records in the release management repoistory")]
        public void ThenThereIsATotalOfReleasedProviderVersionRecordsInTheReleaseManagementRepoistory(int count)
        {
            _repoInMemory.ReleasedProviderVersionsCount.Should().Be(count);
        }


        [Then(@"there is a released provider version channel record created in the release management repository")]
        public async Task ThenThereIsAReleasedProviderVersionChannelRecordCreatedInTheReleaseManagementRepository(Table table)
        {
            ReleasedProviderVersionChannel expected = await CreateReleasedProviderVersionChannelFromTable(table);

            ReleasedProviderVersionChannel actual = await _repoInMemory.GetReleasedProviderVersionChannel(expected.ReleasedProviderVersionChannelId);

            actual.Should().BeEquivalentTo(expected);
        }

        private async Task<ReleasedProviderVersionChannel> CreateReleasedProviderVersionChannelFromTable(Table table)
        {
            ExpectedProviderVersionChannel expectedProviderVersionChannel = table.CreateInstance<ExpectedProviderVersionChannel>();

            Channel channel = await _repoInMemory.GetChannelByChannelCode(expectedProviderVersionChannel.Channel);

            ReleasedProviderVersionChannel expected = new ReleasedProviderVersionChannel()
            {
                AuthorId = expectedProviderVersionChannel.AuthorId,
                AuthorName = expectedProviderVersionChannel.AuthorName,
                ReleasedProviderVersionChannelId = expectedProviderVersionChannel.ReleasedProviderVersionChannelId,
                ReleasedProviderVersionId = expectedProviderVersionChannel.ReleasedProviderVersionId,
                StatusChangedDate = expectedProviderVersionChannel.StatusChangedDate,
                ChannelId = channel.ChannelId,
            };
            return expected;
        }

        [Then(@"there are a total of '([^']*)' released provider version channel records created in the release management repository")]
        public void ThenThereAreATotalOfReleasedProviderVersionChannelRecordsCreatedInTheReleaseManagementRepository(int count)
        {
            _repoInMemory.ReleasedProviderVersionChannelCount.Should().Be(count);
        }

        [Then(@"there is a released provider channel variation created in the release management repository")]
        public async Task ThenThereIsAReleasedProviderChannelVariationCreatedInTheReleaseManagementRepository(Table table)
        {
            ExpectedProviderVariationReason expectedInput = table.CreateInstance<ExpectedProviderVariationReason>();

            VariationReasonSql variationReason = await GetVariationReasonAndAssertNotNull(expectedInput.VariationReason);

            ReleasedProviderChannelVariationReason expected = new ReleasedProviderChannelVariationReason()
            {
                ReleasedProviderChannelVariationReasonId = expectedInput.ReleasedProviderChannelVariationReasonId,
                VariationReasonId = variationReason.VariationReasonId,
                ReleasedProviderVersionChannelId = expectedInput.ReleasedProviderVersionChannelId,
            };

            ReleasedProviderChannelVariationReason actual = await _repoInMemory.GetReleasedProviderVersionChannelVariationReason(expected.ReleasedProviderChannelVariationReasonId);

            actual.Should().BeEquivalentTo(expected);
        }

        [Then(@"there are a total of '([^']*)' released provider version channel variation reason records created in the release management repository")]
        public void ThenThereAreATotalOfReleasedProviderVersionChannelVariationReasonRecordsCreatedInTheReleaseManagementRepository(int count)
        {
            _repoInMemory.ReleasedProviderVersionVariationReasonsCount.Should().Be(count);
        }

        [Then(@"there is a funding group created in the release management repository")]
        public async Task ThenThereIsAFundingGroupCreatedInTheReleaseManagementRepository(Table table)
        {
            FundingGroup expected = await GenerateFundingGroupFromTable(table);

            FundingGroup actual = await _repoInMemory.GetFundingGroup(expected.FundingGroupId);

            actual.Should().BeEquivalentTo(expected);
        }

        private async Task<FundingGroup> GenerateFundingGroupFromTable(Table table)
        {
            ExpectedFundingGroup expectedInput = table.CreateInstance<ExpectedFundingGroup>();

            Channel channel = await _repoInMemory.GetChannelByChannelCode(expectedInput.Channel);
            IEnumerable<GroupingReason> groupingReasons = await _repoInMemory.GetGroupingReasons();

            GroupingReason groupingReason = groupingReasons.Single(_ => _.GroupingReasonCode == expectedInput.GroupingReason);

            FundingGroup expected = new FundingGroup()
            {
                ChannelId = channel.ChannelId,
                FundingGroupId = expectedInput.FundingGroupId,
                GroupingReasonId = groupingReason.GroupingReasonId,
                OrganisationGroupIdentifierValue = expectedInput.OrganisationGroupIdentifierValue,
                OrganisationGroupName = expectedInput.OrganisationGroupName,
                OrganisationGroupSearchableName = expectedInput.OrganisationGroupSearchableName,
                OrganisationGroupTypeClassification = expectedInput.OrganisationGroupTypeClassification,
                OrganisationGroupTypeCode = expectedInput.OrganisationGroupTypeCode,
                OrganisationGroupTypeIdentifier = expectedInput.OrganisationGroupTypeIdentifier,
                SpecificationId = expectedInput.SpecificationId,
            };
            return expected;
        }

        [Given(@"a total of '([^']*)' funding group records exist in the release management repository")]
        [Then(@"there are a total of '([^']*)' funding group records created in the release management repository")]
        public void ThenThereAreATotalOfFundingGroupRecordsCreatedInTheReleaseManagementRepository(int expectedCount)
        {
            _repoInMemory.FundingGroupsCount.Should().Be(expectedCount);
        }

        [Then(@"there is a funding group version created in the release management repository")]
        public async Task ThenThereIsAFundingGroupVersionCreatedInTheReleaseManagementRepository(Table table)
        {
            FundingGroupVersion expected = await GenerateFundingGroupVersionFromTable(table);

            FundingGroupVersion actual = await _repoInMemory.GetFundingGroupVersion(expected.FundingGroupVersionId);

            actual.Should().BeEquivalentTo(expected);
        }

        private async Task<FundingGroupVersion> GenerateFundingGroupVersionFromTable(Table table)
        {
            ExpectedFundingGroupVersion expectedInput = table.CreateInstance<ExpectedFundingGroupVersion>();

            Channel channel = await _repoInMemory.GetChannelByChannelCode(expectedInput.Channel);
            IEnumerable<GroupingReason> groupingReasons = await _repoInMemory.GetGroupingReasons();

            GroupingReason groupingReason = groupingReasons.Single(_ => _.GroupingReasonCode == expectedInput.GroupingReason);

            if (expectedInput.JobId == "<JobId>")
            {
                expectedInput.JobId = _currentJobStepContext.JobId;
            }

            FundingGroupVersion expected = new FundingGroupVersion()
            {
                ChannelId = channel.ChannelId,
                FundingGroupId = expectedInput.FundingGroupId,
                GroupingReasonId = groupingReason.GroupingReasonId,
                CorrelationId = expectedInput.CorrelationId,
                EarliestPaymentAvailableDate = expectedInput.EarliestPaymentAvailableDate,
                ExternalPublicationDate = expectedInput.ExternalPublicationDate,
                FundingGroupVersionId = expectedInput.FundingGroupVersionId,
                FundingId = expectedInput.FundingId,
                FundingPeriodId = expectedInput.FundingPeriodId,
                FundingStreamId = expectedInput.FundingStreamId,
                JobId = expectedInput.JobId,
                MajorVersion = expectedInput.MajorVersion,
                MinorVersion = expectedInput.MinorVersion,
                SchemaVersion = expectedInput.SchemaVersion,
                StatusChangedDate = expectedInput.StatusChangedDate,
                TemplateVersion = expectedInput.TemplateVersion,
                TotalFunding = expectedInput.TotalFunding,
            };
            return expected;
        }

        [Given(@"a total of '([^']*)' funding group version records exist in the release management repository")]
        [Then(@"there are a total of '([^']*)' funding group version records created in the release management repository")]
        public void ThenThereAreATotalOfFundingGroupVersionRecordsCreatedInTheReleaseManagementRepository(int expectedCount)
        {
            _repoInMemory.FundingGroupsVersionsCount.Should().Be(expectedCount);
        }

        [Then(@"there is a funding group variation reason created in the release management repository")]
        public async Task ThenThereIsAFundingGroupVariationReasonCreatedInTheReleaseManagementRepository(Table table)
        {
            ExpectedFundingGroupVariationReason expectedInput = table.CreateInstance<ExpectedFundingGroupVariationReason>();
            VariationReasonSql variationReason = await GetVariationReasonAndAssertNotNull(expectedInput.VariationReason);

            FundingGroupVersionVariationReason expected = new FundingGroupVersionVariationReason()
            {
                FundingGroupVersionVariationReasonId = expectedInput.FundingGroupVersionVariationReasonId,
                VariationReasonId = variationReason.VariationReasonId,
                FundingGroupVersionId = expectedInput.FundingGroupVersionId,
            };

            FundingGroupVersionVariationReason actual = await _repoInMemory.GetFundingGroupVersionVariationReason(expected.FundingGroupVersionVariationReasonId);

            actual.Should().BeEquivalentTo(expected);
        }

        private async Task<VariationReasonSql> GetVariationReasonAndAssertNotNull(string variationReasonCode)
        {
            var variationReasons = await _repoInMemory.GetVariationReasons();
            VariationReason variationReason = variationReasons.SingleOrDefault(_ => _.VariationReasonCode == variationReasonCode);

            variationReason.Should().NotBeNull($"Unable to find variation reason '{variationReasonCode}'");
            return variationReason;
        }

        [Then(@"there are a total of '([^']*)' funding group version variation reason records created in the release management repository")]
        public void ThenThereAreATotalOfFundingGroupVersionVariationReasonRecordsCreatedInTheReleaseManagementRepository(int expectedCount)
        {
            _repoInMemory.FundingGroupsVariationReasonsCount.Should().Be(expectedCount);
        }

        [Then(@"there is the provider version associated with the funding group version in the release management repository")]
        public async Task ThenThereIsTheProviderVersionAssociatedWithTheFundingGroupVersionInTheReleaseManagementRepository(Table table)
        {
            FundingGroupProvider expected = table.CreateInstance<FundingGroupProvider>();

            FundingGroupProvider actualProvider = await _repoInMemory.GetFundingGroupProviderById(expected.FundingGroupProviderId);

            actualProvider.Should().BeEquivalentTo(expected);
        }

        [Given(@"a total of '([^']*)' funding group providers exist in the release management repository")]
        [Then(@"there are a total of '([^']*)' funding group providers created in the release management repository")]
        public void ThenThereAreATotalOfFundingGroupProvidersCreatedInTheReleaseManagementRepository(int expectedCount)
        {
            _repoInMemory.FundingGroupProvidersCount.Should().Be(expectedCount);
        }
    }
}
