using System.Linq;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Profiling;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Profiling
{
    [TestClass]
    public class YearMonthOrderedProfilePeriodsTests : ProfilingTestBase 
    {
        [TestMethod]
        public void ProjectsDistributionPeriodsAndProfilePeriodsOrderedByYearThenProfilePeriodMonthThenOccurence_ExampleOne()
        {
            ProfilePeriod lastPeriod = NewProfilePeriod(2, 2021, "December");
            ProfilePeriod firstPeriod = NewProfilePeriod(0, 2020, "December");
            ProfilePeriod secondPeriod = NewProfilePeriod(1, 2020, "December");
            ProfilePeriod thirdPeriod = NewProfilePeriod(0, 2021, "January");

            FundingLine fundingLine = NewFundingLine(_ => _.WithDistributionPeriods(NewDistributionPeriod(
                    dp => dp.WithProfilePeriods(thirdPeriod, secondPeriod)),
                NewDistributionPeriod(dp => dp.WithProfilePeriods(lastPeriod, firstPeriod))));

            ProfilePeriod[] orderedProfilePeriods = new YearMonthOrderedProfilePeriods(fundingLine)
                .ToArray();

            orderedProfilePeriods[0]
                .Should()
                .BeSameAs(firstPeriod);
            
            orderedProfilePeriods[1]
                .Should()
                .BeSameAs(secondPeriod);
            
            orderedProfilePeriods[2]
                .Should()
                .BeSameAs(thirdPeriod);

            orderedProfilePeriods[3]
                .Should()
                .BeSameAs(lastPeriod);
        }
    }
}