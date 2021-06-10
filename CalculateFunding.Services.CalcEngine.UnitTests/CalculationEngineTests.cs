using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Aggregations;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.ProviderLegacy;
using CalculateFunding.Services.CalcEngine;
using CalculateFunding.Services.CalcEngine.Interfaces;
using CalculateFunding.Services.CalcEngine.UnitTests;
using CalculateFunding.Services.Core.Interfaces.Logging;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Serilog;

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

            IAllocationModel mockAllocationModel = Substitute.For<IAllocationModel>();
            mockAllocationModel
                .Execute(Arg.Any<Dictionary<string, ProviderSourceDataset>>(), Arg.Any<ProviderSummary>())
                .Throws(new DivideByZeroException());

            List<CalculationSummaryModel> models = new List<CalculationSummaryModel>
            {
                new CalculationSummaryModel(){CalculationType = CalculationType.Template, Name = "Test", Id = "Test"}
            };

            ProviderSummary provider = new ProviderSummary();
            Dictionary<string, ProviderSourceDataset> sourceDataset = new Dictionary<string, ProviderSourceDataset>();

            // Act
            Action calculateProviderResultMethod = () =>
            {
                calcEngine.CalculateProviderResults(mockAllocationModel, CreateBuildProject().SpecificationId, models, provider, sourceDataset);
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
                .Execute(
                    Arg.Any<Dictionary<string, ProviderSourceDataset>>(), 
                    Arg.Any<ProviderSummary>(),
                    Arg.Any<IEnumerable<CalculationAggregation>>())
                .Returns(new CalculationResultContainer
                {
                    FundingLineResults =  new List<FundingLineResult>()
                });

            CalculationEngine calculationEngine = CreateCalculationEngine();
            ProviderSummary providerSummary = CreateDummyProviderSummary();
            BuildProject buildProject = CreateBuildProject();

            // Act
            ProviderResult result = calculationEngine.CalculateProviderResults(
                mockAllocationModel, 
                buildProject.SpecificationId, 
                null,
                providerSummary, 
                new Dictionary<string, ProviderSourceDataset>());

            // Assert
            result.CalculationResults.Should().BeNull();
            result.Provider.Should().Be(providerSummary);
            result.SpecificationId.Should().BeEquivalentTo(buildProject.SpecificationId);
            result.Id.Should().BeEquivalentTo(GenerateId(providerSummary.Id, buildProject.SpecificationId));
            result.IsIndicativeProvider.Should().BeFalse();
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

            Reference fundingCalcReference = new Reference("CalcF1", "Funding calc 1");

            Reference numbercalcReference = new Reference("CalcF2", "Funding calc 2");

            Reference booleancalcReference = new Reference("CalcF3", "Funding calc 3");

            Reference fundingLineCalcReference = new Reference("FL1", "Funding line calc 1");

            CalculationResult fundingCalcReturned = new CalculationResult()
            {
                Calculation = fundingCalcReference,
                Value = 10000
            };
            CalculationResult fundingCalcReturned2 = new CalculationResult()
            {
                Calculation = numbercalcReference,
                Value = 20000
            };
            CalculationResult fundingCalcReturned3 = new CalculationResult()
            {
                Calculation = booleancalcReference,
                Value = true
            };

            CalculationResultContainer calculationResultContainer = new CalculationResultContainer();

            List<CalculationResult> calculationResults = new List<CalculationResult>()
            {
                fundingCalcReturned,
                fundingCalcReturned2,
                fundingCalcReturned3
            };
            
            calculationResultContainer.CalculationResults = calculationResults;

            string fundingStreamId = "FS1";

            FundingLineResult fundingLineResult = new FundingLineResult
            {
                Value = 1000,
                FundingLine = fundingLineCalcReference,
                FundingLineFundingStreamId = fundingStreamId
            };

            List<FundingLineResult> fundingLineResults = new List<FundingLineResult>
            {
                fundingLineResult
            };

            calculationResultContainer.FundingLineResults = fundingLineResults;

            IAllocationModel mockAllocationModel = Substitute.For<IAllocationModel>();
            mockAllocationModel
                .Execute(Arg.Any<Dictionary<string, ProviderSourceDataset>>(), Arg.Any<ProviderSummary>())
                .Returns(calculationResultContainer);

            IEnumerable<string> indicativeOpenerProviderStatuses = new string[] { "StatusOne", "StatusTwo" };

            CalculationEngine calculationEngine = CreateCalculationEngine();
            ProviderSummary providerSummary = CreateDummyProviderSummary("StatusTwo");
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
                providerSummary, new Dictionary<string, ProviderSourceDataset>(), indicativeOpenerProviderStatuses: indicativeOpenerProviderStatuses);
            ProviderResult result = calculateProviderResults;

            // Assert
            result.Provider.Should().Be(providerSummary);
            result.SpecificationId.Should().BeEquivalentTo(buildProject.SpecificationId);
            result.Id.Should().BeEquivalentTo(GenerateId(providerSummary.Id, buildProject.SpecificationId));
            result.CalculationResults.Should().HaveCount(3);
            result.FundingLineResults.Should().HaveCount(1);
            result.IsIndicativeProvider.Should().BeTrue();

            CalculationResult fundingCalcResult = result.CalculationResults.First(cr => cr.Calculation.Id == fundingCalcReference.Id);
            fundingCalcResult.Calculation.Should().BeEquivalentTo(fundingCalcReference);
            fundingCalcResult.CalculationType.Should().BeEquivalentTo(CalculationType.Template);
            fundingCalcResult.Value.Should().Be(fundingCalcReturned.Value);
            fundingCalcResult.CalculationDataType.Should().Be(CalculationDataType.Decimal);

            CalculationResult numberCalcResult = result.CalculationResults.First(cr => cr.Calculation.Id == numbercalcReference.Id);
            numberCalcResult.Calculation.Should().BeEquivalentTo(numbercalcReference);
            numberCalcResult.CalculationType.Should().BeEquivalentTo(CalculationType.Template);
            numberCalcResult.Value.Should().Be(fundingCalcReturned2.Value);
            numberCalcResult.CalculationDataType.Should().Be(CalculationDataType.Decimal);

            CalculationResult booleanCalcResult = result.CalculationResults.First(cr => cr.Calculation.Id == booleancalcReference.Id);
            booleanCalcResult.Calculation.Should().BeEquivalentTo(booleancalcReference);
            booleanCalcResult.CalculationType.Should().BeEquivalentTo(CalculationType.Template);
            booleanCalcResult.Value.Should().Be(fundingCalcReturned3.Value);
            booleanCalcResult.CalculationDataType.Should().Be(CalculationDataType.Boolean);

            FundingLineResult fundingLineCalcResult = result.FundingLineResults.First(cr => cr.FundingLine.Id == fundingLineCalcReference.Id);
            fundingLineCalcResult.FundingLine.Should().BeEquivalentTo(fundingLineCalcReference);
            fundingLineCalcResult.Value.Should().Be(fundingLineResult.Value);
            fundingLineCalcResult.FundingLineFundingStreamId.Should().Be(fundingStreamId);
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
                logger ?? CreateLogger(),
                CreateTelemetry());
        }

        private static ProviderSummary CreateDummyProviderSummary(string status = null)
        {
            return new ProviderSummary()
            {
                Name = ProviderName,
                UKPRN = ProviderId,
                Id = "KH18778",
                Status = status
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

        private static ITelemetry CreateTelemetry()
        {
            return Substitute.For<ITelemetry>();
        }
    }
}
