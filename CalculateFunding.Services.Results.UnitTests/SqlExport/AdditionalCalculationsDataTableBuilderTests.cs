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

            calculationOneName = "AA";
            calculationTwoName = "BB";
            calculationThreeName = "CC";
            calculationFourName = "DD";

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
            DataTableBuilder = new AdditionalCalculationsDataTableBuilder(calculations, new SqlNameGenerator(), SpecificationIdentifierName);
        }

        [TestMethod]
        public void MapsTemplateCalculationsIntoDataTable()
        {
            CalculationResult firstCalculationResultOne = NewCalculationResult(_ => _.WithCalculation(NewReference(r => r.WithId(calculationIdOne.ToString()).WithName(calculationOneName))).WithValue(NewRandomNumber()));
            CalculationResult firstCalculationResultTwo = NewCalculationResult(_ => _.WithCalculation(NewReference(r => r.WithId(calculationIdTwo.ToString()).WithName(calculationTwoName))).WithValue(NewRandomNumber()));
            CalculationResult firstCalculationResultThree = NewCalculationResult(_ => _.WithCalculation(NewReference(r => r.WithId(calculationIdThree.ToString()).WithName(calculationThreeName))).WithValue(NewRandomNumber()));
            CalculationResult firstCalculationResultFour = NewCalculationResult(_ => _.WithCalculation(NewReference(r => r.WithId(calculationIdFour.ToString()).WithName(calculationFourName))).WithValue(NewRandomNumber()));

            CalculationResult secondCalculationResultOne = NewCalculationResult(_ => _.WithCalculation(NewReference(r => r.WithId(calculationIdOne.ToString()).WithName(calculationOneName))).WithValue(NewRandomNumber()));
            CalculationResult secondCalculationResultTwo = NewCalculationResult(_ => _.WithCalculation(NewReference(r => r.WithId(calculationIdTwo.ToString()).WithName(calculationTwoName))).WithValue(NewRandomNumber()));
            CalculationResult secondCalculationResultThree = NewCalculationResult(_ => _.WithCalculation(NewReference(r => r.WithId(calculationIdThree.ToString()).WithName(calculationThreeName))).WithValue(NewRandomNumber()));
            CalculationResult secondCalculationResultFour = NewCalculationResult(_ => _.WithCalculation(NewReference(r => r.WithId(calculationIdFour.ToString()).WithName(calculationFourName))).WithValue(NewRandomNumber()));

            ProviderResult rowOne = NewProviderResult(_ => _.WithCalculationResults(firstCalculationResultFour, firstCalculationResultThree, firstCalculationResultTwo, firstCalculationResultOne).WithProviderSummary(NewProviderSummary()).WithSpecificationId(SpecificationId));
            ProviderResult rowTwo = NewProviderResult(_ => _.WithCalculationResults(secondCalculationResultOne, secondCalculationResultTwo, secondCalculationResultThree, secondCalculationResultFour).WithProviderSummary(NewProviderSummary()).WithSpecificationId(SpecificationId));

            WhenTheRowsAreAdded(rowOne, rowTwo);

            ThenTheDataTableHasColumnsMatching(NewDataColumn<string>("ProviderId", maxLength: 128),
                NewDataColumn<decimal>($"Calc_{calculationIdOne}_{calculationOneName}", allowNull: true),
                NewDataColumn<decimal>($"Calc_{calculationIdTwo}_{calculationTwoName}", allowNull: true),
                NewDataColumn<decimal>($"Calc_{calculationIdThree}_{calculationThreeName}", allowNull: true),
                NewDataColumn<decimal>($"Calc_{calculationIdFour}_{calculationFourName}", allowNull: true));
            AndTheDataTableHasRowsMatching(
                NewRow(
                    rowOne.Provider.Id,
                    firstCalculationResultOne.Value,
                    firstCalculationResultTwo.Value,
                    firstCalculationResultThree.Value,
                    firstCalculationResultFour.Value),
                NewRow(
                    rowTwo.Provider.Id,
                    secondCalculationResultOne.Value,
                    secondCalculationResultTwo.Value,
                    secondCalculationResultThree.Value,
                    secondCalculationResultFour.Value));
            AndTheTableNameIs($"[dbo].[{SpecificationIdentifierName}_AdditionalCalculations]");
        }
    }
}
