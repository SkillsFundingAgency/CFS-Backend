using System.Collections.Generic;
using System.Reflection;
using CalculateFunding.Api.CosmosDbScaling.Controllers;
using CalculateFunding.API.CosmosDbScaling;
using CalculateFunding.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Api.CosmosDbScaling.UnitTests
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
                { "CosmosDbSettings:ContainerName", "cosmosscalingconfig" },
                { "CosmosDbSettings:DatabaseName", "calculate-funding" },
                { "CosmosDbSettings:ConnectionString", "AccountEndpoint=https://test.documents.azure.com:443/;AccountKey=dGVzdA==;" },
                { "resultsClient:ApiEndpoint", "https://localhost:7005/api/" },
                { "resultsClient:ApiKey", "Local" },
                { "calcsClient:ApiEndpoint", "https://localhost:7002/api/" },
                { "calcsClient:ApiKey", "Local" },
                { "jobsClient:ApiEndpoint", "https://localhost:7010/api/"},
                { "jobsClient:ApiKey", "Local"},
                { "cosmosDbScalingClient:ApiEndpoint", "https://localhost:7003/api" },
                { "cosmosDbScalingClient:ApiKey", "Local" },
            };

            return configData;
        }
        
        protected override Assembly EntryAssembly => typeof(CosmosDbScalingController).Assembly;
        
        protected override void RegisterDependencies()
        {
            new Startup(CreateTestConfiguration())
                .ConfigureServices(ServiceCollection);
        }
    }
}
