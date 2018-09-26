using CalculateFunding.Models;
using CalculateFunding.Models.Gherkin;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Services.TestRunner.Vocab.Calculation;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CalculateFunding.Services.TestRunner.UnitTests.Vocab
{
    [TestClass]
    public class GivenSourceFieldTests
    {
        [TestMethod]
        public void Execute_GivenProviderSourceDatasetIsNull_ReturnsParseResultWithError()
        {
            //Arrange
            ProviderResult providerResult = new ProviderResult
            {
                Provider = new ProviderSummary
                {
                    Id = "p1"
                }

            };

            IEnumerable<ProviderSourceDataset> datasets = new List<ProviderSourceDataset>();

            GivenSourceField givenSourceField = new GivenSourceField
            {
                DatasetName = "ds1",
                FieldName = "f1",
                Operator = ComparisonOperator.GreaterThan,
                Value = "5"
            };

            //Act
            GherkinParseResult parseResult = givenSourceField.Execute(providerResult, datasets);

            //Assert
            parseResult
                .Abort
                .Should()
                .BeFalse();

            parseResult
                .HasErrors
                .Should()
                .BeTrue();

            parseResult
                .Errors
                .First()
                .ErrorMessage
                .Should()
                .Be("f1 in ds1 was not found");
        }
    }
}
