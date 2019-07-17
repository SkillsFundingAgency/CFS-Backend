using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CalculateFunding.Services.Publishing;
using FluentAssertions;
using CalculateFunding.Services.Publishing.Interfaces;
using NSubstitute;

namespace CalculateFunding.Services.Publishing.UnitTests.Services
{
    [TestClass]
    public class PublishedResultsServiceTests
    {
        [TestMethod]
        public void PublishProviderResults_WhenMessageIsNull_ThenArgumentNullExceptionThrown()
        {
            // Arrange
            PublishedResultService resultsService = CreateResultsService();

            // Act
            Func<Task> test = () => resultsService.GetProviderResultsBySpecificationId(null);

            //Assert
            test
                .Should().
                ThrowExactly<ArgumentNullException>()
                .And
                .ParamName
                .Should()
                .Be("specificationId");
        }

        static PublishedResultService CreateResultsService(
    IPublishingResiliencePolicies resiliencePolicies = null,
    ICalculationResultsRepository calculationResultsRepository = null)
        {
            return new PublishedResultService(
                resiliencePolicies ?? PublishingResiliencePolicies(),
                calculationResultsRepository ?? CalculationResultsRepository()
                );
        }

        static IPublishingResiliencePolicies PublishingResiliencePolicies()
        {
            return Substitute.For<IPublishingResiliencePolicies>();
        }

        static ICalculationResultsRepository CalculationResultsRepository()
        {
            return Substitute.For<ICalculationResultsRepository>();
        }
    }
}
