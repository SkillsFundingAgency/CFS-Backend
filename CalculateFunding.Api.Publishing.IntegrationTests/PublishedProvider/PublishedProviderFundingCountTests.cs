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
    public class PublishedProviderFundingCountTests : IntegrationTest
    {
        private IPublishingApiClient _publishing;
        private PublishedProviderDataContext _publishedProviderDataContext;

        private string _specificationId;
        private string _paidProviderId;
        private string _indicativeProviderId;

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
            _paidProviderId = NewRandomString();
            _indicativeProviderId = NewRandomString();
        }

        [TestMethod]
        public async Task PaidAndIndicativeProvidersAndFundingTotalsIncludedOnCountResult()
        {
            PublishedProviderTemplateParameters paidPublishedProviderTemplateParameters = NewPublishedProviderTemplateParameters(_ => _
                .WithIsIndicative(false)
                .WithSpecificationId(_specificationId)
                .WithStatus(PublishedProviderStatus.Approved.ToString())
                .WithTotalFunding(100)
                .WithPublishedProviderId(_paidProviderId));
            PublishedProviderTemplateParameters indicativePublishedProviderTemplateParameters = NewPublishedProviderTemplateParameters(_ => _
                .WithIsIndicative(true)
                .WithSpecificationId(_specificationId)
                .WithStatus(PublishedProviderStatus.Approved.ToString())
                .WithTotalFunding(200)
                .WithPublishedProviderId(_indicativeProviderId));

            await GivenThePublishedProvider(new[] { paidPublishedProviderTemplateParameters, indicativePublishedProviderTemplateParameters });
            
            PublishedProviderIdsRequest publishedProviderIdsRequest =
                NewPublishedProviderIdsRequest(_ => _.WithProviders( new[] { _paidProviderId, _indicativeProviderId }));

            ApiResponse<PublishedProviderFundingCount> response = await WhenProviderBatchForReleaseCountQueried(publishedProviderIdsRequest, _specificationId);

            response.StatusCode
                .IsSuccess()
                .Should()
                .BeTrue($"ProviderBatchForReleaseCount request failed with status code {response.StatusCode}");

            PublishedProviderFundingCount publishedProviderFundingCount = response?.Content;

            publishedProviderFundingCount
                .Should()
                .NotBeNull();

            publishedProviderFundingCount
                .Count
                .Should()
                .Be(2);

            publishedProviderFundingCount
                .IndicativeProviderCount
                .Should()
                .Be(1);

            publishedProviderFundingCount
                .PaidProviderCount
                .Should()
                .Be(1);

            publishedProviderFundingCount
                .TotalFunding
                .Should()
                .Be(300);

            publishedProviderFundingCount
                .IndicativeProviderTotalFunding
                .Should()
                .Be(200);

            publishedProviderFundingCount
                .PaidProvidersTotalFunding
                .Should()
                .Be(100);
        }

        protected static void AddPublishingApiClient(IServiceCollection serviceCollection,
            IConfiguration configuration) =>
            serviceCollection.AddSingleton<IPublishingApiClient, PublishingApiClient>();

        private async Task GivenThePublishedProvider(PublishedProviderTemplateParameters[] publishedProviderTemplateParameters)
            => await _publishedProviderDataContext.CreateContextData(publishedProviderTemplateParameters);

        private async Task<ApiResponse<PublishedProviderFundingCount>> WhenProviderBatchForReleaseCountQueried(
            PublishedProviderIdsRequest publishedProviderIdsRequest,
            string specificationId)
            => await _publishing.GetProviderBatchForReleaseCount(publishedProviderIdsRequest, specificationId);

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
