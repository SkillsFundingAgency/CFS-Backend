using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.ProviderLegacy;
using CalculateFunding.Services.CalcEngine;
using CalculateFunding.Services.CalcEngine.Interfaces;
using CalculateFunding.Services.CalcEngine.UnitTests;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Serilog;
using Funding = CalculateFunding.Services.CalcEngine.Funding;

namespace CalculateFunding.Services.Calculator
{
    [TestClass]
    public class CalculationEngineTests
    {
        const string ProviderId = "12345";
        const string ProviderName = "Awesome School";

        [TestMethod]
        public void CalculateProviderResult_WhenAllocationModelThrowsException_ShouldThrowException()
        {
            // Arrange
            CalculationEngine calcEngine = CreateCalculationEngine();

            IDictionary<string, Funding> fundingLines = new Dictionary<string, Funding>();

            IAllocationModel mockAllocationModel = Substitute.For<IAllocationModel>();
            mockAllocationModel
                .Execute(Arg.Any<List<ProviderSourceDataset>>(), Arg.Any<ProviderSummary>(),
                    Arg.Any<IDictionary<string, Funding>>())
                .Throws(new DivideByZeroException());

            List<CalculationSummaryModel> models = new List<CalculationSummaryModel>
            {
                new CalculationSummaryModel(){CalculationType = CalculationType.Template, Name = "Test", Id = "Test"}
            };

            ProviderSummary provider = new ProviderSummary();
            List<ProviderSourceDataset> sourceDataset = new List<ProviderSourceDataset>();

            // Act
            Action calculateProviderResultMethod = () =>
            {
                calcEngine.CalculateProviderResults(mockAllocationModel, CreateBuildProject().SpecificationId, models, provider, sourceDataset,
                    fundingLines);
            };

            // Assert
            calculateProviderResultMethod
                .Should()
                .ThrowExactly<DivideByZeroException>();
        }

        [TestMethod]
        public void CalculateProviderResult_WhenCalculationsAreNull_ShouldReturnResultWithEmptyCalculations()
        {
            // Arrange
            IAllocationModel mockAllocationModel = Substitute.For<IAllocationModel>();
            mockAllocationModel
                .Execute(Arg.Any<List<ProviderSourceDataset>>(), Arg.Any<ProviderSummary>(),
                    Arg.Any<IDictionary<string, Funding>>())
                .Returns(new List<CalculationResult>());

            CalculationEngine calculationEngine = CreateCalculationEngine();
            ProviderSummary providerSummary = CreateDummyProviderSummary();
            BuildProject buildProject = CreateBuildProject();

            // Act
            ProviderResult result = calculationEngine.CalculateProviderResults(mockAllocationModel, buildProject.SpecificationId, null,
                providerSummary, new List<ProviderSourceDataset>(),
                Arg.Any<IDictionary<string, Funding>>());

            // Assert
            result.CalculationResults.Should().BeEmpty();
            result.Provider.Should().Be(providerSummary);
            result.SpecificationId.Should().BeEquivalentTo(buildProject.SpecificationId);
            result.Id.Should().BeEquivalentTo(GenerateId(providerSummary.Id, buildProject.SpecificationId));
        }

