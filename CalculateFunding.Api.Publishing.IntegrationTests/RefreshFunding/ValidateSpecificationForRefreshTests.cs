using CalculateFunding.Api.Publishing.IntegrationTests.Data;
using CalculateFunding.Common.ApiClient;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Common.ApiClient.Publishing;
using CalculateFunding.Common.ApiClient.Publishing.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Config.ApiClient;
using CalculateFunding.IntegrationTests.Common;
using CalculateFunding.Services.Core.Extensions;
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

namespace CalculateFunding.Api.Publishing.IntegrationTests.RefreshFunding
{
    [TestClass]
    [TestCategory(nameof(IntegrationTest))]
    public class ValidateSpecificationForRefreshTests : IntegrationTest
    {
        private static readonly Assembly ResourceAssembly = typeof(ValidateSpecificationForRefreshTests).Assembly;

        private IPublishingApiClient _publishing;
        private PublishedProviderDataContext _publishedProviderDataContext;
        private SpecificationDataContext _specificationDataContext;
        private FundingStreamPaymentDatesDataContext _fundingStreamPaymentDatesDataContext;
        private ProfilePatternDataContext _profilePatternDataContext;
        private TemplateMappingsContext _templateMappingsContext;
        private ProviderVersionBlobContext _providerVersionBlobContext;
        private ProviderSourceDatasetContext _providerSourceDatasetContext;

        private string _specificationId;

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
            _publishedProviderDataContext = new PublishedProviderDataContext(Configuration);
            _specificationDataContext = new SpecificationDataContext(Configuration, ResourceAssembly);
            _fundingStreamPaymentDatesDataContext = new FundingStreamPaymentDatesDataContext(Configuration, ResourceAssembly);
            _profilePatternDataContext = new ProfilePatternDataContext(Configuration);
            _templateMappingsContext = new TemplateMappingsContext(Configuration);
            _providerVersionBlobContext = new ProviderVersionBlobContext(Configuration, ResourceAssembly);
            _providerSourceDatasetContext = new ProviderSourceDatasetContext(Configuration);

            TrackForTeardown(
                _publishedProviderDataContext,
                _specificationDataContext,
                _fundingStreamPaymentDatesDataContext,
                _profilePatternDataContext,
                _templateMappingsContext,
                _providerVersionBlobContext,
                _providerSourceDatasetContext);

            _publishing = GetService<IPublishingApiClient>();

            _specificationId = NewRandomString();
        }

