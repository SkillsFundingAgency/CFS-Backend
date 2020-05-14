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
            int minPageCount,
            int expectedLastPage)
        {
            int actualLastPage = new LastPage(totalCount, top, minPageCount);

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
                1,
                6
            };
            yield return new object[]
            {
                660,
                100,
                1,
                7
            };
            yield return new object[]
            {
                600,
                100,
                1,
                6
            };
            //page cannot be less than 1
            yield return new object[]
            {
                0,
                100,
                1,
                1
            };
            //page can be less than 1 with min page length
            yield return new object[]
            {
                0,
                100,
                0,
                0
            };
        }
    }
}