using CalculateFunding.Api.External.V3.Controllers;
using CalculateFunding.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Reflection;

namespace CalculateFunding.Api.External.UnitTests
{
    [TestClass]
    public class StartupTests : ControllerIoCUnitTestBase
    {
        protected override Dictionary<string, string> AddToConfiguration()
        {
            var configData = new Dictionary<string, string>
            {
                { "CosmosDbSettings:ContainerName", "publishedfunding" },
                { "CosmosDbSettings:DatabaseName", "calculate-funding" },
                { "CosmosDbSettings:ConnectionString", "AccountEndpoint=https://test.documents.azure.com:443/;AccountKey=dGVzdA==;" },
                { "SearchServiceName", "ss-t1te-cfs"},
                { "SearchServiceKey", "test" },
                { "AzureStorageSettings:ConnectionString", "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=AccountKey=dGVzdA==;EndpointSuffix=core.windows.net" },
                { "providerProfilingClient:ApiEndpoint", "https://localhost:5001/api" },
                { "providersClient:ApiEndpoint", "https://localhost:7011/api/" },
                { "providersClient:ApiKey", "Local" },
                { "releaseManagementSql:ConnectionString", "Server=.\\;Initial Catalog=ReleaseManagement;Integrated Security=SSPI" },
            };

            return configData;
        }

        protected override Assembly EntryAssembly => typeof(FundingFeedItemController).Assembly;

        protected override void RegisterDependencies()
        {
            new Startup(CreateTestConfiguration())
                .ConfigureServices(ServiceCollection);
        }
    }
}