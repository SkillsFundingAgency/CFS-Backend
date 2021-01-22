using CalculateFunding.Services.CodeGeneration.VisualBasic.Type;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.CodeMetadataGenerator.Vb.UnitTests
{
    [TestClass]
    public class VbTypeGeneratorTests
    {
        [TestMethod]
        [DataRow("Range 3", "Range3")]
        [DataRow("Range < 3", "RangeLessThan3")]
        [DataRow("Range > 3", "RangeGreaterThan3")]
        [DataRow("Range £ 3", "RangePound3")]
        [DataRow("Range % 3", "RangePercent3")]
        [DataRow("Range = 3", "RangeEquals3")]
        [DataRow("Range + 3", "RangePlus3")]
        [DataRow("Nullable(Of Decimal)", "Nullable(Of Decimal)")]
        [DataRow("Nullable(Of Integer)", "Nullable(Of Integer)")]
        public void GenerateIdentifier_IdentifiersSubstituted(string input, string expected)
        {
            // Act
            string result = new VisualBasicTypeIdentifierGenerator().GenerateIdentifier(input);

            // Assert
            result
                .Should()
                .Be(expected);
        }
    }
}
