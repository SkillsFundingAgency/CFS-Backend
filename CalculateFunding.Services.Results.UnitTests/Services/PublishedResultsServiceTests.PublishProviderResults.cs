using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Models;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Results.Interfaces;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Results.Services
{
    public partial class PublishedResultsServiceTests
    {
        private const string SpecificationId1 = "specId1";
        private const string RedisPrependKey = "calculation-progress:";

        [TestMethod]
        public void PublishProviderResults_WhenMessageIsNull_ThenArgumentNullExceptionThrown()
        {
            // Arrange
            PublishedResultsService resultsService = CreateResultsService();

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
            PublishedResultsService resultsService = CreateResultsService();
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
            PublishedResultsService resultsService = CreateResultsService();
            Message message = new Message();
            message.UserProperties["specification-id"] = "-1";

            // Act
            Func<Task> test = () => resultsService.PublishProviderResults(message);

            //Assert
            test.Should().ThrowExactly<ArgumentException>().And.Message.Should().Be("Specification not found for specification id -1");
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
            PublishedResultsService resultsService = CreateResultsService(resultsRepository: resultsRepository);

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

            SpecificationCurrentVersion specificationCurrentVersion = new SpecificationCurrentVersion
            {
                Id = specificationId
            };

            IEnumerable<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult()
            };

            List<PublishedProviderResult> publishedProviderResults = new List<PublishedProviderResult>()
            {
                new PublishedProviderResult
                {
                    FundingStreamResult = new PublishedFundingStreamResult
                    {
                        AllocationLineResult = new PublishedAllocationLineResult()
                    }
                }
            };

            IEnumerable<PublishedProviderCalculationResult> publishedProviderCalculationResults = new[]
            {
                new PublishedProviderCalculationResult
                {
                    Current = new PublishedProviderCalculationResultVersion
                    {
                        Provider = new ProviderSummary
                        {
                            Id = "prov-1"
                        }
                    },
                    Specification = new Reference{ Id = "spec-1", Name ="spec1" },
                    CalculationSpecification = new Reference { Id = "calc-1", Name = "calc1" }
                }
            };

            ICalculationResultsRepository resultsRepository = CreateResultsRepository();
            resultsRepository.GetProviderResultsBySpecificationId(Arg.Is(specificationId), Arg.Is(-1))
                .Returns(Task.FromResult(providerResults));
            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository.GetCurrentSpecificationById(Arg.Is(specificationId))
                .Returns(specificationCurrentVersion);
            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();
            publishedProviderResultsRepository.SavePublishedResults(Arg.Any<IEnumerable<PublishedProviderResult>>())
                .Returns(ex => { throw new Exception("Error saving published results"); });

            IPublishedProviderResultsAssemblerService assembler = CreateResultsAssembler();
            assembler
               .AssemblePublishedCalculationResults(Arg.Is(providerResults), Arg.Any<Reference>(), specificationCurrentVersion)
               .Returns(publishedProviderCalculationResults);

            assembler
                .GeneratePublishedProviderResultsToSave(Arg.Any<IEnumerable<PublishedProviderResult>>(), Arg.Any<IEnumerable<PublishedProviderResultExisting>>())
                .Returns((publishedProviderResults, Enumerable.Empty<PublishedProviderResultExisting>()));

            PublishedResultsService resultsService = CreateResultsService(
                resultsRepository: resultsRepository,
                publishedProviderResultsRepository: publishedProviderResultsRepository,
                specificationsRepository: specificationsRepository,
                publishedProviderResultsAssemblerService: assembler);

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

            SpecificationCurrentVersion specificationCurrentVersion = new SpecificationCurrentVersion
            {
                Id = specificationId
            };

            IEnumerable<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult()
            };

            List<PublishedProviderResult> publishedProviderResults = new List<PublishedProviderResult>()
            {
                new PublishedProviderResult()
                {
                    ProviderId = "1",
                    FundingStreamResult = new PublishedFundingStreamResult()
                    {
                        AllocationLineResult = new PublishedAllocationLineResult()
                        {
                            AllocationLine = new AllocationLine()
                            {
                                Id = "AAAAA",
                                Name = "Allocation line 1",
                            },
                            Current = new PublishedAllocationLineResultVersion()
                            {
                                Value = 1,
                            }
                        }
                    }
                },
            };

           IEnumerable<PublishedProviderCalculationResult> publishedProviderCalculationResults = new[]
           {
                new PublishedProviderCalculationResult
                {
                    Current = new PublishedProviderCalculationResultVersion
                    {
                        Provider = new ProviderSummary
                        {
                            Id = "prov-1"
                        }
                    },
                    Specification = new Reference{ Id = "spec-1", Name ="spec1" },
                    CalculationSpecification = new Reference { Id = "calc-1", Name = "calc1" }
                }
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

            IVersionRepository<PublishedAllocationLineResultVersion> versionRepository = CreatePublishedProviderResultsVersionRepository();
            versionRepository.SaveVersions(Arg.Any<IEnumerable<KeyValuePair<string, PublishedAllocationLineResultVersion>>>())
                .Returns(ex => { throw new Exception("Error saving published results version history"); });

            IPublishedProviderResultsAssemblerService assembler = CreateResultsAssembler();

            assembler
                .GeneratePublishedProviderResultsToSave(Arg.Any<IEnumerable<PublishedProviderResult>>(), Arg.Any<IEnumerable<PublishedProviderResultExisting>>())
                .Returns((publishedProviderResults, Enumerable.Empty<PublishedProviderResultExisting>()));

            assembler
              .AssemblePublishedCalculationResults(Arg.Is(providerResults), Arg.Any<Reference>(), specificationCurrentVersion)
              .Returns(publishedProviderCalculationResults);


            PublishedResultsService resultsService = CreateResultsService(
                resultsRepository: resultsRepository,
                publishedProviderResultsRepository: publishedProviderResultsRepository,
                specificationsRepository: specificationsRepository,
                publishedProviderResultsAssemblerService: assembler,
                publishedProviderResultsVersionRepository: versionRepository);

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
            IVersionRepository<PublishedAllocationLineResultVersion> versionRepository = CreatePublishedProviderResultsVersionRepository();
            versionRepository.SaveVersions(Arg.Any<IEnumerable<PublishedAllocationLineResultVersion>>())
                .Returns(Task.CompletedTask);
            
            IPublishedProviderCalculationResultsRepository publishedProviderCalculationResultsRepository = CreatePublishedProviderCalculationResultsRepository();
            publishedProviderCalculationResultsRepository.CreatePublishedCalculationResults(Arg.Any<IEnumerable<PublishedProviderCalculationResult>>())
                .Returns(ex => { throw new Exception("Error saving published calculation results"); });

            PublishedResultsService resultsService = CreateResultsService(resultsRepository: resultsRepository,
                publishedProviderResultsRepository: publishedProviderResultsRepository,
                specificationsRepository: specificationsRepository,
                publishedProviderCalculationResultsRepository: publishedProviderCalculationResultsRepository,
                publishedProviderResultsVersionRepository: versionRepository);

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

            SpecificationCurrentVersion specificationCurrentVersion = new SpecificationCurrentVersion
            {
                Id = specificationId
            };

            IEnumerable<PublishedProviderCalculationResult> publishedProviderCalculationResults = new[]
            {
                new PublishedProviderCalculationResult
                {
                    Current = new PublishedProviderCalculationResultVersion
                    {
                        Provider = new ProviderSummary
                        {
                            Id = "prov-1"
                        }
                    },
                    Specification = new Reference{ Id = "spec-1", Name ="spec1" },
                    CalculationSpecification = new Reference { Id = "calc-1", Name = "calc1" }
                }
            };

            IEnumerable<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult()
            };

            ICalculationResultsRepository resultsRepository = CreateResultsRepository();
            resultsRepository.GetProviderResultsBySpecificationId(Arg.Is(specificationId), Arg.Is(-1))
                .Returns(Task.FromResult(providerResults));

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository.GetCurrentSpecificationById(Arg.Is(specificationId))
                .Returns(specificationCurrentVersion);

            specificationsRepository.UpdatePublishedRefreshedDate(Arg.Is(specificationId), Arg.Any<DateTimeOffset>())
                .Returns(Task.FromResult(HttpStatusCode.OK));

            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();
            publishedProviderResultsRepository.SavePublishedResults(Arg.Any<IEnumerable<PublishedProviderResult>>())
                .Returns(Task.CompletedTask);

            IVersionRepository<PublishedAllocationLineResultVersion> versionRepository = CreatePublishedProviderResultsVersionRepository();
            versionRepository.SaveVersions(Arg.Any<IEnumerable<PublishedAllocationLineResultVersion>>())
                .Returns(Task.CompletedTask);

            IPublishedProviderCalculationResultsRepository publishedProviderCalculationResultsRepository = CreatePublishedProviderCalculationResultsRepository();
            publishedProviderCalculationResultsRepository.CreatePublishedCalculationResults(Arg.Any<IEnumerable<PublishedProviderCalculationResult>>())
                .Returns(Task.CompletedTask);

            bool hasCalcsChanged = true;

            IPublishedProviderResultsAssemblerService assembler = CreateResultsAssembler();
            assembler
               .AssemblePublishedCalculationResults(Arg.Is(providerResults), Arg.Any<Reference>(), specificationCurrentVersion)
               .Returns(publishedProviderCalculationResults);

            ILogger logger = CreateLogger();

            PublishedResultsService resultsService = CreateResultsService(resultsRepository: resultsRepository,
                publishedProviderResultsRepository: publishedProviderResultsRepository,
                specificationsRepository: specificationsRepository,
                publishedProviderCalculationResultsRepository: publishedProviderCalculationResultsRepository,
                publishedProviderResultsVersionRepository: versionRepository,
                publishedProviderResultsAssemblerService: assembler,
                logger: logger);

            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;

            // Act
            Func<Task> test = () => resultsService.PublishProviderResults(message);

            //Assert
            test.Should().NotThrow();
            logger.DidNotReceive().Error(Arg.Any<string>());
            logger.Received(1).Information(Arg.Is($"Updated the published refresh date on the specification with id: {specificationId}"));
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
                    Current = new PublishedProviderCalculationResultVersion
                    {
                        Provider = new ProviderSummary
                        {
                            Id = "prov-1"
                        }
                    },
                    Specification = new Reference{ Id = "spec-1", Name ="spec1" },
                    CalculationSpecification = new Reference { Id = "calc-1", Name = "calc1" }
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

            IVersionRepository<PublishedAllocationLineResultVersion> versionRepository = CreatePublishedProviderResultsVersionRepository();
            versionRepository.SaveVersions(Arg.Any<IEnumerable<PublishedAllocationLineResultVersion>>())
                .Returns(Task.CompletedTask);

            IVersionRepository<PublishedProviderCalculationResultVersion> calcsVersionRepository = CreatePublishedProviderCalcResultsVersionRepository();

            IPublishedProviderResultsAssemblerService assembler = CreateResultsAssembler();
            assembler
                .AssemblePublishedCalculationResults(Arg.Is(providerResults), Arg.Any<Reference>(), specificationCurrentVersion)
                .Returns(publishedProviderCalculationResults);

            IPublishedProviderCalculationResultsRepository publishedProviderCalculationResultsRepository = CreatePublishedProviderCalculationResultsRepository();

            PublishedResultsService resultsService = CreateResultsService(resultsRepository: resultsRepository,
                publishedProviderResultsRepository: publishedProviderResultsRepository,
                specificationsRepository: specificationsRepository,
                publishedProviderCalculationResultsRepository: publishedProviderCalculationResultsRepository,
                publishedProviderResultsAssemblerService: assembler,
                publishedProviderResultsVersionRepository: versionRepository,
                publishedProviderCalcResultsVersionRepository: calcsVersionRepository);

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
                calcsVersionRepository
                .Received(1)
                .SaveVersions(Arg.Is<IEnumerable<KeyValuePair<string,PublishedProviderCalculationResultVersion>>>(m => m.Count() == 1));
        }

        [TestMethod]
        public async Task PublishProviderResults_WhenCalcResultsAssembledButNoCurrent_LogsAndDoesNotSaveHistory()
        {
            // Arrange
            string specificationId = "spec-1";
            string calculationId = "calc-1";
            string providerId = "prov-1";

            IEnumerable<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult()
            };

            IEnumerable<PublishedProviderCalculationResult> publishedProviderCalculationResults = new[]
            {
                new PublishedProviderCalculationResult
                {
                    Specification = new Reference{ Id = specificationId, Name ="spec1" },
                    CalculationSpecification = new Reference { Id = calculationId, Name = "calc1" },
                    ProviderId = providerId
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

            IVersionRepository<PublishedAllocationLineResultVersion> versionRepository = CreatePublishedProviderResultsVersionRepository();
            versionRepository.SaveVersions(Arg.Any<IEnumerable<PublishedAllocationLineResultVersion>>())
                .Returns(Task.CompletedTask);

            IVersionRepository<PublishedProviderCalculationResultVersion> calcsVersionRepository = CreatePublishedProviderCalcResultsVersionRepository();

            IPublishedProviderResultsAssemblerService assembler = CreateResultsAssembler();
            assembler
                .AssemblePublishedCalculationResults(Arg.Is(providerResults), Arg.Any<Reference>(), specificationCurrentVersion)
                .Returns(publishedProviderCalculationResults);

            IPublishedProviderCalculationResultsRepository publishedProviderCalculationResultsRepository = CreatePublishedProviderCalculationResultsRepository();

            ILogger logger = CreateLogger();
            PublishedResultsService resultsService = CreateResultsService(
                logger,
                resultsRepository: resultsRepository,
                publishedProviderResultsRepository: publishedProviderResultsRepository,
                specificationsRepository: specificationsRepository,
                publishedProviderCalculationResultsRepository: publishedProviderCalculationResultsRepository,
                publishedProviderResultsAssemblerService: assembler,
                publishedProviderResultsVersionRepository: versionRepository,
                publishedProviderCalcResultsVersionRepository: calcsVersionRepository);

            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;

            string resultId = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{specificationId}{providerId}{calculationId}"));

            // Act
            await resultsService.PublishProviderResults(message);

            //Assert
            await
                calcsVersionRepository
                    .DidNotReceive()
                    .SaveVersions(Arg.Any<IEnumerable<PublishedProviderCalculationResultVersion>>());
        }

        [TestMethod]
        public async Task PublishProviderResults_WhenCalcResultsAssembledWithNullHistory_EnsuresSavesCalcResultsAndHistoryWithCorrectValues()
        {
            // Arrange
            string specificationId = "spec-1";
            string calculationId = "calc-1";
            string providerId = "prov-1";

            string resultId = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{specificationId}{providerId}{calculationId}"));

            IEnumerable<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult()
            };

            IEnumerable<PublishedProviderCalculationResult> publishedProviderCalculationResults = new[]
            {
                new PublishedProviderCalculationResult
                {
                    Specification = new Reference{ Id = specificationId, Name ="spec1" },
                    CalculationSpecification = new Reference { Id = calculationId, Name = "calc1" },
                    ProviderId = providerId,
                    Current = new PublishedProviderCalculationResultVersion
                    {
                        Provider = new ProviderSummary
                        {
                            Id = providerId
                        },
                        CalculationType = PublishedCalculationType.Funding,
                        Author = new Reference("author-1", "author1"),
                        Version = 1,
                        Value = 100,
                        Date = DateTimeOffset.Now,
                        Commment = "comment",
                        CalculationnResultId = resultId,
                        ProviderId = providerId,
                        SpecificationId = specificationId
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

            IVersionRepository<PublishedAllocationLineResultVersion> versionRepository = CreatePublishedProviderResultsVersionRepository();
            versionRepository.SaveVersions(Arg.Any<IEnumerable<PublishedAllocationLineResultVersion>>())
                .Returns(Task.CompletedTask);

            IVersionRepository<PublishedProviderCalculationResultVersion> calcsVersionRepository = CreatePublishedProviderCalcResultsVersionRepository();

            IPublishedProviderResultsAssemblerService assembler = CreateResultsAssembler();
            assembler
                .AssemblePublishedCalculationResults(Arg.Is(providerResults), Arg.Any<Reference>(), specificationCurrentVersion)
                .Returns(publishedProviderCalculationResults);

            IPublishedProviderCalculationResultsRepository publishedProviderCalculationResultsRepository = CreatePublishedProviderCalculationResultsRepository();

            PublishedResultsService resultsService = CreateResultsService(resultsRepository: resultsRepository,
                publishedProviderResultsRepository: publishedProviderResultsRepository,
                specificationsRepository: specificationsRepository,
                publishedProviderCalculationResultsRepository: publishedProviderCalculationResultsRepository,
                publishedProviderResultsAssemblerService: assembler,
                publishedProviderResultsVersionRepository: versionRepository,
                publishedProviderCalcResultsVersionRepository: calcsVersionRepository);

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
                        m.First().Id == resultId &&
                        m.First().Current.CalculationType == PublishedCalculationType.Funding &&
                        m.First().Current.Author.Id == "author-1" &&
                        m.First().Current.Author.Name == "author1" &&
                        m.First().Current.Commment == "comment" &&
                        m.First().Current.Date.Date == DateTimeOffset.Now.Date &&
                        m.First().Current.Value == 100 &&
                        m.First().Current.Version == 1 &&
                        m.First().Current.Provider.Id == "prov-1"));

            await
                calcsVersionRepository
                .Received(1)
                .SaveVersions(Arg.Is<IEnumerable<KeyValuePair<string, PublishedProviderCalculationResultVersion>>>(
                    m => m.Count() == 1 &&
                    m.First().Key == providerId &&
                    m.First().Value.Id == $"{resultId}_version_1" &&
                    m.First().Value.ProviderId == providerId &&
                    m.First().Value.CalculationnResultId == resultId &&
                    m.First().Value.Author.Id == "author-1" &&
                    m.First().Value.Author.Name == "author1" &&
                    m.First().Value.Commment == "comment" &&
                    m.First().Value.Date.Date == DateTimeOffset.Now.Date &&
                    m.First().Value.Value == 100 &&
                    m.First().Value.Version == 1 &&
                    m.First().Value.Provider.Id == providerId));
        }

        [TestMethod]
        public async Task PublishProviderResults_WhenCalcResultsAssembledWithNoHistory_EnsuresSavesCalcResultsAndHistoryWithCorrectValues()
        {
            // Arrange
            string specificationId = "spec-1";
            string calculationId = "calc-1";
            string providerId = "prov-1";

            string resultId = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{specificationId}{providerId}{calculationId}"));

            IEnumerable<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult()
            };

            IEnumerable<PublishedProviderCalculationResult> publishedProviderCalculationResults = new[]
            {
                new PublishedProviderCalculationResult
                {
                    Specification = new Reference{ Id = specificationId, Name ="spec1" },
                    CalculationSpecification = new Reference { Id = calculationId, Name = "calc1" },
                    ProviderId = providerId,
                    Current = new PublishedProviderCalculationResultVersion
                    {
                        Provider = new ProviderSummary
                        {
                            Id = providerId
                        },
                        CalculationType = PublishedCalculationType.Funding,
                        Author = new Reference("author-1", "author1"),
                        Version = 1,
                        Value = 100,
                        Date = DateTimeOffset.Now,
                        Commment = "comment",
                        CalculationnResultId = resultId,
                        ProviderId = providerId,
                        SpecificationId = specificationId
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

            IVersionRepository<PublishedAllocationLineResultVersion> versionRepository = CreatePublishedProviderResultsVersionRepository();
            versionRepository.SaveVersions(Arg.Any<IEnumerable<PublishedAllocationLineResultVersion>>())
                .Returns(Task.CompletedTask);

            IVersionRepository<PublishedProviderCalculationResultVersion> calcsVersionRepository = CreatePublishedProviderCalcResultsVersionRepository();

            IPublishedProviderResultsAssemblerService assembler = CreateResultsAssembler();
            assembler
                .AssemblePublishedCalculationResults(Arg.Is(providerResults), Arg.Any<Reference>(), specificationCurrentVersion)
                .Returns(publishedProviderCalculationResults);

            IPublishedProviderCalculationResultsRepository publishedProviderCalculationResultsRepository = CreatePublishedProviderCalculationResultsRepository();

            PublishedResultsService resultsService = CreateResultsService(resultsRepository: resultsRepository,
                publishedProviderResultsRepository: publishedProviderResultsRepository,
                specificationsRepository: specificationsRepository,
                publishedProviderCalculationResultsRepository: publishedProviderCalculationResultsRepository,
                publishedProviderResultsAssemblerService: assembler,
                publishedProviderResultsVersionRepository: versionRepository,
                publishedProviderCalcResultsVersionRepository: calcsVersionRepository);

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
                        m.First().Id == resultId &&
                        m.First().Current.CalculationType == PublishedCalculationType.Funding &&
                        m.First().Current.Author.Id == "author-1" &&
                        m.First().Current.Author.Name == "author1" &&
                        m.First().Current.Commment == "comment" &&
                        m.First().Current.Date.Date == DateTimeOffset.Now.Date &&
                        m.First().Current.Value == 100 &&
                        m.First().Current.Version == 1 &&
                        m.First().Current.Provider.Id == providerId));

            await
                calcsVersionRepository
                .Received(1)
                .SaveVersions(Arg.Is<IEnumerable<KeyValuePair<string,PublishedProviderCalculationResultVersion>>>(
                    m => m.Count() == 1 &&
                    m.First().Key == "prov-1" &&
                    m.First().Value.Id == $"{resultId}_version_1" &&
                    m.First().Value.ProviderId == "prov-1" &&
                    m.First().Value.CalculationnResultId == resultId &&
                    m.First().Value.Author.Id == "author-1" &&
                    m.First().Value.Author.Name == "author1" &&
                    m.First().Value.Commment == "comment" &&
                    m.First().Value.Date.Date == DateTimeOffset.Now.Date &&
                    m.First().Value.Value == 100 &&
                    m.First().Value.Version == 1 &&
                    m.First().Value.Provider.Id == "prov-1"));
        }

        [TestMethod]
        public async Task PublishProviderResults_WhenCalcResultsAssembledWithHistory_EnsuresSavesCalcResultsAndHistoryWithCorrectValues()
        {
            // Arrange
            string specificationId = "spec-1";
            string calculationId = "calc-1";
            string providerId = "prov-1";

            string resultId = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{specificationId}{providerId}{calculationId}"));

            IEnumerable<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult()
            };

            IEnumerable<PublishedProviderCalculationResult> publishedProviderCalculationResults = new[]
            {
                new PublishedProviderCalculationResult
                {
                    Specification = new Reference{ Id = specificationId, Name ="spec1" },
                    CalculationSpecification = new Reference { Id = calculationId, Name = "calc1" },
                    ProviderId = providerId,
                    Current = new PublishedProviderCalculationResultVersion
                    {
                        Provider = new ProviderSummary
                        {
                            Id = providerId
                        },
                        CalculationType = PublishedCalculationType.Funding,
                        Author = new Reference("author-2", "author2"),
                        Version = 2,
                        Value = 200,
                        Date = DateTimeOffset.Now,
                        Commment = "comment",
                        ProviderId = providerId
                    }
                }
            };

            IEnumerable<PublishedProviderCalculationResultHistory> publishedProviderCalculationResultsHistory = new[]
            {
                new PublishedProviderCalculationResultHistory
                {
                    CalculationnResultId = resultId,
                    SpecificationId = specificationId,
                    ProviderId = providerId,
                    History = new[]
                    {
                        new PublishedProviderCalculationResultVersion
                        {
                            Provider = new ProviderSummary
                            {
                                Id = providerId
                            },
                            CalculationType = PublishedCalculationType.Funding,
                            Author = new Reference("author-1", "author1"),
                            Version = 1,
                            Value = 100,
                            Date = DateTimeOffset.Now,
                            Commment = "comment",
                            ProviderId = providerId
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

            IVersionRepository<PublishedAllocationLineResultVersion> versionRepository = CreatePublishedProviderResultsVersionRepository();
            versionRepository.SaveVersions(Arg.Any<IEnumerable<PublishedAllocationLineResultVersion>>())
                .Returns(Task.CompletedTask);

            IPublishedProviderResultsAssemblerService assembler = CreateResultsAssembler();
            assembler
                .AssemblePublishedCalculationResults(Arg.Is(providerResults), Arg.Any<Reference>(), specificationCurrentVersion)
                .Returns(publishedProviderCalculationResults);

            IPublishedProviderCalculationResultsRepository publishedProviderCalculationResultsRepository = CreatePublishedProviderCalculationResultsRepository();

            IVersionRepository<PublishedProviderCalculationResultVersion> calcsVersionRepository = CreatePublishedProviderCalcResultsVersionRepository();

            PublishedResultsService resultsService = CreateResultsService(resultsRepository: resultsRepository,
                publishedProviderResultsRepository: publishedProviderResultsRepository,
                specificationsRepository: specificationsRepository,
                publishedProviderCalculationResultsRepository: publishedProviderCalculationResultsRepository,
                publishedProviderResultsAssemblerService: assembler,
                publishedProviderResultsVersionRepository: versionRepository,
                publishedProviderCalcResultsVersionRepository: calcsVersionRepository);

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
                calcsVersionRepository
                .Received(1)
                .SaveVersions(Arg.Is<IEnumerable<KeyValuePair<string, PublishedProviderCalculationResultVersion>>>(
                    m => m.Count() == 1 &&
                    m.First().Key == providerId &&
                    m.First().Value.Author.Id == "author-2" &&
                    m.First().Value.Author.Name == "author2" &&
                    m.First().Value.Commment == "comment" &&
                    m.First().Value.Date.Date == DateTimeOffset.Now.Date &&
                    m.First().Value.Value == 200 &&
                    m.First().Value.Version == 2 &&
                    m.First().Value.Provider.Id == providerId));
        }

        [TestMethod]
        public void PublishProviderResults_WhenNoExceptionThrown_ShouldReportProgressOnCacheCorrectly()
        {
            // Arrange
            string specificationId = SpecificationId1;
            IEnumerable<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult()
            };

            List<PublishedProviderResult> publishedProviderResults = new List<PublishedProviderResult>()
            {
                new PublishedProviderResult()
                {
                    ProviderId = "1",
                    FundingStreamResult = new PublishedFundingStreamResult()
                    {
                        AllocationLineResult = new PublishedAllocationLineResult()
                        {
                            AllocationLine = new AllocationLine()
                            {
                                Id = "AAAAA",
                                Name = "Allocation line 1",
                            },
                            Current = new PublishedAllocationLineResultVersion()
                            {
                                Value = 1,
                            }
                        }
                    }
                },
            };

            ICalculationResultsRepository resultsRepository = CreateResultsRepository();
            resultsRepository.GetProviderResultsBySpecificationId(Arg.Is(specificationId), Arg.Is(-1))
                .Returns(Task.FromResult(providerResults));

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository.GetCurrentSpecificationById(Arg.Is(specificationId))
                .Returns(Task.FromResult(new SpecificationCurrentVersion()));

            IPublishedProviderResultsRepository publishedProviderResultsRepository =
                CreatePublishedProviderResultsRepository();
            publishedProviderResultsRepository.SavePublishedResults(Arg.Any<IEnumerable<PublishedProviderResult>>())
                .Returns(Task.CompletedTask);

            IVersionRepository<PublishedAllocationLineResultVersion> versionRepository = CreatePublishedProviderResultsVersionRepository();
            versionRepository.SaveVersions(Arg.Any<IEnumerable<PublishedAllocationLineResultVersion>>())
                .Returns(Task.CompletedTask);


            IPublishedProviderCalculationResultsRepository publishedProviderCalculationResultsRepository =
                CreatePublishedProviderCalculationResultsRepository();
            publishedProviderCalculationResultsRepository
                .CreatePublishedCalculationResults(Arg.Any<IEnumerable<PublishedProviderCalculationResult>>())
                .Returns(Task.CompletedTask);

            IPublishedProviderResultsAssemblerService assembler = CreateResultsAssembler();

            assembler
                .GeneratePublishedProviderResultsToSave(Arg.Any<IEnumerable<PublishedProviderResult>>(), Arg.Any<IEnumerable<PublishedProviderResultExisting>>())
                .Returns((publishedProviderResults, Enumerable.Empty<PublishedProviderResultExisting>()));

            ICacheProvider mockCacheProvider = CreateCacheProvider();

            SpecificationCalculationExecutionStatus expectedProgressCall1 = CreateSpecificationCalculationProgress(c =>
            {
                c.PercentageCompleted = 0;
                c.CalculationProgress = CalculationProgressStatus.InProgress;
            });
            SpecificationCalculationExecutionStatus expectedProgressCall2 = CreateSpecificationCalculationProgress(c => c.PercentageCompleted = 5);
            SpecificationCalculationExecutionStatus expectedProgressCall3 = CreateSpecificationCalculationProgress(c => c.PercentageCompleted = 10);
            SpecificationCalculationExecutionStatus expectedProgressCall4 = CreateSpecificationCalculationProgress(c => c.PercentageCompleted = 15);
            SpecificationCalculationExecutionStatus expectedProgressCall5 = CreateSpecificationCalculationProgress(c => c.PercentageCompleted = 22);
            SpecificationCalculationExecutionStatus expectedProgressCall6 = CreateSpecificationCalculationProgress(c => c.PercentageCompleted = 32);
            SpecificationCalculationExecutionStatus expectedProgressCall7 = CreateSpecificationCalculationProgress(c => c.PercentageCompleted = 85);
            SpecificationCalculationExecutionStatus expectedProgressCall8 = CreateSpecificationCalculationProgress(c => c.PercentageCompleted = 90);
            SpecificationCalculationExecutionStatus expectedProgressCall9 = CreateSpecificationCalculationProgress(c => c.PercentageCompleted = 95);
            SpecificationCalculationExecutionStatus expectedProgressCall10 = CreateSpecificationCalculationProgress(c => c.PercentageCompleted = 100);
            SpecificationCalculationExecutionStatus expectedProgressCall11 = CreateSpecificationCalculationProgress(c =>
            {
                c.PercentageCompleted = 100;
                c.CalculationProgress = CalculationProgressStatus.Finished;
            });

            PublishedResultsService resultsService = CreateResultsService(resultsRepository: resultsRepository,
                publishedProviderResultsRepository: publishedProviderResultsRepository,
                specificationsRepository: specificationsRepository,
                publishedProviderCalculationResultsRepository: publishedProviderCalculationResultsRepository,
                cacheProvider: mockCacheProvider,
                publishedProviderResultsAssemblerService: assembler,
                publishedProviderResultsVersionRepository: versionRepository);

            Message message = new Message();
            message.UserProperties["specification-id"] = SpecificationId1;

            // Act
            Func<Task> publishProviderResultsAction = () => resultsService.PublishProviderResults(message);

            //Assert
            publishProviderResultsAction.Should().NotThrow();

            mockCacheProvider.Received().SetAsync($"{RedisPrependKey}{SpecificationId1}", expectedProgressCall1, TimeSpan.FromHours(6), false);
            mockCacheProvider.Received().SetAsync($"{RedisPrependKey}{SpecificationId1}", expectedProgressCall2, TimeSpan.FromHours(6), false);
            mockCacheProvider.Received().SetAsync($"{RedisPrependKey}{SpecificationId1}", expectedProgressCall3, TimeSpan.FromHours(6), false);
            mockCacheProvider.Received().SetAsync($"{RedisPrependKey}{SpecificationId1}", expectedProgressCall4, TimeSpan.FromHours(6), false);
            mockCacheProvider.Received().SetAsync($"{RedisPrependKey}{SpecificationId1}", expectedProgressCall5, TimeSpan.FromHours(6), false);
            mockCacheProvider.Received().SetAsync($"{RedisPrependKey}{SpecificationId1}", expectedProgressCall6, TimeSpan.FromHours(6), false);
            mockCacheProvider.Received().SetAsync($"{RedisPrependKey}{SpecificationId1}", expectedProgressCall7, TimeSpan.FromHours(6), false);
            mockCacheProvider.Received().SetAsync($"{RedisPrependKey}{SpecificationId1}", expectedProgressCall8, TimeSpan.FromHours(6), false);
            mockCacheProvider.Received().SetAsync($"{RedisPrependKey}{SpecificationId1}", expectedProgressCall9, TimeSpan.FromHours(6), false);
            mockCacheProvider.Received().SetAsync($"{RedisPrependKey}{SpecificationId1}", expectedProgressCall10, TimeSpan.FromHours(6), false);
            mockCacheProvider.Received().SetAsync($"{RedisPrependKey}{SpecificationId1}", expectedProgressCall11, TimeSpan.FromHours(6), false);
            mockCacheProvider.Received(11).SetAsync(Arg.Any<string>(), Arg.Any<SpecificationCalculationExecutionStatus>(), Arg.Any<TimeSpan>(), Arg.Any<bool>());
        }

        [TestMethod]
        public void PublishProviderResults_WhenAnExceptionIsThrownAtSomePoint_ThenErrorIsReportedOnCache()
        {
            // Arrange
            IEnumerable<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult()
            };

            ICalculationResultsRepository resultsRepository = CreateResultsRepository();
            resultsRepository.GetProviderResultsBySpecificationId(Arg.Is(SpecificationId1), Arg.Is(-1))
                .Returns(Task.FromResult(providerResults));

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository.GetCurrentSpecificationById(Arg.Is(SpecificationId1))
                .Returns(Task.FromResult(new SpecificationCurrentVersion()));

            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();
            publishedProviderResultsRepository.SavePublishedResults(Arg.Any<IEnumerable<PublishedProviderResult>>())
                .Returns(Task.CompletedTask);

            IVersionRepository<PublishedAllocationLineResultVersion> versionRepository = CreatePublishedProviderResultsVersionRepository();
            versionRepository.SaveVersions(Arg.Any<IEnumerable<PublishedAllocationLineResultVersion>>())
                .Returns(Task.CompletedTask);


            IPublishedProviderCalculationResultsRepository publishedProviderCalculationResultsRepository = CreatePublishedProviderCalculationResultsRepository();
            publishedProviderCalculationResultsRepository.CreatePublishedCalculationResults(Arg.Any<IEnumerable<PublishedProviderCalculationResult>>())
                .Returns(ex => { throw new Exception("Error saving published calculation results"); });

            ICacheProvider mockCacheProvider = CreateCacheProvider();

            SpecificationCalculationExecutionStatus expectedErrorProgress = CreateSpecificationCalculationProgress(c =>
            {
                c.PercentageCompleted = 15;
                c.CalculationProgress = CalculationProgressStatus.Error;
                c.ErrorMessage = "Failed to create published provider calculation results";
            });

            PublishedResultsService resultsService = CreateResultsService(resultsRepository: resultsRepository,
                publishedProviderResultsRepository: publishedProviderResultsRepository,
                specificationsRepository: specificationsRepository,
                publishedProviderCalculationResultsRepository: publishedProviderCalculationResultsRepository,
                cacheProvider: mockCacheProvider,
                publishedProviderResultsVersionRepository: versionRepository);

            Message message = new Message();
            message.UserProperties["specification-id"] = SpecificationId1;

            // Act
            Func<Task> publishProviderAction = () => resultsService.PublishProviderResults(message);

            //Assert
            publishProviderAction.Should().ThrowExactly<Exception>();
            mockCacheProvider.Received().SetAsync($"{RedisPrependKey}{SpecificationId1}", expectedErrorProgress, TimeSpan.FromHours(6), false);
        }

        [TestMethod]
        public async Task PublishProviderResults_WhenNoExisitingAllocationResultsPublished_ThenResultsAreSaved()
        {
            // Arrange
            string specificationId = "spec-1";
            string calculationId = "calc-1";
            string providerId = "prov-1";
            Reference author = new Reference("author-1", "author1");
            Period fundingPeriod = new Period()
            {
                Id = "fp1",
                Name = "Funding Period 1",
            };

            string resultId = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{specificationId}{providerId}{calculationId}"));

            IEnumerable<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult()
            };

            IEnumerable<PublishedProviderCalculationResult> publishedProviderCalculationResults = Enumerable.Empty<PublishedProviderCalculationResult>();

            IEnumerable<PublishedProviderCalculationResultHistory> publishedProviderCalculationResultsHistory = Enumerable.Empty<PublishedProviderCalculationResultHistory>();

            SpecificationCurrentVersion specificationCurrentVersion = new SpecificationCurrentVersion
            {
                Id = specificationId
            };

            AllocationLine allocationLine1 = new AllocationLine()
            {
                Id = "AAAAA",
                Name = "Allocation Line 1",
            };

            List<PublishedProviderResult> publishedProviderResults = new List<PublishedProviderResult>();
            publishedProviderResults.Add(new PublishedProviderResult()
            {
                FundingPeriod = fundingPeriod,
                ProviderId = providerId,
                SpecificationId = specificationId,
                FundingStreamResult = new PublishedFundingStreamResult()
                {
                    AllocationLineResult = new PublishedAllocationLineResult()
                    {
                        AllocationLine = allocationLine1,
                        Current = new PublishedAllocationLineResultVersion()
                        {
                            Author = author,
                            Status = AllocationLineStatus.Held,
                            Value = 1
                        }
                    }
                }
            });

            ICalculationResultsRepository resultsRepository = CreateResultsRepository();
            resultsRepository.GetProviderResultsBySpecificationId(Arg.Is(specificationId), Arg.Is(-1))
                .Returns(Task.FromResult(providerResults));
            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository.GetCurrentSpecificationById(Arg.Is(specificationId))
                .Returns(specificationCurrentVersion);
            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();
            publishedProviderResultsRepository.SavePublishedResults(Arg.Any<IEnumerable<PublishedProviderResult>>())
                .Returns(Task.CompletedTask);

            IVersionRepository<PublishedAllocationLineResultVersion> versionRepository = CreatePublishedProviderResultsVersionRepository();
            versionRepository.SaveVersions(Arg.Any<IEnumerable<PublishedAllocationLineResultVersion>>())
                .Returns(Task.CompletedTask);

            bool hasCalcChanges = true;

            IPublishedProviderResultsAssemblerService assembler = CreateResultsAssembler();
            assembler
                .AssemblePublishedCalculationResults(Arg.Is(providerResults), Arg.Any<Reference>(), specificationCurrentVersion)
                .Returns(publishedProviderCalculationResults);

            assembler
                .AssemblePublishedProviderResults(Arg.Any<IEnumerable<ProviderResult>>(), Arg.Any<Reference>(), Arg.Any<SpecificationCurrentVersion>())
                .Returns(publishedProviderResults);

            assembler
                .GeneratePublishedProviderResultsToSave(Arg.Any<IEnumerable<PublishedProviderResult>>(), Arg.Any<IEnumerable<PublishedProviderResultExisting>>())
                .Returns((publishedProviderResults, Enumerable.Empty<PublishedProviderResultExisting>()));

            IVersionRepository<PublishedProviderCalculationResultVersion> calcsVersionRepository = CreatePublishedProviderCalcResultsVersionRepository();

            IPublishedProviderCalculationResultsRepository publishedProviderCalculationResultsRepository = CreatePublishedProviderCalculationResultsRepository();

            PublishedResultsService resultsService = CreateResultsService(resultsRepository: resultsRepository,
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
                publishedProviderResultsRepository
                    .Received(1)
                    .SavePublishedResults(Arg.Is<IEnumerable<PublishedProviderResult>>(a =>
                    a.First().ProviderId == providerId &&
                    a.First().FundingStreamResult.AllocationLineResult.Current.Value == 1 &&
                    a.First().FundingStreamResult.AllocationLineResult.Current.Status == AllocationLineStatus.Held));

            await
                publishedProviderResultsRepository
                    .Received(1)
                    .SavePublishedResults(Arg.Is<IEnumerable<PublishedProviderResult>>(a => a.Count() == 1));

        }

        [TestMethod]
        public async Task PublishProviderResults_WhenExisitingAllocationResultsShouldBeExcluded_ThenResultsAreSaved()
        {
            // Arrange
            string specificationId = "spec-1";
            string calculationId = "calc-1";
            string providerId = "prov-1";
            Reference author = new Reference("author-1", "author1");
            Period fundingPeriod = new Period()
            {
                Id = "fp1",
                Name = "Funding Period 1",
            };

            string resultId = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{specificationId}{providerId}{calculationId}"));

            IEnumerable<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult()
            };

            IEnumerable<PublishedProviderCalculationResult> publishedProviderCalculationResults = Enumerable.Empty<PublishedProviderCalculationResult>();

            IEnumerable<PublishedProviderCalculationResultHistory> publishedProviderCalculationResultsHistory = Enumerable.Empty<PublishedProviderCalculationResultHistory>();

            SpecificationCurrentVersion specificationCurrentVersion = new SpecificationCurrentVersion
            {
                Id = specificationId
            };

            AllocationLine allocationLine1 = new AllocationLine()
            {
                Id = "AAAAA",
                Name = "Allocation Line 1",
            };

            List<PublishedProviderResult> publishedProviderResults = new List<PublishedProviderResult>();

            List<PublishedProviderResultExisting> existingToRemove = new List<PublishedProviderResultExisting>()
            {
                new PublishedProviderResultExisting()
                {
                    Id = "c3BlYy0xMTIzQUFBQUE=",
                    AllocationLineId = allocationLine1.Id,
                    ProviderId = "123",
                    Status = AllocationLineStatus.Approved,
                    Value = 51,
                }
            };

            PublishedProviderResult existingProviderResultToRemove = new PublishedProviderResult()
            {
                ProviderId = "123",
                SpecificationId = specificationId,
                FundingStreamResult = new PublishedFundingStreamResult()
                {
                    AllocationLineResult = new PublishedAllocationLineResult()
                    {
                        AllocationLine = allocationLine1,
                        Current = new PublishedAllocationLineResultVersion()
                        {
                            Value = 51,
                            Status = AllocationLineStatus.Approved,
                            Provider = new ProviderSummary()
                            {
                                Name = "Provider Name",
                                Id = "123",
                            }
                        }
                    },
                    FundingStream = new FundingStream()
                    {
                        Id = "fsId",
                        Name = "Funding Stream Name",
                        PeriodType = new PeriodType()
                        {
                            Name = "Test Period Type",
                            Id = "tpt",
                        }
                    },
                    FundingStreamPeriod = "fsp",
                },
                FundingPeriod = new Period()
                {
                    Id = "fundingPeriodId",
                    Name = "Funding Period Name"
                },
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

            IVersionRepository<PublishedAllocationLineResultVersion> versionRepository = CreatePublishedProviderResultsVersionRepository();
            versionRepository.SaveVersions(Arg.Any<IEnumerable<PublishedAllocationLineResultVersion>>())
                .Returns(Task.CompletedTask);

            IPublishedProviderResultsAssemblerService assembler = CreateResultsAssembler();
            assembler
                .AssemblePublishedCalculationResults(Arg.Is(providerResults), Arg.Any<Reference>(), specificationCurrentVersion)
                .Returns(publishedProviderCalculationResults);

            assembler
                .AssemblePublishedProviderResults(Arg.Any<IEnumerable<ProviderResult>>(), Arg.Any<Reference>(), Arg.Any<SpecificationCurrentVersion>())
                .Returns(publishedProviderResults);

            assembler
                .GeneratePublishedProviderResultsToSave(Arg.Any<IEnumerable<PublishedProviderResult>>(), Arg.Any<IEnumerable<PublishedProviderResultExisting>>())
                .Returns((publishedProviderResults, existingToRemove));

            IPublishedProviderCalculationResultsRepository publishedProviderCalculationResultsRepository = CreatePublishedProviderCalculationResultsRepository();
           
            publishedProviderResultsRepository
                .GetPublishedProviderResultForId("c3BlYy0xMTIzQUFBQUE=", "123")
                .Returns(existingProviderResultToRemove);

            PublishedResultsService resultsService = CreateResultsService(resultsRepository: resultsRepository,
                publishedProviderResultsRepository: publishedProviderResultsRepository,
                specificationsRepository: specificationsRepository,
                publishedProviderCalculationResultsRepository: publishedProviderCalculationResultsRepository,
                publishedProviderResultsAssemblerService: assembler,
                publishedProviderResultsVersionRepository: versionRepository);

            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;

            // Act
            await resultsService.PublishProviderResults(message);

            //Assert

            decimal? expectedValue = 0;

            await
                publishedProviderResultsRepository
                    .Received(1)
                    .SavePublishedResults(Arg.Is<IEnumerable<PublishedProviderResult>>(a =>
                    a.First().ProviderId == "123" &&
                    a.First().FundingStreamResult.AllocationLineResult.Current.Value == expectedValue &&
                    a.First().FundingStreamResult.AllocationLineResult.Current.Status == AllocationLineStatus.Updated));

            await
                publishedProviderResultsRepository
                    .Received(1)
                    .SavePublishedResults(Arg.Is<IEnumerable<PublishedProviderResult>>(a => a.Count() == 1));

        }

        [TestMethod]
        public void PublishProviderResults_WhenFailsToUpdateSpecificationRefreshDate_ThenNoExceptionThrown()
        {
            // Arrange
            string specificationId = "1";

            SpecificationCurrentVersion specificationCurrentVersion = new SpecificationCurrentVersion
            {
                Id = specificationId
            };

            IEnumerable<PublishedProviderCalculationResult> publishedProviderCalculationResults = new[]
            {
                new PublishedProviderCalculationResult
                {
                    Current = new PublishedProviderCalculationResultVersion
                    {
                        Provider = new ProviderSummary
                        {
                            Id = "prov-1"
                        }
                    },
                    Specification = new Reference{ Id = specificationId, Name ="spec1" },
                    CalculationSpecification = new Reference { Id = "calc-1", Name = "calc1" }
                }
            };

            IEnumerable<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult()
            };

            ICalculationResultsRepository resultsRepository = CreateResultsRepository();
            resultsRepository.GetProviderResultsBySpecificationId(Arg.Is(specificationId), Arg.Is(-1))
                .Returns(Task.FromResult(providerResults));

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository.GetCurrentSpecificationById(Arg.Is(specificationId))
                .Returns(specificationCurrentVersion);
            specificationsRepository.UpdatePublishedRefreshedDate(Arg.Is(specificationId), Arg.Any<DateTimeOffset>())
                .Returns(Task.FromResult(HttpStatusCode.InternalServerError));

            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();
            publishedProviderResultsRepository.SavePublishedResults(Arg.Any<IEnumerable<PublishedProviderResult>>())
                .Returns(Task.CompletedTask);

            IVersionRepository<PublishedAllocationLineResultVersion> versionRepository = CreatePublishedProviderResultsVersionRepository();
            versionRepository.SaveVersions(Arg.Any<IEnumerable<PublishedAllocationLineResultVersion>>())
                .Returns(Task.CompletedTask);

            IPublishedProviderCalculationResultsRepository publishedProviderCalculationResultsRepository = CreatePublishedProviderCalculationResultsRepository();
            publishedProviderCalculationResultsRepository.CreatePublishedCalculationResults(Arg.Any<IEnumerable<PublishedProviderCalculationResult>>())
                .Returns(Task.CompletedTask);

            IPublishedProviderResultsAssemblerService assembler = CreateResultsAssembler();
            assembler
                .AssemblePublishedCalculationResults(Arg.Is(providerResults), Arg.Any<Reference>(), specificationCurrentVersion)
                .Returns(publishedProviderCalculationResults);

            ILogger logger = CreateLogger();

            PublishedResultsService resultsService = CreateResultsService(resultsRepository: resultsRepository,
                publishedProviderResultsRepository: publishedProviderResultsRepository,
                specificationsRepository: specificationsRepository,
                publishedProviderCalculationResultsRepository: publishedProviderCalculationResultsRepository,
                publishedProviderResultsVersionRepository: versionRepository,
                logger: logger,
                publishedProviderResultsAssemblerService: assembler);

            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;

            // Act
            Func<Task> test = () => resultsService.PublishProviderResults(message);

            //Assert
            test.Should().NotThrow();
            logger.Received(1).Error(Arg.Is($"Failed to update the published refresh date on the specification with id: {specificationId}. Failed with code: InternalServerError"));
            logger.DidNotReceive().Information(Arg.Is($"Updated the published refresh date on the specification with id: {specificationId}"));
        }

        private static SpecificationCalculationExecutionStatus CreateSpecificationCalculationProgress(Action<SpecificationCalculationExecutionStatus> defaultModelAction)
        {
            SpecificationCalculationExecutionStatus defaultModel = new SpecificationCalculationExecutionStatus(SpecificationId1, 0, CalculationProgressStatus.InProgress);
            defaultModelAction(defaultModel);
            return defaultModel;
        }
    }
}
