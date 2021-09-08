using CalculateFunding.Common.ApiClient;
using CalculateFunding.Common.ApiClient.Publishing;
using CalculateFunding.Common.ApiClient.Publishing.Models;
using CalculateFunding.Common.Config.ApiClient;
using CalculateFunding.IntegrationTests.Common;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Polly;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace CalculateFunding.Api.Publishing.IntegrationTests.ReleaseManagement
{
    [TestClass]
    [TestCategory(nameof(IntegrationTest))]
    public class ReleaseProvidersToChannelsTests : IntegrationTestWithJobMonitoring
    {
        private static readonly Assembly ResourceAssembly = 
            typeof(ReleaseProvidersToChannelsTests).Assembly;

        private IPublishingApiClient _publishing;

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
            _publishing = GetService<IPublishingApiClient>();

            _specificationId = NewRandomString();
        }

        [TestMethod]
        public async Task RunsReleaseProviderVersionsJobForTheSpecificationIdProvidersAndChannels()
        {
            JobCreationResponse job = await WhenTheReleaseProviderVersionsJobIsQueued(_specificationId, NewReleaseProvidersToChannelRequest());

            job?.JobId
                .Should()
                .NotBeNullOrWhiteSpace();

            // TODO: Will be added as part of adding unit tests for ReleaseProviderVersions method
            //await ThenTheJobSucceeds(job.JobId, "Expected ProcessDatasetObsoleteItemsJob to complete and succeed.");
        }

        private async Task<JobCreationResponse> WhenTheReleaseProviderVersionsJobIsQueued(
            string specificationId,
            ReleaseProvidersToChannelRequest request)
            => (await _publishing.QueueReleaseProviderVersions(specificationId, request))?.Content;

        protected static void AddPublishingApiClient(IServiceCollection serviceCollection,
            IConfiguration configuration) =>
            serviceCollection.AddSingleton<IPublishingApiClient, PublishingApiClient>();

        private ReleaseProvidersToChannelRequest NewReleaseProvidersToChannelRequest(Action<ReleaseProvidersToChannelRequestBuilder> setup = null)
            => BuildNewModel<ReleaseProvidersToChannelRequest, ReleaseProvidersToChannelRequestBuilder>(setup);

        private T BuildNewModel<T, TB>(Action<TB> setup) where TB : TestEntityBuilder, new()
        {
            dynamic builder = new TB();
            setup?.Invoke(builder);
            return builder.Build();
        }
    }
}
