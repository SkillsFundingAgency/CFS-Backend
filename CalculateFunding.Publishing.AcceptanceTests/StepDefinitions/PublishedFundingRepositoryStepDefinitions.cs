using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Publishing.AcceptanceTests.Contexts;
using CalculateFunding.Publishing.AcceptanceTests.Models;
using FluentAssertions;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace CalculateFunding.Publishing.AcceptanceTests.StepDefinitions
{
    [Binding]
    public class PublishedFundingRepositoryStepDefinitions
    {
        private readonly IPublishedFundingRepositoryStepContext _publishedFundingRepositoryStepContext;
        private readonly ICurrentSpecificationStepContext _currentSpecificationStepContext;

        public PublishedFundingRepositoryStepDefinitions(
            IPublishedFundingRepositoryStepContext publishedFundingRepositoryStepContext,
            ICurrentSpecificationStepContext currentSpecificationStepContext)
        {
            _publishedFundingRepositoryStepContext = publishedFundingRepositoryStepContext;
            _currentSpecificationStepContext = currentSpecificationStepContext;
        }

        [Given(@"the following Published Provider has been previously generated for the current specification")]
        public void GivenTheFollowingPublishedProviderHasBeenPreviouslyGeneratedForTheCurrentSpecification(Table table)
        {
            PublishedProviderVersion publishedProviderVersion = table.CreateInstance<PublishedProviderVersion>();
            publishedProviderVersion.SpecificationId = _currentSpecificationStepContext.SpecificationId;

            PublishedProvider publishedProvider = new PublishedProvider()
            {
                Current = publishedProviderVersion,
            };

            _publishedFundingRepositoryStepContext.CurrentPublishedProvider = publishedProvider;
        }

        [Given(@"the Published Provider has the following funding lines")]
        public void GivenThePublishedProviderHasTheFollowingFundingLines(Table table)
        {
            _publishedFundingRepositoryStepContext.CurrentPublishedProvider
                .Should()
                .NotBeNull();

            IEnumerable<FundingLine> fundingLines = table.CreateSet<FundingLine>();

            _publishedFundingRepositoryStepContext.CurrentPublishedProvider.Current.FundingLines = fundingLines;
        }

        [Given(@"the Published Provider has the following distribution period for funding line '(.*)'")]
        public void GivenThePublishedProviderHasTheFollowingDistributionPeriodForFundingLine(string fundingLineCode, Table table)
        {
            _publishedFundingRepositoryStepContext.CurrentPublishedProvider
                .Should()
                .NotBeNull();

            FundingLine fundingLine = _publishedFundingRepositoryStepContext.CurrentPublishedProvider.Current.FundingLines.SingleOrDefault(f => f.FundingLineCode == fundingLineCode && f.Type == OrganisationGroupingReason.Payment);
            fundingLine
                .Should()
                .NotBeNull($"funding line should exist with code '{fundingLineCode}'");

            List<DistributionPeriod> distributionPeriods = new List<DistributionPeriod>();
            if (fundingLine.DistributionPeriods.AnyWithNullCheck())
            {
                distributionPeriods.AddRange(fundingLine.DistributionPeriods);
            }

            distributionPeriods.AddRange(table.CreateSet<DistributionPeriod>());

            fundingLine.DistributionPeriods = distributionPeriods;

        }

        [Given(@"the Published Providers distribution period has the following profiles for funding line '(.*)'")]
        public void GivenThePublishedProvidersDistributionPeriodHasTheFollowingProfilesForFundingLine(string fundingLineCode, Table table)
        {
            _publishedFundingRepositoryStepContext.CurrentPublishedProvider
                .Should()
                .NotBeNull();

            FundingLine fundingLine = _publishedFundingRepositoryStepContext.CurrentPublishedProvider.Current.FundingLines.SingleOrDefault(f => f.FundingLineCode == fundingLineCode && f.Type == OrganisationGroupingReason.Payment);
            fundingLine
                .Should()
                .NotBeNull($"funding line should exist with code '{fundingLineCode}'");

            Dictionary<string, DistributionPeriod> distributionPeriods = new Dictionary<string, DistributionPeriod>();
            if (fundingLine.DistributionPeriods.AnyWithNullCheck())
            {
                foreach (DistributionPeriod distributionPeriod in fundingLine.DistributionPeriods)
                {
                    distributionPeriods.Add(distributionPeriod.DistributionPeriodId, distributionPeriod);
                }
            }

            IEnumerable<ProfilePeriod> profilePeriods = table.CreateSet<ProfilePeriod>();

            foreach (ProfilePeriod profilePeriod in profilePeriods)
            {
                distributionPeriods
                    .Should()
                    .ContainKey(profilePeriod.DistributionPeriodId, "expected distribution period to exist to add profile to");

                DistributionPeriod distributionPeriod = distributionPeriods[profilePeriod.DistributionPeriodId];

                List<ProfilePeriod> profilePeriodsForDistributionPeriod = new List<ProfilePeriod>();
                if (distributionPeriod.ProfilePeriods.AnyWithNullCheck())
                {
                    profilePeriodsForDistributionPeriod.AddRange(distributionPeriod.ProfilePeriods);
                }

                profilePeriodsForDistributionPeriod.Add(profilePeriod);

                distributionPeriod.ProfilePeriods = profilePeriodsForDistributionPeriod;
            }
        }

        [Given(@"the Published Provider contains the following calculation results")]
        public void GivenThePublishedProviderContainsTheFollowingCalculationResults(Table table)
        {
            _publishedFundingRepositoryStepContext.CurrentPublishedProvider
                .Should()
                .NotBeNull();

            List<FundingCalculationTestModel> calculations = new List<FundingCalculationTestModel>(table.CreateSet<FundingCalculationTestModel>());

            Console.WriteLine($"Adding a total of {calculations.Count()} calculation for provider {_publishedFundingRepositoryStepContext.CurrentPublishedProvider.Current.ProviderId}");

            _publishedFundingRepositoryStepContext
                .CurrentPublishedProvider
                .Current
                .Calculations = calculations
                .Select(c => new FundingCalculation() { TemplateCalculationId = c.TemplateCalculationId, Value = c.Value });
        }

        [Given(@"the Published Provider has the following provider information")]
        public void GivenThePublishedProviderHasTheFollowingProviderInformation(Table table)
        {
            _publishedFundingRepositoryStepContext.CurrentPublishedProvider
                            .Should()
                            .NotBeNull();

            Provider provider = table.CreateInstance<Provider>();

            _publishedFundingRepositoryStepContext.CurrentPublishedProvider.Current.Provider = provider;
        }

        [Given(@"the Published Provider is available in the repository for this specification")]
        public void GivenThePublishedProviderIsAvailableInTheRepositoryForThisSpecification()
        {
            Guard.IsNullOrWhiteSpace(_currentSpecificationStepContext.SpecificationId, nameof(_currentSpecificationStepContext.SpecificationId));
            Guard.ArgumentNotNull(_publishedFundingRepositoryStepContext.CurrentPublishedProvider, nameof(_publishedFundingRepositoryStepContext.CurrentPublishedProvider));

            _publishedFundingRepositoryStepContext.Repo.AddPublishedProvider(_currentSpecificationStepContext.SpecificationId, _publishedFundingRepositoryStepContext.CurrentPublishedProvider);

            _publishedFundingRepositoryStepContext.CurrentPublishedProvider = null;
        }

    }
}
