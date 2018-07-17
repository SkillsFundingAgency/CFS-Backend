using CalculateFunding.Api.External;
using CalculateFunding.Api.External.Controllers;
using CalculateFunding.Tests.Common;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

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

            // Assert
            ResolveType<AllocationsController>().Should().NotBeNull(nameof(AllocationsController));
            ResolveType<AllocationNotificationsController>().Should().NotBeNull(nameof(AllocationNotificationsController));
            ResolveType<FundingStreamController>().Should().NotBeNull(nameof(FundingStreamController));
            ResolveType<ProviderResultsController>().Should().NotBeNull(nameof(ProviderResultsController));
        }
    }
}