using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace CalculateFunding.Tests.Common
{
    public abstract class IoCUnitTestBase
    {
        /// <summary>
        /// Gets the service collection for DI
        /// </summary>
        protected IServiceCollection Services { get; } = new ServiceCollection();

        /// <summary>
        /// Gets the service provider for DI
        /// </summary>
        protected ServiceProvider ServiceProvider
        {
            get { return Services.BuildServiceProvider(); }
        }

        /// <summary>
        /// If implemented in a base class provides additional configuration for the service
        /// </summary>
        /// <returns>Name/value pair collection</returns>
        protected virtual Dictionary<string, string> AddToConfiguration()
        {
            return null;
        }

        /// <summary>
        /// Creates the basic configuration used by all services. Calls thet AddToConfiguration method to provide additional config
        /// </summary>
        /// <returns>A IConfigurationRoot object</returns>
        protected virtual IConfigurationRoot CreateTestConfiguration()
        {
            var configData = new Dictionary<string, string>
            {
                { "AzureWebJobsStorage", "UseDevelopmentStorage=true" },
                { "AzureWebJobsDashboard", "UseDevelopmentStorage=true"},
                { "AzureStorageSettings:ConnectionString", "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=test==" },
                { "ApplicationInsightsOptions:InstrumentationKey", "test" },
                { "Logging:LogLevel:Default", "Debug" },
                { "Logging:LogLevel:System", "Information" },
                { "Logging:LogLevel:Microsoft", "Information" },
                { "apiKeyMiddleware:apiKey", "Local" }
            };

            var cb = new ConfigurationBuilder()
                .AddInMemoryCollection(configData);

            Dictionary<string, string> customConfig = AddToConfiguration();
            if (customConfig != null)
            {
                cb.AddInMemoryCollection(customConfig);
            }

            return cb.Build();
        }

        /// <summary>
        /// Resolves a type using the type activator cache, just as ASP.NET Core does
        /// </summary>
        /// <typeparam name="T">The type to resolve</typeparam>
        /// <returns>The resolved type, or null if cannot be resolved</returns>
        protected T ResolveType<T>()
        {
            TypeActivatorCache typeActivatorCache = new TypeActivatorCache();
            return typeActivatorCache.CreateInstance<T>(ServiceProvider, typeof(T));
        }
    }
}
