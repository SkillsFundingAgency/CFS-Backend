using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Threading;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Specifications;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;
using Serilog.Core;

namespace CalculateFunding.Services.Publishing.UnitTests.Specifications
{
    [TestClass]
    public class PublishedProviderFundingSummaryProcessorTests
    {
        private readonly string Contracting = "Contracting";
        private readonly string Statement = "Statement";
        private readonly int ContractingChannelId = 1;
        private readonly int StatementChannelId = 2;

        private string _specificationId;
        private string[] _pageOne;
        private string[] _pageTwo;
        private string[] _pageThree;
        private string[] _publishedProviderIds;
        private SpecificationSummary _specificationSummary;
        private FundingConfiguration _fundingConfiguration;
        private PublishedProviderFundingSummaryProcessor _summaryProcessor;
        private List<Channel> _channels;

        private Mock<IPublishedFundingRepository> _publishedFunding;
        private Mock<IReleaseManagementRepository> _releaseManagementRepo;
        private Mock<IProvidersForChannelFilterService> _channelFilterService;
        private Mock<IChannelOrganisationGroupGeneratorService> _organisationGroupGenerator;

        [TestInitialize]
        public void SetUp()
        {
            _publishedFunding = new Mock<IPublishedFundingRepository>();
            _releaseManagementRepo = new Mock<IReleaseManagementRepository>();
            _channelFilterService = new Mock<IProvidersForChannelFilterService>();
            _organisationGroupGenerator = new Mock<IChannelOrganisationGroupGeneratorService>();

            _specificationId = NewRandomString();
            _specificationSummary = NewSpecificationSummary(_ => _.WithId(_specificationId));
            _fundingConfiguration = NewFundingConfiguration();

            _pageOne = NewRandomPublishedProviderIdsPage().ToArray();
            _pageTwo = NewRandomPublishedProviderIdsPage().ToArray();
            _pageThree = NewRandomPublishedProviderIdsPage().ToArray();
            _publishedProviderIds = Join(_pageOne, _pageTwo, _pageThree);

            SetUpChannels();

            _summaryProcessor = new PublishedProviderFundingSummaryProcessor(new ProducerConsumerFactory(),
                _publishedFunding.Object,
                new ResiliencePolicies
                {
                    PublishedFundingRepository = Policy.NoOpAsync()
                },
                _releaseManagementRepo.Object,
                _channelFilterService.Object,
                _organisationGroupGenerator.Object,
                Logger.None);
        }

        /// <summary>
        /// 3 Published Providers and 2 Channels
        /// All to be released into both channels
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task ReleaseAllProvidersIntoAllChannels()
        {
            PublishedProviderFundingSummary[] fundings = GenerateFundings();

            GivenProviderVersionInChannels(fundings.Select(_ => new ProviderVersionInChannel
            {
                ProviderId = _.ProviderId,
                MajorVersion = _.MajorVersion + fundings.Count(), // Generate a different major version
                ChannelId = ContractingChannelId
            }).ToArray());

            GivenOrganisationGroupResults(AsArray(
                NewOrganisationGroupResult(_ => _.WithProviders(AsArray(
                new Common.ApiClient.Providers.Models.Provider { ProviderId = fundings[0].ProviderId },
                new Common.ApiClient.Providers.Models.Provider { ProviderId = fundings[1].ProviderId },
                new Common.ApiClient.Providers.Models.Provider { ProviderId = fundings[2].ProviderId }
                )))));

            ReleaseFundingPublishedProvidersSummary actualSummary = await WhenTheFundingSummaryIsProcessed(_channels.Select(_ => _.ChannelCode));

            actualSummary
                .TotalProviders
                .Should()
                .Be(fundings.Count());

            actualSummary
                .TotalIndicativeProviders
                .Should()
                .Be(fundings.Where(_ => _.IsIndicative).Count());

            actualSummary
                .TotalFunding
                .Should()
                .Be(fundings[0].TotalFunding + fundings[1].TotalFunding + fundings[2].TotalFunding);

            actualSummary
                .ChannelFundings
                .Should()
                .BeEquivalentTo(new[]
                {
                    new ChannelFunding
                    {
                        ChannelCode = Contracting,
                        ChannelName = Contracting,
                        TotalProviders = fundings.Count(),
                        TotalFunding = fundings[0].TotalFunding + fundings[1].TotalFunding + fundings[2].TotalFunding
                    },
                    new ChannelFunding
                    {
                        ChannelCode = Statement,
                        ChannelName = Statement,
                        TotalProviders = fundings.Count(),
                        TotalFunding = fundings[0].TotalFunding + fundings[1].TotalFunding + fundings[2].TotalFunding
                    }
                });
        }

