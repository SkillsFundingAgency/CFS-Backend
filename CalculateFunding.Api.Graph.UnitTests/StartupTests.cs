using System.Collections.Generic;
using CalculateFunding.Api.Graph;
using CalculateFunding.Api.Graph.Controllers;
using CalculateFunding.Common.Graph.Interfaces;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Services.Graph.Interfaces;
using CalculateFunding.Tests.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Api.Graph.UnitTests
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
            ServiceCollection collection = new ServiceCollection();
            target.RegisterComponents(collection);
            ServiceProvider provider = collection.BuildServiceProvider();

            // Act
            target.ConfigureServices(Services);

            // Assert
            ResolveType<GraphController>().Should().NotBeNull(nameof(GraphController));

            using (IServiceScope scope = provider.CreateScope())
            {
                scope.ServiceProvider.GetService<IHealthChecker>().Should().NotBeNull(nameof(IHealthChecker));
                scope.ServiceProvider.GetService<ICypherBuilderHost>().Should().NotBeNull(nameof(ICypherBuilderHost));
                scope.ServiceProvider.GetService<IGraphRepository>().Should().NotBeNull(nameof(IGraphRepository));
                scope.ServiceProvider.GetService<ISpecificationRepository>().Should().NotBeNull(nameof(ISpecificationRepository));
                scope.ServiceProvider.GetService<ICalculationRepository>().Should().NotBeNull(nameof(ICalculationRepository));
                scope.ServiceProvider.GetService<IGraphService>().Should().NotBeNull(nameof(IGraphService));
            }
        }

        protected override Dictionary<string, string> AddToConfiguration()
        {
            return new Dictionary<string, string>
            {
                { "GraphDbSettings:Url", "bolt://localhost:7687" },
                { "GraphDbSettings:Username", "neo4j" },
                { "GraphDbSettings:Password", "password" }
            };
        }
    }
}