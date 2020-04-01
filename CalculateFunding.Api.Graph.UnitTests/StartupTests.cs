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
                { "GraphDbSettings:Url", "bolt://localhost:7687" },
                { "GraphDbSettings:Username", "neo4j" },
                { "GraphDbSettings:Password", "password" }
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