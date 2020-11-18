using CalculateFunding.Services.Publishing.SqlExport;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.SqlExport
{
    [TestClass]
    public class SqlNameGeneratorTests
    {
        private SqlNameGenerator _sqlNameGenerator;

        [TestInitialize]
        public void SetUp()
        {
            _sqlNameGenerator = new SqlNameGenerator();
        }

        [TestMethod]
        [DataRow("<>%£=+", "LessThanGreaterThanPercentPoundEqualsPlus")]
        [DataRow("sentences use pascal casing", "SentencesUsePascalCasing")]
        [DataRow("1 23 ", "_123")]
        [DataRow("", null)]
        [DataRow(null, null)]
        [DataRow("   ", null)]
        public void CleansSuppliedNamesIntoValidSchemaObjectNames(string input,
            string expectedOutput)
        {
            TheGeneratedIdentifierFor(input)
                .Should()
                .Be(expectedOutput);
        }

        private string TheGeneratedIdentifierFor(string input) => _sqlNameGenerator.GenerateIdentifier(input);
    }
}