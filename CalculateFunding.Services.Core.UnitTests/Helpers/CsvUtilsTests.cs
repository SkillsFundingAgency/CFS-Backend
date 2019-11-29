using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Core.Helpers
{
    [TestClass]
    public class CsvUtilsTests
    {
        [TestMethod]
        [DynamicData(nameof(CreateCsvExpandoTestCases), DynamicDataSourceType.Method)]
        public void CreateCsv_ProjectsSuppliedItemsIntoExpandoObjectRows(IEnumerable<dynamic> input, string expectedOutput, bool outputHeaders)
        {
            string actualCsvOutput = new CsvUtils().AsCsv(input, outputHeaders);

            actualCsvOutput
                .Should()
                .Be(expectedOutput);
        }

        private static IEnumerable<object[]> CreateCsvExpandoTestCases()
        {
            yield return new object[]
            {
                new dynamic[0],
                null,
                true
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
                "\"Name\",\"Country\"\r\n\"Elizabeth\",\"UK\"\r\n\"Donald\",\"USA\"\r\n",
                true
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
                "\"Junior\",\"SA\"\r\n\"Senior\",\"GB\"\r\n\"Middle\",\"JP\"\r\n",
                false
            };
        }
    }
}