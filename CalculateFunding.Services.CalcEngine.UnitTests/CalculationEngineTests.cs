using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.CalcEngine.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Serilog;

namespace CalculateFunding.Services.CalcEngine.UnitTests
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
                .Should()
                .ThrowExactly<Exception>();
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

            //we also need to check we order by calc id as we need a stable sort for the caching hash checks
            results
                .First()
                .CalculationResults
                .Select(_ => _.Calculation.Id)
                .Should()
                .BeEquivalentTo(new[] {CalculationId, "calc2", "calc3"});
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
                new CalculationSummaryModel(){CalculationType = CalculationType.Template, Name = "Test", Id = "Test"}
            };

            ProviderSummary provider = new ProviderSummary();
            List<ProviderSourceDataset> sourceDataset = new List<ProviderSourceDataset>();

            // Act
            Action calculateProviderResultMethod = () =>
            {
                calcEngine.CalculateProviderResults(mockAllocationModel, CreateBuildProject(), models, provider, sourceDataset);
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

            Reference fundingCalcReference = new Reference("CalcF1", "Funding calc 1");

            Reference numbercalcReference = new Reference("CalcF2", "Funding calc 2");

            CalculationResult fundingCalcReturned = new CalculationResult()
            {
                CalculationType = CalculationType.Template,
                Calculation = fundingCalcReference,
                Value = 10000
            };
            CalculationResult fundingCalcReturned2 = new CalculationResult()
            {
                CalculationType = CalculationType.Template,
                Calculation = numbercalcReference,
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
                CalculationType = CalculationType.Template
            };
            IEnumerable<CalculationSummaryModel> calculationSummaryModels = new[]
            {
                new CalculationSummaryModel()
                {
                    Id = fundingCalcReference.Id,
                    Name = fundingCalcReference.Name,
                    CalculationType = CalculationType.Template
                },
                new CalculationSummaryModel()
                {
                    Id = numbercalcReference.Id,
                    Name = numbercalcReference.Name,
                    CalculationType = CalculationType.Template
                },
                nonMatchingCalculationModel
            };

            // Act
            var calculateProviderResults = calculationEngine.CalculateProviderResults(mockAllocationModel, buildProject, calculationSummaryModels,
                providerSummary, new List<ProviderSourceDataset>());
            ProviderResult result = calculateProviderResults;

            // Assert
            result.Provider.Should().Be(providerSummary);
            result.SpecificationId.Should().BeEquivalentTo(buildProject.SpecificationId);
            result.Id.Should().BeEquivalentTo(GenerateId(providerSummary.Id, buildProject.SpecificationId));
            result.CalculationResults.Should().HaveCount(3);

            CalculationResult fundingCalcResult = result.CalculationResults.First(cr => cr.Calculation.Id == fundingCalcReference.Id);
            fundingCalcResult.Calculation.Should().BeEquivalentTo(fundingCalcReference);
            fundingCalcResult.CalculationType.Should().BeEquivalentTo(fundingCalcReturned.CalculationType);
            fundingCalcResult.Value.Should().Be(fundingCalcReturned.Value.Value);

            CalculationResult numberCalcResult = result.CalculationResults.First(cr => cr.Calculation.Id == numbercalcReference.Id);
            numberCalcResult.Calculation.Should().BeEquivalentTo(numbercalcReference);
            numberCalcResult.CalculationType.Should().BeEquivalentTo(fundingCalcReturned2.CalculationType);
            numberCalcResult.Value.Should().Be(fundingCalcReturned2.Value.Value);

            CalculationResult nonMatchingCalcResult = result.CalculationResults.First(cr => cr.Calculation.Id == "Non matching calculation");
            nonMatchingCalcResult.Calculation.Should().BeEquivalentTo(new Reference(nonMatchingCalculationModel.Id, nonMatchingCalculationModel.Name));
            nonMatchingCalcResult.CalculationType.Should().BeEquivalentTo(nonMatchingCalculationModel.CalculationType);
            nonMatchingCalcResult.Value.Should().BeNull();
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
