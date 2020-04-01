using System.Collections.Generic;
using System.Reflection;
using CalculateFunding.Functions.Results.ServiceBus;
using CalculateFunding.Tests.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Functions.Results.UnitTests
{
    [TestClass]
    public class IocConfigTests : FunctionIoCUnitTestBase
    {
        protected override Dictionary<string, string> AddToConfiguration()
        {
            Dictionary<string, string> configData = new Dictionary<string, string>
            {
                { "SearchServiceName", "ss-t1te-cfs"},
                { "SearchServiceKey", "test" },
                { "CosmosDbSettings:DatabaseName", "calculate-funding" },
                { "CosmosDbSettings:ContainerName", "calcs" },
                { "CosmosDbSettings:ConnectionString", "AccountEndpoint=https://test.documents.azure.com:443/;AccountKey=dGVzdA==;" },
                { "specificationsClient:ApiEndpoint", "https://localhost:7001/api/" },
                { "specificationsClient:ApiKey", "Local" },
                { "resultsClient:ApiEndpoint", "https://localhost:7005/api/" },
                { "resultsClient:ApiKey", "Local" },
                { "calcsClient:ApiEndpoint", "https://localhost:7002/api/" },
                { "calcsClient:ApiKey", "Local" },
                { "providerProfilingClient:ApiEndpoint", "https://funding-profiling/" },
                { "providerProfilingAzureBearerTokenOptions:Url", "https://wahetever-token" },
                { "providerProfilingAzureBearerTokenOptions:GrantType", "client_credentials" },
                { "providerProfilingAzureBearerTokenOptions:Scope", "https://wahetever-scope" },
                { "providerProfilingAzureBearerTokenOptions:ClientId", "client-id" },
                { "providerProfilingAzureBearerTokenOptions:ClientSecret", "client-secret"},
                { "redisSettings:CacheConnection", "CacheConnection"},
                { "AzureStorageSettings:ConnectionString", "StorageConnection" },
                { "providersClient:ApiEndpoint", "https://localhost:7010/api/" },
                { "providersClient:ApiKey", "Local" },
                { "jobsClient:ApiEndpoint", "https://localhost:7010/api/" },
                { "jobsClient:ApiKey", "Local" },
            };

            return configData;
        }
        
        protected override Assembly EntryAssembly => typeof(OnCalculationResultsCsvGeneration).Assembly;

        protected override void RegisterDependencies()
        {            
            Startup.RegisterComponents(ServiceCollection, CreateTestConfiguration());
        }
    }
}
