using CalculateFunding.Api.Publishing.IntegrationTests.Data;
using CalculateFunding.Api.Publishing.IntegrationTests.RefreshFunding;
using CalculateFunding.Common.ApiClient;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Common.ApiClient.Publishing;
using CalculateFunding.Common.ApiClient.Publishing.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Config.ApiClient;
using CalculateFunding.IntegrationTests.Common;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace CalculateFunding.Api.Publishing.IntegrationTests.ProviderProfileInformation
{
    [TestClass]
    [TestCategory(nameof(IntegrationTest))]
    public class ProviderProfileInformationTests : IntegrationTest
    {
        private static readonly Assembly ResourceAssembly = typeof(ProviderProfileInformationTests).Assembly;


        private IPublishingApiClient _publishing;

        private string _specificationId;
        private SpecificationDataContext _specificationDataContext;
        private ProfilePatternDataContext _profilePatternDataContext;
        private FundingTemplateDataContext _fundingTemplateDataContext;


        [ClassInitialize]
        public static void FixtureSetUp(TestContext testContext)
        {
            SetUpConfiguration();
            SetUpServices((sc,
                        c)
                    => sc.AddHttpClient(HttpClientKeys.Publishing,
                        c =>
                        {
                            ApiOptions opts = GetConfigurationOptions<ApiOptions>("publishingClient");

                            Common.Config.ApiClient.ApiClientConfigurationOptions.SetDefaultApiClientConfigurationOptions(c, opts);
                        })
                    .ConfigurePrimaryHttpMessageHandler(() => new ApiClientHandler())
                    .AddTransientHttpErrorPolicy(c => c.WaitAndRetryAsync(new[] { TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5) }))
                    .AddTransientHttpErrorPolicy(c => c.CircuitBreakerAsync(100, default)),
                AddNullLogger,
                AddUserProvider,
                AddPublishingApiClient);
        }

        [TestInitialize]
        public void SetUp()
        {
            _specificationDataContext = new SpecificationDataContext(Configuration, ResourceAssembly);
            _profilePatternDataContext = new ProfilePatternDataContext(Configuration);
            _fundingTemplateDataContext = new FundingTemplateDataContext(Configuration);



            TrackForTeardown(
                _specificationDataContext,
                _profilePatternDataContext,
                _fundingTemplateDataContext);

            _publishing = GetService<IPublishingApiClient>();

            _specificationId = NewRandomString();
        }

        [TestMethod]
        public async Task AvailableVariationPointerFundingLineDataIncludedOnWhenAvailableFundingLineProfilePeriodsForVariationPointersQueriedResponse()
        {
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string templateId = NewRandomString();
            string fundingVersion = NewRandomString();
            string fundingLineCode = "DSG-002";

            int occurrenceOne = 1;
            string typeValueOne = "May";
            int yearOne = 2021;

            int occurrenceTwo = 1;
            string typeValueTwo = "June";
            int yearTwo = 2021;

            ProfileVariationPointer profileVariationPointer = NewProfileVariationPointer(_
                => _.WithFundingStreamId(fundingStreamId)
                .WithFundingLineId(fundingLineCode)
                .WithPeriodType(PeriodType.CalendarMonth.ToString())
                .WithYear(yearOne)
                .WithTypeValue(typeValueOne)
                .WithOccurence(1));

            SpecificationTemplateParameters specification = NewSpecification(_
            => _.WithId(_specificationId)
                .WithFundingPeriodId(fundingPeriodId)
                .WithFundingStreamId(fundingStreamId)
                .WithTemplateIds((fundingStreamId, templateId))
                .WithProfileVariationPointers(profileVariationPointer));
            await AndTheSpecification(specification);

            FundingTemplateParameters fundingTemplateParameters = NewFundingTemplatePatameters(_ =>
                                                _.WithId($"{fundingStreamId}-{fundingPeriodId}-RA-{fundingVersion}")
                                                 .WithFundingStreamId(fundingStreamId)
                                                 .WithFundingPeriodId(fundingPeriodId)
                                                 .WithFundingStreamName(fundingStreamId)
                                                 .WithTemplateVersion(templateId)
                                                 );
            await AndFundingTemplate(fundingTemplateParameters);

            ProfilePatternTemplateParameters fundingStreamPeriodProfilePatternParameter = NewFundingStreamPeriodProfilePattern(_ => 
                                                _.WithFundingLineId(fundingLineCode)
                                                .WithFundingStream(fundingStreamId)
                                                .WithFundingPeriodId(fundingPeriodId)
                                                .WithProfilePattern(
                                                    NewProfilePeriodPattern(ppp => ppp
                                                        .WithOccurrence(occurrenceOne)
                                                        .WithPeriod(typeValueOne)
                                                        .WithType(PeriodType.CalendarMonth)
                                                        .WithYear(yearOne)),
                                                    NewProfilePeriodPattern(ppp => ppp
                                                        .WithOccurrence(occurrenceTwo)
                                                        .WithPeriod(typeValueTwo)
                                                        .WithType(PeriodType.CalendarMonth)
                                                        .WithYear(yearTwo)))
                );
            await AndTheProfilePattern(fundingStreamPeriodProfilePatternParameter);



            ApiResponse<IEnumerable<AvailableVariationPointerFundingLine>> response
                = await WhenAvailableFundingLineProfilePeriodsForVariationPointersQueried(_specificationId);

            response.StatusCode
                .IsSuccess()
                .Should()
                .BeTrue($"{nameof(AvailableVariationPointerFundingLine)} request failed with status code {response.StatusCode}");

            IEnumerable<AvailableVariationPointerFundingLine> availableVariationPointerFundingLines = response?.Content;

            availableVariationPointerFundingLines.Count().Should().Be(1);

            AvailableVariationPointerFundingLine availableVariationPointerFundingLine = availableVariationPointerFundingLines.FirstOrDefault();

            availableVariationPointerFundingLine.SelectedPeriod.Year.Should().Be(yearOne);
            availableVariationPointerFundingLine.SelectedPeriod.Period.Should().Be(typeValueOne);
            availableVariationPointerFundingLine.SelectedPeriod.Occurrence.Should().Be(occurrenceOne);

        }

        private async Task AndTheSpecification(SpecificationTemplateParameters parameters)
            => await _specificationDataContext.CreateContextData(parameters);

        private async Task AndTheProfilePattern(ProfilePatternTemplateParameters profilePatternTemplateParameters)
            => await _profilePatternDataContext.CreateContextData(profilePatternTemplateParameters);

        private async Task AndFundingTemplate(FundingTemplateParameters fundingTemplatePatameters)
            => await _fundingTemplateDataContext.CreateContextData(fundingTemplatePatameters);
        

        private async Task<ApiResponse<IEnumerable<AvailableVariationPointerFundingLine>>> WhenAvailableFundingLineProfilePeriodsForVariationPointersQueried(
            string specificationId)
            => await _publishing.GetAvailableFundingLineProfilePeriodsForVariationPointers(specificationId);

        private SpecificationTemplateParameters NewSpecification(Action<SpecificationTemplateParametersBuilder> setup = null)
            => BuildNewModel<SpecificationTemplateParameters, SpecificationTemplateParametersBuilder>(setup);

        private ProfileVariationPointer NewProfileVariationPointer(Action<ProfileVariationPointerBuilder> setup = null)
            => BuildNewModel<ProfileVariationPointer, ProfileVariationPointerBuilder>(setup);

        private FundingTemplateParameters NewFundingTemplatePatameters(Action<FundingTemplateParametersBuilder> setup = null)
            => BuildNewModel<FundingTemplateParameters, FundingTemplateParametersBuilder>(setup);

        private ProfilePatternTemplateParameters NewFundingStreamPeriodProfilePattern(Action<ProfilePatternTemplateParametersBuilder> setup = null)
            => BuildNewModel<ProfilePatternTemplateParameters, ProfilePatternTemplateParametersBuilder>(setup);

        private ProfilePeriodPattern NewProfilePeriodPattern(Action<ProfilePeriodPatternBuilder> setup = null)
            => BuildNewModel<ProfilePeriodPattern, ProfilePeriodPatternBuilder>(setup);


        protected static void AddPublishingApiClient(IServiceCollection serviceCollection,
            IConfiguration configuration) =>
            serviceCollection.AddSingleton<IPublishingApiClient, PublishingApiClient>();

        private T BuildNewModel<T, TB>(Action<TB> setup) where TB : TestEntityBuilder, new()
        {
            dynamic builder = new TB();
            setup?.Invoke(builder);
            return builder.Build();
        }
    }
}
