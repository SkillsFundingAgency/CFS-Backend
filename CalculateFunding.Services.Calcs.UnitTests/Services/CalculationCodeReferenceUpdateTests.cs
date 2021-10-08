using System.Collections.Generic;
using CalculateFunding.Models.Calcs;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Calcs.UnitTests.Services
{
    [TestClass]
    public class CalculationCodeReferenceUpdateTests
    {
        [TestMethod]
        [DynamicData(nameof(CalculationNameChangeExamples), DynamicDataSourceType.Method)]
        public void RenameCalculation(string oldName,
            string newName,
            string fundingStreamId,
            CalculationNamespace calculationNamespace,
            string originalSource,
            string expectedSource)
        {
            Calculation calculation = new Calculation
            {
                FundingStreamId = fundingStreamId,
                Current = new CalculationVersion
                {
                    SourceCode = originalSource,
                    Namespace = calculationNamespace
                }
            };

            string actualSource = new CalculationCodeReferenceUpdate()
                .ReplaceSourceCodeReferences(calculation.Current.SourceCode, oldName, newName, calculation.Namespace);

            actualSource
                .Should()
                .Be(expectedSource);
        }

        private static IEnumerable<object[]> CalculationNameChangeExamples()
        {
            yield return new object[]
            {
                "MSSFinancialDisadvantageFunding",
                "MSSFinancialDisadvantageFundingNew",
                "PSG",
                CalculationNamespace.Additional,
                @"Dim ProvTypeMSS? As Decimal = Calculations.FundingFlagLAForHighNeeds()
Dim FD? As Decimal = PSG.mssFinancialDisadvantageFunding ()
Dim Travel? As Decimal = PSG.Calculations.mssFinancialDisadvantageFunding() + Calculations.mssFinancialDisadvantageFundingTest() + PSG.mssFinancialDisadvantageFunding()
If ProvTypeMSS.HasValue AndAlso FD.HasValue AndAlso Travel.HasValue Then Return FD + Travel
Return Exclude()",
                @"Dim ProvTypeMSS? As Decimal = Calculations.FundingFlagLAForHighNeeds()
Dim FD? As Decimal = PSG.mssFinancialDisadvantageFunding ()
Dim Travel? As Decimal = PSG.Calculations.MSSFinancialDisadvantageFundingNew() + Calculations.mssFinancialDisadvantageFundingTest() + PSG.mssFinancialDisadvantageFunding()
If ProvTypeMSS.HasValue AndAlso FD.HasValue AndAlso Travel.HasValue Then Return FD + Travel
Return Exclude()"
            };
            yield return new object[]
            {
                "MSSFinancialDisadvantageFunding",
                "MSSFinancialDisadvantageFundingNew",
                "PSG",
                CalculationNamespace.Additional,
                @"Dim ProvTypeMSS? As Decimal = Calculations.FundingFlagLAForHighNeeds()
Dim FD? As Decimal = PSG.mssFinancialDisadvantageFunding ()
Dim Travel? As Decimal = Calculations.mssFinancialDisadvantageFunding()
If ProvTypeMSS.HasValue AndAlso FD.HasValue AndAlso Travel.HasValue Then Return FD + Travel
Return Exclude()",
                @"Dim ProvTypeMSS? As Decimal = Calculations.FundingFlagLAForHighNeeds()
Dim FD? As Decimal = PSG.mssFinancialDisadvantageFunding ()
Dim Travel? As Decimal = Calculations.MSSFinancialDisadvantageFundingNew()
If ProvTypeMSS.HasValue AndAlso FD.HasValue AndAlso Travel.HasValue Then Return FD + Travel
Return Exclude()"
            };
            yield return new object[]
            {
                "MSSFinancialDisadvantageFunding",
                "MSSFinancialDisadvantageFundingNew",
                "PSG",
                CalculationNamespace.Additional,
                @"Dim ProvTypeMSS? As Decimal = Calculations.FundingFlagLAForHighNeeds()
Dim FD? As Decimal = Calculations.mssFinancialDisadvantageFunding ()
Dim Travel? As Decimal = Calculations.MSSStudentCostsTravelFunding()
If ProvTypeMSS.HasValue AndAlso FD.HasValue AndAlso Travel.HasValue Then Return FD + Travel
Return Exclude()",
                @"Dim ProvTypeMSS? As Decimal = Calculations.FundingFlagLAForHighNeeds()
Dim FD? As Decimal = Calculations.MSSFinancialDisadvantageFundingNew ()
Dim Travel? As Decimal = Calculations.MSSStudentCostsTravelFunding()
If ProvTypeMSS.HasValue AndAlso FD.HasValue AndAlso Travel.HasValue Then Return FD + Travel
Return Exclude()"
            };
            yield return new object[]
            {
                "MSSFinancialDisadvantageFunding",
                "MSSFinancialDisadvantageFundingNew",
                "PSG",
                CalculationNamespace.Template,
                @"Dim ProvTypeMSS? As Decimal = PSG.FundingFlagLAForHighNeeds()
Dim FD? As Decimal = psg. _
MSsfinancialdisadvantageFunding ()
Dim Travel? As Decimal = PSG.MSSStudentCostsTravelFunding()
If ProvTypeMSS.HasValue AndAlso FD.HasValue AndAlso Travel.HasValue Then Return FD + Travel
Return Exclude()",
                @"Dim ProvTypeMSS? As Decimal = PSG.FundingFlagLAForHighNeeds()
Dim FD? As Decimal = psg. _
MSSFinancialDisadvantageFundingNew ()
Dim Travel? As Decimal = PSG.MSSStudentCostsTravelFunding()
If ProvTypeMSS.HasValue AndAlso FD.HasValue AndAlso Travel.HasValue Then Return FD + Travel
Return Exclude()"
            };
        }
    }
}