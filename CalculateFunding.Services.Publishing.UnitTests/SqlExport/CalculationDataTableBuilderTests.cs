using System.Collections.Generic;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.SqlExport;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.SqlExport
{
    [TestClass]
    public class CalculationDataTableBuilderTests : DataTableBuilderTest<CalculationDataTableBuilder>
    {
        private Dictionary<uint, string> _calculationNames;

        private uint _templateCalculationIdOne;
        private string _calculationNameOne;
        private uint _templateCalculationIdTwo;
        private string _calculationNameTwo;
        private uint _templateCalculationIdThree;
        private string _calculationNameThree;

        [TestInitialize]
        public void SetUp()
        {
            _templateCalculationIdOne = NewRandomUnsignedNumber();
            _calculationNameOne = NewRandomString();
            _templateCalculationIdTwo = NewRandomUnsignedNumber();
            _calculationNameTwo = NewRandomString();
            _templateCalculationIdThree = NewRandomUnsignedNumber();
            _calculationNameThree = NewRandomString();

            _calculationNames = new Dictionary<uint, string>
            {
                {
                    _templateCalculationIdOne, _calculationNameOne
                },
                {
                    _templateCalculationIdTwo, _calculationNameTwo
                },
                {
                    _templateCalculationIdThree, _calculationNameThree
                }
            };

            DataTableBuilder = new CalculationDataTableBuilder(_calculationNames);
        }

        [TestMethod]
        public void MapsPaymentFundingLinesIntoDataTable()
        {
            decimal valueOne = NewRandomNumber();
            decimal valueTwo = NewRandomNumber();
            decimal valueThree = NewRandomNumber();
            decimal valueFour = NewRandomNumber();
            decimal valueFive = NewRandomNumber();
            decimal valueSix = NewRandomNumber();

            PublishedProviderVersion rowOne = NewPublishedProviderVersion(_ => _.WithFundingCalculations(
                    NewFundingCalculation(cal => cal.WithTemplateCalculationId(_templateCalculationIdOne)
                        .WithValue(valueOne)),
                    NewFundingCalculation(cal => cal.WithTemplateCalculationId(_templateCalculationIdTwo)
                        .WithValue(valueTwo)),
                    NewFundingCalculation(cal => cal.WithTemplateCalculationId(_templateCalculationIdThree)
                        .WithValue(valueThree)))
                .WithFundingStreamId(FundingStreamId)
                .WithFundingPeriodId(FundingPeriodId));
            PublishedProviderVersion rowTwo = NewPublishedProviderVersion(_ => _.WithFundingCalculations(
                    NewFundingCalculation(cal => cal.WithTemplateCalculationId(_templateCalculationIdOne)
                        .WithValue(valueFour)),
                    NewFundingCalculation(cal => cal.WithTemplateCalculationId(_templateCalculationIdTwo)
                        .WithValue(valueFive)),
                    NewFundingCalculation(cal => cal.WithTemplateCalculationId(_templateCalculationIdThree)
                        .WithValue(valueSix)))
                .WithFundingStreamId(FundingStreamId)
                .WithFundingPeriodId(FundingPeriodId));

            WhenTheRowsAreAdded(rowOne, rowTwo);

            ThenTheDataTableHasColumnsMatching(NewDataColumn<string>("PublishedProviderId", 128),
                NewDataColumn<decimal>($"Calc_{_templateCalculationIdOne}_{_calculationNameOne}", allowNull: true),
                NewDataColumn<decimal>($"Calc_{_templateCalculationIdTwo}_{_calculationNameTwo}", allowNull: true),
                NewDataColumn<decimal>($"Calc_{_templateCalculationIdThree}_{_calculationNameThree}", allowNull: true));
            AndTheDataTableHasRowsMatching(NewRow(rowOne.PublishedProviderId, valueOne, valueTwo, valueThree),
                NewRow(rowTwo.PublishedProviderId, valueThree, valueFour, valueFive));
            AndTheTableNameIs($"[dbo].[{FundingStreamId}_{FundingPeriodId}_Calculations]");
        }
    }
}