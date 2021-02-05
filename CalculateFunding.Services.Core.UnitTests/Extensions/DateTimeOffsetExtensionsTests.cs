using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Core.Extensions
{
    [TestClass]
    public class DateTimeOffsetExtensionsTests
    {

        [TestMethod]
        public void CosmosDateTimeConverted_WhenTimeAm()
        {
            DateTimeOffset dateTimeOffset = new DateTimeOffset(2021, 02, 15, 8, 25, 37, TimeSpan.Zero);

            string result = dateTimeOffset.ToCosmosString();

            result
                .Should()
                .Be("2021-02-15T08:25:37.37Z");
        }

        [TestMethod]
        public void CosmosDateTimeConverted_WhenTimePm()
        {
            DateTimeOffset dateTimeOffset = new DateTimeOffset(2021, 02, 15, 17, 25, 37, TimeSpan.Zero);

            string result = dateTimeOffset.ToCosmosString();

            result
                .Should()
                .Be("2021-02-15T17:25:37.37Z");
        }
    }
}
