using System.Collections.Generic;
using System.Reflection;
using CalculateFunding.Api.Publishing.Controllers;
using CalculateFunding.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace CalculateFunding.Api.Publishing.UnitTests
{
    [TestClass]
    public class StartupTests : ControllerIoCUnitTestBase
    {

        protected override Dictionary<string, string> AddToConfiguration()
        {
            return new Dictionary<string, string>
            {
               { "CosmosDbSettings:ContainerName", "publishedfunding" },
               { "CosmosDbSettings:DatabaseName", "calculate-funding" },
               { "CosmosDbSettings:ConnectionString", "AccountEndpoint=https://test.documents.azure.com:443/;AccountKey=dGVzdA==;" },
               { "SearchServiceName", "ss-t1te-cfs"},
                { "SearchServiceKey", "test" },
                { "jobsClient:ApiEndpoint", "https://localhost:7010/api/"},
                { "jobsClient:ApiKey", "Local"},
                { "providersClient:ApiEndpoint", "https://localhost:7011/api/" },
                { "providersClient:ApiKey", "Local" },
                { "AzureStorageSettings:ConnectionString", "StorageConnection" },
                { "providerProfilingClient:ApiEndpoint", "https://funding-profiling/" },
                { "providerProfilingAzureBearerTokenOptions:Url", "https://wahetever-token" },
                { "providerProfilingAzureBearerTokenOptions:GrantType", "client_credentials" },
                { "providerProfilingAzureBearerTokenOptions:Scope", "https://wahetever-scope" },
                { "providerProfilingAzureBearerTokenOptions:ClientId", "client-id" },
                { "providerProfilingAzureBearerTokenOptions:ClientSecret", "client-secret"}
            };
        }
        
        protected override Assembly EntryAssembly => typeof(PublishingController).Assembly;
        
        protected override void RegisterDependencies()
        {
            new Startup(CreateTestConfiguration())
                .ConfigureServices(ServiceCollection);
        }
    }
}