        /// <summary>
        /// 3 Published Providers and 2 Channels
        /// All to be released into one channel only
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task ReleaseAllProvidersIntoChannelWhereNoMajorVersion()
        {
            PublishedProviderFundingSummary[] fundings = GenerateFundings();

            GivenProviderVersionInChannels(fundings.Select(_ => new ProviderVersionInChannel
            {
                ProviderId = _.ProviderId,
                MajorVersion = _.MajorVersion, // Major version already released for Contracting
                ChannelId = ContractingChannelId
            }).ToArray());

            GivenOrganisationGroupResults(AsArray(
            NewOrganisationGroupResult(_ => _.WithProviders(AsArray(
            new Common.ApiClient.Providers.Models.Provider { ProviderId = fundings[0].ProviderId },
            new Common.ApiClient.Providers.Models.Provider { ProviderId = fundings[1].ProviderId },
            new Common.ApiClient.Providers.Models.Provider { ProviderId = fundings[2].ProviderId }
            )))));

            ReleaseFundingPublishedProvidersSummary actualSummary = await WhenTheFundingSummaryIsProcessed(_channels.Select(_ => _.ChannelCode));

            actualSummary
                .TotalProviders
                .Should()
                .Be(fundings.Count());

            actualSummary
                .TotalIndicativeProviders
                .Should()
                .Be(fundings.Where(_ => _.IsIndicative).Count());

            actualSummary
                .TotalFunding
                .Should()
                .Be(fundings[0].TotalFunding + fundings[1].TotalFunding + fundings[2].TotalFunding);

            actualSummary
                .ChannelFundings
                .Should()
                .BeEquivalentTo(new[]
                {
                    new ChannelFunding
                    {
                        ChannelCode = Statement,
                        ChannelName = Statement,
                        TotalProviders = fundings.Count(),
                        TotalFunding = fundings[0].TotalFunding + fundings[1].TotalFunding + fundings[2].TotalFunding
                    }
                });
        }

        /// <summary>
        /// 3 Published Providers and 2 Channels
        /// All to be released into both channels except those not in organisation group result
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task ReleaseOnlyProvidersWithOrganisationGroupResultIntoAllChannels()
        {
            PublishedProviderFundingSummary[] fundings = GenerateFundings();

            GivenProviderVersionInChannels(fundings.Select(_ => new ProviderVersionInChannel
            {
                ProviderId = _.ProviderId,
                MajorVersion = _.MajorVersion + fundings.Count(), // Generate a different major version
                ChannelId = ContractingChannelId
            }).ToArray());

            GivenOrganisationGroupResults(AsArray(
                NewOrganisationGroupResult(_ => _.WithProviders(AsArray(
                // Note missing provider 1
                new Common.ApiClient.Providers.Models.Provider { ProviderId = fundings[1].ProviderId },
                new Common.ApiClient.Providers.Models.Provider { ProviderId = fundings[2].ProviderId }
                )))));

            ReleaseFundingPublishedProvidersSummary actualSummary = await WhenTheFundingSummaryIsProcessed(_channels.Select(_ => _.ChannelCode));

            actualSummary
                .TotalProviders
                .Should()
                .Be(fundings.Count() - 1);

            actualSummary
                .TotalIndicativeProviders
                .Should()
                .Be(0);

            actualSummary
                .TotalFunding
                .Should()
                .Be(fundings[1].TotalFunding + fundings[2].TotalFunding);

            actualSummary
                .ChannelFundings
                .Should()
                .BeEquivalentTo(new[]
                {
                    new ChannelFunding
                    {
                        ChannelCode = Contracting,
                        ChannelName = Contracting,
                        TotalProviders = fundings.Count() - 1,
                        TotalFunding = fundings[1].TotalFunding + fundings[2].TotalFunding
                    },
                    new ChannelFunding
                    {
                        ChannelCode = Statement,
                        ChannelName = Statement,
                        TotalProviders = fundings.Count() - 1,
                        TotalFunding = fundings[1].TotalFunding + fundings[2].TotalFunding
                    }
                });
        }

