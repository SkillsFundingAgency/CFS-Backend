using System;
using System.Collections.Generic;
using System.Globalization;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
// ReSharper disable NonReadonlyMemberInGetHashCode

namespace CalculateFunding.Services.Core.Helpers
{
    [TestClass]
    public class CsvUtilsTests
    {
        private CsvUtils _csvUtils;

        [TestInitialize]
        public void SetUp()
        {
            _csvUtils = new CsvUtils();
        }

        [TestMethod]
        [DynamicData(nameof(AsPocosExamples), DynamicDataSourceType.Method)]
        public void AsPocos_DeserializesSuppliedRowsIntoSuppliedType(string csv,
            Poco[] expectedInstances)
        {
            IEnumerable<Poco> actualInstances = _csvUtils.AsPocos<Poco>(csv);

            actualInstances
                .Should()
                .BeEquivalentTo(expectedInstances, 
                    cfg => cfg.WithStrictOrdering());
        }
        
        [TestMethod]
        [DynamicData(nameof(AsCsvExpandoExamples), DynamicDataSourceType.Method)]
        public void AsCsv_ProjectsSuppliedItemsIntoExpandoObjectRows(IEnumerable<dynamic> input, 
            string expectedOutput, 
            bool outputHeaders)
        {
            string actualCsvOutput = _csvUtils.AsCsv(input, outputHeaders);

            actualCsvOutput
                .Should()
                .Be(expectedOutput);
        }
        
        private static IEnumerable<object[]> AsPocosExamples()
        {
            yield return new object[]
            {
                "One,Two,Three\r\n1,two,21/12/2012\r\n2,three,21/12/2012\r\n",
                new[]
                {
                    NewPoco(1, "two", "21/12/2012"),
                    NewPoco(2, "three", "21/12/2012")
                }
            };
            yield return new object[]
            {
                null,
                new Poco[0]
            };
            yield return new object[]
            {
                "",
                new Poco[0]
            };
        }

        public class Poco
        {
            public int One { get; set; }
            
            public string Two { get; set; }
            
            public DateTimeOffset Three { get; set; }
            public override bool Equals(object obj)
            {
                return GetHashCode().Equals(obj?.GetHashCode());
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(One, Two, Three);
            }
        }

        private static Poco NewPoco(int one, string two, string three)
        {
            return new Poco
            {
                One = one,
                Two = two,
                Three = DateTimeOffset.ParseExact(three, "dd/MM/yyyy", CultureInfo.InvariantCulture)
            };
        }

        private static IEnumerable<object[]> AsCsvExpandoExamples()
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