using CalculateFunding.Api.Publishing.IntegrationTests.Data;
using CalculateFunding.Common.ApiClient;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Publishing;
using CalculateFunding.Common.ApiClient.Publishing.Models;
using CalculateFunding.Common.Config.ApiClient;
using CalculateFunding.IntegrationTests.Common;
using CalculateFunding.Services.Core.Extensions;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Polly;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Api.Publishing.IntegrationTests.PublishedProvider
{
    [TestClass]
    [TestCategory(nameof(IntegrationTest))]
    public class GenerateCsvForBatchPublishedProvidersTests : IntegrationTest
    {
        private IPublishingApiClient _publishing;
        private PublishedProviderDataContext _publishedProviderDataContext;

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

            TrackForTeardown(_publishedProviderDataContext);

            _publishing = GetService<IPublishingApiClient>();

            _specificationId = NewRandomString();
        }

        [TestMethod]
        public async Task PublishedProviderDataIncludedOnGenerateCsvForBatchPublishedProvidersForApprovalResult()
        {
            string _providerIdOne = NewRandomString();

            string _fundingStreamId = NewRandomString();
            string _fundingPeriodId = NewRandomString();
            string _providerId = NewRandomString();
            string _ukPrn = NewRandomString();
            string _urn = NewRandomString();
            string _upin = NewRandomString();
            string _name = NewRandomString();
            int _totalFunding = NewRandomInteger();
            int _majorVersion = NewRandomInteger();
            int _minorVersion = NewRandomInteger();
            string[] _variationReasons = new[] { NewRandomString() };
            bool _isIndicative = NewRandomFlag();

            PublishedProviderTemplateParameters publishedProviderTemplateParameters = NewPublishedProviderTemplateParameters(_ => _
                .WithSpecificationId(_specificationId)
                .WithFundingStream(_fundingStreamId)
                .WithFundingPeriodId(_fundingPeriodId)
                .WithStatus(PublishedProviderStatus.Updated.ToString())
                .WithProviderId(_providerId)
                .WithUKPRN(_ukPrn)
                .WithURN(_urn)
                .WithUPIN(_upin)
                .WithName(_name)
                .WithTotalFunding(_totalFunding)
                .WithMajorVersion(_majorVersion)
                .WithMinorVersion(_minorVersion)
                .WithIsIndicative(_isIndicative)
                .WithPublishedProviderId(_providerIdOne));

            await GivenThePublishedProvider(new[] { publishedProviderTemplateParameters });

            PublishedProviderIdsRequest publishedProviderIdsRequest =
                NewPublishedProviderIdsRequest(_ => _.WithProviders(new[] { _providerIdOne }));

            ApiResponse<PublishedProviderDataDownload> response = await WhenGenerateCsvForBatchPublishedProvidersForApprovalQueried(
                publishedProviderIdsRequest, 
                _specificationId);

            response.StatusCode
                .IsSuccess()
                .Should()
                .BeTrue($"ProviderBatchForReleaseCount request failed with status code {response.StatusCode}");

            PublishedProviderDataDownload publishedProviderDataDownload = response?.Content;

            publishedProviderDataDownload
                ?.Url
                .Should()
                .NotBeNull();
        }

        [TestMethod]
        public async Task PublishedProviderDataIncludedOnGenerateCsvForBatchPublishedProvidersForReleaseResult()
        {
            string _providerIdOne = NewRandomString();

            string _fundingStreamId = NewRandomString();
            string _fundingPeriodId = NewRandomString();
            string _providerId = NewRandomString();
            string _ukPrn = NewRandomString();
            string _urn = NewRandomString();
            string _upin = NewRandomString();
            string _name = NewRandomString();
            int _totalFunding = NewRandomInteger();
            int _majorVersion = NewRandomInteger();
            int _minorVersion = NewRandomInteger();
            string[] _variationReasons = new[] { NewRandomString() };
            bool _isIndicative = NewRandomFlag();

            PublishedProviderTemplateParameters publishedProviderTemplateParameters = NewPublishedProviderTemplateParameters(_ => _
                .WithSpecificationId(_specificationId)
                .WithFundingStream(_fundingStreamId)
                .WithFundingPeriodId(_fundingPeriodId)
                .WithStatus(PublishedProviderStatus.Approved.ToString())
                .WithProviderId(_providerId)
                .WithUKPRN(_ukPrn)
                .WithURN(_urn)
                .WithUPIN(_upin)
                .WithName(_name)
                .WithTotalFunding(_totalFunding)
                .WithMajorVersion(_majorVersion)
                .WithMinorVersion(_minorVersion)
                .WithIsIndicative(_isIndicative)
                .WithPublishedProviderId(_providerIdOne));

            await GivenThePublishedProvider(new[] { publishedProviderTemplateParameters });

            PublishedProviderIdsRequest publishedProviderIdsRequest =
                NewPublishedProviderIdsRequest(_ => _.WithProviders(new[] { _providerIdOne }));

            ApiResponse<PublishedProviderDataDownload> response = await WhenGenerateCsvForBatchPublishedProvidersForReleaseQueried(
                publishedProviderIdsRequest,
                _specificationId);

            response.StatusCode
                .IsSuccess()
                .Should()
                .BeTrue($"ProviderBatchForReleaseCount request failed with status code {response.StatusCode}");

            PublishedProviderDataDownload publishedProviderDataDownload = response?.Content;

            publishedProviderDataDownload
                ?.Url
                .Should()
                .NotBeNull();
        }

        private async Task<ApiResponse<PublishedProviderDataDownload>> WhenGenerateCsvForBatchPublishedProvidersForApprovalQueried(
            PublishedProviderIdsRequest publishedProviderIdsRequest,
            string specificationId)
            => await _publishing.GenerateCsvForBatchPublishedProvidersForApproval(publishedProviderIdsRequest, specificationId);

        private async Task<ApiResponse<PublishedProviderDataDownload>> WhenGenerateCsvForBatchPublishedProvidersForReleaseQueried(
            PublishedProviderIdsRequest publishedProviderIdsRequest,
            string specificationId)
            => await _publishing.GenerateCsvForBatchPublishedProvidersForRelease(publishedProviderIdsRequest, specificationId);

        protected static void AddPublishingApiClient(IServiceCollection serviceCollection,
            IConfiguration configuration) =>
            serviceCollection.AddSingleton<IPublishingApiClient, PublishingApiClient>();

        private async Task GivenThePublishedProvider(PublishedProviderTemplateParameters[] publishedProviderTemplateParameters)
            => await _publishedProviderDataContext.CreateContextData(publishedProviderTemplateParameters);

        private PublishedProviderIdsRequest NewPublishedProviderIdsRequest(Action<PublishedProviderIdsRequestBuilder> setUp = null)
        {
            PublishedProviderIdsRequestBuilder publishedProviderIdsRequestBuilder = new PublishedProviderIdsRequestBuilder();

            setUp?.Invoke(publishedProviderIdsRequestBuilder);

            return publishedProviderIdsRequestBuilder.Build();
        }

        private PublishedProviderTemplateParameters NewPublishedProviderTemplateParameters(Action<PublishedProviderTemplateParametersBuilder> setUp = null)
        {
            PublishedProviderTemplateParametersBuilder publishedProviderTemplateParametersBuilder = new PublishedProviderTemplateParametersBuilder();

            setUp?.Invoke(publishedProviderTemplateParametersBuilder);

            return publishedProviderTemplateParametersBuilder.Build();
        }

    }
}
