using CalculateFunding.Services.CodeGeneration.VisualBasic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.CodeMetadataGenerator.Vs.UnitTests
{
    [TestClass]
    public class CalculationTypeGeneratorTests
    {
        [TestMethod]
        public void WhenLessThanSymbolInName_ThenIdentifierIsSubsititued()
        {
            // Arrange
            string inputName = "Range < 3";

            // Act
            string result = CalculationTypeGenerator.GenerateIdentifier(inputName);

            // Assert
            result
                .Should()
                .Be("RangeLessThan3");
        }

        [TestMethod]
        public void WhenGreaterThanSymbolInName_ThenIdentifierIsSubsititued()
        {
            // Arrange
            string inputName = "Range > 3";

            // Act
            string result = CalculationTypeGenerator.GenerateIdentifier(inputName);

            // Assert
            result
                .Should()
                .Be("RangeGreaterThan3");
        }

        [TestMethod]
        public void WhenPoundSymbolInName_ThenIdentifierIsSubsititued()
        {
            // Arrange
            string inputName = "Range £ 3";

            // Act
            string result = CalculationTypeGenerator.GenerateIdentifier(inputName);

            // Assert
            result
                .Should()
                .Be("RangePound3");
        }

        [TestMethod]
        public void WhenPercentSymbolInName_ThenIdentifierIsSubsititued()
        {
            // Arrange
            string inputName = "Range % 3";

            // Act
            string result = CalculationTypeGenerator.GenerateIdentifier(inputName);

            // Assert
            result
                .Should()
                .Be("RangePercent3");
        }

        [TestMethod]
        public void WhenEqualsSymbolInName_ThenIdentifierIsSubsititued()
        {
            // Arrange
            string inputName = "Range = 3";

            // Act
            string result = CalculationTypeGenerator.GenerateIdentifier(inputName);

            // Assert
            result
                .Should()
                .Be("RangeEquals3");
        }

        [TestMethod]
        public void WhenPlusSymbolInName_ThenIdentifierIsSubsititued()
        {
            // Arrange
            string inputName = "Range + 3";

            // Act
            string result = CalculationTypeGenerator.GenerateIdentifier(inputName);

            // Assert
            result
                .Should()
                .Be("RangePlus3");
        }
    }
}
