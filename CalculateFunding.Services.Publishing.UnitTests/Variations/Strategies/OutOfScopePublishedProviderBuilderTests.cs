using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Common.ApiClient.Providers.Models.Search;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Variations;
using CalculateFunding.Services.Publishing.Variations.Strategies;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Strategies
{
    [TestClass]
    public class OutOfScopePublishedProviderBuilderTests : ProviderVariationContextTestBase
    {
        private Mock<IProvidersApiClient> _providers;
        private Mock<IMapper> _mapper;

        private OutOfScopePublishedProviderBuilder _builder;

        [TestInitialize]
        public void SetUp()
        {
            _providers = new Mock<IProvidersApiClient>();
            _mapper = new Mock<IMapper>();
            
            _builder = new OutOfScopePublishedProviderBuilder(_providers.Object,
                new ResiliencePolicies
                {
                    ProvidersApiClient = Policy.NoOpAsync()
                }, 
                _mapper.Object);
            
            VariationContext.AllPublishedProviderSnapShots = new Dictionary<string, PublishedProviderSnapShots>();
            VariationContext.AllPublishedProvidersRefreshStates = new Dictionary<string, PublishedProvider>();
        }

        [TestMethod]
        public async Task ReturnsNullIfNoCoreProviderDataForTheSuccessorId()
        {
            PublishedProvider missingPublishedProvider = await WhenTheMissingPublishedProviderIsBuilt(NewRandomString());

            missingPublishedProvider
                .Should()
                .BeNull();
        }

        [TestMethod]
        public async Task CreatesNewPublishedProviderWithZeroedDeepCopyOfPredecessorProviderFundingLinesAndCoreProviderData()
        {
            string successorId = NewRandomString();
            
            GivenTheFundingLines(NewFundingLine(_ => _.WithFundingLineCode("fl1")
                .WithOrganisationGroupingReason(OrganisationGroupingReason.Payment)
                .WithDistributionPeriods(NewDistributionPeriod(dp => 
                    dp.WithProfilePeriods(NewProfilePeriod(pp => pp.WithAmount(100)),
                        NewProfilePeriod(pp => pp.WithAmount(101)),
                            NewProfilePeriod(pp => pp.WithAmount(101)))))),
                NewFundingLine(_ => _.WithFundingLineCode("fl2")
                    .WithOrganisationGroupingReason(OrganisationGroupingReason.Payment)
                    .WithDistributionPeriods(NewDistributionPeriod(dp => 
                        dp.WithProfilePeriods(NewProfilePeriod(pp => pp.WithAmount(100)),
                            NewProfilePeriod(pp => pp.WithAmount(101)),
                            NewProfilePeriod(pp => pp.WithAmount(101)))))));

            GivenTheCalcaulations(NewFundingCalculation(_ => _.WithValue("1234")));
            
            ProviderVersionSearchResult coreProviderData = NewProviderVersionSearchResult();
            Provider missingProvider = NewProvider();
            
            AndTheCoreProviderData(coreProviderData, successorId);
            AndTheProviderMapping(coreProviderData, missingProvider);

            PublishedProvider missingPublishedProvider = await WhenTheMissingPublishedProviderIsBuilt(successorId);

            VariationContext.NewProvidersToAdd
                .Should()
                .BeEquivalentTo(missingPublishedProvider);

            VariationContext.AllPublishedProvidersRefreshStates[successorId]
                .Should()
                .BeSameAs(missingPublishedProvider);
            VariationContext.AllPublishedProviderSnapShots[successorId]
                .Should()
                .NotBeNull();

            missingPublishedProvider
                .Should()
                .NotBeNull();

            PublishedProviderVersion missingPublishedProviderVersion = missingPublishedProvider.Current;
            
            missingPublishedProviderVersion?.Provider
                .Should()
                .BeSameAs(missingProvider);

            foreach(FundingCalculation fundingCalculation in VariationContext.PublishedProvider.Current.Calculations)
            {
                FundingCalculation fundingCalculationCopy = missingPublishedProviderVersion.Calculations
                    .SingleOrDefault(_ => _.TemplateCalculationId == fundingCalculation.TemplateCalculationId);

                fundingCalculationCopy
                    .Should()
                    .NotBeNull();

                fundingCalculationCopy.Value
                    .Should()
                    .Be(fundingCalculation.Value);
            }

            foreach (FundingLine fundingLine in VariationContext.PublishedProvider.Current.FundingLines)
            {
                FundingLine fundingLineCopy = missingPublishedProviderVersion.FundingLines
                    .SingleOrDefault(_ => _.FundingLineCode == fundingLine.FundingLineCode);

                fundingLineCopy
                    .Should()
                    .NotBeNull();

                foreach (DistributionPeriod distributionPeriod in fundingLine.DistributionPeriods)
                {
                    DistributionPeriod distributionPeriodCopy = fundingLineCopy.DistributionPeriods
                        .SingleOrDefault(_ => _.DistributionPeriodId == distributionPeriod.DistributionPeriodId);

                    distributionPeriodCopy
                        .Should()
                        .NotBeNull();
                    
                    distributionPeriod
                        .Should()
                        .BeEquivalentTo(distributionPeriodCopy,
                            opt => opt.Excluding(_ => _.ProfilePeriods));

                    for (int profile = 0; profile < distributionPeriod.ProfilePeriods.Count(); profile++)
                    {
                        ProfilePeriod profilePeriod = distributionPeriod.ProfilePeriods.ElementAt(profile);
                        ProfilePeriod profilePeriodCopy = distributionPeriodCopy.ProfilePeriods.ElementAt(profile);
                        
                        profilePeriod
                            .Should()
                            .BeEquivalentTo(profilePeriodCopy, 
                                opt => opt.Excluding(_ => _.ProfiledValue));

                        profilePeriodCopy.ProfiledValue
                            .Should()
                            .Be(0);
                    }
                }
            }
        }

        private void AndTheCoreProviderData(ProviderVersionSearchResult providerVersionSearchResult, string providerId)
        {
            _providers.Setup(_ => _.GetProviderByIdFromMaster(providerId))
                .ReturnsAsync(new ApiResponse<ProviderVersionSearchResult>(HttpStatusCode.OK, providerVersionSearchResult));
        }

        private async Task<PublishedProvider> WhenTheMissingPublishedProviderIsBuilt(string successorId)
        {
            return await _builder.CreateMissingPublishedProviderForPredecessor(VariationContext.PublishedProvider, successorId, VariationContext);
        }

        private void AndTheProviderMapping(ProviderVersionSearchResult coreProviderData, Provider provider)
        {
            _mapper.Setup(_ => _.Map<Provider>(coreProviderData))
                .Returns(provider);
        }

        private ProviderVersionSearchResult NewProviderVersionSearchResult() => new ProviderVersionSearchResultBuilder()
            .Build();
    }
}