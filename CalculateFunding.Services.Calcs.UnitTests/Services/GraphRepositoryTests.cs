using CalculateFunding.Common.ApiClient.Graph;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Calcs.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using GraphModels = CalculateFunding.Common.ApiClient.Graph.Models;
using CalculateFunding.Services.CodeGeneration.VisualBasic;
using Serilog;
using System.Linq;
using FluentAssertions;
using CalculateFunding.Common.ApiClient.Models;
using System.Net;
using Newtonsoft.Json.Linq;
using CalculateFunding.Tests.Common.Helpers;
using GraphCalculation = CalculateFunding.Common.ApiClient.Graph.Models.Calculation;
using GraphEntity = CalculateFunding.Common.ApiClient.Graph.Models.Entity<CalculateFunding.Common.ApiClient.Graph.Models.Calculation>;
using CalculationEntity = CalculateFunding.Models.Graph.Entity<CalculateFunding.Common.ApiClient.Graph.Models.Calculation, CalculateFunding.Common.ApiClient.Graph.Models.Relationship>;
using GraphRelationship = CalculateFunding.Common.ApiClient.Graph.Models.Relationship;

namespace CalculateFunding.Services.Calcs.UnitTests.Services
{
    [TestClass]
    public class GraphRepositoryTests
    {
        ICalculationsFeatureFlag _calculationsFeatureFlag;
        IGraphApiClient _graphApiClient;
        IGraphRepository _graphRepository;
        ILogger _logger;

        [TestInitialize]
        public void SetUp()
        {
            _calculationsFeatureFlag = Substitute.For<ICalculationsFeatureFlag>();
            _graphApiClient = Substitute.For<IGraphApiClient>();
            ResiliencePolicies resiliencePolicies = new ResiliencePolicies { GraphApiClientPolicy = Polly.Policy.NoOpAsync() };
            _logger = Substitute.For<ILogger>();
            _graphRepository = new GraphRepository(_graphApiClient, resiliencePolicies, _logger, _calculationsFeatureFlag);
        }

        [TestMethod]
        public async Task GetCircularDependencies_WhenFeatureNotEnabledAndSpecificationSuppliedWithCalculation_NoCircularDependenciesReturned()
        {
            SpecificationSummary specificationSummary = NewSpecification();
            
            _calculationsFeatureFlag
                            .IsGraphEnabled()
                .Returns(false);

            IEnumerable<CalculationEntity> circularDependencies = await _graphRepository.GetCircularDependencies(specificationSummary.Id);

            circularDependencies
                .IsNullOrEmpty()
                .Should()
                .Be(true);
        }

        [TestMethod]
        public async Task GetCircularDependencies_WhenFeatureEnabledAndSpecificationSuppliedWithCalculationWithNoCircularDependencies_NoCircularDependenciesReturned()
        {
            SpecificationSummary specificationSummary = NewSpecification();

            _graphApiClient
                .GetCircularDependencies(specificationSummary.Id)
                .Returns(new ApiResponse<IEnumerable<GraphEntity>>(HttpStatusCode.OK));

            _calculationsFeatureFlag
                .IsGraphEnabled()
                .Returns(false);

            IEnumerable<CalculationEntity> circularDependencies = await _graphRepository.GetCircularDependencies(specificationSummary.Id);

            circularDependencies
                .IsNullOrEmpty()
                .Should()
                .Be(true);
        }

        [TestMethod]
        public async Task GetCircularDependencies_WhenFeatureEnabledAndSpecificationSuppliedWithCalculationWithCircularDependencies_CircularDependenciesReturned()
        {
            SpecificationSummary specificationSummary = NewSpecification(_ => _.WithId("ebc5153d-0a3b-44aa-ab4b-aea405cb0df0"));

            GraphCalculation calculation1 = NewGraphCalculation(_ => _.WithSpecificationId(specificationSummary.Id)
            .WithId("41402e30-f8a7-40bc-a4fc-4cd256e2fbeb"));

            GraphCalculation calculation2 = NewGraphCalculation(_ => _.WithSpecificationId(specificationSummary.Id));

            GraphRelationship calculation1Relationship = Substitute.For<GraphRelationship>();
            calculation1Relationship.One = calculation1;
            calculation1Relationship.Two = calculation2;

            GraphRelationship calculation2Relationship = Substitute.For<GraphRelationship>();
            calculation2Relationship.One = calculation2;
            calculation2Relationship.Two = calculation1;


            GraphEntity entity = new GraphEntity { Node = calculation1, Relationships = new[] { calculation1Relationship, calculation2Relationship } };

            _calculationsFeatureFlag
                .IsGraphEnabled()
                .Returns(true);

            _graphApiClient
                .GetCircularDependencies(Arg.Is(specificationSummary.Id))
                .Returns(new ApiResponse<IEnumerable<GraphEntity>>(HttpStatusCode.OK, new[] { entity }));

            IEnumerable<CalculationEntity> circularDependencies = await _graphRepository.GetCircularDependencies(specificationSummary.Id);

            circularDependencies
                .IsNullOrEmpty()
                .Should()
                .Be(false);

            circularDependencies
                .Where(_ => _.Node.CalculationId == calculation1.CalculationId)
                .FirstOrDefault()
                .Relationships
                .Count()
                .Should()
                .Be(2);
        }

        [TestMethod]
        public async Task PersistToGraph_WhenFeatureNotEnabledAndCalculationsListSuppliedWithCalculation_CalculationGraphNotPersisted()
        {
            Calculation calculation = NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion()));

