using CalculateFunding.Common.Caching;
using CalculateFunding.Models.Aggregations;
using CalculateFunding.Services.CalcEngine;
using CalculateFunding.Services.CalcEngine.Interfaces;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calculator
{
    [TestClass]
    public class CalculationAggregationServiceTests
    {
        private CalculationAggregationService _calculationAggregationService;
        private ICacheProvider _mockCacheProvider;
        private IDatasetAggregationsRepository _mockDatasetAggregationsRepository;
        private EngineSettings engineSettings;
        private ICalculatorResiliencePolicies _mockResiliencePolicies;

        [TestInitialize]
        public void Initialize()
        {
            _mockCacheProvider = Substitute.For<ICacheProvider>();
            _mockDatasetAggregationsRepository = Substitute.For<IDatasetAggregationsRepository>();
            _mockResiliencePolicies = new CalculatorResiliencePolicies
            {
                CacheProvider = Policy.NoOpAsync()
            };

            engineSettings = new EngineSettings();

            _calculationAggregationService = new CalculationAggregationService(
                _mockCacheProvider,
                _mockDatasetAggregationsRepository,
                engineSettings,
                _mockResiliencePolicies);
        }

        [TestMethod]
        public async Task GenerateAllocations_GivenCachedAggregateValuesExistAndAggregationsToAggregateInMessageAreInAnyCase_EnsuresAllocationModelCalledWithCachedAggregates()
        {
            const string specificationId = "spec1";

            Dictionary<string, List<decimal>> cachedCalculationAggregates = new Dictionary<string, List<decimal>>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "Calc1", new List<decimal>{ 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10 } },
                { "Calc2", new List<decimal>{ 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20 } },
                { "Calc3", new List<decimal>{ 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30 } }
            };

            _mockCacheProvider
                .GetAsync<Dictionary<string, List<decimal>>>($"{CacheKeys.CalculationAggregations}{specificationId}_1")
                .Returns(cachedCalculationAggregates);

            BuildAggregationRequest buildAggregationRequest = new BuildAggregationRequest 
            { 
                SpecificationId = specificationId,
                BatchCount = 3
            };

            IEnumerable<CalculationAggregation> calculationAggregations = 
                await _calculationAggregationService.BuildAggregations(buildAggregationRequest);

            Assert.IsTrue(calculationAggregations.Count() == 3 &&
                        calculationAggregations.ElementAt(0).Values.ElementAt(0).Value == 200 &&
                        calculationAggregations.ElementAt(0).Values.ElementAt(1).Value == 10 &&
                        calculationAggregations.ElementAt(0).Values.ElementAt(2).Value == 10 &&
                        calculationAggregations.ElementAt(0).Values.ElementAt(3).Value == 10 &&
                        calculationAggregations.ElementAt(1).Values.ElementAt(0).Value == 400 &&
                        calculationAggregations.ElementAt(1).Values.ElementAt(1).Value == 20 &&
                        calculationAggregations.ElementAt(1).Values.ElementAt(2).Value == 20 &&
                        calculationAggregations.ElementAt(1).Values.ElementAt(3).Value == 20 &&
                        calculationAggregations.ElementAt(2).Values.ElementAt(0).Value == 600 &&
                        calculationAggregations.ElementAt(2).Values.ElementAt(1).Value == 30 &&
                        calculationAggregations.ElementAt(2).Values.ElementAt(2).Value == 30 &&
                        calculationAggregations.ElementAt(2).Values.ElementAt(3).Value == 30);
        }
    }
}
