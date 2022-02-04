using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Publishing.AcceptanceTests.Contexts;
using CalculateFunding.Publishing.AcceptanceTests.Extensions;
using FluentAssertions;
using Newtonsoft.Json;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace CalculateFunding.Publishing.AcceptanceTests.StepDefinitions
{
    [Binding]
    public class PoliciesStepDefinitions : StepDefinitionBase
    {
        private readonly IPoliciesStepContext _policiesStepContext;

        public PoliciesStepDefinitions(IPoliciesStepContext policiesStepContext)
        {
            _policiesStepContext = policiesStepContext;
        }

        [Given(@"a funding configuration exists for funding stream '(.*)' in funding period '(.*)'")]
        public void GivenAFundingConfigurationExistsForFundingStreamInFundingPeriod(string fundingStreamId, string fundingPeriodId, Table table)
        {
            fundingStreamId
                .Should()
                .NotBeNullOrWhiteSpace();

            fundingPeriodId
                .Should()
                .NotBeNullOrWhiteSpace();

            _policiesStepContext.CreateFundingStreamId = fundingStreamId;
            _policiesStepContext.CreateFundingPeriodId = fundingPeriodId;

            FundingConfiguration fundingConfiguration = table.CreateInstance<FundingConfiguration>();
            fundingConfiguration.FundingPeriodId = fundingPeriodId;
            fundingConfiguration.FundingStreamId = fundingStreamId;
            _policiesStepContext.CreateFundingConfiguration = fundingConfiguration;

            _policiesStepContext.Repo.SetFundingConfiguration(fundingPeriodId, fundingStreamId, fundingConfiguration);
        }

        [Given(@"a funding configuration exists for funding stream '(.*)' in funding period '(.*)' in resources")]
        public void GivenAFundingConfigurationExistsForFundingStreamInFundingPeriodInResource(string fundingStreamId, string fundingPeriodId)
        {
            fundingStreamId
                .Should()
                .NotBeNullOrWhiteSpace();

            fundingPeriodId
                .Should()
                .NotBeNullOrWhiteSpace();

            _policiesStepContext.CreateFundingStreamId = fundingStreamId;
            _policiesStepContext.CreateFundingPeriodId = fundingPeriodId;

            string contents = GetTestDataContents($"ReleaseManagementData.FundingConfigurations.{fundingStreamId}-{fundingPeriodId}.json");

            FundingConfiguration fundingConfiguration = JsonConvert.DeserializeObject<FundingConfiguration>(contents);

            fundingConfiguration.FundingPeriodId = fundingPeriodId;
            fundingConfiguration.FundingStreamId = fundingStreamId;
            _policiesStepContext.CreateFundingConfiguration = fundingConfiguration;

            _policiesStepContext.Repo.SetFundingConfiguration(fundingStreamId, fundingPeriodId, fundingConfiguration);
        }

        [Given(@"the funding configuration has the following organisation group and provider status list '(.*)'")]
        public void GivenTheFundingConfigurationHasTheFollowingOrganisationGroupAndProviderStatusList(string statusList, Table table)
        {
            _policiesStepContext.CreateFundingConfiguration.Should().NotBeNull();

            OrganisationGroupingConfiguration configuration = table.CreateInstance<OrganisationGroupingConfiguration>();

            List<OrganisationGroupingConfiguration> newGroupings = new List<OrganisationGroupingConfiguration>();
            if (!_policiesStepContext.CreateFundingConfiguration.OrganisationGroupings.IsNullOrEmpty())
            {
                newGroupings.AddRange(_policiesStepContext.CreateFundingConfiguration.OrganisationGroupings);
            }

            if (!string.IsNullOrWhiteSpace(statusList))
            {
                configuration.ProviderStatus = statusList.Split(';');
            }

            newGroupings.Add(configuration);

            _policiesStepContext.CreateFundingConfiguration.OrganisationGroupings = newGroupings;
        }


        [Given(@"the funding configuration has the following organisation group")]
        public void GivenTheFundingConfigurationHasTheFollowingOrganisationGroup(Table table)
        {
            GivenTheFundingConfigurationHasTheFollowingOrganisationGroupAndProviderStatusList(string.Empty, table);
        }

        [Given(@"the funding configuration has the following funding variations")]
        public void GivenTheFundingConfigurationHasTheFollowingFundingVariations(IEnumerable<FundingVariation> fundingVariations)
        {
            //TODO; this is all a bit frankenstein - remove the variable and access it via the in memory repo methods ideally
            FundingConfiguration fundingConfiguration = _policiesStepContext
                .CreateFundingConfiguration;

            fundingConfiguration
                .Should()
                .NotBeNull();

            fundingConfiguration
                    .Variations = fundingVariations ?? new FundingVariation[0];
        }


        [Given(@"the funding configuration has the following provider type matches")]
        public void GivenTheFundingConfigurationHasTheFollowingProviderTypeMatches(Table table)
        {
            _policiesStepContext.CreateFundingConfiguration.Should().NotBeNull();
            _policiesStepContext.CreateFundingConfiguration.OrganisationGroupings.Should().NotBeNull();
            _policiesStepContext.CreateFundingConfiguration.OrganisationGroupings.Should().HaveCountGreaterThan(0);

            OrganisationGroupingConfiguration organisationGrouping = _policiesStepContext.CreateFundingConfiguration.OrganisationGroupings.Last();

            organisationGrouping.ProviderTypeMatch = table.CreateSet<ProviderTypeMatch>();
        }

        [Given(@"the funding configuration has the following indicative opener provider status")]
        public void GivenTheFundingConfigurationHasTheFollowingIndicativeOpenerProviderStatus(Table table)
        {
            FundingConfiguration fundingConfiguration = _policiesStepContext
                .CreateFundingConfiguration;

            fundingConfiguration
                .Should()
                .NotBeNull();

            fundingConfiguration
                    .IndicativeOpenerProviderStatus = table.AsStrings() ?? Array.Empty<string>();
        }

        [Given(@"the funding configuration is available in the policies repository")]
        public async Task GivenTheFundingConfigurationIsAvailableInThePoliciesRepository()
        {
            _policiesStepContext.CreateFundingStreamId
                .Should()
                .NotBeNullOrWhiteSpace();

            _policiesStepContext.CreateFundingPeriodId
                .Should()
                .NotBeNullOrWhiteSpace();

            _policiesStepContext
                .CreateFundingConfiguration
                .Should()
                .NotBeNull();

            await _policiesStepContext.Repo.SaveFundingConfiguration(_policiesStepContext.CreateFundingStreamId, _policiesStepContext.CreateFundingPeriodId, _policiesStepContext.CreateFundingConfiguration);
        }

        [Given(@"the funding period exists in the policies service")]
        public void GivenTheFundingPeriodExistsInThePoliciesService(Table table)
        {
            FundingPeriod fundingPeriod = table.CreateInstance<FundingPeriod>();

            _policiesStepContext.Repo.SaveFundingPeriod(fundingPeriod);
        }
    }
}
