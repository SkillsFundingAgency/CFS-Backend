using CalculateFunding.Api.External.V1.Controllers;
using CalculateFunding.Services.Results;
using CalculateFunding.Services.Results.Interfaces;
using CalculateFunding.Tests.Common;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace CalculateFunding.Api.External.UnitTests
{
    [TestClass]
    public class StartupTests : IoCUnitTestBase
    {
        bool enableMajorMinorVersioning = true;

        bool disableProviderProfiling = true;

        [TestInitialize()]
        public void BeforeTest()
        {
            enableMajorMinorVersioning = true;
            disableProviderProfiling = true;
        }

        [TestMethod]
        public void ConfigureServices_RegisterDependenciesCorrectly()
        {
            // Arrange
            IConfigurationRoot configuration = CreateTestConfiguration();
            Startup target = new Startup(configuration);

            // Act
            target.ConfigureServices(Services);

            // Assert
            ResolveType<AllocationsController>().Should().NotBeNull(nameof(AllocationsController));
            ResolveType<AllocationNotificationsController>().Should().NotBeNull(nameof(AllocationNotificationsController));
            ResolveType<FundingStreamController>().Should().NotBeNull(nameof(FundingStreamController));
            ResolveType<ProviderResultsController>().Should().NotBeNull(nameof(ProviderResultsController));
			ResolveType<TimePeriodsController>().Should().NotBeNull(nameof(TimePeriodsController));
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

        [TestMethod]
        public void ConfigureServices_WhenMajorMinorVersioningIsDisabled_RegisterDependenciesCorrectly()
        {
            // Arrange
            enableMajorMinorVersioning = false;

            IConfigurationRoot configuration = CreateTestConfiguration();

            Startup target = new Startup(configuration);

            // Act
            target.ConfigureServices(Services);

            // Assert
            IServiceProvider serviceProvider = target.ServiceProvider;

            IPublishedAllocationLineLogicalResultVersionService service = (IPublishedAllocationLineLogicalResultVersionService)serviceProvider.GetService(typeof(IPublishedAllocationLineLogicalResultVersionService));

            service
               .Should()
               .BeOfType<RedundantPublishedAllocationLineLogicalResultVersionService>();
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
                { "providerProfilingClient:ApiEndpoint", "https://funding-profiling/" },
                { "providerProfilingAzureBearerTokenOptions:Url", "https://wahetever-token" },
                { "providerProfilingAzureBearerTokenOptions:GrantType", "client_credentials" },
                { "providerProfilingAzureBearerTokenOptions:Scope", "https://wahetever-scope" },
                { "providerProfilingAzureBearerTokenOptions:ClientId", "client-id" },
                { "providerProfilingAzureBearerTokenOptions:ClientSecret", "client-secret"},
                { "features:allocationLineMajorMinorVersioningEnabled", enableMajorMinorVersioning.ToString()}
            };

            return configData;
        }
    }
}