using System.Collections.Generic;
using System.Reflection;
using CalculateFunding.Api.External.V3.Controllers;
using CalculateFunding.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Api.External.UnitTests
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
                { "AzureStorageSettings:ConnectionString", "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=AccountKey=dGVzdA==;EndpointSuffix=core.windows.net" },
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