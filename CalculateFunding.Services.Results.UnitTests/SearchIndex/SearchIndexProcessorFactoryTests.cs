using CalculateFunding.Services.Results.Interfaces;
using CalculateFunding.Services.Results.SearchIndex;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;

namespace CalculateFunding.Services.Results.UnitTests.SearchIndex
{
    [TestClass]
    public class SearchIndexProcessorFactoryTests
    {
        private SearchIndexProcessorFactory _factory;
        private ISearchIndexProcessor _processorOne;
        private ISearchIndexProcessor _processorTwo;

        [TestInitialize]
        public void Setup()
        {
            _processorOne = Substitute.For<ISearchIndexProcessor>();
            _processorOne.IndexWriterType.Returns("IndexOne");
            _processorTwo = Substitute.For<ISearchIndexProcessor>();
            _processorTwo.IndexWriterType.Returns("IndexTwo");
            _factory = new SearchIndexProcessorFactory(new[] { _processorOne, _processorTwo });
        }

        [TestMethod]
        public void ShouldRaiseAnException_WhenNoIndexProcessorForGivenIndexWriterType()
        {
            string indexWriterType = new RandomString();
            string expectedErrorMessage = $"Search index writer not supported for {indexWriterType}";

            Func<ISearchIndexProcessor> test = () => _factory.CreateProcessor(indexWriterType);

            test
                .Should()
                .ThrowExactly<NotSupportedException>()
                .Which
                .Message
                .Should()
                .Be(expectedErrorMessage);
        }

        [DataTestMethod]
        [DataRow("IndexOne")]
        [DataRow("IndexTwo")]
        public void ShouldReturnCorrectProcessor_WhenIndexProcessorAvailableForGivenIndexWriterType(string indexWriterType)
        {
            ISearchIndexProcessor processor = _factory.CreateProcessor(indexWriterType);

            processor.IndexWriterType.Should().Be(indexWriterType);
        }
    }
}
