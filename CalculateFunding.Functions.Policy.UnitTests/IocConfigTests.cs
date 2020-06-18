using System.Collections.Generic;
using System.Reflection;
using CalculateFunding.Functions.Policy.ServiceBus;
using CalculateFunding.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Functions.Policy.UnitTests
{
    [TestClass]
    public class IocConfigTests : FunctionIoCUnitTestBase
    {
        protected override Dictionary<string, string> AddToConfiguration()
        {
            return new Dictionary<string, string>
            {
                { "AzureConfiguration:ConnectionString", "connectionString"},
                { "APPINSIGHTS_INSTRUMENTATIONKEY", "GUID"},
                { "ApplicationInsightsOptions:InstrumentationKey", "GUID"},
                { "ApplicationInsights:InstrumentationKey", "GUID"},
                { "AzureStorageSettings:ConnectionString", "StorageConnection" },
                { "RedisSettings:CacheConnection", "redisConnection" },
                { "ServiceBusSettings:ConnectionString", "serviceBusConnection" },
                { "CosmosDbSettings:DatabaseName", "calculate-funding" },
                { "CosmosDbSettings:ConnectionString", "AccountEndpoint=https://test.documents.azure.com:443/;AccountKey=dGVzdA==;" },
                { "SearchServiceName", "ss-t1te-cfs"},
                { "SearchServiceKey", "test" },
                { "jobsClient:ApiEndpoint", "https://localhost:7010/api/"},
                { "jobsClient:ApiKey", "Local"},
            };
        }

        protected override Assembly EntryAssembly => typeof(OnReIndexTemplates).Assembly;

        protected override void RegisterDependencies()
        {
            Startup.RegisterComponents(ServiceCollection, CreateTestConfiguration());
        }

    }
}
