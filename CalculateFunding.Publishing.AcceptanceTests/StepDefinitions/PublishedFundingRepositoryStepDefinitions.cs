using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Publishing.AcceptanceTests.Contexts;
using CalculateFunding.Publishing.AcceptanceTests.Extensions;
using CalculateFunding.Publishing.AcceptanceTests.Models;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Newtonsoft.Json;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace CalculateFunding.Publishing.AcceptanceTests.StepDefinitions
{
    [Binding]
    public class PublishedFundingRepositoryStepDefinitions : StepDefinitionBase
    {
        private readonly IPublishFundingStepContext _publishFundingStepContext;
        private readonly IPublishedFundingRepositoryStepContext _publishedFundingRepositoryStepContext;
        private readonly ICurrentSpecificationStepContext _currentSpecificationStepContext;

        public PublishedFundingRepositoryStepDefinitions(IPublishFundingStepContext publishFundingStepContext,
            IPublishedFundingRepositoryStepContext publishedFundingRepositoryStepContext,
            ICurrentSpecificationStepContext currentSpecificationStepContext)
        {
            _publishFundingStepContext = publishFundingStepContext;
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

            if (publishedProviderVersion.Status == PublishedProviderStatus.Released)
            {
                publishedProvider.Released = publishedProviderVersion.DeepCopy();
            }

            _publishedFundingRepositoryStepContext.CurrentPublishedProvider = publishedProvider;
        }

        [Given(@"the provider with id '(.*)' is excluded")]
        public void GivenTheProviderWithIdForAndIsExcluded(string publishedProviderId)
        {
            PublishedProvider publishedProvider = GetPublishedProvider(publishedProviderId);

            publishedProvider.Current.FundingLines?.ForEach(_ => _.Value = null);
            publishedProvider.Current.Calculations?.ForEach(_ => _.Value = null);
        }

        [Given(@"the Published Provider '(.*)' has been been previously generated for the current specification")]
        public void GivenThePublishedProviderHasBeenBeenPreviouslyGeneratedForTheCurrentSpecification(string publishedProviderFilename)
        {
            string publishedProviderJson = ResourceHelper.GetResourceContent("Input.PublishedProviders", $"{publishedProviderFilename}.json");
            PublishedProvider publishedProvider = JsonConvert.DeserializeObject<PublishedProvider>(publishedProviderJson);

            _publishedFundingRepositoryStepContext.CurrentPublishedProvider = publishedProvider;

            _publishFundingStepContext.CalculationsInMemoryRepository.AddProviderResults(_publishedFundingRepositoryStepContext.CurrentPublishedProvider.Current.ProviderId,
                    publishedProvider.Current.Calculations.Select(_ =>
                        new CalculationResult
                        {
                            Id = _publishFundingStepContext.CalculationsInMemoryClient.Mapping.TemplateMappingItems.FirstOrDefault(t => t.TemplateId == _.TemplateCalculationId)?.CalculationId,
                            Value = _.Value
                        }).ToArray()
                );
        }

        [Given(@"the Published Provider has the following funding lines")]
        public void GivenThePublishedProviderHasTheFollowingFundingLines(IEnumerable<FundingLine> fundingLines)
        {
            var currentPublishedProvider = _publishedFundingRepositoryStepContext.CurrentPublishedProvider;

            currentPublishedProvider
                .Should()
                .NotBeNull();

            currentPublishedProvider.Current.FundingLines = fundingLines;

            if (currentPublishedProvider.Released != null)
            {
                currentPublishedProvider.Released.FundingLines = fundingLines.DeepCopy();
            }
        }

        [Given(@"the Published Provider '(.*)' has the following funding lines")]
        public void GivenThePublishedProviderHasTheFollowingFundingLines(string publishedProviderId,
            IEnumerable<FundingLine> fundingLines)
        {
            PublishedProvider publishedProvider = GetPublishedProvider(publishedProviderId);

            publishedProvider.Current.FundingLines = fundingLines.ToArray();
        }


        [Given(@"the Published Provider '(.*)' has the following distribution period for funding line '(.*)'")]
        public void GivenThePublishedProviderHasTheFollowingDistributionPeriodForFundingLine(string providerId,
            string fundingLineCode,
            Table table)
        {
            SetFundingLineOnPublishedProvider(fundingLineCode,
                table,
                GetPublishedProvider(providerId));
        }


        [Given(@"the Published Provider has the following distribution period for funding line '(.*)'")]
        public void GivenThePublishedProviderHasTheFollowingDistributionPeriodForFundingLine(string fundingLineCode, Table table)
        {
            SetFundingLineOnPublishedProvider(fundingLineCode,
                table,
                _publishedFundingRepositoryStepContext.CurrentPublishedProvider);
        }

        private void SetFundingLineOnPublishedProvider(string fundingLineCode, Table table, PublishedProvider publishedProvider)
        {
            publishedProvider
                .Should()
                .NotBeNull();


            FundingLine[] fundingLines = new[] {publishedProvider.Current.FundingLines.SingleOrDefault(f =>
                    f.FundingLineCode == fundingLineCode && f.Type == FundingLineType.Payment) };

            if (publishedProvider.Released != null)
            {
                fundingLines = fundingLines.Concat(new[] {publishedProvider.Released.FundingLines.SingleOrDefault(f =>
                    f.FundingLineCode == fundingLineCode && f.Type == FundingLineType.Payment) }).ToArray();
            }

            foreach (FundingLine fundingLine in fundingLines)
            {
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
        }

        [Given(@"the Published Provider '(.*)' distribution period has the following profiles for funding line '(.*)'")]
        public void GivenThePublishedProviderDistributionPeriodHasTheFollowingProfilesForFundingLine(string providerId, string fundingLineCode, Table table)
        {
            SetProfilePeriodsOnPublishedProvider(fundingLineCode,
                table,
                GetPublishedProvider(providerId));
        }

        private PublishedProvider GetPublishedProvider(string providerId)
        {
            return _publishedFundingRepositoryStepContext.Repo
                .GetInMemoryPublishedProviders(_currentSpecificationStepContext.SpecificationId)
                .FirstOrDefault(_ => _.Value.Current.ProviderId == providerId).Value;
        }


        [Given(@"the Published Providers distribution period has the following profiles for funding line '(.*)'")]
        public void GivenThePublishedProvidersDistributionPeriodHasTheFollowingProfilesForFundingLine(string fundingLineCode, Table table)
        {
            SetProfilePeriodsOnPublishedProvider(fundingLineCode,
                table,
                _publishedFundingRepositoryStepContext.CurrentPublishedProvider);
        }

        private void SetProfilePeriodsOnPublishedProvider(string fundingLineCode, Table table, PublishedProvider publishedProvider)
        {
            publishedProvider
                .Should()
                .NotBeNull();

            FundingLine[] fundingLines = new[] {publishedProvider.Current.FundingLines.SingleOrDefault(f =>
                    f.FundingLineCode == fundingLineCode && f.Type == FundingLineType.Payment) };

            if (publishedProvider.Released != null)
            {
                fundingLines = fundingLines.Concat(new[] {publishedProvider.Released.FundingLines.SingleOrDefault(f =>
                    f.FundingLineCode == fundingLineCode && f.Type == FundingLineType.Payment) }).ToArray();
            }

            foreach (FundingLine fundingLine in fundingLines)
            {
                fundingLine
                    .Should()
                    .NotBeNull($"funding line should exist with code '{fundingLineCode}'");

                Dictionary<string, DistributionPeriod> distributionPeriods = new Dictionary<string, DistributionPeriod>();
                if (fundingLine.DistributionPeriods.AnyWithNullCheck())
                {
                    foreach (DistributionPeriod distributionPeriod in fundingLine.DistributionPeriods)
                    {
                        if (distributionPeriods.ContainsKey(distributionPeriod.DistributionPeriodId))
                        {
                            distributionPeriods[distributionPeriod.DistributionPeriodId].Value = distributionPeriod.Value;
                        }
                        else
                        {
                            distributionPeriods.Add(distributionPeriod.DistributionPeriodId, distributionPeriod);
                        }
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

                _publishFundingStepContext.ProfilingInMemoryClient.AddFundingValueProfileSplit(
                    (fundingLine.Value,
                        publishedProvider.Current.FundingStreamId,
                        publishedProvider.Current.FundingPeriodId,
                        fundingLine.FundingLineCode,
                        profilePeriods.Select(_ =>
                            new Common.ApiClient.Profiling.Models.ProfilingPeriod
                            {
                                DistributionPeriod = _.DistributionPeriodId,
                                Occurrence = _.Occurrence,
                                Type = _.Type.ToString(),
                                Period = _.TypeValue,
                                Value = _.ProfiledValue,
                                Year = _.Year
                            }),
                        distributionPeriods.Values.Select(_ =>
                            new Common.ApiClient.Profiling.Models.DistributionPeriods
                            {
                                DistributionPeriodCode = _.DistributionPeriodId,
                                Value = _.Value
                            })));
            }
        }

        [Given(@"the Published Provider contains the following calculation results")]
        public void GivenThePublishedProviderContainsTheFollowingCalculationResults(Table table)
        {
            _publishedFundingRepositoryStepContext.CurrentPublishedProvider
                .Should()
                .NotBeNull();

            List<FundingCalculationTestModel> calculations = new List<FundingCalculationTestModel>(table.CreateSet<FundingCalculationTestModel>());

            //TODO: if possible refactor out the deep method chaining (train wrecks)

            _publishFundingStepContext.CalculationsInMemoryRepository.AddProviderResults(_publishedFundingRepositoryStepContext.CurrentPublishedProvider.Current.ProviderId,
                    calculations.Select(_ =>
                        new CalculationResult
                        {
                            Id = _publishFundingStepContext.CalculationsInMemoryClient.Mapping.TemplateMappingItems.FirstOrDefault(t => t.TemplateId == _.TemplateCalculationId)?.CalculationId,
                            Value = _.Value
                        }).ToArray()
                );

            _publishedFundingRepositoryStepContext
                .CurrentPublishedProvider
                .Current
                .Calculations = calculations
                .Select(c => new FundingCalculation
                {
                    TemplateCalculationId = c.TemplateCalculationId,
                    Value = c.Value
                });

            if (_publishedFundingRepositoryStepContext.CurrentPublishedProvider.Released != null)
            {
                _publishedFundingRepositoryStepContext
                .CurrentPublishedProvider
                .Released
                .Calculations = calculations
                .Select(c => new FundingCalculation
                {
                    TemplateCalculationId = c.TemplateCalculationId,
                    Value = c.Value
                });
            }
        }


        [Given(@"the Published Provider has the following provider information")]
        public void GivenThePublishedProviderHasTheFollowingProviderInformation(Table table)
        {
            var currentPublishedProvider = _publishedFundingRepositoryStepContext.CurrentPublishedProvider;

            currentPublishedProvider
                            .Should()
                            .NotBeNull();

            Provider provider = table.CreateInstance<Provider>();

            currentPublishedProvider.Current.Provider = provider;

            if (currentPublishedProvider.Released != null)
            {
                currentPublishedProvider.Released.Provider = provider.DeepCopy();
            }
        }

        [Given(@"the Published Provider is available in the repository for this specification")]
        public void GivenThePublishedProviderIsAvailableInTheRepositoryForThisSpecification()
        {
            Guard.IsNullOrWhiteSpace(_currentSpecificationStepContext.SpecificationId, nameof(_currentSpecificationStepContext.SpecificationId));
            Guard.ArgumentNotNull(_publishedFundingRepositoryStepContext.CurrentPublishedProvider, nameof(_publishedFundingRepositoryStepContext.CurrentPublishedProvider));

            _publishedFundingRepositoryStepContext.Repo.AddPublishedProvider(_currentSpecificationStepContext.SpecificationId, _publishedFundingRepositoryStepContext.CurrentPublishedProvider);

            _publishedFundingRepositoryStepContext.CurrentPublishedProvider = null;
        }

        [Given(@"published provider '([^']*)' exists for funding string '([^']*)' in period '([^']*)' in cosmos from json")]
        public void GivenPublishedProviderExistsForFundingStringInPeriodInCosmosFromJson(string providerId, string fundingStreamId, string fundingPeriodId)
        {
            string contents = GetTestDataContents($"ReleaseManagementData.PublishedProviders.publishedprovider-{providerId}-{fundingPeriodId}-{fundingStreamId}.json");
            if (string.IsNullOrWhiteSpace(contents))
            {
                throw new InvalidOperationException("Content for published provider is empty or null");
            }

            DocumentEntity<PublishedProvider> publishedProvider = JsonConvert.DeserializeObject<DocumentEntity<PublishedProvider>>(contents);

            _publishedFundingRepositoryStepContext.Repo.AddPublishedProvider(_currentSpecificationStepContext.SpecificationId, publishedProvider.Content);
        }


        [Given(@"published provider exists '([^']*)' in cosmos from json")]
        public void GivenPublishedProviderExistsInCosmosFromJson(string providerId)
        {

        }
    }
}
