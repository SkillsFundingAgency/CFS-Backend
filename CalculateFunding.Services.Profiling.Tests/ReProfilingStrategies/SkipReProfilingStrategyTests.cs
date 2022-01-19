using CalculateFunding.Services.Profiling.ReProfilingStrategies;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Profiling.Tests.ReProfilingStrategies
{
    [TestClass]
    public class SkipReProfilingStrategyTests : ReProfilingStrategyTest
    {
        [TestInitialize]
        public void SetUp()
        {
            ReProfiling = new SkipReProfilingStrategy();
        }

        [TestMethod]
        public void SkipReProfilingStrategy_ReturnsSkipReProfilingResultWhenReProfiled()
        {
            WhenTheFundingLineIsReProfiled();

            Result.DistributionPeriods.Should().BeNull();
            Result.CarryOverAmount.Should().Be(0);
            Result.SkipReProfiling.Should().Be(true);
        }
    }
}