        /// <summary>
        /// 3 Published Providers and 2 Channels
        /// All to be released into one channel
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task ReleaseAllProvidersIntoOneChannel()
        {
            PublishedProviderFundingSummary[] fundings = GenerateFundings();

            GivenProviderVersionInChannels(fundings.Select(_ => new ProviderVersionInChannel
            {
                ProviderId = _.ProviderId,
                MajorVersion = _.MajorVersion + fundings.Count(), // Generate a different major version
                ChannelId = ContractingChannelId
            }).ToArray());

            GivenOrganisationGroupResults(AsArray(
                NewOrganisationGroupResult(_ => _.WithProviders(AsArray(
                new Common.ApiClient.Providers.Models.Provider { ProviderId = fundings[0].ProviderId },
                new Common.ApiClient.Providers.Models.Provider { ProviderId = fundings[1].ProviderId },
                new Common.ApiClient.Providers.Models.Provider { ProviderId = fundings[2].ProviderId }
                )))));

            ReleaseFundingPublishedProvidersSummary actualSummary = await WhenTheFundingSummaryIsProcessed(new string[] { Contracting });

            actualSummary
                .TotalProviders
                .Should()
                .Be(fundings.Count());

            actualSummary
                .TotalIndicativeProviders
                .Should()
                .Be(fundings.Where(_ => _.IsIndicative).Count());

            actualSummary
                .TotalFunding
                .Should()
                .Be(fundings[0].TotalFunding + fundings[1].TotalFunding + fundings[2].TotalFunding);

            actualSummary
                .ChannelFundings
                .Should()
                .BeEquivalentTo(new[]
                {
                    new ChannelFunding
                    {
                        ChannelCode = Contracting,
                        ChannelName = Contracting,
                        TotalProviders = fundings.Count(),
                        TotalFunding = fundings[0].TotalFunding + fundings[1].TotalFunding + fundings[2].TotalFunding
                    }
                });
        }

        /// <summary>
        /// 3 Published Providers and 2 Channels
        /// All to be released into one channel where only some providers need to be released
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task ReleaseSomeProvidersIntoOneChannel()
        {
            PublishedProviderFundingSummary[] fundings = GenerateFundings();

            GivenProviderVersionInChannels(fundings.Select(_ => new ProviderVersionInChannel
            {
                ProviderId = _.ProviderId,
                MajorVersion = _.MajorVersion,
                ChannelId = ContractingChannelId
            }).Take(1).ToArray());

            GivenOrganisationGroupResults(AsArray(
                NewOrganisationGroupResult(_ => _.WithProviders(AsArray(
                new Common.ApiClient.Providers.Models.Provider { ProviderId = fundings[0].ProviderId },
                new Common.ApiClient.Providers.Models.Provider { ProviderId = fundings[1].ProviderId },
                new Common.ApiClient.Providers.Models.Provider { ProviderId = fundings[2].ProviderId }
                )))));

            ReleaseFundingPublishedProvidersSummary actualSummary = await WhenTheFundingSummaryIsProcessed(new string[] { Contracting });

            actualSummary
                .TotalProviders
                .Should()
                .Be(fundings.Count() - 1);

            actualSummary
                .TotalFunding
                .Should()
                .Be(fundings[1].TotalFunding + fundings[2].TotalFunding);

            actualSummary
                .ChannelFundings
                .Should()
                .BeEquivalentTo(new[]
                {
                    new ChannelFunding
                    {
                        ChannelCode = Contracting,
                        ChannelName = Contracting,
                        TotalProviders = fundings.Count() - 1,
                        TotalFunding = fundings[1].TotalFunding + fundings[2].TotalFunding
                    }
                });
        }

        private void SetUpChannels()
        {
            _channels = new List<Channel>
            {
                new Channel { ChannelId = ContractingChannelId, ChannelCode = Contracting, ChannelName = Contracting },
                new Channel { ChannelId = StatementChannelId, ChannelCode = Statement, ChannelName = Statement }
            };

            foreach (Channel channel in _channels)
            {
                _releaseManagementRepo.Setup(_ => _.GetChannelByChannelCode(It.Is<string>(s => s == channel.ChannelCode)))
                .ReturnsAsync(channel);
            }

            // Filter returns inputted IEnumerable<PublishedProviderVersion> for simplicity
            // No need to test FilterProvidersForChannel logic here
            _channelFilterService.Setup(_ => _.FilterProvidersForChannel(
                It.IsAny<Channel>(),
                It.IsAny<IEnumerable<PublishedProviderVersion>>(),
                It.IsAny<FundingConfiguration>()))
                .Returns((Channel a, IEnumerable<PublishedProviderVersion> b, FundingConfiguration c) => b);
        }

