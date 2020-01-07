using System.Collections.Generic;
using CalculateFunding.Api.Publishing.Controllers;
using CalculateFunding.Tests.Common;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Api.Publishing.UnitTests
{
    [TestClass]
    public class StartupTests : IoCUnitTestBase
    {
        [TestMethod]
        public void ConfigureServices_RegisterDependenciesCorrectly()
        {
            // Arrange
            Services.AddSingleton<IHostingEnvironment>(new HostingEnvironment());
            IConfigurationRoot configuration = CreateTestConfiguration();
            Startup target = new Startup(configuration);

            // Act
            target.ConfigureServices(Services);

            // Assert
            ResolveType<PublishingController>().Should().NotBeNull(nameof(PublishingController));
            ResolveType<PublishedProvidersController>().Should().NotBeNull(nameof(PublishedProvidersController));

        }

        protected override Dictionary<string, string> AddToConfiguration()
        {
            return new Dictionary<string, string>
            {
               { "CosmosDbSettings:ContainerName", "publishedfunding" },
               { "CosmosDbSettings:DatabaseName", "calculate-funding" },
               { "CosmosDbSettings:ConnectionString", "AccountEndpoint=https://test.documents.azure.com:443/;AccountKey=dGVzdA==;" },
               { "SearchServiceName", "ss-t1te-cfs"},
                { "SearchServiceKey", "test" },
                { "jobsClient:ApiEndpoint", "https://localhost:7010/api/"},
                { "jobsClient:ApiKey", "Local"},
                { "providersClient:ApiEndpoint", "https://localhost:7011/api/" },
                { "providersClient:ApiKey", "Local" },
                { "AzureStorageSettings:ConnectionString", "StorageConnection" },
            };
        }
    }
}
