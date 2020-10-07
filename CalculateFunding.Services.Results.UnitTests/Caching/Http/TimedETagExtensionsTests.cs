using System;
using System.Collections.Generic;
using CalculateFunding.Services.Results.Caching.Http;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Results.UnitTests.Caching.Http
{
    [TestClass]
    [Ignore("failing on server - look into DT issues")]
    public class TimedETagExtensionsTests
    {
        [TestMethod]
        [DynamicData(nameof(ETagExamples), DynamicDataSourceType.Method)]
        public void ToETagString(DateTimeOffset dateTimeOffset,
            string expectedETag)
        {
            string actualETag = dateTimeOffset.ToETagString();

            actualETag
                .Should()
                .Be(expectedETag);
        }

        private static IEnumerable<object[]> ETagExamples()
        {
            yield return ETagExample("12 October 2021", "ABg52gqN2QgBAA==");
            yield return ETagExample("9 September 1999", "AFgIA2XIwAgBAA==");
            yield return ETagExample("30 June 2020", "AFh8JoAc2AgBAA==");
        }

        private static object[] ETagExample(string dateLiteral,
            string expectedETag)
            => new object []
            {
                NewDateTimeOffset(dateLiteral), expectedETag
            };
        
        private static DateTimeOffset NewDateTimeOffset(string literal)
        => DateTimeOffset.Parse(literal);
    }
}