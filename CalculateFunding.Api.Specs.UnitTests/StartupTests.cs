using System.Collections.Generic;
using System.Reflection;
using CalculateFunding.Api.Specs.Controllers;
using CalculateFunding.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace CalculateFunding.Api.Specs.UnitTests
{
    [TestClass]
    public class StartupTests : ControllerIoCUnitTestBase
    {
        protected override Dictionary<string, string> AddToConfiguration()
        {
            Dictionary<string, string> configData = new Dictionary<string, string>
            {
                { "SearchServiceName", "ss-t1te-cfs"},
                { "SearchServiceKey", "test" },
                { "CosmosDbSettings:ContainerName", "specs" },
                { "CosmosDbSettings:DatabaseName", "calculate-funding" },
                { "CosmosDbSettings:ConnectionString", "AccountEndpoint=https://test.documents.azure.com:443/;AccountKey=dGVzdA==;" },
                { "resultsClient:ApiEndpoint", "https://localhost:7005/api/" },
                { "resultsClient:ApiKey", "Local" },
                { "calcsClient:ApiEndpoint", "https://localhost:7002/api" },
                { "calcsClient:ApiKey", "Local" },
                { "jobsClient:ApiEndpoint", "https://localhost:7010/api/"},
                { "jobsClient:ApiKey", "Local"},
                { "graphClient:ApiEndpoint", "https://localhost:7020/api/" },
                { "graphClient:ApiKey", "Local"},
                { "providersClient:ApiEndpoint", "https://localhost:7011/api/" },
                { "providersClient:ApiKey", "Local" },
                { "datasetsClient:ApiEndpoint", "https://localhost:7011/api/" },
                { "datasetsClient:ApiKey", "Local" }
            };

            return configData;
        }
        
        protected override Assembly EntryAssembly => typeof(SpecificationsController).Assembly;
        
        protected override void RegisterDependencies()
        {
            new Startup(CreateTestConfiguration())
                .ConfigureServices(ServiceCollection);
        }
    }
}