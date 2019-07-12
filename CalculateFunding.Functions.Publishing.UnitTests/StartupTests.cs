using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Tests.Common;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Functions.Publishing.UnitTests
{
    [TestClass]
    public class StartupTests : IoCUnitTestBase
    {
        [TestMethod]
        public void ConfigureServices_RegisterDependenciesCorrectly()
        {
            // Arrange
            IConfigurationRoot configuration = CreateTestConfiguration();

            // Act
            using (IServiceScope scope = Startup.RegisterComponents(new ServiceCollection(), configuration).CreateScope())
            {
                // Assert
                scope.ServiceProvider.GetService<IPublishService>().Should().NotBeNull(nameof(IPublishService));
                scope.ServiceProvider.GetService<IApproveService>().Should().NotBeNull(nameof(IApproveService));
                scope.ServiceProvider.GetService<IRefreshService>().Should().NotBeNull(nameof(IRefreshService));
                
            }
        }
    }
}
