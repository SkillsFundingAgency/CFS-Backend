using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CalculateFunding.Services.Compiler.UnitTests
{
    [TestClass]
    public class SourceCodeHelpersTests
    {
        [TestMethod]
        public void GetReferencedCalculations_GivenListOfCalcsAndSourceCodeContainsTwoCalcs_ReturnsTwoCalcs()
        {
            //Arrange
            string sourceCode = " calc1() + calc2()";

            IEnumerable<string> calcNames = new[] { "calc1", "calc2", "calc3" };

            //Act
            IEnumerable<string> results = SourceCodeHelpers.GetReferencedCalculations(calcNames, sourceCode);

            //Assert
            results
                .Count()
                .Should()
                .Be(2);

            results
                .ElementAt(0)
                .Should()
                .Be("calc1");

            results
                .ElementAt(1)
                .Should()
                .Be("calc2");
        }

        [TestMethod]
        public void GetReferencedCalculations_GivenListOfCalcsAndSourceCodeDoesNotContainCalc_ReturnsEmptyList()
        {
            //Arrange
            string sourceCode = " calc4() + calc5()";

            IEnumerable<string> calcNames = new[] { "calc1", "calc2", "calc3" };

            //Act
            IEnumerable<string> results = SourceCodeHelpers.GetReferencedCalculations(calcNames, sourceCode);

            //Assert
            results
                .Any()
                .Should()
                .BeFalse();
        }

        [TestMethod]
        public void GetReferencedCalculations_GivenAnEmptySourceCodeString_ReturnsEmptyList()
        {
            //Arrange
            string sourceCode = "";

            IEnumerable<string> calcNames = new[] { "calc1", "calc2", "calc3" };

            //Act
            IEnumerable<string> results = SourceCodeHelpers.GetReferencedCalculations(calcNames, sourceCode);

            //Assert
            results
                .Any()
                .Should()
                .BeFalse();
        }

        [TestMethod]
        public void IsCalcfReferencedInAnAggregate_GivenCalcIsNotReferencedInAnAggregate_ReturnsFalse()
        {
            //Arrange
            Dictionary<string, string> functions = new Dictionary<string, string>
            {
                { "Calc1","Return Calc2()" },
                { "Calc2","Return 1" },
                { "Calc3","Return Sum(Calc2) + 5" },
                { "Calc4","Return Sum(Calc1) + Avg(Calc3) + 5" }
            };

            string calcToCheck = "Calc4";

            //Act
            bool result = SourceCodeHelpers.IsCalcReferencedInAnAggregate(functions, calcToCheck);

            //Assert
            result
                .Should()
                .BeFalse();
        }

        [TestMethod]
        public void IsCalcfReferencedInAnAggregate_GivenCalcIsReferencedInAnAggregate_ReturnsTrue()
        {
            //Arrange
            Dictionary<string, string> functions = new Dictionary<string, string>
            {
                { "Calc1","Return Calc2()" },
                { "Calc2","Return 1" },
                { "Calc3","Return Sum(Calc2) + 5" },
                { "Calc4","Return Sum(Calc1) + Avg(Calc3) + 5" }
            };

            string calcToCheck = "Calc3";

            //Act
            bool result = SourceCodeHelpers.IsCalcReferencedInAnAggregate(functions, calcToCheck);

            //Assert
            result
                .Should()
                .BeTrue();
        }


        [TestMethod]
        public void IsCalcReferencedInAnAggregate_GivenCalcIsReferencedInAnAggregate_ReturnsTrue1()
        {
            //Arrange
            Dictionary<string, string> functions = new Dictionary<string, string>
            {
                { "Calc1","Return Calc2() + Sum(Calc6)" },
                { "Calc2","Return 1" },
                { "Calc3","Return Sum(Calc2) + 5" },
                { "Calc4","Return Sum(Calc1) + Avg(Calc3) + 5" },
                { "Calc5","Return Calc1()" },
                { "Calc6","Return Calc5()" },
            };

            string calcToCheck = "Calc6";

            //Act
            bool result = SourceCodeHelpers.IsCalcReferencedInAnAggregate(functions, calcToCheck);

            //Assert
            result
                .Should()
                .BeTrue();
        }

        [TestMethod]
        public void CheckSourceForExistingCalculationAggregates_GivenCalcIsReferencedInAnAggregateWithDeepNesting_ReturnsTrue()
        {
            //Arrange
            Dictionary<string, string> functions = new Dictionary<string, string>
            {
                { "Calc1","Return Calc2() + Sum(Calc6)" },
                { "Calc2","Return 1" },
                { "Calc3","Return Sum(Calc2) + 5" },
                { "Calc4","Return Sum(Calc1) + Avg(Calc3) + 5" },
                { "Calc5","Return Calc1()" },
                { "Calc6","Return Calc5()" },
                { "Calc7","Return Calc6()" }
            };

            string calcToCheck = "Return Calc7()";

            //Act
            bool result = SourceCodeHelpers.CheckSourceForExistingCalculationAggregates(functions, calcToCheck);

            //Assert
            result
                .Should()
                .BeTrue();
        }

        [TestMethod]
        public void CheckSourceForExistingCalculationAggregates_GivenCalcIsReferencedInAnAggregateWithDeepNestingAndFurtherLevels_ReturnsTrue()
        {
            //Arrange
            Dictionary<string, string> functions = new Dictionary<string, string>
            {
                { "Calc1","Return Calc2() + Sum(Calc6)" },
                { "Calc2","Return 1" },
                { "Calc3","Return Sum(Calc2) + 5" },
                { "Calc4","Return Sum(Calc2) + Avg(Calc2) + Sum(Calc3)" },
                { "Calc5","Return Calc1()" },
                { "Calc6","Return Calc5()" },
                { "Calc7","Return Calc6()" }
            };

            string calcToCheck = "Return Sum(Calc2) + Avg(Calc2) + Sum(Calc3)";

            //Act
            bool result = SourceCodeHelpers.CheckSourceForExistingCalculationAggregates(functions, calcToCheck);

            //Assert
            result
                .Should()
                .BeTrue();
        }

        [TestMethod]
        public void CheckSourceForExistingCalculationAggregates_GivenCalcIsReferencedInAnAggregateWithDeepNestingAndFurtherLevelsAndIncludeWhitespaceInAggregateCall_ReturnsTrue()
        {
            //Arrange
            Dictionary<string, string> functions = new Dictionary<string, string>
            {
                { "Calc1","Return Calc2() + Min( Calc6)" },
                { "Calc2","Return 1" },
                { "Calc3","Return Sum(Calc2) + 5" },
                { "Calc4","Return Sum(Calc2) + Avg(Calc2) + Min( Calc3)" },
                { "Calc5","Return Calc1()" },
                { "Calc6","Return Calc5()" },
                { "Calc7","Return Calc6()" }
            };

            string calcToCheck = "Return Sum(Calc2) + Avg(Calc2) + Sum(Calc3)";

            //Act
            bool result = SourceCodeHelpers.CheckSourceForExistingCalculationAggregates(functions, calcToCheck);

            //Assert
            result
                .Should()
                .BeTrue();
        }

        [TestMethod]
        public void CheckSourceForExistingCalculationAggregates_GivenCalcIsReferencedInAnAggregateAndAlsoContainsCalcReferenceThatIncludesAvgInTheName_ReturnsTrue()
        {
            //Arrange
            Dictionary<string, string> functions = new Dictionary<string, string>
            {
                { "Calc1","Return Calc2() + Min( Calc6)" },
                { "Calc2","Return 1" },
                { "Calc3","Return Sum(Calc2) + 5" },
                { "Calc4","Return Sum(Calc2) + Avg(Calc2) + Min( Calc3)" },
                { "Calc5","Return CalcAvg1()" },
                { "Calc6","Return Calc5()" },
                { "Calc7","Return Calc6()" }
            };

            string calcToCheck = "Return Sum(Calc2) + Avg(Calc2) + Sum(Calc3) + CalcAvg1()";

            //Act
            bool result = SourceCodeHelpers.CheckSourceForExistingCalculationAggregates(functions, calcToCheck);

            //Assert
            result
                .Should()
                .BeTrue();
        }

        [TestMethod]
        public void CheckSourceForExistingCalculationAggregates_GivenCalcContainsCalcReferenceThatIncludesAvgInTheNameButNoOtherAggregateRefrences_ReturnsFalse()
        {
            //Arrange
            Dictionary<string, string> functions = new Dictionary<string, string>
            {
                { "Calc1","Return Calc2() + Min( Calc6)" },
                { "Calc2","Return 1" },
                { "Calc3","Return Sum(Calc2) + 5" },
                { "Calc4","Return Sum(Calc2) + Avg(Calc2) + Min( Calc3)" },
                { "Calc5","Return CalcAvg1()" },
                { "Calc6","Return Calc5()" },
                { "Calc7","Return Calc6()" }
            };

            string calcToCheck = "Return CalcAvg1() = Calc6()";

            //Act
            bool result = SourceCodeHelpers.CheckSourceForExistingCalculationAggregates(functions, calcToCheck);

            //Assert
            result
                .Should()
                .BeFalse();
        }

        [TestMethod]
        public void GetDatasetAggregateFunctionParameters_GivenSourceHasOneDatasetParameter_ReturnsOneParameter()
        {
            //Arrange
            string sourceCode = "Return Sum(Datasets.Testing1) + Sum(Calc1)";

            //Act
            IEnumerable<string> aggregateParameters = SourceCodeHelpers.GetDatasetAggregateFunctionParameters(sourceCode);

            //Assert
            aggregateParameters
                .Count()
                .Should()
                .Be(1);

            aggregateParameters
                .First()
                .Should()
                .Be("Datasets.Testing1");
        }

        [TestMethod]
        public void GetDatasetAggregateFunctionParameters_GivenSourceHasTwoDatasetParameters_ReturnsTwoParameters()
        {
            //Arrange
            string sourceCode = "Return Sum(Datasets.Testing1) + Sum(Datasets.Testing2) + Sum(Calc1)";

            //Act
            IEnumerable<string> aggregateParameters = SourceCodeHelpers.GetDatasetAggregateFunctionParameters(sourceCode);

            //Assert
            aggregateParameters
                .Count()
                .Should()
                .Be(2);

            aggregateParameters
                .ElementAt(0)
                .Should()
                .Be("Datasets.Testing1");

            aggregateParameters
                .ElementAt(1)
                .Should()
                .Be("Datasets.Testing2");
        }

        [TestMethod]
        public void GetDatasetAggregateFunctionParameters_GivenSourceHasZeroDatasetParameters_ReturnsEmptyList()
        {
            //Arrange
            string sourceCode = "Return Sum(Calc1)";

            //Act
            IEnumerable<string> aggregateParameters = SourceCodeHelpers.GetDatasetAggregateFunctionParameters(sourceCode);

            //Assert
            aggregateParameters
                .Should()
                .BeEmpty();
        }

        [TestMethod]
        public void GetCalculationAggregateFunctionParameters_GivenSourceHasOneCalculationParameter_ReturnsOneParameter()
        {
            //Arrange
            string sourceCode = "Return Sum(Datasets.Testing1) + Sum(Calc1)";

            //Act
            IEnumerable<string> aggregateParameters = SourceCodeHelpers.GetCalculationAggregateFunctionParameters(sourceCode);

            //Assert
            aggregateParameters
                .Count()
                .Should()
                .Be(1);

            aggregateParameters
                .First()
                .Should()
                .Be("Calc1");
        }

        [TestMethod]
        public void GetCalculationAggregateFunctionParameters_GivenSourceHasTwoCalculationParameters_ReturnsTwoParameters()
        {
            //Arrange
            string sourceCode = "Return Sum(Datasets.Testing1) + Avg(Calc1) + Sum(Calc2)";

            //Act
            IEnumerable<string> aggregateParameters = SourceCodeHelpers.GetCalculationAggregateFunctionParameters(sourceCode);

            //Assert
            aggregateParameters
                .Count()
                .Should()
                .Be(2);

            aggregateParameters
                .ElementAt(0)
                .Should()
                .Be("Calc1");

            aggregateParameters
                .ElementAt(1)
                .Should()
                .Be("Calc2");
        }

        [TestMethod]
        public void GetCalculationAggregateFunctionParameters_GivenSourceHasZeroDatasetParameters_ReturnsEmptyList()
        {
            //Arrange
            string sourceCode = "Return Sum(Datasets.Testing1)";

            //Act
            IEnumerable<string> aggregateParameters = SourceCodeHelpers.GetCalculationAggregateFunctionParameters(sourceCode);

            //Assert
            aggregateParameters
                .Should()
                .BeEmpty();
        }

        [TestMethod]
        public void HasCalculationAggregateFunctionParameters_GivenSourceContainsAggregateParameter_ReturnsTrue()
        {
            //Arrange
            IEnumerable<string> sourceCodes = new[]
            {
                "Return Sum(Datasets.Testing1",
                "Return Sum(Datasets.Testing1) + Avg(Calc1) + Sum(Calc2)"
            };

            //Act
            bool result = SourceCodeHelpers.HasCalculationAggregateFunctionParameters(sourceCodes);

            //Assert
            result
                .Should()
                .BeTrue();
        }

        [TestMethod]
        public void HasCalculationAggregateFunctionParameters_GivenSourceDoesNotContainAggregateParameter_ReturnsFalse()
        {
            //Arrange
            IEnumerable<string> sourceCodes = new[]
            {
                "Return Sum(Datasets.Testing1",
                "Return Sum(Datasets.Testing2)"
            };

            //Act
            bool result = SourceCodeHelpers.HasCalculationAggregateFunctionParameters(sourceCodes);

            //Assert
            result
                .Should()
                .BeFalse();
        }

        [TestMethod]
        public void HasCalculationAggregateFunctionParameters_GivenSourceDoesNotContainAggregateParameterButCalcNameContainsAggregateFunction_ReturnsFalse()
        {
            //Arrange
            IEnumerable<string> sourceCodes = new[]
            {
                "Return TestSum()"
            };

            //Act
            bool result = SourceCodeHelpers.HasCalculationAggregateFunctionParameters(sourceCodes);

            //Assert
            result
                .Should()
                .BeFalse();
        }

        [TestMethod]
        public void HasCalculationAggregateFunctionParameters_GivenSourceDoesNotContainAggregateParameterButCalcNameStartsWithAggregateFunction_ReturnsFalse()
        {
            //Arrange
            IEnumerable<string> sourceCodes = new[]
            {
                "Return SumTest()"
            };

            //Act
            bool result = SourceCodeHelpers.HasCalculationAggregateFunctionParameters(sourceCodes);

            //Assert
            result
                .Should()
                .BeFalse();
        }

        [TestMethod]
        public void HasCalculationAggregateFunctionParameters_GivenSourceDoesNotContainAggregateParameterButCalcNameContainsAggregateFunctionCaseInsensitive_ReturnsFalse()
        {
            //Arrange
            IEnumerable<string> sourceCodes = new[]
            {
                "Return TESTSUM()"
            };

            //Act
            bool result = SourceCodeHelpers.HasCalculationAggregateFunctionParameters(sourceCodes);

            //Assert
            result
                .Should()
                .BeFalse();
        }

        [TestMethod]
        public void HasCalculationAggregateFunctionParameters_GivenSourceContainsAggregateParameterCaseInsensitive_ReturnsTrue()
        {
            //Arrange
            IEnumerable<string> sourceCodes = new[]
            {
                "Return SuM(Datasets.Testing1",
                "Return sUm(Datasets.Testing1) + AVG(Calc1) + SUm(Calc2)"
            };

            //Act
            bool result = SourceCodeHelpers.HasCalculationAggregateFunctionParameters(sourceCodes);

            //Assert
            result
                .Should()
                .BeTrue();
        }

        [TestMethod]
        public void HasCalculationAggregateFunctionParameters_GivenSourceContainsAggregateParameterCaseInsensitiveAndSpaces_ReturnsTrue()
        {
            //Arrange
            IEnumerable<string> sourceCodes = new[]
            {
                //"Return SuM    (Datasets.Testing1)",
                "Return mIn(Datasets.Testing1) + AVG(Calc1) + SuM   (Calc2)"
            };

            //Act
            bool result = SourceCodeHelpers.HasCalculationAggregateFunctionParameters(sourceCodes);

            //Assert
            result
                .Should()
                .BeTrue();
        }
    }
}
