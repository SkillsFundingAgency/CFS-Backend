using CalculateFunding.Common.Models;
using CalculateFunding.Models.Scenarios;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestResult = CalculateFunding.Models.Scenarios.TestResult;


namespace CalculateFunding.Models.UnitTests
{
    [TestClass]
    public class TestScenarioResultTests
    {
        [TestMethod]
        public void TestScenarioResult_WhenPropertiesValid_ThenValidationPasses()
        {
            // Arrange
            TestScenarioResult result = new TestScenarioResult()
            {
                Provider = new Reference("Ref ID", "Reference Name"),
                Specification = new Reference("SpecificationId", "Specification Name"),
                TestResult = TestResult.Passed,
                TestScenario = new Reference("TestScenarioId", "Test Scenario Name")
            };

            // Act
            bool isValid = result.IsValid();

            // Assert
            isValid.Should().BeTrue();
        }

        [TestMethod]
        public void TestScenarioResult_WhenPropertiesAreInvalid_ThenValidIsFalse()
        {
            // Arrange
            TestScenarioResult result = new TestScenarioResult()
            {
            };

            // Act
            bool isValid = result.IsValid();

            // Assert
            isValid.Should().BeFalse();
        }
    }
}
