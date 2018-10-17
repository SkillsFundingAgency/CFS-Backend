using CalculateFunding.Api.Results;
using CalculateFunding.Api.Results.Controllers;
using CalculateFunding.Services.Results;
using CalculateFunding.Services.Results.Interfaces;
using CalculateFunding.Tests.Common;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace CalculateFunding.Api.Results.UnitTests
{
    [TestClass]
    public class StartupTests : IoCUnitTestBase
    {
        bool enableMajorMinorVersioning = true;

        [TestInitialize()]
        public void BeforeTest()
        {
            enableMajorMinorVersioning = true;
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
            ResolveType<ResultsController>().Should().NotBeNull(nameof(ResultsController));
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
                { "features:allocationLineMajorMinorVersioningEnabled", enableMajorMinorVersioning.ToString().ToLowerInvariant()}
            };

            return configData;
        }
    }
}