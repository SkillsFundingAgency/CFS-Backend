using System.Collections.Generic;
using CalculateFunding.Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using CalculateFunding.Functions.CosmosDbScaling.EventHubs;

namespace CalculateFunding.Functions.CosmosDbScaling.UnitTests
{
    [TestClass]
    public class IocConfigTests : FunctionIoCUnitTestBase
    {
        protected override Dictionary<string, string> AddToConfiguration()
        {
            var configData = new Dictionary<string, string>
            {
                { "SearchServiceName", "ss-t1te-cfs"},
                { "SearchServiceKey", "test" },
                { "CosmosDbSettings:DatabaseName", "calculate-funding" },
                { "CosmosDbSettings:ConnectionString", "AccountEndpoint=https://test.documents.azure.com:443/;AccountKey=dGVzdA==;" },
                { "specificationsClient:ApiEndpoint", "https://localhost:7001/api/" },
                { "specificationsClient:ApiKey", "Local" },
                { "calcsClient:ApiEndpoint", "https://localhost:7002/api/" },
                { "calcsClient:ApiKey", "Local" },
                { "datasetsClient:ApiEndpoint", "https://localhost:7004/api/"},
                { "datasetsClient:ApiKey", "Local"},
                { "jobsClient:ApiEndpoint", "https://localhost:7010/api/"},
                { "jobsClient:ApiKey", "Local"},
                { "AzureStorageSettings:ConnectionString", "StorageConnection" }
            };

            return configData;
        }
        protected override Assembly EntryAssembly => typeof(OnCosmosDbDiagnosticsReceived).Assembly;

        protected override void RegisterDependencies()
        {
            Startup.RegisterComponents(ServiceCollection, CreateTestConfiguration());
        }
    }
}
