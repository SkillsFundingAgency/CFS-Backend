using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.CalcEngine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Serilog;
using System.Collections.Generic;
using System.Reflection;
using CalculateFunding.Models.ProviderLegacy;
using System;
using CalculateFunding.Models.Calcs;
using System.Linq;
using FluentAssertions;

namespace CalculateFunding.Services.Calculator
{
    [TestClass]
    public class AllocationModelTests
    {
        private AllocationModel _allocationModel;
        private Mock<ILogger> _logger;

        [TestInitialize]
        public void SetUp()
        {
            _logger = new Mock<ILogger>();
        }

        // Assembly Source Code
        //Dim DtOp as Date? = provider.DateOpened
        //Dim result as Decimal?
        //If Provider.DateOpened.hasvalue then
        //    DtOp = Provider.DateOpened
        //End If
        //If DtOp >= #09/01/2021# and 
        //    DtOp <= #08/31/2022# then 
        //    result = 1 
        //else 
        //    If DtOp >= #09/01/2020# and 
        //        DtOp <= #08/31/2021# then 
        //        result = 2 
        //    else
        //        If DtOp >= #09/01/2019# and 
        //            DtOp <= #08/31/2020# then 
        //            result = 3 
        //        Else
        //            result = 0
        //        End if
        //    End if
        //End If
        //Return result

        [DataTestMethod]
        [DynamicData("TestMethodInput")]
        public void Execute_ExecutesCodeReturningResultByCalculatingProviderDateOpened_ReturnedCalculatedResult(DateTime dateOpened, int expectedResult)
        {
            string assemblyFilePath = "Resources\\implementation-test-dateopened.dll";

            _allocationModel = new AllocationModel(SetupAssembly(assemblyFilePath), _logger.Object);

            IDictionary<string, ProviderSourceDataset> datasets = new Dictionary<string, ProviderSourceDataset>
            {
                { "", new ProviderSourceDataset{ } }
            };

            ProviderSummary providerSummary = new ProviderSummary
            {
                DateOpened = dateOpened
            };

            CalculationResultContainer calculationResultContainer =
                _allocationModel.Execute(datasets, providerSummary, null);

            calculationResultContainer
                .CalculationResults
                .SingleOrDefault(_ => _.Calculation.Id == "694381ba-66f5-4005-aba2-8ff28d333086")
                .Value
                .Should()
                .BeOfType<decimal>()
                .And
                .Be(expectedResult);
        }

        public static IEnumerable<object[]> TestMethodInput
        {
            get
            {
                return new[]
                {
                    new object[] { new DateTime(2022, 1, 1), 1 },
                    new object[] { new DateTime(2021, 1, 1), 2 },
                    new object[] { new DateTime(2020, 1, 1), 3 },
                    new object[] { new DateTime(2019, 1, 1), 0 },
                };
            }
        }

        private Assembly SetupAssembly(string assemblyFilePath)
        {
            return Assembly.LoadFrom(assemblyFilePath);
        }
    }
}
