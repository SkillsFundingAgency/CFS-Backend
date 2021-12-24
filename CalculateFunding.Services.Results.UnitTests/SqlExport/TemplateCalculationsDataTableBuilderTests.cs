﻿using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Results.SqlExport;
using CalculateFunding.Services.SqlExport;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using CalcsApiCalculation = CalculateFunding.Common.ApiClient.Calcs.Models.Calculation;
using CalcsApiCalculationType = CalculateFunding.Common.ApiClient.Calcs.Models.CalculationType;
using TemplateMetadataCalculation = CalculateFunding.Common.TemplateMetadata.Models.Calculation;

namespace CalculateFunding.Services.Results.UnitTests.SqlExport
{
    [TestClass]
    public class TemplateCalculationsDataTableBuilderTests : DataTableBuilderTest<TemplateCalculationsDataTableBuilder>
    {
        private string calculationIdOne;
        private string calculationIdTwo;
        private string calculationIdThree;
        private string calculationIdFour;

        private uint templateMetadataCalculationIdOne;
        private uint templateMetadataCalculationIdTwo;
        private uint templateMetadataCalculationIdThree;
        private uint templateMetadataCalculationIdFour;

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

            templateMetadataCalculationIdOne = 1;
            templateMetadataCalculationIdTwo = 2;
            templateMetadataCalculationIdThree = 3;
            templateMetadataCalculationIdFour = 4;

            calculationOneName = "1";
            calculationTwoName = "2";
            calculationThreeName = "3";
            calculationFourName = "4";

            CalcsApiCalculation calculationOne = NewApiCalculation(_ => _.WithType(CalcsApiCalculationType.Template)
                .WithId(calculationIdOne)
                .WithName(calculationOneName));
            CalcsApiCalculation calculationTwo = NewApiCalculation(_ => _.WithType(CalcsApiCalculationType.Template)
                .WithId(calculationIdTwo)
                .WithName(calculationTwoName));
            CalcsApiCalculation calculationThree = NewApiCalculation(_ => _.WithType(CalcsApiCalculationType.Template)
                .WithId(calculationIdThree)
                .WithName(calculationThreeName));
            CalcsApiCalculation calculationFour = NewApiCalculation(_ => _.WithType(CalcsApiCalculationType.Template)
                .WithId(calculationIdFour)
                .WithName(calculationFourName));
            IEnumerable<CalcsApiCalculation> calculations = new[] { 
                calculationOne, 
                calculationTwo, 
                calculationThree, 
                calculationFour };

            TemplateMetadataCalculation templateMetadataCalculationOne = NewTemplateMetadataCalculation(_ => 
                _.WithTemplateCalculationId(templateMetadataCalculationIdOne).WithName(calculationOneName));
            TemplateMetadataCalculation templateMetadataCalculationTwo = NewTemplateMetadataCalculation(_ =>
                _.WithTemplateCalculationId(templateMetadataCalculationIdTwo).WithName(calculationTwoName));
            TemplateMetadataCalculation templateMetadataCalculationThree = NewTemplateMetadataCalculation(_ =>
                _.WithTemplateCalculationId(templateMetadataCalculationIdThree).WithName(calculationThreeName));
            TemplateMetadataCalculation templateMetadataCalculationFour = NewTemplateMetadataCalculation(_ =>
                _.WithTemplateCalculationId(templateMetadataCalculationIdFour).WithName(calculationFourName));

            IEnumerable<TemplateMetadataCalculation> templateMetadataCalculations = new[] { 
                templateMetadataCalculationOne,
                templateMetadataCalculationTwo,
                templateMetadataCalculationThree,
                templateMetadataCalculationFour };
            DataTableBuilder = new TemplateCalculationsDataTableBuilder(calculations, new SqlNameGenerator(), templateMetadataCalculations);
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
                NewDataColumn<decimal>($"Calc_{templateMetadataCalculationIdOne}_{calculationOneName}", allowNull: true),
                NewDataColumn<decimal>($"Calc_{templateMetadataCalculationIdTwo}_{calculationTwoName}", allowNull: true),
                NewDataColumn<decimal>($"Calc_{templateMetadataCalculationIdThree}_{calculationThreeName}", allowNull: true),
                NewDataColumn<decimal>($"Calc_{templateMetadataCalculationIdFour}_{calculationFourName}", allowNull: true));
            AndTheDataTableHasRowsMatching(
                NewRow(
                    rowOne.Provider.Id,
                    calculationResultOne.Value,
                    calculationResultTwo.Value),
                NewRow(
                    rowTwo.Provider.Id,
                    calculationResultThree.Value,
                    calculationResultFour.Value));
            AndTheTableNameIs($"[dbo].[{SpecificationId}_TemplateCalculations]");
        }
    }
}
