using CalculateFunding.Common.ApiClient;
using CalculateFunding.Common.ApiClient.Publishing;
using CalculateFunding.Common.Config.ApiClient;
using CalculateFunding.IntegrationTests.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Polly;
using System;
using System.Reflection;

namespace CalculateFunding.Api.Publishing.IntegrationTests.ReleaseManagement
{
    [TestClass]
    [TestCategory(nameof(IntegrationTest))]
    public partial class FundingManagementTests
    {
        private static readonly Assembly ResourceAssembly =
            typeof(FundingManagementTests).Assembly;

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
    }
}
