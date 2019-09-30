using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Publishing.AcceptanceTests.Contexts;
using FluentAssertions;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace CalculateFunding.Publishing.AcceptanceTests.StepDefinitions
{
    [Binding]
    public class PoliciesStepDefinitions
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
        }

        [Given(@"the funding configuration has the following organisation group")]
        public void GivenTheFundingConfigurationHasTheFollowingOrganisationGroup(Table table)
        {
            _policiesStepContext.CreateFundingConfiguration.Should().NotBeNull();

            OrganisationGroupingConfiguration configuration = table.CreateInstance<OrganisationGroupingConfiguration>();

            List<OrganisationGroupingConfiguration> newGroupings = new List<OrganisationGroupingConfiguration>();
            if (!_policiesStepContext.CreateFundingConfiguration.OrganisationGroupings.IsNullOrEmpty())
            {
                newGroupings.AddRange(_policiesStepContext.CreateFundingConfiguration.OrganisationGroupings);
            }

            newGroupings.Add(configuration);

            _policiesStepContext.CreateFundingConfiguration.OrganisationGroupings = newGroupings;
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