            IEnumerable<Calculation> calculations = new[] { calculation, 
                NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion())), 
                NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion()))};

            SpecificationSummary specificationSummary = NewSpecification();

            _calculationsFeatureFlag
                .IsGraphEnabled()
                .Returns(false);

            await _graphRepository.PersistToGraph(calculations, specificationSummary, calculation.Id);

            await _graphApiClient
                .DidNotReceive()
                .UpsertSpecifications(Arg.Any<GraphModels.Specification[]> ());

            await _graphApiClient
                .DidNotReceive()
                .UpsertCalculations(Arg.Any<GraphModels.Calculation[]>());
        }

        [TestMethod]
        public async Task PersistToGraph_WhenFeatureNotEnabledAndCalculationsListSuppliedWithNoCalculation_CalculationsGraphNotPersisted()
        {
            Calculation calculation = NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion()));

            IEnumerable<Calculation> calculations = new[] { calculation, 
                NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion())), 
                NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion()))};

            SpecificationSummary specificationSummary = NewSpecification();

            _calculationsFeatureFlag
                .IsGraphEnabled()
                .Returns(false);

            await _graphRepository.PersistToGraph(calculations, 
                specificationSummary, 
                calculation.Current.CalculationId);

            await _graphApiClient
                .DidNotReceive()
                .UpsertSpecifications(Arg.Any<GraphModels.Specification[]>());

            await _graphApiClient
                .DidNotReceive()
                .UpsertCalculations(Arg.Any<GraphModels.Calculation[]>());

        }

        [TestMethod]
        public async Task PersistToGraph_WhenFeatureEnabledAndCalculationsListSuppliedWithCalculation_CalculationGraphPersisted()
        {
            Calculation calcChildReference = NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion()));
            string functionName = $"{calcChildReference.Namespace}.{VisualBasicTypeGenerator.GenerateIdentifier(calcChildReference.Name)}";
            Calculation calculation = NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(version => version.WithSourceCode($"return {functionName}()"))));

            IEnumerable<Calculation> calculations = new[] { calculation,
                calcChildReference, 
                NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion()))};

            SpecificationSummary specificationSummary = NewSpecification();

            _calculationsFeatureFlag
                .IsGraphEnabled()
                .Returns(true);

            await _graphRepository.PersistToGraph(calculations, 
                specificationSummary, 
                calculation.Id,
                true);

            await _graphApiClient
                .Received(1)
                .UpsertSpecifications(Arg.Is<GraphModels.Specification[]>(_ => _[0].SpecificationId == specificationSummary.Id && _.Length == 1));

            await _graphApiClient
                .Received(1)
                .DeleteCalculation(Arg.Is<string>(calculation.Id));

            await _graphApiClient
                .Received(1)
                .UpsertCalculations(Arg.Is<GraphModels.Calculation[]>(_ => _[0].CalculationId == calculation.Id && _.Length == 1));

            await _graphApiClient
                .Received(1)
                .UpsertCalculationCalculationsRelationships(calculation.Id, Arg.Is<string[]>(_ => _[0] == calcChildReference.Id));
        }

        [TestMethod]
        public async Task PersistToGraph_WhenFeatureEnabledAndCalculationsListSuppliedWithNoCalculation_CalculationsGraphPersisted()
        {
            Calculation calculation = NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion()));

            IEnumerable<Calculation> calculations = new[] { calculation, 
                NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion())), 
                NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion()))};

            SpecificationSummary specificationSummary = NewSpecification();

            _calculationsFeatureFlag
                .IsGraphEnabled()
                .Returns(true);

            await _graphRepository.PersistToGraph(calculations,
                specificationSummary);

            await _graphApiClient
                .Received(1)
                .UpsertSpecifications(Arg.Is<GraphModels.Specification[]>(_ => _[0].SpecificationId == specificationSummary.Id && _.Length == 1));

            await _graphApiClient
                .Received(1)
                .UpsertCalculations(Arg.Is<GraphModels.Calculation[]>(_ => _[0].CalculationId == calculation.Id && _.Length == 3));
        }

        protected static SpecificationSummary NewSpecification(Action<SpecificationSummaryBuilder> setUp = null)
        {
            SpecificationSummaryBuilder specificationSummaryBuilder = new SpecificationSummaryBuilder();

            setUp?.Invoke(specificationSummaryBuilder);

            return specificationSummaryBuilder.Build();
        }

        protected static Calculation NewCalculation(Action<CalculationBuilder> setUp = null)
        {
            CalculationBuilder calculationBuilder = new CalculationBuilder();

            setUp?.Invoke(calculationBuilder);

            return calculationBuilder.Build();
        }

        protected static GraphCalculation NewGraphCalculation(Action<GraphCalculationBuilder> setUp = null)
        {
            GraphCalculationBuilder graphCalculationBuilder = new GraphCalculationBuilder();

            setUp?.Invoke(graphCalculationBuilder);

            return graphCalculationBuilder.Build();
        }

        protected static CalculationVersion NewCalculationVersion(Action<CalculationVersionBuilder> setUp = null)
        {
            CalculationVersionBuilder calculationVersionBuilder = new CalculationVersionBuilder();

            setUp?.Invoke(calculationVersionBuilder);

            return calculationVersionBuilder.Build();
        }

        public string GetResourceString(string resourceName)
        {
            return typeof(GraphRepositoryTests)
                .Assembly
                .GetEmbeddedResourceFileContents(resourceName);
        }
    }
}
