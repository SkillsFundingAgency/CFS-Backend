using AutoMapper;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Generators.OrganisationGroup.Enums;
using CalculateFunding.Generators.OrganisationGroup.Interfaces;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApiProvider = CalculateFunding.Common.ApiClient.Providers.Models.Provider;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class PublishedFundingOrganisationGroupingServiceTests
    {
        private PublishedFundingOrganisationGroupingService _publishedFundingOrganisationGroupingService;
        private IPoliciesService _policiesService;
        private IProviderService _providerService;
        private IOrganisationGroupGenerator _organisationGroupGenerator;
        private IMapper _mapper;
        private IPublishedFundingChangeDetectorService _publishedFundingChangeDetectorService;

        private FundingConfiguration _fundingConfiguration;
        private IDictionary<string, PublishedProvider> _publishedProvidersForFundingStream;
        private IDictionary<string, PublishedProvider> _scopedPublishedProviders;
        private IEnumerable<OrganisationGroupResult> _organisationGroupResults;
        private IEnumerable<PublishedFundingOrganisationGrouping> _publishedFundingOrganisationGrouping;

        private bool _includeHistory;
        private string _fundingStreamId;
        private string _fundingPeriodId;
        private SpecificationSummary _specificationSummary;
        private IEnumerable<PublishedFundingVersion> _publishedFundingVersions;

        [TestInitialize]
        public void SetUp()
        {
            _policiesService = Substitute.For<IPoliciesService>();
            _providerService = Substitute.For<IProviderService>();
            _organisationGroupGenerator = Substitute.For<IOrganisationGroupGenerator>();
            _mapper = Substitute.For<IMapper>();
            _publishedFundingChangeDetectorService = Substitute.For<IPublishedFundingChangeDetectorService>();

            _publishedFundingOrganisationGroupingService = new PublishedFundingOrganisationGroupingService(
                _policiesService,
                _providerService,
                _organisationGroupGenerator,
                _mapper,
                _publishedFundingChangeDetectorService
                );
        }

        [TestMethod]
        public async Task GeneratePublishedFundingOrganisationGrouping_Succeed_GroupingReturned()
        {
            _fundingStreamId = "fundingStream1";
            _fundingPeriodId = "fundingPeriod1";

            GivenSpecificationSummary();

            GivenFundingConfiguration();
            GivenFundingConfigurationRetrieved();

            GivenPublishedProvidersForFundingStream();
            GivenScopedPublishedProviders();
            GivenPublishedProvidersRetrieved();

            GivenOrganisationGroupResults();
            GivenGenerateOrganisationGroup();

            GivenPublishedFundingOrganisationGrouping();
            GivenGenerateOrganisationGroupings();

            await WhenPublishedFundingOrganisationGroupingGenerated();
        }

        private void GivenSpecificationSummary()
        {
            _specificationSummary = NewSpecificationSummary(_=>_.WithFundingPeriodId(_fundingPeriodId));
        }

        private void GivenFundingConfiguration()
        {
            _fundingConfiguration = NewFundingConfiguration();
        }

        private void GivenFundingConfigurationRetrieved()
        {
            _policiesService
                .GetFundingConfiguration(Arg.Is(_fundingStreamId), Arg.Is(_fundingPeriodId))
                .Returns(_fundingConfiguration);
        }

        private void GivenPublishedProvidersForFundingStream()
        {
            _publishedProvidersForFundingStream = new Dictionary<string, PublishedProvider>
            {
                { "key1", NewPublishedProvider() }
            };
        }

        private void GivenScopedPublishedProviders()
        {
            _scopedPublishedProviders = new Dictionary<string, PublishedProvider>
            {
                { "key2", NewPublishedProvider(_ => 
                _.WithCurrent(
                    NewPublishedProviderVersion(ppv => 
                    ppv.WithVersion(1)))) }
            };
        }

        private void GivenPublishedProvidersRetrieved()
        {
            _providerService
                .GetPublishedProviders(Arg.Any<Reference>(), Arg.Any<SpecificationSummary>())
                .Returns((_publishedProvidersForFundingStream, _scopedPublishedProviders));
        }

        private void GivenOrganisationGroupResults()
        {
            _organisationGroupResults = NewOrganisationGroupResults(_ =>
                _.WithGroupTypeCode(OrganisationGroupTypeCode.LocalAuthority));
        }
        
        private void GivenGenerateOrganisationGroup()
        {
            _organisationGroupGenerator
                .GenerateOrganisationGroup(Arg.Any<FundingConfiguration>(), Arg.Any<IEnumerable<ApiProvider>>(), Arg.Any<string>())
                .Returns(_organisationGroupResults);
        }

        private void GivenPublishedFundingOrganisationGrouping()
        {
            _publishedFundingOrganisationGrouping = NewPublishedFundingOrganisationGroupings(_ =>
            _.WithOrganisationGroupResult(
                NewOrganisationGroupResult( ogr =>
                    ogr.WithGroupTypeCode(OrganisationGroupTypeCode.LocalAuthority)
                )));
        }

        private void GivenGenerateOrganisationGroupings()
        {
            _publishedFundingChangeDetectorService
                .GenerateOrganisationGroupings(
                Arg.Any<IEnumerable<OrganisationGroupResult>>(), 
                Arg.Any<IEnumerable<PublishedFundingVersion>>(), 
                Arg.Any<IDictionary<string, PublishedProvider>>(), 
                Arg.Any<bool>())
                .Returns(_publishedFundingOrganisationGrouping);
        }

        private async Task<IEnumerable<PublishedFundingOrganisationGrouping>> WhenPublishedFundingOrganisationGroupingGenerated()
        {
            return await _publishedFundingOrganisationGroupingService
                .GeneratePublishedFundingOrganisationGrouping(_includeHistory, _fundingStreamId, _specificationSummary, _publishedFundingVersions);
        }

        private FundingConfiguration NewFundingConfiguration(Action<FundingConfigurationBuilder> setUp = null)
        {
            FundingConfigurationBuilder fundingConfigurationBuilder = new FundingConfigurationBuilder();

            setUp?.Invoke(fundingConfigurationBuilder);

            return fundingConfigurationBuilder.Build();
        }

        private PublishedProvider NewPublishedProvider(Action<PublishedProviderBuilder> setUp = null)
        {
            PublishedProviderBuilder publishedProviderBuilder = new PublishedProviderBuilder();

            setUp?.Invoke(publishedProviderBuilder);

            return publishedProviderBuilder.Build();
        }

        private PublishedProviderVersion NewPublishedProviderVersion(Action<PublishedProviderVersionBuilder> setUp = null)
        {
            PublishedProviderVersionBuilder publishedProviderVersionBuilder = new PublishedProviderVersionBuilder();

            setUp?.Invoke(publishedProviderVersionBuilder);

            return publishedProviderVersionBuilder.Build();
        }

        private OrganisationGroupResult NewOrganisationGroupResult(Action<OrganisationGroupResultBuilder> setUp = null)
        {
            OrganisationGroupResultBuilder organisationGroupResultBuilder = new OrganisationGroupResultBuilder();

            setUp?.Invoke(organisationGroupResultBuilder);

            return organisationGroupResultBuilder.Build();
        }

        private IEnumerable<OrganisationGroupResult> NewOrganisationGroupResults(params Action<OrganisationGroupResultBuilder>[] setUp)
        {
            return setUp.Select(NewOrganisationGroupResult);
        }

        private PublishedFundingOrganisationGrouping NewPublishedFundingOrganisationGrouping(Action<PublishedFundingOrganisationGroupingBuilder> setUp = null)
        {
            PublishedFundingOrganisationGroupingBuilder publishedFundingOrganisationGroupingBuilder = new PublishedFundingOrganisationGroupingBuilder();

            setUp?.Invoke(publishedFundingOrganisationGroupingBuilder);

            return publishedFundingOrganisationGroupingBuilder.Build();
        }

        private IEnumerable<PublishedFundingOrganisationGrouping> NewPublishedFundingOrganisationGroupings(params Action<PublishedFundingOrganisationGroupingBuilder>[] setUp )
        {
            return setUp.Select(NewPublishedFundingOrganisationGrouping);
        }

        private SpecificationSummary NewSpecificationSummary(Action<SpecificationSummaryBuilder> setUp = null)
        {
            SpecificationSummaryBuilder specificationSummaryBuilder = new SpecificationSummaryBuilder();

            setUp?.Invoke(specificationSummaryBuilder);

            return specificationSummaryBuilder.Build();
        }
    }
}
