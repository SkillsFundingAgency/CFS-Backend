using System.Collections.Generic;
using System.Reflection;
using CalculateFunding.Functions.Calcs.ServiceBus;
using CalculateFunding.Tests.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Functions.Calcs.UnitTests
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
                { "CosmosDbSettings:ContainerName", "calcs" },
                { "CosmosDbSettings:ConnectionString", "AccountEndpoint=https://test.documents.azure.com:443/;AccountKey=dGVzdA==;" },
                { "specificationsClient:ApiEndpoint", "https://localhost:7001/api/" },
                { "specificationsClient:ApiKey", "Local" },
                { "resultsClient:ApiEndpoint", "https://localhost:7005/api/" },
                { "resultsClient:ApiKey", "Local" },
                { "datasetsClient:ApiEndpoint", "https://localhost:7004/api/"},
                { "datasetsClient:ApiKey", "Local"},
                { "calcsClient:ApiEndpoint", "https://localhost:7004/api/"},
                { "calcsClient:ApiKey", "Local"},
                { "jobsClient:ApiEndpoint", "https://localhost:7010/api/"},
                { "jobsClient:ApiKey", "Local"},
                { "AzureStorageSettings:ConnectionString", "StorageConnection" },
                { "providersClient:ApiEndpoint", "https://localhost:7011/api/" },
                { "providersClient:ApiKey", "Local" },
                { "graphClient:ApiEndpoint", "https://localhost:7015/api/" },
                { "graphClient:ApiKey", "Local" }
            };

            return configData;
        }

        protected override Assembly EntryAssembly => typeof(OnDeleteCalculations).Assembly;

        protected override void RegisterDependencies()
        {           
            Startup.RegisterComponents(ServiceCollection, CreateTestConfiguration());
        }
    }
}
