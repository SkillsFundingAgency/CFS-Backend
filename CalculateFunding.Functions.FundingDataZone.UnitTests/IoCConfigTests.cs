using System.Collections.Generic;
using System.Reflection;
using CalculateFunding.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Functions.FundingDataZone.UnitTests
{
    [TestClass]
    public class IoCConfigTests : FunctionIoCUnitTestBase
    {
        protected override Dictionary<string, string> AddToConfiguration()
        {
            return new Dictionary<string, string>
            {
                {"FDZSqlStorageSettings:ConnectionString", "summit"}
            };
        }

        protected override Assembly EntryAssembly => typeof(StartUp).Assembly;

        protected override void RegisterDependencies()
        {            
            StartUp.RegisterComponents(ServiceCollection, CreateTestConfiguration());
        }
    }
}