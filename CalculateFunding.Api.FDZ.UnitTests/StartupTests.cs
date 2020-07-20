using CalculateFunding.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Reflection;

namespace CalculateFunding.Api.FDZ.UnitTests
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
