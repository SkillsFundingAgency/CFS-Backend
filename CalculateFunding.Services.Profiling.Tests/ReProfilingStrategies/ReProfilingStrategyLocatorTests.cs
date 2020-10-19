using System;
using System.Collections.Generic;
using CalculateFunding.Services.Profiling.ReProfilingStrategies;
using CalculateFunding.Services.Profiling.Tests.Services;
using CalculateFunding.Services.Profiling.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Profiling.Tests.ReProfilingStrategies
{
    [TestClass]
    public class ReProfilingStrategyLocatorTests
    {
        private ReProfilingStrategyLocator _serviceLocator;

        [TestMethod]
        public void LocatesStrategiesByStrategyKey()
        {
            string key = NewRandomString();

            IReProfilingStrategy expectedStrategy = NewStrategyStub(_ => _.WithKey(key));
            
            GivenTheSupportedStrategies(NewStrategyStub(),
                NewStrategyStub(),
                expectedStrategy,
                NewStrategyStub());

            IReProfilingStrategy actualStrategy = WhenTheStrategyIsLocated(key);

            actualStrategy
                .Should()
                .BeSameAs(expectedStrategy);
        }
        
        [TestMethod]
        public void HasKeyIsTrueIfHasStrategyWithSuppliedKey()
        {
            string key = NewRandomString();

            IReProfilingStrategy expectedStrategy = NewStrategyStub(_ => _.WithKey(key));
            
            GivenTheSupportedStrategies(NewStrategyStub(),
                NewStrategyStub(),
                expectedStrategy,
                NewStrategyStub());

            bool hasStrategy = WhenTheStrategyKeyIsChecked(key);

            hasStrategy
                .Should()
                .BeTrue();
        }
        
        [TestMethod]
        public void HasKeyIsFalseIfDoesNotHaveStrategyWithSuppliedKey()
        {
            GivenTheSupportedStrategies(NewStrategyStub(),
                NewStrategyStub(),
                NewStrategyStub());

            bool hasStrategy = WhenTheStrategyKeyIsChecked(NewRandomString());

            hasStrategy
                .Should()
                .BeFalse();
        }

        [TestMethod]
        public void GetsAllSupportedStrategies()
        {
            IReProfilingStrategy[] expectedStrategies = new[]
            {
                NewStrategyStub(),
                NewStrategyStub(),
                NewStrategyStub(),
                NewStrategyStub(),
                NewStrategyStub(),
                NewStrategyStub()
            };
            
            GivenTheSupportedStrategies(expectedStrategies);

            IEnumerable<IReProfilingStrategy> actualStrategies = WhenAllOfTheStrategiesAreQueried();

            actualStrategies
                .Should()
                .BeEquivalentTo<IReProfilingStrategy>(expectedStrategies);
        }

        private void GivenTheSupportedStrategies(params IReProfilingStrategy[] strategies)
        {
            _serviceLocator = new ReProfilingStrategyLocator(strategies);
        }

        private IReProfilingStrategy WhenTheStrategyIsLocated(string key)
            => _serviceLocator.GetStrategy(key);

        private bool WhenTheStrategyKeyIsChecked(string key)
            => _serviceLocator.HasStrategy(key);

        private IEnumerable<IReProfilingStrategy> WhenAllOfTheStrategiesAreQueried()
            => _serviceLocator.GetAllStrategies();

        private IReProfilingStrategy NewStrategyStub(Action<ReProfilingStrategyStubBuilder> setUp = null)
        {
            ReProfilingStrategyStubBuilder builder = new ReProfilingStrategyStubBuilder();

            setUp?.Invoke(builder);
            
            return builder.Build();
        }

        private string NewRandomString() => new RandomString();
    }
}