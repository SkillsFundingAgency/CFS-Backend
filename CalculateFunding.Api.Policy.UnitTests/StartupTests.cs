using System.Collections.Generic;
using System.Reflection;
using CalculateFunding.Api.Policy.Controllers;
using CalculateFunding.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Api.Policy.UnitTests
{
    [TestClass]
    public class StartupTests : ControllerIoCUnitTestBase
    {
        protected override Dictionary<string, string> AddToConfiguration()
        {
            var configData = new Dictionary<string, string>
            {
                { "CosmosDbSettings:DatabaseName", "calculate-funding" },
                { "CosmosDbSettings:ConnectionString", "AccountEndpoint=https://test.documents.azure.com:443/;AccountKey=dGVzdA==;" },
                { "SearchServiceName", "ss-t1te-cfs"},
                { "SearchServiceKey", "test" },
                { "jobsClient:ApiEndpoint", "https://localhost:7010/api/"},
                { "jobsClient:ApiKey", "Local"},
                { "specificationsClient:ApiKey", "Local"},
                { "specificationsClient:ApiEndpoint", "https://localhost:7010/api/"},
                { "resultsClient:ApiKey", "Local"},
                { "resultsClient:ApiEndpoint", "https://localhost:7010/api/"},
                { "calcsClient:ApiKey", "Local"},
                { "calcsClient:ApiEndpoint", "https://localhost:7010/api/"},
            };

            return configData;
        }
        
        protected override Assembly EntryAssembly => typeof(TemplateController).Assembly;
        
        protected override void RegisterDependencies()
        {
            new Startup(CreateTestConfiguration())
                .ConfigureServices(ServiceCollection);
        }
    }
}
