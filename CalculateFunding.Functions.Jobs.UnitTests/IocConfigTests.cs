using System.Collections.Generic;
using System.Reflection;
using CalculateFunding.Functions.Jobs.ServiceBus;
using CalculateFunding.Tests.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Functions.Jobs.UnitTests
{
    [TestClass]
    public class IocConfigTests : FunctionIoCUnitTestBase
    {
        protected override Dictionary<string, string> AddToConfiguration()
        {
            Dictionary<string, string> configData = new Dictionary<string, string>
            {
                { "CosmosDbSettings:DatabaseName", "calculate-funding" },
                { "CosmosDbSettings:ContainerName", "jobs" },
                { "CosmosDbSettings:ConnectionString", "AccountEndpoint=https://test.documents.azure.com:443/;AccountKey=dGVzdA==;" }
            };

            return configData;
        }
        
        protected override Assembly EntryAssembly => typeof(OnDeleteJobs).Assembly;

        protected override void RegisterDependencies()
        {            
            Startup.RegisterComponents(ServiceCollection, CreateTestConfiguration());
        }
    }
}
