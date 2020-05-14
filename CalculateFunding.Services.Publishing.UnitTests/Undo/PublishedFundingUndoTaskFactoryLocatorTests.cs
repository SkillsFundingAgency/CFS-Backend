using System;
using CalculateFunding.Services.Publishing.Interfaces.Undo;
using CalculateFunding.Services.Publishing.Undo;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Serilog.Core;

namespace CalculateFunding.Services.Publishing.UnitTests.Undo
{
    [TestClass]
    public class PublishedFundingUndoTaskFactoryLocatorTests
    {
        private PublishedFundingUndoTaskFactoryLocator _factoryLocator;
        private Mock<IPublishedFundingUndoTaskFactory> _factoryOne;
        private Mock<IPublishedFundingUndoTaskFactory> _factoryTwo;

        private Mock<IPublishedFundingUndoTaskFactory>[] _factories;
        
        [TestInitialize]
        public void SetUp()
        {
            _factoryOne = NewMockFactory();    
            _factoryTwo = NewMockFactory();   
            
            _factories = new[]
            {
                _factoryOne,
                _factoryTwo
            };
            
            _factoryLocator = new PublishedFundingUndoTaskFactoryLocator(new []
            {
                _factoryOne.Object,
                _factoryTwo.Object
            }, Logger.None);
        }

        [TestMethod]
        public void GuardsAgainstUnsupportedParameters()
        {
            Func<IPublishedFundingUndoTaskFactory> invocation = () => WhenTheFactoryIsLocated(NewParameters());

            invocation
                .Should()
                .Throw<ArgumentOutOfRangeException>()
                .Which
                .ParamName
                .Should()
                .Be("parameters");
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        public void LocatesFactoryThatSupportsSuppliedParameters(int factoryIndex)
        {
            Mock<IPublishedFundingUndoTaskFactory> expectedFactory = _factories[factoryIndex];
            PublishedFundingUndoJobParameters parameters = NewParameters();

            GivenTheFactorySupportsTheParameters(expectedFactory, parameters);
            
            IPublishedFundingUndoTaskFactory actualFactory = WhenTheFactoryIsLocated(parameters);

            actualFactory
                .Should()
                .BeSameAs(expectedFactory.Object);
        }

        private void GivenTheFactorySupportsTheParameters(Mock<IPublishedFundingUndoTaskFactory> factory,
            PublishedFundingUndoJobParameters parameters)
        {
            factory.Setup(_ => _.IsForJob(parameters))
                .Returns(true);
        }
        
        private PublishedFundingUndoJobParameters NewParameters() 
            => new PublishedFundingUndoJobParametersBuilder().Build();

        private IPublishedFundingUndoTaskFactory WhenTheFactoryIsLocated(PublishedFundingUndoJobParameters parameters)
            => _factoryLocator.GetTaskFactoryFor(parameters);
        
        private Mock<IPublishedFundingUndoTaskFactory> NewMockFactory() => new Mock<IPublishedFundingUndoTaskFactory>();
        
    }
}