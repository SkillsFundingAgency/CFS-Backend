using System.Collections.Generic;
using System.Reflection;
using CalculateFunding.Api.FundingDataZone.Controllers;
using CalculateFunding.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Api.FundingDataZone.UnitTests
{
    [TestClass]
    public class StartupTests : ControllerIoCUnitTestBase
    {
        protected override Dictionary<string, string> AddToConfiguration()
        {
            return new Dictionary<string, string>();
        }

        protected override Assembly EntryAssembly => typeof(DataDownloadController).Assembly;

        protected override void RegisterDependencies()
        {
            new Startup(CreateTestConfiguration())
                .ConfigureServices(ServiceCollection);
        }
    }
}
