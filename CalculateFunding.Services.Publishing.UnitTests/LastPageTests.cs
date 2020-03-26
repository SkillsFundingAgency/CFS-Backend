using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class LastPageTests
    {
        [TestMethod]
        [DynamicData(nameof(LastPageExamples), DynamicDataSourceType.Method)]
        public void PicksMaxPageBasedOnTopAndTotalCountSupplied(int totalCount,
            int top,
            int expectedLastPage)
        {
            int actualLastPage = new LastPage(totalCount, top);

            actualLastPage
                .Should()
                .Be(expectedLastPage);
        }

        private static IEnumerable<object[]> LastPageExamples()
        {
            yield return new object[]
            {
                257,
                50,
                6
            };
            yield return new object[]
            {
                660,
                100,
                7
            };
            yield return new object[]
            {
                600,
                100,
                6
            };
            //page cannot be less than 1
            yield return new object[]
            {
                0,
                100,
                1
            };
        }
    }
}