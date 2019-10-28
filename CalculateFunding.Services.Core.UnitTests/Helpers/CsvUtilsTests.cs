using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Core.Helpers
{
    [TestClass]
    public class CsvUtilsTests
    {
#if NCRUNCH
        [Ignore]
#endif
        [TestMethod]
        [DynamicData(nameof(CreateCsvExpandoTestCases), DynamicDataSourceType.Method)]
        public void CreateCsvExpando_CreatesAsExpected(IEnumerable<dynamic> input, string expectedOutput)
        {
            new CsvUtils()
                .AsCsv(input)
                .Should()
                .Be(expectedOutput);
        }

        private static IEnumerable<object[]> CreateCsvExpandoTestCases()
        {
            yield return new object[]
            {
                new dynamic[0],
                ""
            };
            yield return new object[]
            {
                new dynamic[]
                {
                    new
                    {
                        Name = "Elizabeth",
                        Country = "UK"
                    },
                    new
                    {
                        Name = "Donald",
                        Country = "USA"
                    }
                },
                "\"Name\",\"Country\"\r\n\"Elizabeth\",\"UK\"\r\n\"Donald\",\"USA\"\r\n"
            };
            yield return new object[]
            {
                new dynamic[]
                {
                    new
                    {
                        Name = "Junior",
                        Country = "SA"
                    },
                    new
                    {
                        Name = "Senior",
                        Country = "GB"
                    },
                    new
                    {
                        Name = "Middle",
                        Country = "JP"
                    }
                },
                "\"Name\",\"Country\"\r\n\"Junior\",\"SA\"\r\n\"Senior\",\"GB\"\r\n\"Middle\",\"JP\"\r\n"
            };
        }
    }
}