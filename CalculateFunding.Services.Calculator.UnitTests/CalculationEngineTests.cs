using CalculateFunding.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Calculator.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

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
                .When(x => x.Execute(Arg.Any<List<ProviderSourceDataset>>()))
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
                .Execute(Arg.Is(Arg.Any<List<ProviderSourceDataset>>()))
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
        async public Task GenerateAllocations_GivenBuildProjectWithNoCalculations_Runs()
        {
            //Arrange
            BuildProject buildProject = CreateBuildProject();

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
                .Execute(Arg.Is(Arg.Any<List<ProviderSourceDataset>>()))
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
