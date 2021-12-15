using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Results.SqlExport;
using CalculateFunding.Services.SqlExport;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using CalcsApiCalculation = CalculateFunding.Common.ApiClient.Calcs.Models.Calculation;
using CalcsApiCalculationType = CalculateFunding.Common.ApiClient.Calcs.Models.CalculationType;

namespace CalculateFunding.Services.Results.UnitTests.SqlExport
{
    [TestClass]
    public class AdditionalCalculationsDataTableBuilderTests : DataTableBuilderTest<AdditionalCalculationsDataTableBuilder>
    {
        private string calculationIdOne;
        private string calculationIdTwo;
        private string calculationIdThree;
        private string calculationIdFour;

        private string calculationOneName;
        private string calculationTwoName;
        private string calculationThreeName;
        private string calculationFourName;

        [TestInitialize]
        public void SetUp()
        {
            calculationIdOne = "1";
            calculationIdTwo = "2";
            calculationIdThree = "3";
            calculationIdFour = "4";

            calculationOneName = "1";
            calculationTwoName = "2";
            calculationThreeName = "3";
            calculationFourName = "4";

            CalcsApiCalculation calculationOne = NewApiCalculation(_ => _.WithType(CalcsApiCalculationType.Additional)
                .WithId(calculationIdOne)
                .WithName(calculationOneName));
            CalcsApiCalculation calculationTwo = NewApiCalculation(_ => _.WithType(CalcsApiCalculationType.Additional)
                .WithId(calculationIdTwo)
                .WithName(calculationTwoName));
            CalcsApiCalculation calculationThree = NewApiCalculation(_ => _.WithType(CalcsApiCalculationType.Additional)
                .WithId(calculationIdThree)
                .WithName(calculationThreeName));
            CalcsApiCalculation calculationFour = NewApiCalculation(_ => _.WithType(CalcsApiCalculationType.Additional)
                .WithId(calculationIdFour)
                .WithName(calculationFourName));

            IEnumerable<CalcsApiCalculation> calculations = new[] { calculationOne, calculationTwo, calculationThree, calculationFour };
            DataTableBuilder = new AdditionalCalculationsDataTableBuilder(calculations, new SqlNameGenerator());
        }

        [TestMethod]
        public void MapsTemplateCalculationsIntoDataTable()
        {
            CalculationResult calculationResultOne = NewCalculationResult(_ => _.WithCalculation(NewReference(r => r.WithId(calculationIdOne.ToString()).WithName(calculationOneName))).WithValue(NewRandomNumber()));
            CalculationResult calculationResultTwo = NewCalculationResult(_ => _.WithCalculation(NewReference(r => r.WithId(calculationIdTwo.ToString()).WithName(calculationTwoName))).WithValue(NewRandomNumber()));
            CalculationResult calculationResultThree = NewCalculationResult(_ => _.WithCalculation(NewReference(r => r.WithId(calculationIdThree.ToString()).WithName(calculationThreeName))).WithValue(NewRandomNumber()));
            CalculationResult calculationResultFour = NewCalculationResult(_ => _.WithCalculation(NewReference(r => r.WithId(calculationIdFour.ToString()).WithName(calculationFourName))).WithValue(NewRandomNumber()));

            ProviderResult rowOne = NewProviderResult(_ => _.WithCalculationResults(calculationResultOne, calculationResultTwo).WithProviderSummary(NewProviderSummary()).WithSpecificationId(SpecificationId));
            ProviderResult rowTwo = NewProviderResult(_ => _.WithCalculationResults(calculationResultThree, calculationResultFour).WithProviderSummary(NewProviderSummary()).WithSpecificationId(SpecificationId));

            WhenTheRowsAreAdded(rowOne, rowTwo);

            ThenTheDataTableHasColumnsMatching(NewDataColumn<string>("ProviderId", maxLength: 128),
                NewDataColumn<decimal>($"Calc_{calculationIdOne}_{calculationOneName}", allowNull: true),
                NewDataColumn<decimal>($"Calc_{calculationIdTwo}_{calculationTwoName}", allowNull: true),
                NewDataColumn<decimal>($"Calc_{calculationIdThree}_{calculationThreeName}", allowNull: true),
                NewDataColumn<decimal>($"Calc_{calculationIdFour}_{calculationFourName}", allowNull: true));
            AndTheDataTableHasRowsMatching(
                NewRow(
                    rowOne.Provider.Id,
                    calculationResultOne.Value,
                    calculationResultTwo.Value),
                NewRow(
                    rowTwo.Provider.Id,
                    calculationResultThree.Value,
                    calculationResultFour.Value));
            AndTheTableNameIs($"[dbo].[{SpecificationId}_AdditionalCalculations]");
        }
    }
}
