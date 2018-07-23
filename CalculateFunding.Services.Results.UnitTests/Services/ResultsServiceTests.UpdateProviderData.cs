using CalculateFunding.Models.Results;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Results.Interfaces;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.Services
{
    public partial class ResultsServiceTests
    {
        [TestMethod]
        public void PublishProviderResults_WhenMessageIsNull_ThenArgumentNullExceptionThrown()
        {
            // Arrange
            ResultsService resultsService = CreateResultsService();

            // Act
            Func<Task> test = () => resultsService.PublishProviderResults(null);

            //Assert
            test.ShouldThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("message");
        }

        [TestMethod]
        public void PublishProviderResults_WhenMessageDoesNotHaveSpecificationId_ThenArgumentExceptionThrown()
        {
            // Arrange
            ResultsService resultsService = CreateResultsService();
            Message message = new Message();

            // Act
            Func<Task> test = () => resultsService.PublishProviderResults(message);

            //Assert
            test.ShouldThrowExactly<ArgumentException>().And.Message.Should().Be("Message must contain a specification id");
        }

        [TestMethod]
        public void PublishProviderResults_WhenNoProviderResultsForSpecification_ThenArgumnetExceptionThrown()
        {
            // Arrange
            ResultsService resultsService = CreateResultsService();
            Message message = new Message();
            message.UserProperties["specification-id"] = "-1";

            // Act
            Func<Task> test = () => resultsService.PublishProviderResults(message);

            //Assert
            test.ShouldThrowExactly<ArgumentException>().And.Message.Should().Be("Could not find any provider results for specification");
        }

        [TestMethod]
        public void PublishProviderResults_WhenSpecificationNotFound_ThenArgumentExceptionThrown()
        {
            // Arrange
            string specificationId = "1";
            IEnumerable<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult()
            };

            ICalculationResultsRepository resultsRepository = CreateResultsRepository();
            resultsRepository.GetProviderResultsBySpecificationId(Arg.Is(specificationId), Arg.Is(-1))
                .Returns(Task.FromResult(providerResults));
            ResultsService resultsService = CreateResultsService(resultsRepository: resultsRepository);

            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;

            // Act
            Func<Task> test = () => resultsService.PublishProviderResults(message);

            //Assert
            test.ShouldThrowExactly<ArgumentException>().And.Message.Should().Be($"Specification not found for specification id {specificationId}");
        }

        [TestMethod]
        public void PublishProviderResults_WhenErrorSavingPublishedResults_ThenExceptionThrown()
        {
            // Arrange
            string specificationId = "1";
            IEnumerable<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult()
            };

            ICalculationResultsRepository resultsRepository = CreateResultsRepository();
            resultsRepository.GetProviderResultsBySpecificationId(Arg.Is(specificationId), Arg.Is(-1))
                .Returns(Task.FromResult(providerResults));
            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository.GetCurrentSpecificationById(Arg.Is(specificationId))
                .Returns(Task.FromResult(new SpecificationCurrentVersion()));
            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();
            publishedProviderResultsRepository.SavePublishedResults(Arg.Any<IEnumerable<PublishedProviderResult>>())
                .Returns(ex => { throw new Exception("Error saving published results"); });
            ResultsService resultsService = CreateResultsService(resultsRepository: resultsRepository,
                publishedProviderResultsRepository: publishedProviderResultsRepository,
                specificationsRepository: specificationsRepository);

            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;

            // Act
            Func<Task> test = () => resultsService.PublishProviderResults(message);

            //Assert
            var thrownException = test.ShouldThrowExactly<Exception>().Subject.First();
            thrownException.Message.Should().Be($"Failed to create published provider results for specification: {specificationId}");
            thrownException.InnerException.Should().NotBeNull();
            thrownException.InnerException.Message.Should().Be("Error saving published results");
        }

        [TestMethod]
        public void PublishProviderResults_WhenErrorSavingPublishedResultsVersionHistory_ThenExceptionThrown()
        {
            // Arrange
            string specificationId = "1";
            IEnumerable<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult()
            };

            ICalculationResultsRepository resultsRepository = CreateResultsRepository();
            resultsRepository.GetProviderResultsBySpecificationId(Arg.Is(specificationId), Arg.Is(-1))
                .Returns(Task.FromResult(providerResults));
            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository.GetCurrentSpecificationById(Arg.Is(specificationId))
                .Returns(Task.FromResult(new SpecificationCurrentVersion()));
            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();
            publishedProviderResultsRepository.SavePublishedResults(Arg.Any<IEnumerable<PublishedProviderResult>>())
                .Returns(Task.CompletedTask);
            publishedProviderResultsRepository.SavePublishedAllocationLineResultsHistory(Arg.Any<IEnumerable<PublishedAllocationLineResultHistory>>())
                .Returns(ex => { throw new Exception("Error saving published results version history"); });
            ResultsService resultsService = CreateResultsService(resultsRepository: resultsRepository,
                publishedProviderResultsRepository: publishedProviderResultsRepository,
                specificationsRepository: specificationsRepository);

            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;

            // Act
            Func<Task> test = () => resultsService.PublishProviderResults(message);

            //Assert
            var thrownException = test.ShouldThrowExactly<Exception>().Subject.First();
            thrownException.Message.Should().Be($"Failed to create published provider results for specification: {specificationId}");
            thrownException.InnerException.Should().NotBeNull();
            thrownException.InnerException.Message.Should().Be("Error saving published results version history");
        }

        [TestMethod]
        public void PublishProviderResults_WhenErrorSavingPublishedCalculationResults_ThenExceptionThrown()
        {
            // Arrange
            string specificationId = "1";
            IEnumerable<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult()
            };

            ICalculationResultsRepository resultsRepository = CreateResultsRepository();
            resultsRepository.GetProviderResultsBySpecificationId(Arg.Is(specificationId), Arg.Is(-1))
                .Returns(Task.FromResult(providerResults));
            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository.GetCurrentSpecificationById(Arg.Is(specificationId))
                .Returns(Task.FromResult(new SpecificationCurrentVersion()));
            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();
            publishedProviderResultsRepository.SavePublishedResults(Arg.Any<IEnumerable<PublishedProviderResult>>())
                .Returns(Task.CompletedTask);
            publishedProviderResultsRepository.SavePublishedAllocationLineResultsHistory(Arg.Any<IEnumerable<PublishedAllocationLineResultHistory>>())
                .Returns(Task.CompletedTask);
            IPublishedProviderCalculationResultsRepository publishedProviderCalculationResultsRepository = CreatePublishedProviderCalculationResultsRepository();
            publishedProviderCalculationResultsRepository.CreatePublishedCalculationResults(Arg.Any<IEnumerable<PublishedProviderCalculationResult>>())
                .Returns(ex => { throw new Exception("Error saving published calculation results"); });
            ResultsService resultsService = CreateResultsService(resultsRepository: resultsRepository,
                publishedProviderResultsRepository: publishedProviderResultsRepository,
                specificationsRepository: specificationsRepository,
                publishedProviderCalculationResultsRepository: publishedProviderCalculationResultsRepository);

            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;

            // Act
            Func<Task> test = () => resultsService.PublishProviderResults(message);

            //Assert
            var thrownException = test.ShouldThrowExactly<Exception>().Subject.First();
            thrownException.Message.Should().Be($"Failed to create published provider calculation results for specification: {specificationId}");
            thrownException.InnerException.Should().NotBeNull();
            thrownException.InnerException.Message.Should().Be("Error saving published calculation results");
        }

        [TestMethod]
        public void PublishProviderResults_WhenCompletesSuccessfully_ThenNoExceptionThrown()
        {
            // Arrange
            string specificationId = "1";
            IEnumerable<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult()
            };

            ICalculationResultsRepository resultsRepository = CreateResultsRepository();
            resultsRepository.GetProviderResultsBySpecificationId(Arg.Is(specificationId), Arg.Is(-1))
                .Returns(Task.FromResult(providerResults));
            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository.GetCurrentSpecificationById(Arg.Is(specificationId))
                .Returns(Task.FromResult(new SpecificationCurrentVersion()));
            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();
            publishedProviderResultsRepository.SavePublishedResults(Arg.Any<IEnumerable<PublishedProviderResult>>())
                .Returns(Task.CompletedTask);
            publishedProviderResultsRepository.SavePublishedAllocationLineResultsHistory(Arg.Any<IEnumerable<PublishedAllocationLineResultHistory>>())
                .Returns(Task.CompletedTask);
            IPublishedProviderCalculationResultsRepository publishedProviderCalculationResultsRepository = CreatePublishedProviderCalculationResultsRepository();
            publishedProviderCalculationResultsRepository.CreatePublishedCalculationResults(Arg.Any<IEnumerable<PublishedProviderCalculationResult>>())
                .Returns(Task.CompletedTask);
            ResultsService resultsService = CreateResultsService(resultsRepository: resultsRepository,
                publishedProviderResultsRepository: publishedProviderResultsRepository,
                specificationsRepository: specificationsRepository,
                publishedProviderCalculationResultsRepository: publishedProviderCalculationResultsRepository);

            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;

            // Act
            Func<Task> test = () => resultsService.PublishProviderResults(message);

            //Assert
            test.ShouldNotThrow();
        }
    }
}
