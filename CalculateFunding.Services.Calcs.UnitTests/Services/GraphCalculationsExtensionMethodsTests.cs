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

namespace CalculateFunding.Services.Calcs.UnitTests.Services
{
    [TestClass]
    public class GraphCalculationsExtensionMethodsTests
    {
        ICalculationsFeatureFlag _calculationsFeatureFlag;
        IGraphApiClient _graphApiClient;
        Polly.Policy _graphApiClientPolicy;

        [TestInitialize]
        public void SetUp()
        {
            _calculationsFeatureFlag = Substitute.For<ICalculationsFeatureFlag>();
            _graphApiClient = Substitute.For<IGraphApiClient>();
            ResiliencePolicies resiliencePolicies = new ResiliencePolicies { GraphApiClientPolicy = Polly.Policy.NoOpAsync() };
            _graphApiClientPolicy = resiliencePolicies.GraphApiClientPolicy;
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

            await calculations.PersistToGraph(_graphApiClient, _graphApiClientPolicy, specificationSummary, _calculationsFeatureFlag, calculation.Id);

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

            await calculations.PersistToGraph(_graphApiClient, 
                _graphApiClientPolicy, 
                specificationSummary, 
                _calculationsFeatureFlag, 
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

            await calculations.PersistToGraph(_graphApiClient, 
                _graphApiClientPolicy, 
                specificationSummary, 
                _calculationsFeatureFlag,
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

            await calculations.PersistToGraph(_graphApiClient, 
                _graphApiClientPolicy, 
                specificationSummary, 
                _calculationsFeatureFlag);

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

        protected static CalculationVersion NewCalculationVersion(Action<CalculationVersionBuilder> setUp = null)
        {
            CalculationVersionBuilder calculationVersionBuilder = new CalculationVersionBuilder();

            setUp?.Invoke(calculationVersionBuilder);

            return calculationVersionBuilder.Build();
        }
    }
}
