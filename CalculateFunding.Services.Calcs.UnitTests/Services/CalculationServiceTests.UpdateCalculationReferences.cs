using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Calcs.Services
{
    public partial class CalculationServiceTests
    {
        [TestMethod]
        public void UpdateCalculationReferences_GivenSourceWithNoRefereences_ReturnsSourceCode()
        {
            //Arrange
            string soureCode = "return 5";

            string existingFunctionName = "Test Calc 1";

            string newFunctionName = "Test Calc 2";

            //Act
            string result = CalculationService.UpdateCalculationReferences(soureCode, existingFunctionName, newFunctionName);

            //Assert
            result
                .Should()
                .Be(soureCode);
        }

        [TestMethod]
        public void UpdateCalculationReferences_GivenSourceWithOneRefrenceToOldCalcs_ReturnsSourceCodeWithReplacedCalc()
        {
            //Arrange
            string soureCode = "return TestCalc1()";

            string excpectedSourceCode = "return TestCalc2()";

            string existingFunctionName = "Test Calc 1";

            string newFunctionName = "Test Calc 2";

            //Act
            string result = CalculationService.UpdateCalculationReferences(soureCode, existingFunctionName, newFunctionName);

            //Assert
            result
                .Should()
                .Be(excpectedSourceCode);
        }

        [TestMethod]
        public void UpdateCalculationReferences_GivenSourceWithOneRefrenceToOldCalcInAggregation_ReturnsSourceCodeWithReplacedCalc()
        {
            //Arrange
            string soureCode = "return Sum(TestCalc1)";

            string excpectedSourceCode = "return Sum(TestCalc2)";

            string existingFunctionName = "Test Calc 1";

            string newFunctionName = "Test Calc 2";

            //Act
            string result = CalculationService.UpdateCalculationReferences(soureCode, existingFunctionName, newFunctionName);

            //Assert
            result
                .Should()
                .Be(excpectedSourceCode);
        }

        [TestMethod]
        public void UpdateCalculationReferences_GivenSourceWithMultipleRefrenceToOldCalcs_ReturnsSourceCodeWithReplacedCalc()
        {
            //Arrange
            string soureCode = "return TestCalc1() + Max(TestCalc1) + TestCalc1";

            string excpectedSourceCode = "return TestCalc2() + Max(TestCalc2) + TestCalc2";

            string existingFunctionName = "Test Calc 1";

            string newFunctionName = "Test Calc 2";

            //Act
            string result = CalculationService.UpdateCalculationReferences(soureCode, existingFunctionName, newFunctionName);

            //Assert
            result
                .Should()
                .Be(excpectedSourceCode);
        }

        [TestMethod]
        public void UpdateCalculationReferences_GivenSourceWithMultipleRefrenceToOldCalcsInDifferentCase_ReturnsSourceCodeWithReplacedCalc()
        {
            //Arrange
            string soureCode = "return TesTCAlc1() + Max(TESTCALC1) + testcalc1";

            string excpectedSourceCode = "return TestCalc2() + Max(TestCalc2) + TestCalc2";

            string existingFunctionName = "Test Calc 1";

            string newFunctionName = "Test Calc 2";

            //Act
            string result = CalculationService.UpdateCalculationReferences(soureCode, existingFunctionName, newFunctionName);

            //Assert
            result
                .Should()
                .Be(excpectedSourceCode);
        }
    }
}
