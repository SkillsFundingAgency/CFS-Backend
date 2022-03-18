using CalculateFunding.Common.ApiClient.Policies.Models;
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.UnitTests.Specifications
{
    [TestClass]
    public class PublishedProviderFundingSummaryProcessorTests
    {
        private readonly string Contracting = "Contracting";
        private readonly string Statement = "Statement";
        private readonly string Payment = "Payment";
        private readonly int ContractingChannelId = 1;
        private readonly int StatementChannelId = 2;

        private string _specificationId;
        private string[] _providersOne;
        private string[] _providersTwo;
        private string[] _providersThree;
        private string[] _publishedProviderIds;
        private SpecificationSummary _specificationSummary;
        private FundingConfiguration _fundingConfiguration;
        private PublishedProviderFundingSummaryProcessor _summaryProcessor;
        private List<Channel> _channels;

        private Mock<IReleaseManagementRepository> _releaseManagementRepo;
        private Mock<IProvidersForChannelFilterService> _channelFilterService;
        private Mock<IChannelOrganisationGroupGeneratorService> _organisationGroupGenerator;
        private Mock<IPublishedProviderLookupService> _publishedProvidersLookupService;

        [TestInitialize]
        public void SetUp()
        {
            _releaseManagementRepo = new Mock<IReleaseManagementRepository>();
            _channelFilterService = new Mock<IProvidersForChannelFilterService>();
            _organisationGroupGenerator = new Mock<IChannelOrganisationGroupGeneratorService>();
            _publishedProvidersLookupService = new Mock<IPublishedProviderLookupService>();
            _specificationId = NewRandomString();
            _specificationSummary = NewSpecificationSummary(_ => _.WithId(_specificationId));
            _fundingConfiguration = NewFundingConfiguration(_ => _.WithReleaseChannels(new FundingConfigurationChannel { 
                ChannelCode = Contracting,
                IsVisible = true 
            },
            new FundingConfigurationChannel
            {
                ChannelCode = Statement,
                IsVisible = true
            },
            new FundingConfigurationChannel
            {
                ChannelCode = Payment
            }));

            _providersOne = NewRandomPublishedProviderIds().ToArray();
            _providersTwo = NewRandomPublishedProviderIds().ToArray();
            _providersThree = NewRandomPublishedProviderIds().ToArray();
            _publishedProviderIds = Join(_providersOne, _providersTwo, _providersThree);

            SetUpChannels();

            _summaryProcessor = new PublishedProviderFundingSummaryProcessor(new ProducerConsumerFactory(),
                _releaseManagementRepo.Object,
                _channelFilterService.Object,
                _organisationGroupGenerator.Object,
                _publishedProvidersLookupService.Object);
        }

        /// <summary>
        /// 3 Published Providers and 2 Channels
        /// All to be released into both channels
        /// Major versions different or do not exist in sql
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task ReleaseAllProvidersIntoAllChannels()
        {
            PublishedProviderFundingSummary[] fundings = GenerateFundings();

            GivenProviderVersionInChannels(fundings.Select(_ => new ProviderVersionInChannel
            {
                ProviderId = _.Provider.ProviderId,
                MajorVersion = 1, // Version in rm sql db
                ChannelId = ContractingChannelId
            }).ToArray());

            GivenOrganisationGroupResults(AsArray(
                NewOrganisationGroupResult(_ => _.WithProviders(AsArray(
                new Common.ApiClient.Providers.Models.Provider { ProviderId = fundings[0].Provider.ProviderId },
                new Common.ApiClient.Providers.Models.Provider { ProviderId = fundings[1].Provider.ProviderId },
                new Common.ApiClient.Providers.Models.Provider { ProviderId = fundings[2].Provider.ProviderId }
                )))));

            ReleaseFundingPublishedProvidersSummary actualSummary = await WhenTheFundingSummaryIsProcessed(_channels.Select(_ => _.ChannelCode));

            actualSummary
                .TotalProviders
                .Should()
                .Be(fundings.Length);

            actualSummary
                .TotalIndicativeProviders
                .Should()
                .Be(fundings.Count(_ => _.IsIndicative));

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
                        TotalProviders = fundings.Length,
                        TotalFunding = fundings[0].TotalFunding + fundings[1].TotalFunding + fundings[2].TotalFunding
                    },
                    new ChannelFunding
                    {
                        ChannelCode = Statement,
                        ChannelName = Statement,
                        TotalProviders = fundings.Length,
                        TotalFunding = fundings[0].TotalFunding + fundings[1].TotalFunding + fundings[2].TotalFunding
                    }
                });
        }

        /// <summary>
        /// 3 Published Providers and 2 Channels
        /// All to be released into both channels
        /// Major versions different or do not exist in sql
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task ReleaseAllProvidersIntoChannelWhereNoMajorVersion()
        {
            PublishedProviderFundingSummary[] fundings = GenerateFundings();

            GivenProviderVersionInChannels(fundings.Select(_ => new ProviderVersionInChannel
            {
                ProviderId = _.Provider.ProviderId,
                MajorVersion = _.MajorVersion, // Major version already released for Contracting
                ChannelId = ContractingChannelId
            }).ToArray());

            GivenOrganisationGroupResults(AsArray(
            NewOrganisationGroupResult(_ => _.WithProviders(AsArray(
            new Common.ApiClient.Providers.Models.Provider { ProviderId = fundings[0].Provider.ProviderId },
            new Common.ApiClient.Providers.Models.Provider { ProviderId = fundings[1].Provider.ProviderId },
            new Common.ApiClient.Providers.Models.Provider { ProviderId = fundings[2].Provider.ProviderId }
            )))));

            ReleaseFundingPublishedProvidersSummary actualSummary = await WhenTheFundingSummaryIsProcessed(_channels.Select(_ => _.ChannelCode));

            actualSummary
                .TotalProviders
                .Should()
                .Be(fundings.Length);

            actualSummary
                .TotalIndicativeProviders
                .Should()
                .Be(fundings.Count(_ => _.IsIndicative));

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
                        TotalProviders = fundings.Count(_ => _.Status == "Approved"),
                        TotalFunding = fundings[0].TotalFunding + fundings[2].TotalFunding
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
        /// All to be released into both channels except those not in organisation group result
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task ReleaseOnlyProvidersWithOrganisationGroupResultIntoAllChannels()
        {
            PublishedProviderFundingSummary[] fundings = GenerateFundings();

            GivenProviderVersionInChannels(fundings.Select(_ => new ProviderVersionInChannel
            {
                ProviderId = _.Provider.ProviderId,
                MajorVersion = _.MajorVersion - 1, // Generate a different major version
                ChannelId = ContractingChannelId
            }).ToArray());

            GivenOrganisationGroupResults(AsArray(
                NewOrganisationGroupResult(_ => _.WithProviders(AsArray(
                // Note missing provider 1
                new Common.ApiClient.Providers.Models.Provider { ProviderId = fundings[1].Provider.ProviderId },
                new Common.ApiClient.Providers.Models.Provider { ProviderId = fundings[2].Provider.ProviderId }
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
                ProviderId = _.Provider.ProviderId,
                MajorVersion = _.MajorVersion,
                ChannelId = ContractingChannelId
            }).ToArray());

            GivenOrganisationGroupResults(AsArray(
                NewOrganisationGroupResult(_ => _.WithProviders(AsArray(
                new Common.ApiClient.Providers.Models.Provider { ProviderId = fundings[0].Provider.ProviderId },
                new Common.ApiClient.Providers.Models.Provider { ProviderId = fundings[1].Provider.ProviderId },
                new Common.ApiClient.Providers.Models.Provider { ProviderId = fundings[2].Provider.ProviderId }
                )))));

            ReleaseFundingPublishedProvidersSummary actualSummary = await WhenTheFundingSummaryIsProcessed(new string[] { Contracting });

            actualSummary
                .TotalProviders
                .Should()
                .Be(fundings.Count(_ => _.Status == "Approved"));

            actualSummary
                .TotalIndicativeProviders
                .Should()
                .Be(fundings.Count(_ => _.IsIndicative && _.Status == "Approved"));

            actualSummary
                .TotalFunding
                .Should()
                .Be(fundings[0].TotalFunding + fundings[2].TotalFunding);

            actualSummary
                .ChannelFundings
                .Should()
                .BeEquivalentTo(new[]
                {
                    new ChannelFunding
                    {
                        ChannelCode = Contracting,
                        ChannelName = Contracting,
                        TotalProviders = fundings.Count(_ => _.Status == "Approved"),
                        TotalFunding = fundings[0].TotalFunding + fundings[2].TotalFunding
                    }
                });
        }

        /// <summary>
        /// 3 Published Providers and 2 Channels
        /// All to be released into one channel
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task ReleaseSomeProvidersIntoOneChannel()
        {
            PublishedProviderFundingSummary[] fundings = GenerateFundings();

            GivenProviderVersionInChannels(fundings.Where(_ => _.Status == "Released" ).Select(_ => new ProviderVersionInChannel
            {
                ProviderId = _.Provider.ProviderId,
                MajorVersion = _.MajorVersion + 1,
                ChannelId = ContractingChannelId
            }).ToArray());

            GivenOrganisationGroupResults(AsArray(
                NewOrganisationGroupResult(_ => _.WithProviders(AsArray(
                new Common.ApiClient.Providers.Models.Provider { ProviderId = fundings[0].Provider.ProviderId },
                new Common.ApiClient.Providers.Models.Provider { ProviderId = fundings[1].Provider.ProviderId },
                new Common.ApiClient.Providers.Models.Provider { ProviderId = fundings[2].Provider.ProviderId }
                )))));

            ReleaseFundingPublishedProvidersSummary actualSummary = await WhenTheFundingSummaryIsProcessed(new string[] { Contracting });

            actualSummary
                .TotalProviders
                .Should()
                .Be(fundings.Length - 1);

            actualSummary
                .TotalFunding
                .Should()
                .Be(fundings[0].TotalFunding + fundings[2].TotalFunding);

            actualSummary
                .ChannelFundings
                .Should()
                .BeEquivalentTo(new[]
                {
                    new ChannelFunding
                    {
                        ChannelCode = Contracting,
                        ChannelName = Contracting,
                        TotalProviders = fundings.Length - 1,
                        TotalFunding = fundings[0].TotalFunding + fundings[2].TotalFunding
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

            int majorVersionOne = 10;
            int majorVersionTwo = 20;
            int majorVersionThree = 30;

            PublishedProviderFundingSummary fundingOne = NewPublishedProviderFunding(_ => _
                .WithSpecificationId(_specificationId)
                .WithStatus("Approved")
                .WithProvider(
                    NewProvider(p => p.WithProviderType(NewRandomString()).WithProviderSubType(NewRandomString()).WithProviderId(_providersOne.First())))
                .WithIsIndicative(true)
                .WithMajorVersion(majorVersionOne)
                .WithMinorVersion(majorVersionOne)
                .WithTotalFunding(totalFunding1));
            PublishedProviderFundingSummary fundingTwo = NewPublishedProviderFunding(_ => _
                .WithSpecificationId(_specificationId)
                .WithStatus("Released")
                .WithProvider(
                    NewProvider(p => p.WithProviderType(NewRandomString()).WithProviderSubType(NewRandomString()).WithProviderId(_providersTwo.Last())))
                .WithIsIndicative(false)
                .WithMajorVersion(majorVersionTwo)
                .WithMinorVersion(majorVersionTwo)
                .WithTotalFunding(totalFunding2));
            PublishedProviderFundingSummary fundingThree = NewPublishedProviderFunding(_ => _
                .WithSpecificationId(_specificationId)
                .WithStatus("Approved")
                .WithProvider(
                    NewProvider(p => p.WithProviderType(NewRandomString()).WithProviderSubType(NewRandomString()).WithProviderId(_providersThree.Skip(1).First())))
                .WithIsIndicative(false)
                .WithMajorVersion(majorVersionThree)
                .WithMinorVersion(majorVersionThree)
                .WithTotalFunding(totalFunding3));

            GivenThePublishedProvidersFundingSummary(_publishedProviderIds, new[] { fundingOne, fundingTwo, fundingThree });
            
            return AsArray(fundingOne, fundingTwo, fundingThree);
        }

        private async Task<ReleaseFundingPublishedProvidersSummary> WhenTheFundingSummaryIsProcessed(IEnumerable<string> channelCodes)
            => await _summaryProcessor.GetFundingSummaryForApprovedPublishedProvidersByChannel(
                _publishedProviderIds, _specificationSummary, _fundingConfiguration, channelCodes);

        private void GivenThePublishedProvidersFundingSummary(IEnumerable<string> publishedProviderIds,
            IEnumerable<PublishedProviderFundingSummary> fundings)
        {
            IEnumerable<PublishedProviderStatus> statuses = new[] { PublishedProviderStatus.Approved, PublishedProviderStatus.Released };

            _publishedProvidersLookupService.Setup(_ => _.GetPublishedProviderFundingSummaries(
                    It.Is<SpecificationSummary>(s => s.Id == _specificationId),
                    It.Is<PublishedProviderStatus[]>(ps => ps.SequenceEqual(statuses)),
                    It.Is<IEnumerable<string>>(p => p.SequenceEqual(_publishedProviderIds))
                ))

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

        private IEnumerable<string> NewRandomPublishedProviderIds()
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

        private Provider NewProvider(Action<ProviderBuilder> setUp = null)
        {
            ProviderBuilder builder = new ProviderBuilder();

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