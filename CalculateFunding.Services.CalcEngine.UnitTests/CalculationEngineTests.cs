using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Calculator.Interfaces;
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
        const string CalculationId = "a82fc74c-59c5-4df7-a3cd-1fbf492d89bf";

        [TestMethod]
        public void GenerateAllocations_GivenModelExecuteThrowsException_ThrowsException()
        {
            //Arrange
            BuildProject buildProject = CreateBuildProject();
            buildProject.Build.Assembly = MockData.GetMockAssembly();

            IEnumerable<ProviderSummary> providers = new[]
            {
                new ProviderSummary{ Id = ProviderId, Name = ProviderName }
            };

            IEnumerable<ProviderSourceDataset> datasets = new[]
            {
                new ProviderSourceDataset()
            };

            Func<string, string, Task<IEnumerable<ProviderSourceDataset>>> func = (s, p) =>
            {
                return Task.FromResult(datasets);
            };

            IEnumerable<CalculationResult> calculationResults = new[]
            {
                new CalculationResult
                {
                    Calculation = new Reference { Id = CalculationId }
                }
            };

            IAllocationModel allocationModel = Substitute.For<IAllocationModel>();
            allocationModel
                .When(x => x.Execute(Arg.Any<List<ProviderSourceDataset>>(), Arg.Any<ProviderSummary>()))
                .Do(x => { throw new Exception(); });

            IAllocationFactory allocationFactory = Substitute.For<IAllocationFactory>();
            allocationFactory
                .CreateAllocationModel(Arg.Any<Assembly>())
                .Returns(allocationModel);

            ILogger logger = CreateLogger();

            CalculationEngine calculationEngine = CreateCalculationEngine(allocationFactory, logger: logger);

            //Act
            Func<Task> test = () => calculationEngine.GenerateAllocations(buildProject, providers, func);

            //Assert
            test
                .ShouldThrowExactly<Exception>();
        }

        [TestMethod]
        async public Task GenerateAllocations_GivenBuildProject_Runs()
        {
            //Arrange
            BuildProject buildProject = CreateBuildProject();
            buildProject.Build.Assembly = MockData.GetMockAssembly();

            IEnumerable<ProviderSummary> providers = new[]
            {
                new ProviderSummary{ Id = ProviderId, Name = ProviderName }
            };

            IEnumerable<ProviderSourceDataset> datasets = new[]
            {
                new ProviderSourceDataset()
            };

            Func<string, string, Task<IEnumerable<ProviderSourceDataset>>> func = (s, p) =>
            {
                return Task.FromResult(datasets);
            };

            IEnumerable<CalculationResult> calculationResults = new[]
            {
                new CalculationResult
                {
                    Calculation = new Reference { Id = CalculationId },
                }
            };

            IAllocationModel allocationModel = Substitute.For<IAllocationModel>();
            allocationModel
                .Execute(Arg.Any<List<ProviderSourceDataset>>(), Arg.Any<ProviderSummary>())
                .Returns(calculationResults);

            IAllocationFactory allocationFactory = Substitute.For<IAllocationFactory>();
            allocationFactory
                .CreateAllocationModel(Arg.Any<Assembly>())
                .Returns(allocationModel);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            List<CalculationSummaryModel> calculations = new List<CalculationSummaryModel>()
            {
                new CalculationSummaryModel()
                {
                    Id = CalculationId,
                },
                new CalculationSummaryModel()
                {
                    Id = "calc2",
                },
                new CalculationSummaryModel()
                {
                    Id = "calc3",
                }
            };

            calculationsRepository
                .GetCalculationSummariesForSpecification(Arg.Any<string>())
                .Returns(calculations);

            CalculationEngine calculationEngine = CreateCalculationEngine(allocationFactory, calculationsRepository, logger: logger);

            //Act
            IEnumerable<ProviderResult> results = await calculationEngine.GenerateAllocations(buildProject, providers, func);

            //Assert
            results
                .Count()
                .Should()
                .Be(1);

            results
                .First()
                .CalculationResults
                .Count
                .Should()
                .Be(3);
        }

        [TestMethod]
        async public Task GenerateAllocations_GivenBuildProject_RunsAndMergesAllocationLineResults()
        {
            //Arrange
            BuildProject buildProject = CreateBuildProject();
            buildProject.Build.Assembly = MockData.GetMockAssembly();

            IEnumerable<ProviderSummary> providers = new[]
            {
                new ProviderSummary{ Id = ProviderId, Name = ProviderName }
            };

            IEnumerable<ProviderSourceDataset> datasets = new[]
            {
                new ProviderSourceDataset()
            };

            Func<string, string, Task<IEnumerable<ProviderSourceDataset>>> func = (s, p) =>
            {
                return Task.FromResult(datasets);
            };

            Reference allocationLine1 = new Reference("al1", "Allocation Line 1");
            Reference allocationLine2 = new Reference("al2", "Allocation Line 2");


            IEnumerable<CalculationResult> calculationResults = new[]
            {
                new CalculationResult
                {
                    Calculation = new Reference { Id = CalculationId },
                     AllocationLine = allocationLine1,
                     Value = 3,
                },
                 new CalculationResult
                {
                    Calculation = new Reference { Id = "calc2" },
                     AllocationLine = allocationLine1,
                     Value = 5,
                },
                  new CalculationResult
                {
                    Calculation = new Reference { Id = "calc3" },
                     AllocationLine = allocationLine2,
                     Value = 7,
                }
            };

            IAllocationModel allocationModel = Substitute.For<IAllocationModel>();
            allocationModel
                .Execute(Arg.Any<List<ProviderSourceDataset>>(), Arg.Any<ProviderSummary>())
                .Returns(calculationResults);

            IAllocationFactory allocationFactory = Substitute.For<IAllocationFactory>();
            allocationFactory
                .CreateAllocationModel(Arg.Any<Assembly>())
                .Returns(allocationModel);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            List<CalculationSummaryModel> calculations = new List<CalculationSummaryModel>()
            {
                new CalculationSummaryModel()
                {
                    Id = CalculationId,
                     CalculationType= CalculationType.Funding,
                },
                new CalculationSummaryModel()
                {
                    Id = "calc2",
                    CalculationType = CalculationType.Funding,
                },
                new CalculationSummaryModel()
                {
                    Id = "calc3",
                    CalculationType = CalculationType.Funding,
                }
            };

            calculationsRepository
                .GetCalculationSummariesForSpecification(Arg.Any<string>())
                .Returns(calculations);

            CalculationEngine calculationEngine = CreateCalculationEngine(allocationFactory, calculationsRepository, logger: logger);

            //Act
            IEnumerable<ProviderResult> results = await calculationEngine.GenerateAllocations(buildProject, providers, func);

            //Assert
            results
                .Count()
                .Should()
                .Be(1);

            results
                .First()
                .CalculationResults
                .Count
                .Should()
                .Be(3);


            results
                .First()
                .AllocationLineResults
                .Should()
                .HaveCount(2);

        }

        [TestMethod]
        async public Task GenerateAllocations_GivenBuildProjectWithNoCalculations_Runs()
        {
            //Arrange
            BuildProject buildProject = CreateBuildProject();
            buildProject.Build.Assembly = MockData.GetMockAssembly();

            IEnumerable<ProviderSummary> providers = new[]
            {
                new ProviderSummary{ Id = ProviderId, Name = ProviderName }
            };

            IEnumerable<ProviderSourceDataset> datasets = new[]
            {
                new ProviderSourceDataset()
            };

            Func<string, string, Task<IEnumerable<ProviderSourceDataset>>> func = (s, p) =>
            {
                return Task.FromResult(datasets);
            };

            IEnumerable<CalculationResult> calculationResults = Enumerable.Empty<CalculationResult>();

            IAllocationModel allocationModel = Substitute.For<IAllocationModel>();
            allocationModel
                .Execute(Arg.Any<List<ProviderSourceDataset>>(), Arg.Any<ProviderSummary>())
                .Returns(calculationResults);

            IAllocationFactory allocationFactory = Substitute.For<IAllocationFactory>();
            allocationFactory
                .CreateAllocationModel(Arg.Any<Assembly>())
                .Returns(allocationModel);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            List<CalculationSummaryModel> calculations = new List<CalculationSummaryModel>()
            {
            };

            calculationsRepository
                .GetCalculationSummariesForSpecification(Arg.Any<string>())
                .Returns(calculations);

            CalculationEngine calculationEngine = CreateCalculationEngine(allocationFactory, calculationsRepository, logger: logger);

            //Act
            IEnumerable<ProviderResult> results = await calculationEngine.GenerateAllocations(buildProject, providers, func);

            //Assert
            results
                .Count()
                .Should()
                .Be(1);

            results
                .First()
                .CalculationResults
                .Count
                .Should()
                .Be(0);

            logger
                .Received(1)
                .Information(Arg.Is("There are no calculations to executed for specification ID {specificationId}"), Arg.Is(buildProject.SpecificationId));
        }

        [TestMethod]
        public void CalculateProviderResult_WhenAllocationModelThrowsException_ShouldThrowException()
        {
            // Arrange
            CalculationEngine calcEngine = CreateCalculationEngine();

            IAllocationModel mockAllocationModel = Substitute.For<IAllocationModel>();
            mockAllocationModel
                .Execute(Arg.Any<List<ProviderSourceDataset>>(), Arg.Any<ProviderSummary>())
                .Throws(new DivideByZeroException());

            List<CalculationSummaryModel> models = new List<CalculationSummaryModel>
            {
                new CalculationSummaryModel(){CalculationType = CalculationType.Number, Name = "Test", Id = "Test"}
            };

            ProviderSummary provider = new ProviderSummary();
            List<ProviderSourceDataset> sourceDataset = new List<ProviderSourceDataset>();

            // Act
            Action calculateProviderResultMethod = () =>
            {
                calcEngine.CalculateProviderResults(mockAllocationModel, CreateBuildProject(), models, provider, sourceDataset);
            };

            // Assert
            calculateProviderResultMethod.ShouldThrow<Exception>();
        }

        [TestMethod]
        public void CalculateProviderResult_WhenCalculationsAreNull_ShouldReturnResultWithEmptyCalculations()
        {
            // Arrange
            IAllocationModel mockAllocationModel = Substitute.For<IAllocationModel>();
            mockAllocationModel
                .Execute(Arg.Any<List<ProviderSourceDataset>>(), Arg.Any<ProviderSummary>())
                .Returns(new List<CalculationResult>());

            CalculationEngine calculationEngine = CreateCalculationEngine();
            ProviderSummary providerSummary = CreateDummyProviderSummary();
            BuildProject buildProject = CreateBuildProject();

            // Act
            ProviderResult result = calculationEngine.CalculateProviderResults(mockAllocationModel, buildProject, null,
                providerSummary, new List<ProviderSourceDataset>());

            // Assert
            result.CalculationResults.Should().BeEmpty();
            result.AllocationLineResults.Should().BeEmpty();
            result.Provider.Should().Be(providerSummary);
            result.SpecificationId.ShouldBeEquivalentTo(buildProject.SpecificationId);
            result.Id.ShouldBeEquivalentTo(GenerateId(providerSummary.Id, buildProject.SpecificationId));
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
            List<Reference> policySpecificationsForNumberCalc = new List<Reference>()
            {
                new Reference("Spec1", "SpecOne"),
            };
            Reference allocationLineReturned = new Reference("allocationLine", "allocation line for Funding Calc and number calc");

            Reference fundingCalcReference = new Reference("CalcF1", "Funding calc 1");
            Reference fundingCalcSpecificationReference = new Reference("FSpect", "FundingSpecification");

            Reference numbercalcReference = new Reference("CalcF2", "Funding calc 2");
            Reference numbercalcSpecificationReference = new Reference("FSpec2", "FundingSpecification2");

            CalculationResult fundingCalcReturned = new CalculationResult()
            {
                CalculationType = CalculationType.Funding,
                Calculation = fundingCalcReference,
                AllocationLine = allocationLineReturned,
                CalculationSpecification = fundingCalcSpecificationReference,
                PolicySpecifications = policySpecificationsForFundingCalc,
                Value = 10000
            };
            CalculationResult fundingCalcReturned2 = new CalculationResult()
            {
                CalculationType = CalculationType.Funding,
                Calculation = numbercalcReference,
                AllocationLine = allocationLineReturned,
                CalculationSpecification = numbercalcSpecificationReference,
                PolicySpecifications = policySpecificationsForNumberCalc,
                Value = 20000
            };

            List<CalculationResult> calculationResults = new List<CalculationResult>()
            {
                fundingCalcReturned,
                fundingCalcReturned2
            };
            IAllocationModel mockAllocationModel = Substitute.For<IAllocationModel>();
            mockAllocationModel
                .Execute(Arg.Any<List<ProviderSourceDataset>>(), Arg.Any<ProviderSummary>())
                .Returns(calculationResults);

            CalculationEngine calculationEngine = CreateCalculationEngine();
            ProviderSummary providerSummary = CreateDummyProviderSummary();
            BuildProject buildProject = CreateBuildProject();

            var nonMatchingCalculationModel = new CalculationSummaryModel()
            {
                Id = "Non matching calculation",
                Name = "Non matching calculation",
                CalculationType = CalculationType.Funding
            };
            IEnumerable<CalculationSummaryModel> calculationSummaryModels = new[]
            {
                new CalculationSummaryModel()
                {
                    Id = fundingCalcReference.Id,
                    Name = fundingCalcReference.Name,
                    CalculationType = CalculationType.Funding
                },
                new CalculationSummaryModel()
                {
                    Id = numbercalcReference.Id,
                    Name = numbercalcReference.Name,
                    CalculationType = CalculationType.Funding
                },
                nonMatchingCalculationModel
            };

            // Act
            var calculateProviderResults = calculationEngine.CalculateProviderResults(mockAllocationModel, buildProject, calculationSummaryModels,
                providerSummary, new List<ProviderSourceDataset>());
            ProviderResult result = calculateProviderResults;

            // Assert
            result.Provider.Should().Be(providerSummary);
            result.SpecificationId.ShouldBeEquivalentTo(buildProject.SpecificationId);
            result.Id.ShouldBeEquivalentTo(GenerateId(providerSummary.Id, buildProject.SpecificationId));
            result.CalculationResults.Should().HaveCount(3);
            result.AllocationLineResults.Should().HaveCount(1);

            AllocationLineResult allocationLine = result.AllocationLineResults[0];
            allocationLine.Value = 30000;

            CalculationResult fundingCalcResult = result.CalculationResults.First(cr => cr.Calculation.Id == fundingCalcReference.Id);
            fundingCalcResult.Calculation.ShouldBeEquivalentTo(fundingCalcReference);
            fundingCalcResult.CalculationType.ShouldBeEquivalentTo(fundingCalcReturned.CalculationType);
            fundingCalcResult.AllocationLine.ShouldBeEquivalentTo(allocationLineReturned);
            fundingCalcResult.CalculationSpecification.ShouldBeEquivalentTo(fundingCalcSpecificationReference);
            fundingCalcResult.PolicySpecifications.ShouldBeEquivalentTo(policySpecificationsForFundingCalc);
            fundingCalcResult.Value.ShouldBeEquivalentTo(fundingCalcReturned.Value.Value);

            CalculationResult numberCalcResult = result.CalculationResults.First(cr => cr.Calculation.Id == numbercalcReference.Id);
            numberCalcResult.Calculation.ShouldBeEquivalentTo(numbercalcReference);
            numberCalcResult.CalculationType.ShouldBeEquivalentTo(fundingCalcReturned2.CalculationType);
            numberCalcResult.AllocationLine.ShouldBeEquivalentTo(allocationLineReturned);
            numberCalcResult.CalculationSpecification.ShouldBeEquivalentTo(numbercalcSpecificationReference);
            numberCalcResult.PolicySpecifications.ShouldBeEquivalentTo(policySpecificationsForNumberCalc);
            numberCalcResult.Value.ShouldBeEquivalentTo(fundingCalcReturned2.Value.Value);

            CalculationResult nonMatchingCalcResult = result.CalculationResults.First(cr => cr.Calculation.Id == "Non matching calculation");
            nonMatchingCalcResult.Calculation.ShouldBeEquivalentTo(new Reference(nonMatchingCalculationModel.Id, nonMatchingCalculationModel.Name));
            nonMatchingCalcResult.CalculationType.ShouldBeEquivalentTo(nonMatchingCalculationModel.CalculationType);
            nonMatchingCalcResult.AllocationLine.Should().BeNull();
            nonMatchingCalcResult.CalculationSpecification.Should().BeNull();
            nonMatchingCalcResult.PolicySpecifications.Should().BeNull();
            nonMatchingCalcResult.Value.Should().BeNull();
        }

        [TestMethod]
        public void CalculateProviderResult_WhenCalculationValuesReturnedWithMultipleAllocationLines_ThenAllocationLineValuesAreSetCorrectly()
        {
            // Arrange
            List<Reference> policySpecificationsForFundingCalc = new List<Reference>()
            {
                new Reference("Spec1", "SpecOne"),
                new Reference("Spec2", "SpecTwo")
            };
            List<Reference> policySpecificationsForNumberCalc = new List<Reference>()
            {
                new Reference("Spec1", "SpecOne"),
            };
            Reference allocationLine1 = new Reference("allocationLine", "allocation line for Funding Calc and number calc");
            Reference allocationLine2 = new Reference("allocationLine2", "Second allocation line");
            Reference allocationLine3 = new Reference("allocationLine3", "Allocation line excluded");

            CalculationResult calc1 = new CalculationResult()
            {
                CalculationType = CalculationType.Funding,
                Calculation = new Reference("calc1", "Calc 1"),
                AllocationLine = allocationLine1,
                CalculationSpecification = new Reference("FSpect", "FundingSpecification"),
                PolicySpecifications = policySpecificationsForFundingCalc,
                Value = 10000
            };
            CalculationResult calc2 = new CalculationResult()
            {
                CalculationType = CalculationType.Funding,
                Calculation = new Reference("calc2", "Calc 2"),
                AllocationLine = allocationLine1,
                CalculationSpecification = new Reference("FSpec2", "FundingSpecification2"),
                PolicySpecifications = policySpecificationsForNumberCalc,
                Value = 20000
            };
            CalculationResult calc3 = new CalculationResult()
            {
                CalculationType = CalculationType.Funding,
                Calculation = new Reference("calc3", "Calc 3"),
                AllocationLine = allocationLine2,
                CalculationSpecification = new Reference("calc3", "Calc 3"),
                PolicySpecifications = policySpecificationsForNumberCalc,
                Value = 67
            };
            CalculationResult calc4 = new CalculationResult()
            {
                CalculationType = CalculationType.Funding,
                Calculation = new Reference("calc4", "Calc 4"),
                AllocationLine = allocationLine3,
                CalculationSpecification = new Reference("calc4", "Calc 4"),
                PolicySpecifications = policySpecificationsForNumberCalc,
                Value = null,
            };
            CalculationResult calc5 = new CalculationResult()
            {
                CalculationType = CalculationType.Funding,
                Calculation = new Reference("calc5", "Calc 5"),
                AllocationLine = allocationLine3,
                CalculationSpecification = new Reference("calc5", "Calc 5"),
                PolicySpecifications = policySpecificationsForNumberCalc,
                Value = 50,
            };
            CalculationResult calc6 = new CalculationResult()
            {
                CalculationType = CalculationType.Funding,
                Calculation = new Reference("calc6", "Calc 6"),
                AllocationLine = allocationLine3,
                CalculationSpecification = new Reference("calc6", "Calc 6"),
                PolicySpecifications = policySpecificationsForNumberCalc,
                Value = 8,
            };

            List<CalculationResult> calculationResults = new List<CalculationResult>()
            {
                calc1,
                calc2,
                calc3,
                calc4,
                calc5,
                calc6,
            };
            IAllocationModel mockAllocationModel = Substitute.For<IAllocationModel>();
            mockAllocationModel
                .Execute(Arg.Any<List<ProviderSourceDataset>>(), Arg.Any<ProviderSummary>())
                .Returns(calculationResults);

            CalculationEngine calculationEngine = CreateCalculationEngine();
            ProviderSummary providerSummary = CreateDummyProviderSummary();
            BuildProject buildProject = CreateBuildProject();


            IEnumerable<CalculationSummaryModel> calculationSummaryModels = new[]
            {
                new CalculationSummaryModel()
                {
                    Id = "calc1",
                    Name = "Calc 1",
                    CalculationType = CalculationType.Funding
                },
                new CalculationSummaryModel()
                {
                    Id = "calc2",
                    Name = "Calc 2",
                    CalculationType = CalculationType.Funding
                },
                new CalculationSummaryModel()
                {
                    Id = "calc3",
                    Name = "Calc 3",
                    CalculationType = CalculationType.Funding
                },
                new CalculationSummaryModel()
                {
                    Id = "calc4",
                    Name = "Calc 4",
                    CalculationType = CalculationType.Funding
                },
                 new CalculationSummaryModel()
                {
                    Id = "calc5",
                    Name = "Calc 5",
                    CalculationType = CalculationType.Funding
                },
                  new CalculationSummaryModel()
                {
                    Id = "calc6",
                    Name = "Calc 6",
                    CalculationType = CalculationType.Funding
                },
            };

            // Act
            var calculateProviderResults = calculationEngine.CalculateProviderResults(mockAllocationModel, buildProject, calculationSummaryModels,
                providerSummary, new List<ProviderSourceDataset>());
            ProviderResult result = calculateProviderResults;

            // Assert
            result
                .AllocationLineResults
                .Should()
                .HaveCount(3);

            // Values are summed for allocation line
            result
                .AllocationLineResults[0]
                .Value
                .Should()
                .Be(30000);

            result
                .AllocationLineResults[1]
                .Value
                .Should()
                .Be(67);

            // All calculations for Allocation Line 3 returned nulls - therefore allocation line value is null
            result
                .AllocationLineResults[2]
                .Value
                .Should()
                .Be(58);
        }

        [TestMethod]
        public void CalculateProviderResult_WhenCalculationValuesReturnedWithMultipleAllocationLinesAndMixOfValuesAndNulls_ThenAllocationLineValuesAreSetCorrectly()
        {
            // Arrange
            List<Reference> policySpecificationsForFundingCalc = new List<Reference>()
            {
                new Reference("Spec1", "SpecOne"),
                new Reference("Spec2", "SpecTwo")
            };
            List<Reference> policySpecificationsForNumberCalc = new List<Reference>()
            {
                new Reference("Spec1", "SpecOne"),
            };
            Reference allocationLine1 = new Reference("allocationLine", "allocation line for Funding Calc and number calc");
            Reference allocationLine2 = new Reference("allocationLine2", "Second allocation line");
            Reference allocationLine3 = new Reference("allocationLine3", "Allocation line excluded");

            CalculationResult calc1 = new CalculationResult()
            {
                CalculationType = CalculationType.Funding,
                Calculation = new Reference("calc1", "Calc 1"),
                AllocationLine = allocationLine1,
                CalculationSpecification = new Reference("FSpect", "FundingSpecification"),
                PolicySpecifications = policySpecificationsForFundingCalc,
                Value = 10000
            };
            CalculationResult calc2 = new CalculationResult()
            {
                CalculationType = CalculationType.Funding,
                Calculation = new Reference("calc2", "Calc 2"),
                AllocationLine = allocationLine1,
                CalculationSpecification = new Reference("FSpec2", "FundingSpecification2"),
                PolicySpecifications = policySpecificationsForNumberCalc,
                Value = 20000
            };
            CalculationResult calc3 = new CalculationResult()
            {
                CalculationType = CalculationType.Funding,
                Calculation = new Reference("calc3", "Calc 3"),
                AllocationLine = allocationLine2,
                CalculationSpecification = new Reference("calc3", "Calc 3"),
                PolicySpecifications = policySpecificationsForNumberCalc,
                Value = 67
            };
            CalculationResult calc4 = new CalculationResult()
            {
                CalculationType = CalculationType.Funding,
                Calculation = new Reference("calc4", "Calc 4"),
                AllocationLine = allocationLine3,
                CalculationSpecification = new Reference("calc4", "Calc 4"),
                PolicySpecifications = policySpecificationsForNumberCalc,
                Value = null,
            };

            List<CalculationResult> calculationResults = new List<CalculationResult>()
            {
                calc1,
                calc2,
                calc3,
                calc4,
            };
            IAllocationModel mockAllocationModel = Substitute.For<IAllocationModel>();
            mockAllocationModel
                .Execute(Arg.Any<List<ProviderSourceDataset>>(), Arg.Any<ProviderSummary>())
                .Returns(calculationResults);

            CalculationEngine calculationEngine = CreateCalculationEngine();
            ProviderSummary providerSummary = CreateDummyProviderSummary();
            BuildProject buildProject = CreateBuildProject();


            IEnumerable<CalculationSummaryModel> calculationSummaryModels = new[]
            {
                new CalculationSummaryModel()
                {
                    Id = "calc1",
                    Name = "Calc 1",
                    CalculationType = CalculationType.Funding
                },
                new CalculationSummaryModel()
                {
                    Id = "calc2",
                    Name = "Calc 2",
                    CalculationType = CalculationType.Funding
                },
                new CalculationSummaryModel()
                {
                    Id = "calc3",
                    Name = "Calc 3",
                    CalculationType = CalculationType.Funding
                },
                new CalculationSummaryModel()
                {
                    Id = "calc4",
                    Name = "Calc 4",
                    CalculationType = CalculationType.Funding
                },
            };

            // Act
            var calculateProviderResults = calculationEngine.CalculateProviderResults(mockAllocationModel, buildProject, calculationSummaryModels,
                providerSummary, new List<ProviderSourceDataset>());
            ProviderResult result = calculateProviderResults;

            // Assert
            result
                .AllocationLineResults
                .Should()
                .HaveCount(3);

            // Values are summed for allocation line
            result
                .AllocationLineResults[0]
                .Value
                .Should()
                .Be(30000);

            result
                .AllocationLineResults[1]
                .Value
                .Should()
                .Be(67);

            // All calculations for Allocation Line 3 returned nulls - therefore allocation line value is null
            result
                .AllocationLineResults[2]
                .Value
                .Should()
                .Be(null);
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
                calculationsRepository ?? CreateCalculationsRepository(),
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

        static IAllocationFactory CreateAllocationFactory(IAllocationModel allocationModel)
        {
            IAllocationFactory allocationFactory = Substitute.For<IAllocationFactory>();
            allocationFactory
                .CreateAllocationModel(Arg.Any<Assembly>())
                .Returns(allocationModel);

            return allocationFactory;
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

        private static ICalculationsRepository CreateCalculationsRepository()
        {
            return Substitute.For<ICalculationsRepository>();
        }

        private static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }
    }
}
