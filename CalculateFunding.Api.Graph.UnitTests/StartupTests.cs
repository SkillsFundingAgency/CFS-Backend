using System.Collections.Generic;
using System.Reflection;
using CalculateFunding.Api.Graph.Controllers;
using CalculateFunding.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Api.Graph.UnitTests
{
    [TestClass]
    public class StartupTests : ControllerIoCUnitTestBase
    {

        protected override Dictionary<string, string> AddToConfiguration()
        {
            return new Dictionary<string, string>
            {
                { "CosmosGraphSettings:EndPointUrl", "localhost" },
                { "CosmosGraphSettings:Port", "443" },
                { "CosmosGraphSettings:ApiKey", "xyz" },
                { "CosmosGraphSettings:ContainerPath", "/dbs/cfs/colls/specs" },
            };
        }
        
        protected override Assembly EntryAssembly => typeof(GraphController).Assembly;
        
        protected override void RegisterDependencies()
        {
            new Startup(CreateTestConfiguration())
                .ConfigureServices(ServiceCollection);
        }
    }
}