        [Ignore]
        [TestMethod]
        public async Task VariationPointerErrorMessageReturnedOnValidateSpecificationForRefresh()
        {
            string errorMessage =
                $"There are payment funding lines with variation instalments set earlier than the current profile instalment.{Environment.NewLine}#VariationInstallmentLink# must be set later or equal to the current profile instalment.";

            string providerVersionId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string ukprnOne = NewRandomString();
            string dataRelationshipId = NewRandomString();

            DateTime paymentDate = DateTime.Today.ToUniversalTime();
            DateTime variationPointerDate = paymentDate.AddMonths(-1);

            ProfileVariationPointer profileVariationPointer = NewProfileVariationPointer(_
                => _.WithFundingStreamId(fundingStreamId)
                .WithPeriodType(PeriodType.CalendarMonth.ToString())
                .WithYear(variationPointerDate.Year)
                .WithTypeValue(variationPointerDate.ToString("MMMM"))
                .WithOccurence(1));

            SpecificationTemplateParameters specification = NewSpecification(_
            => _.WithId(_specificationId)
                .WithProviderVersionId(providerVersionId)
                .WithFundingPeriodId(fundingPeriodId)
                .WithFundingStreamId(fundingStreamId)
                .WithPublishStatus(PublishStatus.Approved)
                .WithProfileVariationPointers(profileVariationPointer));
            await AndTheSpecification(specification);

            FundingStreamPaymentDate fundingStreamPaymentDate = NewPaymentDate(_
                => _.WithType(ProfilePeriodType.CalendarMonth)
                .WithYear(paymentDate.Year)
                .WithTypeValue(paymentDate.ToString("MMMM"))
                .WithOccurence(1));

            FundingStreamPaymentDatesParameters fundingStreamPaymentDatesParameters = NewFundingStreamPaymentDatesParameters(_
                => _.WithFundingPeriodId(fundingPeriodId)
                .WithFundingStreamId(fundingStreamId)
                .WithFundingStreamPaymentDates(fundingStreamPaymentDate));
            await AndTheFundingStreamPaymentDates(fundingStreamPaymentDatesParameters);

            ProfilePatternTemplateParameters profilePatternTemplateParameters = NewProfilePatternTemplateParameters(_ => _
                .WithFundingStream(fundingStreamId)
                .WithFundingPeriodId(fundingPeriodId)
                .WithProfilePattern(NewProfilePeriodPattern(_ => _.WithType(PeriodType.CalendarMonth)
                        .WithYear(variationPointerDate.Year)
                        .WithPeriod(variationPointerDate.ToString("MMMM"))
                        .WithOccurrence(1)
                        .Build())));
            await AndTheProfilePattern(profilePatternTemplateParameters);

            TemplateMappingsParameters templateMappings = NewTemplateMappingsParameters(_ =>
                                                 _.WithId($"templatemapping-{_specificationId}-{fundingStreamId}")
                                                .WithFundingStreamId(fundingStreamId)
                                                .WithSpecificationId(_specificationId));
            await AndTemplateMappings(templateMappings);

            ProviderDatasetRowParameters[] providerVersionRows =
            {
                NewProviderDatasetRowParameters(pr => pr.WithUkprn(ukprnOne))
            };

            await AndTheProviderVersionDocument(NewProviderVersionTemplateParameters(_ =>
                _.WithId(providerVersionId)
                    .WithProviders(providerVersionRows)));

            ProviderSourceDatasetParameters providerSourceDatasetParameters = NewProviderSourceDatasetParameters(_
                => _.WithProviderId(ukprnOne)
                .WithSpecificationId(_specificationId)
                .WithDataRelationshipId(dataRelationshipId));
            await AndProviderSourceDataset(providerSourceDatasetParameters);

            ValidatedApiResponse<IEnumerable<string>> response =
                await WhenValidateSpecificationForRefreshQueried(_specificationId);

            response.StatusCode
                .IsSuccess()
                .Should()
                .BeFalse($"ValidateSpecificationForRefresh request failed with status code {response.StatusCode}");

            IEnumerable<string> errors = response?.ModelState.SelectMany(_ => _.Value);

            errors
                .Should()
                .Contain(errorMessage);
        }

        private async Task<ValidatedApiResponse<IEnumerable<string>>> WhenValidateSpecificationForRefreshQueried(
            string specificationId)
            => await _publishing.ValidateSpecificationForRefresh(specificationId);

        protected static void AddPublishingApiClient(IServiceCollection serviceCollection,
            IConfiguration configuration) =>
            serviceCollection.AddSingleton<IPublishingApiClient, PublishingApiClient>();

        private SpecificationTemplateParameters NewSpecification(Action<SpecificationTemplateParametersBuilder> setUp = null)
        {
            SpecificationTemplateParametersBuilder specificationTemplateParametersBuilder = new SpecificationTemplateParametersBuilder();

            setUp?.Invoke(specificationTemplateParametersBuilder);

            return specificationTemplateParametersBuilder.Build();
        }

        private ProviderVersionTemplateParameters NewProviderVersionTemplateParameters(Action<ProviderVersionTemplateParametersBuilder> setUp = null)
        {
            ProviderVersionTemplateParametersBuilder providerVersionTemplateParametersBuilder = new ProviderVersionTemplateParametersBuilder();

            setUp?.Invoke(providerVersionTemplateParametersBuilder);

            return providerVersionTemplateParametersBuilder.Build();
        }