        [TestMethod]
        public void CalculateProviderResult_WhenCalculationsAreNotEmpty_ShouldReturnCorrectResult()
        {
            // Arrange
            List<Reference> policySpecificationsForFundingCalc = new List<Reference>()
            {
                new Reference("Spec1", "SpecOne"),
                new Reference("Spec2", "SpecTwo")
            };

            IDictionary<string, Funding> fundingLines = new Dictionary<string, Funding>();

            Reference fundingCalcReference = new Reference("CalcF1", "Funding calc 1");

            Reference numbercalcReference = new Reference("CalcF2", "Funding calc 2");

            Reference booleancalcReference = new Reference("CalcF3", "Funding calc 3");

            CalculationResult fundingCalcReturned = new CalculationResult()
            {
                CalculationType = CalculationType.Template,
                Calculation = fundingCalcReference,
                Value = 10000,
                CalculationDataType = CalculationDataType.Decimal
            };
            CalculationResult fundingCalcReturned2 = new CalculationResult()
            {
                CalculationType = CalculationType.Template,
                Calculation = numbercalcReference,
                Value = 20000,
                CalculationDataType = CalculationDataType.Decimal
            };
            CalculationResult fundingCalcReturned3 = new CalculationResult()
            {
                CalculationType = CalculationType.Template,
                Calculation = booleancalcReference,
                Value = true,
                CalculationDataType = CalculationDataType.Boolean
            };

            List<CalculationResult> calculationResults = new List<CalculationResult>()
            {
                fundingCalcReturned,
                fundingCalcReturned2,
                fundingCalcReturned3
            };
            IAllocationModel mockAllocationModel = Substitute.For<IAllocationModel>();
            mockAllocationModel
                .Execute(Arg.Any<List<ProviderSourceDataset>>(), Arg.Any<ProviderSummary>(),
                    Arg.Any<IDictionary<string, Funding>>())
                .Returns(calculationResults);

            CalculationEngine calculationEngine = CreateCalculationEngine();
            ProviderSummary providerSummary = CreateDummyProviderSummary();
            BuildProject buildProject = CreateBuildProject();

            var nonMatchingCalculationModel = new CalculationSummaryModel()
            {
                Id = "Non matching calculation",
                Name = "Non matching calculation",
                CalculationType = CalculationType.Template,
                CalculationValueType = CalculationValueType.Number
            };
            IEnumerable<CalculationSummaryModel> calculationSummaryModels = new[]
            {
                new CalculationSummaryModel()
                {
                    Id = fundingCalcReference.Id,
                    Name = fundingCalcReference.Name,
                    CalculationType = CalculationType.Template,
                    CalculationValueType = CalculationValueType.Number
                },
                new CalculationSummaryModel()
                {
                    Id = numbercalcReference.Id,
                    Name = numbercalcReference.Name,
                    CalculationType = CalculationType.Template,
                    CalculationValueType = CalculationValueType.Number
                },
                new CalculationSummaryModel()
                {
                    Id = booleancalcReference.Id,
                    Name = booleancalcReference.Name,
                    CalculationType = CalculationType.Template,
                    CalculationValueType = CalculationValueType.Boolean
                },
                nonMatchingCalculationModel
            };

            // Act
            var calculateProviderResults = calculationEngine.CalculateProviderResults(mockAllocationModel, buildProject.SpecificationId, calculationSummaryModels,
                providerSummary, new List<ProviderSourceDataset>(),
                fundingLines);
            ProviderResult result = calculateProviderResults;

            // Assert
            result.Provider.Should().Be(providerSummary);
            result.SpecificationId.Should().BeEquivalentTo(buildProject.SpecificationId);
            result.Id.Should().BeEquivalentTo(GenerateId(providerSummary.Id, buildProject.SpecificationId));
            result.CalculationResults.Should().HaveCount(4);

            CalculationResult fundingCalcResult = result.CalculationResults.First(cr => cr.Calculation.Id == fundingCalcReference.Id);
            fundingCalcResult.Calculation.Should().BeEquivalentTo(fundingCalcReference);
            fundingCalcResult.CalculationType.Should().BeEquivalentTo(fundingCalcReturned.CalculationType);
            fundingCalcResult.Value.Should().Be(fundingCalcReturned.Value);
            fundingCalcResult.CalculationDataType.Should().Be(CalculationDataType.Decimal);

            CalculationResult numberCalcResult = result.CalculationResults.First(cr => cr.Calculation.Id == numbercalcReference.Id);
            numberCalcResult.Calculation.Should().BeEquivalentTo(numbercalcReference);
            numberCalcResult.CalculationType.Should().BeEquivalentTo(fundingCalcReturned2.CalculationType);
            numberCalcResult.Value.Should().Be(fundingCalcReturned2.Value);
            numberCalcResult.CalculationDataType.Should().Be(CalculationDataType.Decimal);

            CalculationResult booleanCalcResult = result.CalculationResults.First(cr => cr.Calculation.Id == booleancalcReference.Id);
            booleanCalcResult.Calculation.Should().BeEquivalentTo(booleancalcReference);
            booleanCalcResult.CalculationType.Should().BeEquivalentTo(fundingCalcReturned3.CalculationType);
            booleanCalcResult.Value.Should().Be(fundingCalcReturned3.Value);
            booleanCalcResult.CalculationDataType.Should().Be(CalculationDataType.Boolean);

            CalculationResult nonMatchingCalcResult = result.CalculationResults.First(cr => cr.Calculation.Id == "Non matching calculation");
            nonMatchingCalcResult.Calculation.Should().BeEquivalentTo(new Reference(nonMatchingCalculationModel.Id, nonMatchingCalculationModel.Name));
            nonMatchingCalcResult.CalculationType.Should().BeEquivalentTo(nonMatchingCalculationModel.CalculationType);
            nonMatchingCalcResult.Value.Should().BeNull();
            nonMatchingCalcResult.CalculationDataType.Should().Be(CalculationDataType.Decimal);
        }

        private static string GenerateId(string providerId, string specificationId)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{providerId}-{specificationId}"));
        }

        private static CalculationEngine CreateCalculationEngine(
            IAllocationFactory allocationFactory = null,
            ICalculationsRepository calculationsRepository = null,
            ILogger logger = null
            )
        {
            return new CalculationEngine(
                allocationFactory ?? CreateAllocationFactory(),
                logger ?? CreateLogger()
                );
        }

        private static ProviderSummary CreateDummyProviderSummary()
        {
            return new ProviderSummary()
            {
                Name = ProviderName,
                UKPRN = ProviderId,
                Id = "KH18778"
            };
        }

        static IAllocationFactory CreateAllocationFactory()
        {
            IAllocationFactory allocationFactory = Substitute.For<IAllocationFactory>();

            return allocationFactory;
        }

        static BuildProject CreateBuildProject()
        {
            BuildProject buildProject = JsonConvert.DeserializeObject<BuildProject>(MockData.SerializedBuildProject());

            return buildProject;
        }

        private static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }
    }
}
