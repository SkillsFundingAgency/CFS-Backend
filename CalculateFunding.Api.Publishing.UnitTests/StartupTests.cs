﻿using System.Collections.Generic;
using CalculateFunding.Api.Publishing.Controllers;
using CalculateFunding.Tests.Common;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Api.Publishing.UnitTests
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

            // Assert
            ResolveType<PublishingController>().Should().NotBeNull(nameof(PublishingController));
            ResolveType<PublishedProvidersController>().Should().NotBeNull(nameof(PublishedProvidersController));

        }

        protected override Dictionary<string, string> AddToConfiguration()
        {
            var configData = new Dictionary<string, string>
            {
               { "CosmosDbSettings:CollectionName", "publishedfunding" },
               { "CosmosDbSettings:DatabaseName", "calculate-funding" },
               { "CosmosDbSettings:ConnectionString", "AccountEndpoint=https://test.documents.azure.com:443/;AccountKey=dGVzdA==;" },
            };

            return configData;
        }
    }
}
