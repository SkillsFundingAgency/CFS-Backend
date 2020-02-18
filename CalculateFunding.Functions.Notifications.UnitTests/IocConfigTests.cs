using System.Collections.Generic;
using System.Reflection;
using CalculateFunding.Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Functions.Notifications.UnitTests
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
        
        protected override Assembly FunctionAssembly => typeof(OnNotificationEventTrigger).Assembly;

        protected override IServiceScope CreateServiceScope() =>
            Startup.RegisterComponents(ServiceCollection, CreateTestConfiguration())
                .CreateScope();
    }
}
