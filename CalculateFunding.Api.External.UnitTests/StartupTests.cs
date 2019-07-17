using System;
using System.Collections.Generic;
using CalculateFunding.Services.Results;
using CalculateFunding.Services.Results.Interfaces;
using CalculateFunding.Tests.Common;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Api.External.UnitTests
{
    [TestClass]
    public class StartupTests : IoCUnitTestBase
    {
        [TestMethod]
        public void ConfigureServices_RegisterDependenciesCorrectly()
        {
            // Arrange
            IConfigurationRoot configuration = CreateTestConfiguration();
            Startup target = new Startup(configuration);

            // Act
            target.ConfigureServices(Services);

            // Assert v1
            ResolveType<V1.Controllers.AllocationsController>().Should().NotBeNull(nameof(V1.Controllers.AllocationsController));
            ResolveType<V1.Controllers.AllocationNotificationsController>().Should().NotBeNull(nameof(V1.Controllers.AllocationNotificationsController));
            ResolveType<V1.Controllers.FundingStreamController>().Should().NotBeNull(nameof(V1.Controllers.FundingStreamController));
            ResolveType<V1.Controllers.TimePeriodsController>().Should().NotBeNull(nameof(V1.Controllers.TimePeriodsController));

            // Assert v2
            ResolveType<V2.Controllers.AllocationsController>().Should().NotBeNull(nameof(V2.Controllers.AllocationsController));
            ResolveType<V2.Controllers.AllocationNotificationsController>().Should().NotBeNull(nameof(V2.Controllers.AllocationNotificationsController));
            ResolveType<V2.Controllers.FundingStreamController>().Should().NotBeNull(nameof(V2.Controllers.FundingStreamController));
            ResolveType<V2.Controllers.TimePeriodsController>().Should().NotBeNull(nameof(V2.Controllers.TimePeriodsController));
        }

        [TestMethod]
        public void ConfigureServices_WhenMajorMinorVersioningIsEnabled_RegisterDependenciesCorrectly()
        {
            // Arrange
            IConfigurationRoot configuration = CreateTestConfiguration();
            Startup target = new Startup(configuration);

            // Act
            target.ConfigureServices(Services);

            // Assert
            IServiceProvider serviceProvider = target.ServiceProvider;

            IPublishedAllocationLineLogicalResultVersionService service = (IPublishedAllocationLineLogicalResultVersionService)serviceProvider.GetService(typeof(IPublishedAllocationLineLogicalResultVersionService));

            service
                .Should()
                .BeOfType<PublishedAllocationLineLogicalResultVersionService>();
        }

        protected override Dictionary<string, string> AddToConfiguration()
        {
            var configData = new Dictionary<string, string>
            {
                { "SearchServiceName", "ss-t1te-cfs"},
                { "SearchServiceKey", "test" },
                { "CosmosDbSettings:CollectionName", "providerresults" },
                { "CosmosDbSettings:DatabaseName", "calculate-funding" },
                { "CosmosDbSettings:ConnectionString", "AccountEndpoint=https://test.documents.azure.com:443/;AccountKey=dGVzdA==;" },
                { "specificationsClient:ApiEndpoint", "https://localhost:7001/api/" },
                { "specificationsClient:ApiKey", "Local" },
                { "resultsClient:ApiEndpoint", "https://localhost:7005/api/" },
                { "resultsClient:ApiKey", "Local" },
                { "jobsClient:ApiKey", "Local" },
                { "jobsClient:ApiEndpoint", "https://localhost:7010/api/" },
                { "providerProfilingClient:ApiEndpoint", "https://funding-profiling/" },
                { "providerProfilingAzureBearerTokenOptions:Url", "https://wahetever-token" },
                { "providerProfilingAzureBearerTokenOptions:GrantType", "client_credentials" },
                { "providerProfilingAzureBearerTokenOptions:Scope", "https://wahetever-scope" },
                { "providerProfilingAzureBearerTokenOptions:ClientId", "client-id" },
                { "providerProfilingAzureBearerTokenOptions:ClientSecret", "client-secret"},
            };

            return configData;
        }
    }
}