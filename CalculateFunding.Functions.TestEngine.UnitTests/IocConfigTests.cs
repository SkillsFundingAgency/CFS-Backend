using System.Collections.Generic;
using System.Reflection;
using CalculateFunding.Functions.TestEngine.ServiceBus;
using CalculateFunding.Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Functions.TestEngine.UnitTests
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
                { "calcsClient:ApiEndpoint", "https://localhost:7002/api/" },
                { "calcsClient:ApiKey", "Local" },
                { "providersClient:ApiEndpoint", "https://localhost:7002/api/" },
                { "providersClient:ApiKey", "Local" },
                { "scenariosClient:ApiEndpoint", "https://localhost:7006/api/" },
                { "scenariosClient:ApiKey", "Local" },
                { "resultsClient:ApiEndpoint", "https://localhost:7005/api/" },
                { "resultsClient:ApiKey", "Local" }
            };

            return configData;
        }
        
        protected override Assembly EntryAssembly => typeof(OnDeleteTestResults).Assembly;

        protected override void RegisterDependencies()
        {
            Startup.RegisterComponents(ServiceCollection, CreateTestConfiguration());
        }
    }
}
