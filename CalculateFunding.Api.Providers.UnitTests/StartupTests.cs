using CalculateFunding.Api.Providers.Controllers;
using CalculateFunding.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Reflection;


namespace CalculateFunding.Api.Providers.UnitTests
{
    [TestClass]
    public class StartupTests : ControllerIoCUnitTestBase
    {
        protected override Dictionary<string, string> AddToConfiguration()
        {
            var configData = new Dictionary<string, string>
            {
                { "SearchServiceName", "ss-t1te-cfs"},
                { "SearchServiceKey", "test" },
                { "CosmosDbSettings:ContainerName", "calcs" },
                { "CosmosDbSettings:DatabaseName", "calculate-funding" },
                { "CosmosDbSettings:ConnectionString", "AccountEndpoint=https://test.documents.azure.com:443/;AccountKey=dGVzdA==;" },
                { "specificationsClient:ApiEndpoint", "https://localhost:7001/api/" },
                { "specificationsClient:ApiKey", "Local" },
                { "resultsClient:ApiEndpoint", "https://localhost:7005/api/" },
                { "resultsClient:ApiKey", "Local" },
                { "datasetsClient:ApiEndpoint", "https://localhost:7004/api/" },
                { "datasetsClient:ApiKey", "Local" },
                { "jobsClient:ApiEndpoint", "https://localhost:7010/api/" },
                { "jobsClient:ApiKey", "Local" },
                { "AzureStorageSettings:ConnectionString", "StorageConnection" }
            };

            return configData;
        }
        
        protected override Assembly EntryAssembly => typeof(ProvidersController).Assembly;
        
        protected override void RegisterDependencies()
        {
            new Startup(CreateTestConfiguration())
                .ConfigureServices(ServiceCollection);
        }
    }
}