        private PublishedProviderFundingSummary[] GenerateFundings()
        {
            decimal totalFunding1 = NewRandomNumber();
            decimal totalFunding2 = NewRandomNumber();
            decimal totalFunding3 = NewRandomNumber();

            int majorVersionOne = 1;
            int majorVersionTwo = 2;
            int majorVersionThree = 3;

            PublishedProviderFundingSummary fundingOne = NewPublishedProviderFunding(_ => _
                .WithSpecificationId(_specificationId)
                .WithProviderId(_pageOne.First())
                .WithProviderType(NewRandomString())
                .WithProviderSubType(NewRandomString())
                .WithIsIndicative(true)
                .WithMajorVersion(majorVersionOne)
                .WithMinorVersion(majorVersionOne)
                .WithTotalFunding(totalFunding1));
            PublishedProviderFundingSummary fundingTwo = NewPublishedProviderFunding(_ => _
                .WithSpecificationId(_specificationId)
                .WithProviderId(_pageTwo.Last())
                .WithProviderType(NewRandomString())
                .WithProviderSubType(NewRandomString())
                .WithIsIndicative(false)
                .WithMajorVersion(majorVersionTwo)
                .WithMinorVersion(majorVersionTwo)
                .WithTotalFunding(totalFunding2));
            PublishedProviderFundingSummary fundingThree = NewPublishedProviderFunding(_ => _
                .WithSpecificationId(_specificationId)
                .WithProviderId(_pageThree.Skip(1).First())
                .WithProviderType(NewRandomString())
                .WithProviderSubType(NewRandomString())
                .WithIsIndicative(false)
                .WithMajorVersion(majorVersionThree)
                .WithMinorVersion(majorVersionThree)
                .WithTotalFunding(totalFunding3));

            GivenThePublishedProvidersFundingSummary(_pageOne, new[] { fundingOne });
            AndThePublishedProviderFundingSummary(_pageTwo, new[] { fundingTwo });
            AndThePublishedProviderFundingSummary(_pageThree, new[] { fundingThree });

            return AsArray(fundingOne, fundingTwo, fundingThree);
        }

        private async Task<ReleaseFundingPublishedProvidersSummary> WhenTheFundingSummaryIsProcessed(IEnumerable<string> channelCodes)
            => await _summaryProcessor.GetFundingSummaryForApprovedPublishedProvidersByChannel(
                _publishedProviderIds, _specificationSummary, _fundingConfiguration, channelCodes);

        private void AndThePublishedProviderFundingSummary(IEnumerable<string> publishedProviderIds,
            IEnumerable<PublishedProviderFundingSummary> fundings)
        {
            GivenThePublishedProvidersFundingSummary(publishedProviderIds, fundings);
        }

        private void GivenThePublishedProvidersFundingSummary(IEnumerable<string> publishedProviderIds,
            IEnumerable<PublishedProviderFundingSummary> fundings)
        {
            _publishedFunding.Setup(_ => _.GetReleaseFundingPublishedProviders(It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(publishedProviderIds)),
                    It.Is<string>(spec => spec == _specificationId),
                    It.Is<PublishedProviderStatus[]>(sts => sts.SequenceEqual(new List<PublishedProviderStatus> { PublishedProviderStatus.Approved }))))
                .ReturnsAsync(fundings);
        }

        private void GivenProviderVersionInChannels(IEnumerable<ProviderVersionInChannel> providerVersionInChannels)
        {
            _releaseManagementRepo.Setup(_ => _.GetLatestPublishedProviderVersions(It.Is<string>(s => s == _specificationId),
                It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(providerVersionInChannels);
        }

        private void GivenOrganisationGroupResults(IEnumerable<OrganisationGroupResult> organisationGroupResults)
        {
            _organisationGroupGenerator.Setup(_ => _.GenerateOrganisationGroups(
                It.IsAny<Channel>(),
                It.IsAny<FundingConfiguration>(),
                It.IsAny<SpecificationSummary>(),
                It.IsAny<IEnumerable<PublishedProviderVersion>>()))
            .ReturnsAsync(organisationGroupResults);
        }

        private string[] Join(params string[][] pages) => pages.SelectMany(_ => _).ToArray();

        private IEnumerable<string> NewRandomPublishedProviderIdsPage()
        {
            for (int id = 0; id < 100; id++)
            {
                yield return NewRandomString();
            }
        }

        private string NewRandomString() => new RandomString();
        private decimal NewRandomNumber() => new RandomNumberBetween(0, int.MaxValue);

        private TItem[] AsArray<TItem>(params TItem[] items) => items;

        private PublishedProviderStatus NewRandomStatus() => new RandomEnum<PublishedProviderStatus>();

        private PublishedProviderFundingSummary NewPublishedProviderFunding(Action<PublishedProviderFundingSummaryBuilder> setUp = null)
        {
            PublishedProviderFundingSummaryBuilder builder = new PublishedProviderFundingSummaryBuilder();

            setUp?.Invoke(builder);

            return builder.Build();
        }

        private SpecificationSummary NewSpecificationSummary(Action<SpecificationSummaryBuilder> setUp = null)
        {
            SpecificationSummaryBuilder builder = new SpecificationSummaryBuilder();

            setUp?.Invoke(builder);

            return builder.Build();
        }

        private FundingConfiguration NewFundingConfiguration(Action<FundingConfigurationBuilder> setUp = null)
        {
            FundingConfigurationBuilder builder = new FundingConfigurationBuilder();

            setUp?.Invoke(builder);

            return builder.Build();
        }

        private OrganisationGroupResult NewOrganisationGroupResult(Action<OrganisationGroupResultBuilder> setup = null)
        {
            OrganisationGroupResultBuilder builder = new OrganisationGroupResultBuilder();

            setup?.Invoke(builder);

            return builder.Build();
        }
    }
}