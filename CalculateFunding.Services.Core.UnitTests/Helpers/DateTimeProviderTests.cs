using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Core.Helpers
{
    [TestClass]
    public class DateTimeProviderTests
    {
        [TestMethod]
        public void UtcNowReturnsCurrentDateTimeOffset()
        {
            DateTime expectedDate = DateTimeOffset.UtcNow.Date;

            DateTimeOffset utcNow = new DateTimeProvider()
                .UtcNow;

            utcNow.Date
                .Should()
                .BeSameDateAs(expectedDate);
        }
    }
}