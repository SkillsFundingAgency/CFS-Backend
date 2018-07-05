using CalculateFunding.Models.Gherkin;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Services.TestRunner.Vocab.Calculation;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.TestRunner.UnitTests.Vocab
{
    [TestClass]
    public class AndProviderIsTests
    {
        [TestMethod]
        public void Execute_GivenLogicResultReturnsTrue_ReturnsParseResultWithoutAborting()
        {
            //Arrange
            ProviderResult providerResult = new ProviderResult
            {
                Provider = new ProviderSummary
                {
                    Id = "p1"
                }

            };

            IEnumerable<ProviderSourceDatasetCurrent> datasets = new List<ProviderSourceDatasetCurrent>();

            AndProviderIs andProviderIs = new AndProviderIs { ProviderId = "p1", Operator = ComparisonOperator.EqualTo };

            //Act
            GherkinParseResult parseResult = andProviderIs.Execute(providerResult, datasets);

            //Assert
            parseResult
                .Abort
                .Should()
                .BeFalse();
        }

        [TestMethod]
        public void Execute_GivenLogicResultReturnsFalse_ReturnsParseResulAndAbort()
        {
            //Arrange
            ProviderResult providerResult = new ProviderResult
            {
                Provider = new ProviderSummary
                {
                    Id = "p1"
                }

            };

            IEnumerable<ProviderSourceDatasetCurrent> datasets = new List<ProviderSourceDatasetCurrent>();

            AndProviderIs andProviderIs = new AndProviderIs { ProviderId = "p2", Operator = ComparisonOperator.EqualTo };

            //Act
            GherkinParseResult parseResult = andProviderIs.Execute(providerResult, datasets);

            //Assert
            parseResult
                .Abort
                .Should()
                .BeTrue();
        }
    }
}
