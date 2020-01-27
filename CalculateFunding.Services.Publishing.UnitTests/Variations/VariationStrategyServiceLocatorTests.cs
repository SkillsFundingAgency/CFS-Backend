using System;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Variations;
using CalculateFunding.Services.Publishing.Variations.Changes;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations
{
    [TestClass]
    public class VariationStrategyServiceLocatorTests
    {
        private VariationStrategyServiceLocator _serviceLocator;
        private IVariationStrategy _strategyOne;
        private IVariationStrategy _strategyTwo;

        [TestInitialize]
        public void SetUp()
        {
            _strategyOne = NewRandomStrategy();
            _strategyTwo = NewRandomStrategy();
            
            _serviceLocator = new VariationStrategyServiceLocator(new []{_strategyOne, _strategyTwo});
        }
        
        [TestMethod]
        public void LocatesRegisteredStrategiesByName_ExampleOne()
        {
            AssertThatStrategyCanBeLocatedByName(_strategyOne);
        }
        
        [TestMethod]
        public void LocatesRegisteredStrategiesByName_ExampleTwo()
        {
            AssertThatStrategyCanBeLocatedByName(_strategyTwo);
        }

        private void AssertThatStrategyCanBeLocatedByName(IVariationStrategy expectedStrategy)
        {
            IVariationStrategy actualStrategy = WhenTheStrategyIsLocated(expectedStrategy.Name);

            actualStrategy
                .Should()
                .Be(expectedStrategy);    
        }

        [TestMethod]
        public void ThrowsExceptionIfNoNameSupplied()
        {
            Func<IVariationStrategy> invocation = () => WhenTheStrategyIsLocated(null);
            
            invocation
                .Should()
                .ThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public void ThrowsExceptionIfNoStrategyForSuppliedName()
        {
            Func<IVariationStrategy> invocation = () => WhenTheStrategyIsLocated(NewRandomString());

            invocation
                .Should()
                .ThrowExactly<ArgumentOutOfRangeException>();
        }

        private IVariationStrategy WhenTheStrategyIsLocated(string name)
        {
            return _serviceLocator.GetService(name);
        }

        private IVariationStrategy NewRandomStrategy()
        {
            IVariationStrategy strategy = Substitute.For<IVariationStrategy>();

            strategy.Name
                .Returns(NewRandomString());
            
            return strategy;
        }

        private static string NewRandomString() => new RandomString();
    }
}