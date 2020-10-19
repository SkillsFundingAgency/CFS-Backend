using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using CalculateFunding.Services.Profiling.Models;
using CalculateFunding.Services.Profiling.ReProfilingStrategies;
using CalculateFunding.Services.Profiling.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CalculateFunding.Services.Profiling.Tests.Services
{
    [TestClass]
    public class ReProfilingStrategyListServiceTests
    {
        private Mock<IReProfilingStrategyLocator> _serviceLocator;
        
        private ReProfilingStrategyListService _service;
        
        [TestInitialize]
        public void SetUp()
        {
            _serviceLocator = new Mock<IReProfilingStrategyLocator>();
            
            _service = new ReProfilingStrategyListService(_serviceLocator.Object);
        }

        [TestMethod]
        public void MapsAllStrategiesInLocatorIntoResponses()
        {
            IReProfilingStrategy strategyOne = NewStrategyStub();
            IReProfilingStrategy strategyTwo = NewStrategyStub();
            IReProfilingStrategy strategyThree = NewStrategyStub();
            IReProfilingStrategy strategyFour = NewStrategyStub();
            
            GivenTheSupportedStrategies(strategyOne, strategyTwo, strategyThree, strategyFour);

            ActionResult<IEnumerable<ReProfilingStrategyResponse>> responses = WhenTheReProfilingStrategiesAreQueried();

            responses?
                .Value
                .Should()
                .BeEquivalentTo(new[]
                {
                    strategyOne,
                    strategyTwo,
                    strategyThree,
                    strategyFour
                }.Select(_ => new ReProfilingStrategyResponse
                {
                    StrategyKey = _.StrategyKey,
                    Description = _.Description,
                    DisplayName = _.DisplayName
                }));
        }

        private ActionResult<IEnumerable<ReProfilingStrategyResponse>> WhenTheReProfilingStrategiesAreQueried()
            => _service.GetAllStrategies();

        private void GivenTheSupportedStrategies(params IReProfilingStrategy[] strategies)
            => _serviceLocator.Setup(_ => _.GetAllStrategies())
                .Returns(strategies);

        private IReProfilingStrategy NewStrategyStub(Action<ReProfilingStrategyStubBuilder> setUp = null)
        {
            ReProfilingStrategyStubBuilder builder = new ReProfilingStrategyStubBuilder();

            setUp?.Invoke(builder);
            
            return builder.Build();
        }
    }
}