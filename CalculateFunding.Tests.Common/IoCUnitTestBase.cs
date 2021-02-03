using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        protected ServiceProvider ServiceProvider => Services.BuildServiceProvider();

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
                {"AzureWebJobsStorage", "UseDevelopmentStorage=true"},
                {"AzureWebJobsDashboard", "UseDevelopmentStorage=true"},
                {"AzureStorageSettings:ConnectionString", "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=test=="},
                {"ApplicationInsightsOptions:InstrumentationKey", "test"},
                {"Logging:LogLevel:Default", "Debug"},
                {"Logging:LogLevel:System", "Information"},
                {"Logging:LogLevel:Microsoft", "Information"},
                {"apiKeyMiddleware:apiKey", "Local"},
                {"specificationsClient:ApiEndpoint", "https://localhost:7001/api/"},
                {"specificationsClient:ApiKey", "Local"},
                {"resultsClient:ApiEndPoint", "https://localhost:7005/api/"},
                {"resultsClient:ApiKey", "Local"},
                {"policiesClient:ApiEndPoint", "https://localhost:7013/api/"},
                {"policiesClient:ApiKey", "Local"},
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
            var activator = ServiceProvider.GetService<IControllerActivator>();

            var actionContext = new ActionContext(
                    new DefaultHttpContext
                    {
                        RequestServices = ServiceProvider
                    },
                    new RouteData(),
                    new ControllerActionDescriptor
                    {
                        ControllerTypeInfo = typeof(T).GetTypeInfo()
                    });
            var controller = activator.Create(new ControllerContext(actionContext));

            if (controller.GetType() == typeof(T))
            {
                return (T)controller;
            }

            return default;
        }
    }
}
