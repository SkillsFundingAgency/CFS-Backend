using AutoMapper;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Generators.OrganisationGroup.Enums;
using CalculateFunding.Generators.OrganisationGroup.Interfaces;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;
using CalculateFunding.Tests.Common.Helpers;
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
        private IDictionary<string, Provider> _scopedProviders;
        private IEnumerable<OrganisationGroupResult> _organisationGroupResults;
        private IEnumerable<PublishedFundingOrganisationGrouping> _publishedFundingOrganisationGrouping;

        private bool _includeHistory;
        private string _fundingStreamId;
        private string _fundingPeriodId;
        private string _providerVersionId;
        private string _providerId;
        private string _fundingConfigurationId;
        private SpecificationSummary _specificationSummary;
        private IEnumerable<PublishedFundingVersion> _publishedFundingVersions;
        private OrganisationGroupTypeCode _organisationGroupTypeCode;

        [TestInitialize]
        public void SetUp()
        {
            _fundingStreamId = NewRandomString();
            _fundingPeriodId = NewRandomString();
            _providerVersionId = NewRandomString();
            _includeHistory = NewRandomBoolean();
            _providerId = NewRandomString();
            _fundingConfigurationId = NewRandomString();
            _organisationGroupTypeCode = NewRandomOrganisationGroupTypeCode();

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
            GivenSpecificationSummary();
            AndPublishedFundingVersions();

            AndFundingConfiguration();
            AndFundingConfigurationRetrieved();

            AndScopedPublishedProviders();
            AndScopedProvidersForSpecificationRetrieved();

            AndMapperConfiguration();
            AndOrganisationGroupResults();
            AndGenerateOrganisationGroup();

            AndPublishedFundingOrganisationGrouping();
            AndGenerateOrganisationGroupings();

            IEnumerable<PublishedFundingOrganisationGrouping> organisationGroupings = 
                await WhenPublishedFundingOrganisationGroupingGenerated();

            ThenOrganisationGroupingsGenerated(organisationGroupings);
        }

        private void ThenOrganisationGroupingsGenerated(IEnumerable<PublishedFundingOrganisationGrouping> organisationGroupings)
        {
            Assert.AreEqual(organisationGroupings.Count(), _publishedFundingOrganisationGrouping.Count());
        }

        private void GivenSpecificationSummary()
        {
            _specificationSummary = NewSpecificationSummary(_=>_
                .WithFundingPeriodId(_fundingPeriodId)
                .WithProviderVersionId(_providerVersionId));
        }

        private void AndFundingConfiguration()
        {
            _fundingConfiguration = NewFundingConfiguration(_=>_.WithId(_fundingConfigurationId));
        }

        private void AndMapperConfiguration()
        {
            _mapper
                .Map<IEnumerable<ApiProvider>>(
                Arg.Is<ICollection<Provider>>(_ => _.Count() == 1 && _.FirstOrDefault().ProviderId == _providerId))
                .Returns(new List<ApiProvider> 
                { 
                    NewApiProvider(_ => _.WithProviderId(_providerId)) 
                });
        }

        private void AndFundingConfigurationRetrieved()
        {
            _policiesService
                .GetFundingConfiguration(_fundingStreamId, _fundingPeriodId)
                .Returns(_fundingConfiguration);
        }

        private void AndScopedPublishedProviders()
        {
            _scopedProviders = new Dictionary<string, Provider>
            {
                { "key2", NewProvider(_ => _.WithProviderId(_providerId)) }
            };
        }

        private void AndScopedProvidersForSpecificationRetrieved()
        {
            _providerService
                .GetScopedProvidersForSpecification(_specificationSummary.Id, _specificationSummary.ProviderVersionId)
                .Returns(Task.FromResult(_scopedProviders));
        }

        private void AndOrganisationGroupResults()
        {
            _organisationGroupResults = NewOrganisationGroupResults(_ =>
                _.WithGroupTypeCode(_organisationGroupTypeCode));
        }
        
        private void AndGenerateOrganisationGroup()
        {
            _organisationGroupGenerator
                .GenerateOrganisationGroup(
                Arg.Is<FundingConfiguration>(_ => _.Id == _fundingConfigurationId), 
                Arg.Is<IEnumerable<ApiProvider>>(_ => 
                    _.Count() == 1 && 
                    _.FirstOrDefault() != null && 
                    _.FirstOrDefault().ProviderId == _providerId ), 
                _providerVersionId)
                .Returns(_organisationGroupResults);
        }

        private void AndPublishedFundingOrganisationGrouping()
        {
            _publishedFundingOrganisationGrouping = NewPublishedFundingOrganisationGroupings(_ =>
            _.WithOrganisationGroupResult(
                NewOrganisationGroupResult( ogr =>
                    ogr.WithGroupTypeCode(OrganisationGroupTypeCode.LocalAuthority)
                )));
        }

        private void AndPublishedFundingVersions()
        {
            _publishedFundingVersions = NewPublishedFundingVersions(_ =>
                _.WithFundingStreamId(_fundingStreamId)
                .WithSpecificationId(_specificationSummary.Id)
                );
        }

        private void AndGenerateOrganisationGroupings()
        {
            _publishedFundingChangeDetectorService
                .GenerateOrganisationGroupings(
                Arg.Is<IEnumerable<OrganisationGroupResult>>(_ => 
                    _.Count() == _organisationGroupResults.Count() && 
                    _.FirstOrDefault() != null && 
                    _.FirstOrDefault().GroupTypeCode == _organisationGroupTypeCode),
                Arg.Is<IEnumerable<PublishedFundingVersion>>(_ =>
                    _.Count() == _publishedFundingVersions.Count() &&
                    _.FirstOrDefault() != null &&
                    _.FirstOrDefault().FundingStreamId == _fundingStreamId &&
                    _.FirstOrDefault().SpecificationId == _specificationSummary.Id),
                _includeHistory)
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

        private Provider NewProvider(Action<ProviderBuilder> setUp = null)
        {
            ProviderBuilder providerBuilder = new ProviderBuilder();

            setUp?.Invoke(providerBuilder);

            return providerBuilder.Build();
        }

        private ApiProvider NewApiProvider(Action<ApiProviderBuilder> setUp = null)
        {
            ApiProviderBuilder apiProviderBuilder = new ApiProviderBuilder();

            setUp?.Invoke(apiProviderBuilder);

            return apiProviderBuilder.Build();
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

        private PublishedFundingVersion NewPublishedFundingVersion(Action<PublishedFundingVersionBuilder> setUp = null)
        {
            PublishedFundingVersionBuilder publishedFundingVersionBuilder = new PublishedFundingVersionBuilder();

            setUp?.Invoke(publishedFundingVersionBuilder);

            return publishedFundingVersionBuilder.Build();
        }

        private IEnumerable<PublishedFundingVersion> NewPublishedFundingVersions(params Action<PublishedFundingVersionBuilder>[] setUp)
        {
            return setUp.Select(NewPublishedFundingVersion);
        }


        private bool NewRandomBoolean() => new RandomBoolean();

        private string NewRandomString() => new RandomString();

        private OrganisationGroupTypeCode NewRandomOrganisationGroupTypeCode() => new RandomEnum<OrganisationGroupTypeCode>();
    }
}