        private FundingStreamPaymentDatesParameters NewFundingStreamPaymentDatesParameters(Action<FundingStreamPaymentDatesParametersBuilder> setUp = null)
        {
            FundingStreamPaymentDatesParametersBuilder fundingStreamPaymentDatesParametersBuilder = new FundingStreamPaymentDatesParametersBuilder();

            setUp?.Invoke(fundingStreamPaymentDatesParametersBuilder);

            return fundingStreamPaymentDatesParametersBuilder.Build();
        }

        private ProfileVariationPointer NewProfileVariationPointer(Action<ProfileVariationPointerBuilder> setUp = null)
        {
            ProfileVariationPointerBuilder profileVariationPointerBuilder = new ProfileVariationPointerBuilder();

            setUp?.Invoke(profileVariationPointerBuilder);

            return profileVariationPointerBuilder.Build();
        }

        private ProviderDatasetRowParameters NewProviderDatasetRowParameters(Action<ProviderDatasetRowParametersBuilder> setUp = null)
        {
            ProviderDatasetRowParametersBuilder providerDatasetRowParametersBuilder = new ProviderDatasetRowParametersBuilder();

            setUp?.Invoke(providerDatasetRowParametersBuilder);

            return providerDatasetRowParametersBuilder.Build();
        }

        private FundingStreamPaymentDate NewPaymentDate(Action<FundingStreamPaymentDateBuilder> setUp = null)
        {
            FundingStreamPaymentDateBuilder paymentDateBuilder = new FundingStreamPaymentDateBuilder();

            setUp?.Invoke(paymentDateBuilder);

            return paymentDateBuilder.Build();
        }

        private ProfilePatternTemplateParameters NewProfilePatternTemplateParameters(Action<ProfilePatternTemplateParametersBuilder> setUp = null)
        {
            ProfilePatternTemplateParametersBuilder profilePatternTemplateParametersBuilder = new ProfilePatternTemplateParametersBuilder();

            setUp?.Invoke(profilePatternTemplateParametersBuilder);

            return profilePatternTemplateParametersBuilder.Build();
        }

        private ProfilePeriodPattern NewProfilePeriodPattern(Action<ProfilePeriodPatternBuilder> setUp = null)
        {
            ProfilePeriodPatternBuilder profilePeriodPatternBuilder = new ProfilePeriodPatternBuilder();

            setUp?.Invoke(profilePeriodPatternBuilder);

            return profilePeriodPatternBuilder.Build();
        }

        private TemplateMappingsParameters NewTemplateMappingsParameters(Action<TemplateMappingsParametersBuilder> setUp = null)
        {
            TemplateMappingsParametersBuilder builder = new TemplateMappingsParametersBuilder();

            setUp?.Invoke(builder);

            return builder.Build();
        }

        private ProviderSourceDatasetParameters NewProviderSourceDatasetParameters(Action<ProviderSourceDatasetParametersBuilder> setUp = null)
        {
            ProviderSourceDatasetParametersBuilder providerSourceDatasetParametersBuilder = new ProviderSourceDatasetParametersBuilder();

            setUp?.Invoke(providerSourceDatasetParametersBuilder);

            return providerSourceDatasetParametersBuilder.Build();
        }

        private async Task AndTheSpecification(SpecificationTemplateParameters parameters)
            => await _specificationDataContext.CreateContextData(parameters);

        private async Task AndTheFundingStreamPaymentDates(FundingStreamPaymentDatesParameters parameters)
            => await _fundingStreamPaymentDatesDataContext.CreateContextData(parameters);

        private async Task AndTheProfilePattern(ProfilePatternTemplateParameters profilePatternTemplateParameters)
            => await _profilePatternDataContext.CreateContextData(profilePatternTemplateParameters);

        private async Task AndTemplateMappings(TemplateMappingsParameters templateMappingsParameters)
            => await _templateMappingsContext.CreateContextData(templateMappingsParameters);

        private async Task AndTheProviderVersionDocument(ProviderVersionTemplateParameters parameters)
            => await _providerVersionBlobContext.CreateContextData(parameters);

        private async Task AndProviderSourceDataset(ProviderSourceDatasetParameters providerSourceDatasetParameters)
            => await _providerSourceDatasetContext.CreateContextData(providerSourceDatasetParameters);
    }
}
