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
using System.Text;
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

        public ReleaseManagementRepositoryStepDefinitions(IReleaseManagementRepository releaseManagementRepository)
        {
            _repo = releaseManagementRepository;
            _repoInMemory = (InMemoryReleaseManagementRepository)releaseManagementRepository;
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

        [Then(@"there is a released provider record in the release management repository")]
        public void ThenThereIsAReleasedProviderVersionInTheReleaseManagementRepository(Table table)
        {
            ReleasedProvider expectedProvider = table.CreateInstance<ReleasedProvider>();

            ReleasedProvider actualProvider = _repoInMemory.GetReleasedProviderById(expectedProvider.ReleasedProviderId);

            actualProvider.Should().BeEquivalentTo(actualProvider);
        }

    }
}
