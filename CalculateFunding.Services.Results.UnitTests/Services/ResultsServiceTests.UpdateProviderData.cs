using CalculateFunding.Models;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Results.Interfaces;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
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
            test
                .Should().
                ThrowExactly<ArgumentNullException>()
                .And
                .ParamName
                .Should()
                .Be("message");
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
            test
                .Should()
                .ThrowExactly<ArgumentException>()
                .And
                .Message
                .Should()
                .Be("Message must contain a specification id");
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
            test.Should().ThrowExactly<ArgumentException>().And.Message.Should().Be("Could not find any provider results for specification");
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
            test.Should().ThrowExactly<ArgumentException>().And.Message.Should().Be($"Specification not found for specification id {specificationId}");
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
            var thrownException = test.Should().ThrowExactly<Exception>().Subject.First();
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
            var thrownException = test.Should().ThrowExactly<Exception>().Subject.First();
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
            var thrownException = test.Should().ThrowExactly<Exception>().Subject.First();
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
            test.Should().NotThrow();
        }

        [TestMethod]
        public async Task PublishProviderResults_WhenCalcResultsAssembled_EnsuresSavesCalcResultsAndHistory()
        {
            // Arrange
            string specificationId = "1";

            IEnumerable<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult()
            };

            IEnumerable<PublishedProviderCalculationResult> publishedProviderCalculationResults = new[]
            {
                new PublishedProviderCalculationResult
                {
                    Current = new PublishedProviderCalculationResultCalculationVersion
                    {
                        Provider = new ProviderSummary
                        {
                            Id = "prov-1"
                        }
                    }
                }
            };

            SpecificationCurrentVersion specificationCurrentVersion = new SpecificationCurrentVersion
            {
                Id = specificationId
            };

            ICalculationResultsRepository resultsRepository = CreateResultsRepository();
            resultsRepository.GetProviderResultsBySpecificationId(Arg.Is(specificationId), Arg.Is(-1))
                .Returns(Task.FromResult(providerResults));
            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository.GetCurrentSpecificationById(Arg.Is(specificationId))
                .Returns(specificationCurrentVersion);
            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();
            publishedProviderResultsRepository.SavePublishedResults(Arg.Any<IEnumerable<PublishedProviderResult>>())
                .Returns(Task.CompletedTask);
            publishedProviderResultsRepository.SavePublishedAllocationLineResultsHistory(Arg.Any<IEnumerable<PublishedAllocationLineResultHistory>>())
                .Returns(Task.CompletedTask);

            IPublishedProviderResultsAssemblerService assembler = CreateResultsAssembler();
            assembler
                .AssemblePublishedCalculationResults(Arg.Is(providerResults), Arg.Any<Reference>(), specificationCurrentVersion)
                .Returns(publishedProviderCalculationResults);

            IPublishedProviderCalculationResultsRepository publishedProviderCalculationResultsRepository = CreatePublishedProviderCalculationResultsRepository();

            ResultsService resultsService = CreateResultsService(resultsRepository: resultsRepository,
                publishedProviderResultsRepository: publishedProviderResultsRepository,
                specificationsRepository: specificationsRepository,
                publishedProviderCalculationResultsRepository: publishedProviderCalculationResultsRepository,
                publishedProviderResultsAssemblerService: assembler);

            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;

            // Act
            await resultsService.PublishProviderResults(message);

            //Assert
            await
                publishedProviderCalculationResultsRepository
                    .Received(1)
                    .CreatePublishedCalculationResults(Arg.Is<IEnumerable<PublishedProviderCalculationResult>>(m => m.Count() == 1));

            await
                publishedProviderCalculationResultsRepository
                .Received(1)
                .SavePublishedCalculationResultsHistory(Arg.Is<IEnumerable<PublishedProviderCalculationResultHistory>>(m => m.Count() == 1));
        }

        [TestMethod]
        public async Task PublishProviderResults_WhenCalcResultsAssembledButNoCurrent_LogsAndDoesNotSaveHistory()
        {
            // Arrange
            string specificationId = "1";

            IEnumerable<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult()
            };

            IEnumerable<PublishedProviderCalculationResult> publishedProviderCalculationResults = new[]
            {
                new PublishedProviderCalculationResult
                {
                    Id = "result-id"
                }
            };

            SpecificationCurrentVersion specificationCurrentVersion = new SpecificationCurrentVersion
            {
                Id = specificationId
            };

            ICalculationResultsRepository resultsRepository = CreateResultsRepository();
            resultsRepository.GetProviderResultsBySpecificationId(Arg.Is(specificationId), Arg.Is(-1))
                .Returns(Task.FromResult(providerResults));
            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository.GetCurrentSpecificationById(Arg.Is(specificationId))
                .Returns(specificationCurrentVersion);
            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();
            publishedProviderResultsRepository.SavePublishedResults(Arg.Any<IEnumerable<PublishedProviderResult>>())
                .Returns(Task.CompletedTask);
            publishedProviderResultsRepository.SavePublishedAllocationLineResultsHistory(Arg.Any<IEnumerable<PublishedAllocationLineResultHistory>>())
                .Returns(Task.CompletedTask);

            IPublishedProviderResultsAssemblerService assembler = CreateResultsAssembler();
            assembler
                .AssemblePublishedCalculationResults(Arg.Is(providerResults), Arg.Any<Reference>(), specificationCurrentVersion)
                .Returns(publishedProviderCalculationResults);

            IPublishedProviderCalculationResultsRepository publishedProviderCalculationResultsRepository = CreatePublishedProviderCalculationResultsRepository();

            ILogger logger = CreateLogger();
            ResultsService resultsService = CreateResultsService(
                logger,
                resultsRepository: resultsRepository,
                publishedProviderResultsRepository: publishedProviderResultsRepository,
                specificationsRepository: specificationsRepository,
                publishedProviderCalculationResultsRepository: publishedProviderCalculationResultsRepository,
                publishedProviderResultsAssemblerService: assembler);

            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;

            // Act
            await resultsService.PublishProviderResults(message);

            //Assert
            logger
                .Received(1)
                .Error("Null current object found on published calculation result for id: result-id");

            await
                publishedProviderCalculationResultsRepository
                    .DidNotReceive()
                    .SavePublishedCalculationResultsHistory(Arg.Any<IEnumerable<PublishedProviderCalculationResultHistory>>());
        }

        [TestMethod]
        public async Task PublishProviderResults_WhenCalcResultsAssembledWithNullHistory_EnsuresSavesCalcResultsAndHistoryWithCorrectValues()
        {
            // Arrange
            string specificationId = "1";

            IEnumerable<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult()
            };

            IEnumerable<PublishedProviderCalculationResult> publishedProviderCalculationResults = new[]
            {
                new PublishedProviderCalculationResult
                {
                    Id = "id-1",
                    Current = new PublishedProviderCalculationResultCalculationVersion
                    {
                        Provider = new ProviderSummary
                        {
                            Id = "prov-1"
                        },
                        CalculationType = PublishedCalculationType.Funding,
                        Author = new Reference("author-1", "author1"),
                        Version = 1,
                        Value = 100,
                        Date = DateTimeOffset.Now,
                        Commment = "comment"
                    }
                }
            };

            SpecificationCurrentVersion specificationCurrentVersion = new SpecificationCurrentVersion
            {
                Id = specificationId
            };

            ICalculationResultsRepository resultsRepository = CreateResultsRepository();
            resultsRepository.GetProviderResultsBySpecificationId(Arg.Is(specificationId), Arg.Is(-1))
                .Returns(Task.FromResult(providerResults));
            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository.GetCurrentSpecificationById(Arg.Is(specificationId))
                .Returns(specificationCurrentVersion);
            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();
            publishedProviderResultsRepository.SavePublishedResults(Arg.Any<IEnumerable<PublishedProviderResult>>())
                .Returns(Task.CompletedTask);
            publishedProviderResultsRepository.SavePublishedAllocationLineResultsHistory(Arg.Any<IEnumerable<PublishedAllocationLineResultHistory>>())
                .Returns(Task.CompletedTask);

            IPublishedProviderResultsAssemblerService assembler = CreateResultsAssembler();
            assembler
                .AssemblePublishedCalculationResults(Arg.Is(providerResults), Arg.Any<Reference>(), specificationCurrentVersion)
                .Returns(publishedProviderCalculationResults);

            IPublishedProviderCalculationResultsRepository publishedProviderCalculationResultsRepository = CreatePublishedProviderCalculationResultsRepository();
            publishedProviderCalculationResultsRepository
                .GetPublishedProviderCalculationHistoryForSpecificationId(Arg.Is(specificationId))
                .Returns((IEnumerable<PublishedProviderCalculationResultHistory>)null);

            ResultsService resultsService = CreateResultsService(resultsRepository: resultsRepository,
                publishedProviderResultsRepository: publishedProviderResultsRepository,
                specificationsRepository: specificationsRepository,
                publishedProviderCalculationResultsRepository: publishedProviderCalculationResultsRepository,
                publishedProviderResultsAssemblerService: assembler);

            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;

            // Act
            await resultsService.PublishProviderResults(message);

            //Assert
            await
                publishedProviderCalculationResultsRepository
                    .Received(1)
                    .CreatePublishedCalculationResults(Arg.Is<IEnumerable<PublishedProviderCalculationResult>>(
                        m => m.Count() == 1 &&
                        m.First().Id == "id-1" &&
                        m.First().Current.CalculationType == PublishedCalculationType.Funding &&
                        m.First().Current.Author.Id == "author-1" &&
                        m.First().Current.Author.Name == "author1" &&
                        m.First().Current.Commment == "comment" &&
                        m.First().Current.Date.Date == DateTimeOffset.Now.Date &&
                        m.First().Current.Value == 100 &&
                        m.First().Current.Version == 1 &&
                        m.First().Current.Provider.Id == "prov-1"));

            await
                publishedProviderCalculationResultsRepository
                .Received(1)
                .SavePublishedCalculationResultsHistory(Arg.Is<IEnumerable<PublishedProviderCalculationResultHistory>>(
                    m => m.Count() == 1 &&
                    m.First().Id == "id-1_hist" &&
                    m.First().ProviderId == "prov-1" &&
                    m.First().CalculationnResultId == "id-1" &&
                    m.First().History.First().Author.Id == "author-1" &&
                    m.First().History.First().Author.Name == "author1" &&
                    m.First().History.First().Commment == "comment" &&
                    m.First().History.First().Date.Date == DateTimeOffset.Now.Date &&
                    m.First().History.First().Value == 100 &&
                    m.First().History.First().Version == 1 &&
                    m.First().History.First().Provider.Id == "prov-1"));
        }

        [TestMethod]
        public async Task PublishProviderResults_WhenCalcResultsAssembledWithNoHistory_EnsuresSavesCalcResultsAndHistoryWithCorrectValues()
        {
            // Arrange
            string specificationId = "1";

            IEnumerable<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult()
            };

            IEnumerable<PublishedProviderCalculationResult> publishedProviderCalculationResults = new[]
            {
                new PublishedProviderCalculationResult
                {
                    Id = "id-1",
                    Current = new PublishedProviderCalculationResultCalculationVersion
                    {
                        Provider = new ProviderSummary
                        {
                            Id = "prov-1"
                        },
                        CalculationType = PublishedCalculationType.Funding,
                        Author = new Reference("author-1", "author1"),
                        Version = 1,
                        Value = 100,
                        Date = DateTimeOffset.Now,
                        Commment = "comment"
                    } 
                }
            };

            SpecificationCurrentVersion specificationCurrentVersion = new SpecificationCurrentVersion
            {
                Id = specificationId
            };

            ICalculationResultsRepository resultsRepository = CreateResultsRepository();
            resultsRepository.GetProviderResultsBySpecificationId(Arg.Is(specificationId), Arg.Is(-1))
                .Returns(Task.FromResult(providerResults));
            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository.GetCurrentSpecificationById(Arg.Is(specificationId))
                .Returns(specificationCurrentVersion);
            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();
            publishedProviderResultsRepository.SavePublishedResults(Arg.Any<IEnumerable<PublishedProviderResult>>())
                .Returns(Task.CompletedTask);
            publishedProviderResultsRepository.SavePublishedAllocationLineResultsHistory(Arg.Any<IEnumerable<PublishedAllocationLineResultHistory>>())
                .Returns(Task.CompletedTask);

            IPublishedProviderResultsAssemblerService assembler = CreateResultsAssembler();
            assembler
                .AssemblePublishedCalculationResults(Arg.Is(providerResults), Arg.Any<Reference>(), specificationCurrentVersion)
                .Returns(publishedProviderCalculationResults);

            IPublishedProviderCalculationResultsRepository publishedProviderCalculationResultsRepository = CreatePublishedProviderCalculationResultsRepository();

            ResultsService resultsService = CreateResultsService(resultsRepository: resultsRepository,
                publishedProviderResultsRepository: publishedProviderResultsRepository,
                specificationsRepository: specificationsRepository,
                publishedProviderCalculationResultsRepository: publishedProviderCalculationResultsRepository,
                publishedProviderResultsAssemblerService: assembler);

            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;

            // Act
            await resultsService.PublishProviderResults(message);

            //Assert
            await
                publishedProviderCalculationResultsRepository
                    .Received(1)
                    .CreatePublishedCalculationResults(Arg.Is<IEnumerable<PublishedProviderCalculationResult>>(
                        m => m.Count() == 1 && 
                        m.First().Id == "id-1" &&
                        m.First().Current.CalculationType == PublishedCalculationType.Funding &&
                        m.First().Current.Author.Id == "author-1" &&
                        m.First().Current.Author.Name == "author1" &&
                        m.First().Current.Commment == "comment" &&
                        m.First().Current.Date.Date == DateTimeOffset.Now.Date && 
                        m.First().Current.Value == 100 &&
                        m.First().Current.Version == 1 &&
                        m.First().Current.Provider.Id == "prov-1"));

            await
                publishedProviderCalculationResultsRepository
                .Received(1)
                .SavePublishedCalculationResultsHistory(Arg.Is<IEnumerable<PublishedProviderCalculationResultHistory>>(
                    m => m.Count() == 1 &&
                    m.First().Id == "id-1_hist" &&
                    m.First().ProviderId == "prov-1" &&
                    m.First().CalculationnResultId == "id-1" &&
                    m.First().History.First().Author.Id == "author-1" &&
                    m.First().History.First().Author.Name == "author1" &&
                    m.First().History.First().Commment == "comment" &&
                    m.First().History.First().Date.Date == DateTimeOffset.Now.Date &&
                    m.First().History.First().Value == 100 &&
                    m.First().History.First().Version == 1 &&
                    m.First().History.First().Provider.Id == "prov-1"));
        }

        [TestMethod]
        public async Task PublishProviderResults_WhenCalcResultsAssembledWithHistory_EnsuresSavesCalcResultsAndHistoryWithCorrectValues()
        {
            // Arrange
            string specificationId = "1";

            IEnumerable<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult()
            };

            IEnumerable<PublishedProviderCalculationResult> publishedProviderCalculationResults = new[]
            {
                new PublishedProviderCalculationResult
                {
                    Id = "id-1",
                    Current = new PublishedProviderCalculationResultCalculationVersion
                    {
                        Provider = new ProviderSummary
                        {
                            Id = "prov-1"
                        },
                        CalculationType = PublishedCalculationType.Funding,
                        Author = new Reference("author-2", "author2"),
                        Version = 2,
                        Value = 200,
                        Date = DateTimeOffset.Now,
                        Commment = "comment"
                    }
                }
            };

            IEnumerable<PublishedProviderCalculationResultHistory> publishedProviderCalculationResultsHistory = new[]
            {
                new PublishedProviderCalculationResultHistory
                {
                    CalculationnResultId = "id-1",
                    History = new[]
                    {
                        new PublishedProviderCalculationResultCalculationVersion
                        {
                            Provider = new ProviderSummary
                            {
                                Id = "prov-1"
                            },
                            CalculationType = PublishedCalculationType.Funding,
                            Author = new Reference("author-1", "author1"),
                            Version = 1,
                            Value = 100,
                            Date = DateTimeOffset.Now,
                            Commment = "comment"
                        }
                    }
                }
            };

            SpecificationCurrentVersion specificationCurrentVersion = new SpecificationCurrentVersion
            {
                Id = specificationId
            };

            ICalculationResultsRepository resultsRepository = CreateResultsRepository();
            resultsRepository.GetProviderResultsBySpecificationId(Arg.Is(specificationId), Arg.Is(-1))
                .Returns(Task.FromResult(providerResults));
            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository.GetCurrentSpecificationById(Arg.Is(specificationId))
                .Returns(specificationCurrentVersion);
            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();
            publishedProviderResultsRepository.SavePublishedResults(Arg.Any<IEnumerable<PublishedProviderResult>>())
                .Returns(Task.CompletedTask);
            publishedProviderResultsRepository.SavePublishedAllocationLineResultsHistory(Arg.Any<IEnumerable<PublishedAllocationLineResultHistory>>())
                .Returns(Task.CompletedTask);

            IPublishedProviderResultsAssemblerService assembler = CreateResultsAssembler();
            assembler
                .AssemblePublishedCalculationResults(Arg.Is(providerResults), Arg.Any<Reference>(), specificationCurrentVersion)
                .Returns(publishedProviderCalculationResults);

            IPublishedProviderCalculationResultsRepository publishedProviderCalculationResultsRepository = CreatePublishedProviderCalculationResultsRepository();
            publishedProviderCalculationResultsRepository
                .GetPublishedProviderCalculationHistoryForSpecificationId(Arg.Is(specificationId))
                .Returns(publishedProviderCalculationResultsHistory);

            ResultsService resultsService = CreateResultsService(resultsRepository: resultsRepository,
                publishedProviderResultsRepository: publishedProviderResultsRepository,
                specificationsRepository: specificationsRepository,
                publishedProviderCalculationResultsRepository: publishedProviderCalculationResultsRepository,
                publishedProviderResultsAssemblerService: assembler);

            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;

            // Act
            await resultsService.PublishProviderResults(message);

            //Assert
            await
                publishedProviderCalculationResultsRepository
                    .Received(1)
                    .CreatePublishedCalculationResults(Arg.Is<IEnumerable<PublishedProviderCalculationResult>>(
                        m => m.Count() == 1 &&
                        m.First().Current.Value == 200 &&
                        m.First().Current.Version == 2));

            await
                publishedProviderCalculationResultsRepository
                .Received(1)
                .SavePublishedCalculationResultsHistory(Arg.Is<IEnumerable<PublishedProviderCalculationResultHistory>>(
                    m => m.Count() == 1 &&
                    m.First().History.Count() == 2 &&
                    m.First().History.First().Author.Id == "author-1" &&
                    m.First().History.First().Author.Name == "author1" &&
                    m.First().History.First().Commment == "comment" &&
                    m.First().History.First().Date.Date == DateTimeOffset.Now.Date &&
                    m.First().History.First().Value == 100 &&
                    m.First().History.First().Version == 1 &&
                    m.First().History.First().Provider.Id == "prov-1" &&
                    m.First().History.ElementAt(1).Author.Id == "author-2" &&
                    m.First().History.ElementAt(1).Author.Name == "author2" &&
                    m.First().History.ElementAt(1).Commment == "comment" &&
                    m.First().History.ElementAt(1).Date.Date == DateTimeOffset.Now.Date &&
                    m.First().History.ElementAt(1).Value == 200 &&
                    m.First().History.ElementAt(1).Version == 2 &&
                    m.First().History.ElementAt(1).Provider.Id == "prov-1"));
        }
    }
}
