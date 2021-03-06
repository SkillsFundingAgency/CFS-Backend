using System;
using System.Collections.Generic;
using CalculateFunding.Common.TemplateMetadata.Enums;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.SqlExport;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.SqlExport
{
    [TestClass]
    public class CalculationDataTableBuilderTests : DataTableBuilderTest<CalculationDataTableBuilder>
    {
        private uint _templateCalculationIdOne;
        private string _calculationNameOne;
        private uint _templateCalculationIdTwo;
        private string _calculationNameTwo;
        private uint _templateCalculationIdThree;
        private string _calculationNameThree;
        private uint _templateCalculationIdFour;
        private string _calculationNameFour;
        private uint _templateCalculationIdFive;
        private string _calculationNameFive;

        private IEnumerable<Calculation> _templateCalculations;

        [TestInitialize]
        public void SetUp()
        {
            _templateCalculationIdOne = NewRandomUnsignedNumber();
            _calculationNameOne = NewRandomString();
            _templateCalculationIdTwo = NewRandomUnsignedNumber();
            _calculationNameTwo = NewRandomString();
            _templateCalculationIdThree = NewRandomUnsignedNumber();
            _calculationNameThree = NewRandomString();
            _templateCalculationIdFour = NewRandomUnsignedNumber();
            _calculationNameFour = NewRandomString();
            _templateCalculationIdFive = NewRandomUnsignedNumber();
            _calculationNameFive = NewRandomString();

            _templateCalculations = AsArray(NewTemplateCalculation(_ => _.WithTemplateCalculationId(_templateCalculationIdOne)
                    .WithName(_calculationNameOne)
                    .WithValueFormat(CalculationValueFormat.Boolean)),
                NewTemplateCalculation(),
                NewTemplateCalculation(_ => _.WithTemplateCalculationId(_templateCalculationIdFive)
                    .WithName(_calculationNameFive)
                    .WithValueFormat(CalculationValueFormat.Number)),
                NewTemplateCalculation(),
                NewTemplateCalculation(_ => _.WithTemplateCalculationId(_templateCalculationIdFour)
                    .WithName(_calculationNameFour)
                    .WithValueFormat(CalculationValueFormat.Percentage)),
                NewTemplateCalculation(),
                NewTemplateCalculation(_ => _.WithTemplateCalculationId(_templateCalculationIdTwo)
                    .WithName(_calculationNameTwo)
                    .WithValueFormat(CalculationValueFormat.String)),
                NewTemplateCalculation(),
                NewTemplateCalculation(_ => _.WithTemplateCalculationId(_templateCalculationIdThree)
                    .WithName(_calculationNameThree)
                    .WithValueFormat(CalculationValueFormat.Currency)));

            DataTableBuilder = new CalculationDataTableBuilder(_templateCalculations);
        }

        [TestMethod]
        public void MapsPaymentFundingLinesIntoDataTable()
        {
            bool valueOne = NewRandomFlag();
            string valueTwo = NewRandomString();
            decimal valueThree = NewRandomNumber();
            decimal valueFour = NewRandomNumber();
            decimal valueFive = NewRandomNumber();
            
            bool valueSix = NewRandomFlag();
            string valueSeven = NewRandomString();
            decimal valueEight = NewRandomNumber();
            decimal valueNine = NewRandomNumber();
            decimal valueTen = NewRandomNumber();

            PublishedProviderVersion rowOne = NewPublishedProviderVersion(_ => _.WithFundingCalculations(
                    NewFundingCalculation(cal => cal.WithTemplateCalculationId(_templateCalculationIdOne)
                        .WithValue(valueOne)),
                    NewFundingCalculation(cal => cal.WithTemplateCalculationId(_templateCalculationIdTwo)
                        .WithValue(valueTwo)),
                    NewFundingCalculation(cal => cal.WithTemplateCalculationId(_templateCalculationIdThree)
                        .WithValue(valueThree)),
                    NewFundingCalculation(cal => cal.WithTemplateCalculationId(_templateCalculationIdFour)
                        .WithValue(valueFour)),
                    NewFundingCalculation(cal => cal.WithTemplateCalculationId(_templateCalculationIdFive)
                        .WithValue(valueFive)))
                .WithFundingStreamId(FundingStreamId)
                .WithFundingPeriodId(FundingPeriodId));
            PublishedProviderVersion rowTwo = NewPublishedProviderVersion(_ => _.WithFundingCalculations(
                    NewFundingCalculation(cal => cal.WithTemplateCalculationId(_templateCalculationIdOne)
                        .WithValue(valueSix)),
                    NewFundingCalculation(cal => cal.WithTemplateCalculationId(_templateCalculationIdTwo)
                        .WithValue(valueSeven)),
                    NewFundingCalculation(cal => cal.WithTemplateCalculationId(_templateCalculationIdThree)
                        .WithValue(valueEight)),
                    NewFundingCalculation(cal => cal.WithTemplateCalculationId(_templateCalculationIdFour)
                        .WithValue(valueNine)),
                    NewFundingCalculation(cal => cal.WithTemplateCalculationId(_templateCalculationIdFive)
                        .WithValue(valueTen)))
                .WithFundingStreamId(FundingStreamId)
                .WithFundingPeriodId(FundingPeriodId));

            WhenTheRowsAreAdded(rowOne, rowTwo);

            ThenTheDataTableHasColumnsMatching(NewDataColumn<string>("PublishedProviderId", 128),
                NewDataColumn<bool>($"Calc_{_templateCalculationIdOne}_{_calculationNameOne}", allowNull: true),
                NewDataColumn<string>($"Calc_{_templateCalculationIdTwo}_{_calculationNameTwo}", maxLength: 128, allowNull: true),
                NewDataColumn<decimal>($"Calc_{_templateCalculationIdThree}_{_calculationNameThree}", allowNull: true),
                NewDataColumn<decimal>($"Calc_{_templateCalculationIdFour}_{_calculationNameFour}", allowNull: true),
                NewDataColumn<decimal>($"Calc_{_templateCalculationIdFive}_{_calculationNameFive}", allowNull: true));
            AndTheDataTableHasRowsMatching(NewRow(rowOne.PublishedProviderId, valueOne, valueTwo, valueThree),
                NewRow(rowTwo.PublishedProviderId, valueThree, valueFour, valueFive));
            AndTheTableNameIs($"[dbo].[{FundingStreamId}_{FundingPeriodId}_Calculations]");
        }

        private Calculation NewTemplateCalculation(Action<TemplateCalculationBuilder> setUp = null)
        {
            TemplateCalculationBuilder templateCalculationBuilder = new TemplateCalculationBuilder();

            setUp?.Invoke(templateCalculationBuilder);
            
            return templateCalculationBuilder.Build();
        }
    }
}