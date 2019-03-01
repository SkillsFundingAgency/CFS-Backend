using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Common.Models;
using CalculateFunding.Models;
using CalculateFunding.Models.Providers;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Interfaces;
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
        [TestMethod]
        public void PublishProviderResultsWithVariations_WhenMessageIsNull_ThenArgumentNullExceptionThrown()
        {
            // Arrange
            PublishedResultsService resultsService = CreateResultsService();

            // Act
            Func<Task> test = () => resultsService.PublishProviderResultsWithVariations(null);

            // Assert
            test
                .Should().
                ThrowExactly<ArgumentNullException>()
                .And
                .ParamName
                .Should()
                .Be("message");
        }

        [TestMethod]
        public void PublishProviderResultsWithVariations_WhenMessageDoesNotHaveJobId_ThenThrowsException()
        {
            // Arrange
            IFeatureToggle featureToggle = InitialiseFeatureToggle();

            PublishedResultsService resultsService = CreateResultsService(featureToggle: featureToggle);

            Message message = new Message();

            // Act
            Func<Task> test = () => resultsService.PublishProviderResultsWithVariations(message);

            // Assert
            test
                .Should()
                .ThrowExactly<NonRetriableException>()
                .And
                .Message
                .Should()
                .Be("No job id was provided to the PublishProviderResultsWithVariations");
        }

        [TestMethod]
        public async Task PublishProviderResultsWithVariations_WhenMessageDoesNotHaveSpecificationId_ThenDoesNotProcess()
        {
            // Arrange
            ILogger logger = CreateLogger();
            ICalculationResultsRepository calculationResultsRepository = CreateResultsRepository();
            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            IPublishedProviderResultsAssemblerService providerResultsAssembler = CreateResultsAssembler();
            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();
            IProviderVariationAssemblerService providerVariationAssemblerService = CreateProviderVariationAssemblerService();

            PublishedResultsService resultsService = InitialisePublishedResultsService(specificationsRepository, calculationResultsRepository, providerResultsAssembler, publishedProviderResultsRepository, providerVariationAssemblerService, logger);

            Message message = new Message();
            message.UserProperties["jobId"] = jobId;

            // Act
            await resultsService.PublishProviderResultsWithVariations(message);

            // Assert
            logger
                .Received(1)
                .Error(Arg.Is("No specification Id was provided to PublishProviderResultsWithVariations"));

            await specificationsRepository
                .DidNotReceive()
                .GetCurrentSpecificationById(Arg.Any<string>());
        }

        [TestMethod]
        public async Task PublishProviderResultsWithVariations_WhenSpecificationNotFound_ThenDoesNotProcess()
        {
            // Arrange
            ILogger logger = CreateLogger();
            ICalculationResultsRepository calculationResultsRepository = CreateResultsRepository();
            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            IPublishedProviderResultsAssemblerService providerResultsAssembler = CreateResultsAssembler();
            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();
            IProviderVariationAssemblerService providerVariationAssemblerService = CreateProviderVariationAssemblerService();

            PublishedResultsService resultsService = InitialisePublishedResultsService(specificationsRepository, calculationResultsRepository, providerResultsAssembler, publishedProviderResultsRepository, providerVariationAssemblerService, logger);

            Message message = new Message();
            message.UserProperties["specification-id"] = "-1";
            message.UserProperties["jobId"] = jobId;

            // Act
            await resultsService.PublishProviderResultsWithVariations(message);

            // Assert
            logger
                .Received(1)
                .Error(Arg.Is($"Specification not found for specification id -1"));

            await calculationResultsRepository
                .DidNotReceive()
                .GetProviderResultsBySpecificationId(Arg.Is("-1"), Arg.Any<int>());
        }

        [TestMethod]
        public async Task PublishProviderResultsWithVariations_WhenNoProviderResultsForSpecification_ThenDoesNotContinue()
        {
            // Arrange
            string specificationId = "1";
            IEnumerable<ProviderResult> providerResults = Enumerable.Empty<ProviderResult>();

            ILogger logger = CreateLogger();
            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCurrentSpecificationById(Arg.Is(specificationId))
                .Returns(new SpecificationCurrentVersion { Id = specificationId });

            ICalculationResultsRepository resultsRepository = CreateResultsRepository();
            resultsRepository
                .GetProviderResultsBySpecificationId(Arg.Is(specificationId), Arg.Is(-1))
                .Returns(Task.FromResult(providerResults));

            IPublishedProviderResultsAssemblerService resultsAssembler = CreateResultsAssembler();

            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();

            IProviderVariationAssemblerService providerVariationAssembler = CreateProviderVariationAssemblerService();

            PublishedResultsService resultsService = InitialisePublishedResultsService(specificationsRepository, resultsRepository, resultsAssembler, publishedProviderResultsRepository, providerVariationAssembler, logger);

            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            // Act
            await resultsService.PublishProviderResultsWithVariations(message);

            // Assert
            logger
                .Received(1)
                .Error(Arg.Is($"Provider results not found for specification id {specificationId}"));
        }

        [TestMethod]
        public void PublishProviderResultsWithVariations_WhenErrorSavingPublishedResults_ThenExceptionThrown()
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
                    FundingPeriod = new Period
                    {
                        Id = "fp-1"
                    },
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
                                Provider = new ProviderSummary
                                {
                                    UKPRN = "1"
                                }
                            }
                        }
                    }
                },
            };

            IEnumerable<PublishedProviderCalculationResult> publishedProviderCalculationResults = new[]
            {
                new PublishedProviderCalculationResult
                {
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
                .GeneratePublishedProviderResultsToSave(Arg.Any<IEnumerable<PublishedProviderResult>>(), Arg.Any<IEnumerable<PublishedProviderResultExisting>>())
                .Returns((publishedProviderResults, Enumerable.Empty<PublishedProviderResultExisting>()));

            IProviderVariationAssemblerService providerVariationAssemblerService = CreateProviderVariationAssemblerService();

            PublishedResultsService resultsService = InitialisePublishedResultsService(specificationsRepository, resultsRepository, assembler, publishedProviderResultsRepository, providerVariationAssemblerService);

            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            // Act
            Func<Task> test = () => resultsService.PublishProviderResultsWithVariations(message);

            //Assert
            Exception thrownException = test.Should().ThrowExactly<RetriableException>().Subject.First();
            thrownException.Message.Should().Be($"Failed to create published provider results for specification: {specificationId}");
            thrownException.InnerException.Should().NotBeNull();
            thrownException.InnerException.Message.Should().Be("Error saving published results");
        }

        [TestMethod]
        public void PublishProviderResultsWithVariations_WhenErrorSavingPublishedResultsVersionHistory_ThenExceptionThrown()
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
                    FundingPeriod = new Period
                    {
                        Id = "fp-1"
                    },
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
                                Provider = new ProviderSummary
                                {
                                    UKPRN = "99999"
                                },
                                Major = 2,
                                Minor = 1
                            }
                        }
                    }
                },
            };

            IEnumerable<PublishedProviderCalculationResult> publishedProviderCalculationResults = new[]
            {
                new PublishedProviderCalculationResult
                {
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

            IFeatureToggle featureToggle = InitialiseFeatureToggle();

            IJobsApiClient jobsApiClient = InitialiseJobsApiClient();

            PublishedResultsService resultsService = CreateResultsService(
                resultsRepository: resultsRepository,
                publishedProviderResultsRepository: publishedProviderResultsRepository,
                specificationsRepository: specificationsRepository,
                publishedProviderResultsAssemblerService: assembler,
                publishedProviderResultsVersionRepository: versionRepository,
                featureToggle: featureToggle, jobsApiClient: jobsApiClient);

            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            // Act
            Func<Task> test = () => resultsService.PublishProviderResultsWithVariations(message);

            //Assert
            Exception thrownException = test.Should().ThrowExactly<RetriableException>().Subject.First();
            thrownException.Message.Should().Be($"Failed to create published provider results for specification: {specificationId}");
            thrownException.InnerException.Should().NotBeNull();
            thrownException.InnerException.Message.Should().Be("Error saving published results version history");

            publishedProviderResults.First().FundingStreamResult.AllocationLineResult.Current.FeedIndexId.Should().Be("AAAAA-fp-1-99999-v2-1");
        }

        [TestMethod]
        public void PublishProviderResultsWithVariations_WhenCompletesSuccessfully_ThenNoExceptionThrown()
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

            IPublishedProviderResultsAssemblerService assembler = CreateResultsAssembler();

            IProviderVariationAssemblerService providerVariationAssemblerService = CreateProviderVariationAssemblerService();

            ILogger logger = CreateLogger();

            PublishedResultsService resultsService = InitialisePublishedResultsService(specificationsRepository, resultsRepository, assembler, publishedProviderResultsRepository, providerVariationAssemblerService, logger);

            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            // Act
            Func<Task> test = () => resultsService.PublishProviderResultsWithVariations(message);

            //Assert
            test.Should().NotThrow();
            logger.DidNotReceive().Error(Arg.Any<string>());
            logger.Received(1).Information(Arg.Is($"Finished processing PublishProviderResult message for specification '{specificationId}' and job '{jobId}'"));
        }

        [TestMethod]
        public void PublishProviderResultsWithVariations_WhenNoExceptionThrown_ShouldReportProgressOnCacheCorrectly()
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
                    FundingPeriod = new Period
                    {
                        Id = "fp-1"
                    },
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
                                Provider = new ProviderSummary
                                {
                                    UKPRN = "99999"
                                },
                                Major = 1,
                                Minor = 1
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

            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();

            IPublishedProviderResultsAssemblerService assembler = CreateResultsAssembler();
            assembler
                .GeneratePublishedProviderResultsToSave(Arg.Any<IEnumerable<PublishedProviderResult>>(), Arg.Any<IEnumerable<PublishedProviderResultExisting>>())
                .Returns((publishedProviderResults, Enumerable.Empty<PublishedProviderResultExisting>()));

            IProviderVariationAssemblerService providerVariationAssemblerService = CreateProviderVariationAssemblerService();

            ICacheProvider mockCacheProvider = CreateCacheProvider();

            SpecificationCalculationExecutionStatus expectedProgressCall1 = CreateSpecificationCalculationProgress(c =>
            {
                c.PercentageCompleted = 0;
                c.CalculationProgress = CalculationProgressStatus.InProgress;
            });
            SpecificationCalculationExecutionStatus expectedProgressCall2 = CreateSpecificationCalculationProgress(c => c.PercentageCompleted = 5);
            SpecificationCalculationExecutionStatus expectedProgressCall3 = CreateSpecificationCalculationProgress(c => c.PercentageCompleted = 10);
            SpecificationCalculationExecutionStatus expectedProgressCall4 = CreateSpecificationCalculationProgress(c => c.PercentageCompleted = 23);
            SpecificationCalculationExecutionStatus expectedProgressCall5 = CreateSpecificationCalculationProgress(c => c.PercentageCompleted = 38);
            SpecificationCalculationExecutionStatus expectedProgressCall6 = CreateSpecificationCalculationProgress(c => c.PercentageCompleted = 53);
            SpecificationCalculationExecutionStatus expectedProgressCall7 = CreateSpecificationCalculationProgress(c => c.PercentageCompleted = 63);
            SpecificationCalculationExecutionStatus expectedProgressCall8 = CreateSpecificationCalculationProgress(c => c.PercentageCompleted = 73);

            PublishedResultsService resultsService = InitialisePublishedResultsService(specificationsRepository, resultsRepository, assembler, publishedProviderResultsRepository, providerVariationAssemblerService, cacheProvider: mockCacheProvider);

            Message message = new Message();
            message.UserProperties["specification-id"] = SpecificationId1;
            message.UserProperties["jobId"] = jobId;

            // Act
            Func<Task> publishProviderResultsAction = () => resultsService.PublishProviderResultsWithVariations(message);

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

            mockCacheProvider.Received(10).SetAsync(Arg.Any<string>(), Arg.Any<SpecificationCalculationExecutionStatus>(), Arg.Any<TimeSpan>(), Arg.Any<bool>());

            publishedProviderResults.First().FundingStreamResult.AllocationLineResult.Current.FeedIndexId.Should().Be("AAAAA-fp-1-99999-v1-2");
        }

        [TestMethod]
        public void PublishProviderResultsWithVariations_WhenAnExceptionIsSavingPublishedResults_ThenErrorIsReportedOnCache()
        {
            // Arrange
            IEnumerable<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult()
            };

            IEnumerable<PublishedProviderResult> publishedProviderResults = new List<PublishedProviderResult>()
            {
                new PublishedProviderResult()
                {
                    ProviderId = "1",
                    FundingPeriod = new Period
                    {
                        Id = "fp-1"
                    },
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
                                Provider = new ProviderSummary
                                {
                                    UKPRN = "99999"
                                },
                                Major = 1,
                                Minor = 1
                            }
                        }
                    }
                },
            };

            IEnumerable<PublishedProviderResultExisting> publishedProviderResultExisting = new List<PublishedProviderResultExisting>
            {
                new PublishedProviderResultExisting()
            };

            IFeatureToggle featureToggle = InitialiseFeatureToggle();

            ICalculationResultsRepository resultsRepository = CreateResultsRepository();
            resultsRepository.GetProviderResultsBySpecificationId(Arg.Is(SpecificationId1), Arg.Is(-1))
                .Returns(Task.FromResult(providerResults));

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository.GetCurrentSpecificationById(Arg.Is(SpecificationId1))
                .Returns(Task.FromResult(new SpecificationCurrentVersion()));

            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();
            publishedProviderResultsRepository
                .When(x => x.SavePublishedResults(Arg.Any<List<PublishedProviderResult>>()))
                .Do(x => { throw new Exception("Error saving published calculation results"); });

            IPublishedProviderResultsAssemblerService assemblerService = CreateResultsAssembler();
            assemblerService
                .GeneratePublishedProviderResultsToSave(Arg.Any<IEnumerable<PublishedProviderResult>>(), Arg.Any<IEnumerable<PublishedProviderResultExisting>>())
                .Returns((publishedProviderResults, publishedProviderResultExisting));

            IVersionRepository<PublishedAllocationLineResultVersion> versionRepository = CreatePublishedProviderResultsVersionRepository();
            versionRepository.SaveVersions(Arg.Any<IEnumerable<PublishedAllocationLineResultVersion>>())
                .Returns(Task.CompletedTask);

            ICacheProvider mockCacheProvider = CreateCacheProvider();

            IJobsApiClient jobsApiClient = InitialiseJobsApiClient();

            SpecificationCalculationExecutionStatus expectedErrorProgress = CreateSpecificationCalculationProgress(c =>
            {
                c.PercentageCompleted = 15;
                c.CalculationProgress = CalculationProgressStatus.Error;
                c.ErrorMessage = "Failed to create published provider calculation results";
            });

            PublishedResultsService resultsService = CreateResultsService(resultsRepository: resultsRepository,
                publishedProviderResultsRepository: publishedProviderResultsRepository,
                specificationsRepository: specificationsRepository,
                cacheProvider: mockCacheProvider,
                publishedProviderResultsAssemblerService: assemblerService,
                publishedProviderResultsVersionRepository: versionRepository,
                featureToggle: featureToggle,
                jobsApiClient: jobsApiClient);

            Message message = new Message();
            message.UserProperties["specification-id"] = SpecificationId1;
            message.UserProperties["jobId"] = jobId;

            // Act
            Func<Task> publishProviderAction = () => resultsService.PublishProviderResultsWithVariations(message);

            //Assert
            publishProviderAction.Should().ThrowExactly<RetriableException>();
            mockCacheProvider.Received().SetAsync($"{RedisPrependKey}{SpecificationId1}", Arg.Any<SpecificationCalculationExecutionStatus>(), TimeSpan.FromHours(6), false);
        }

        [TestMethod]
        public async Task PublishProviderResultsWithVariations_WhenNoExisitingAllocationResultsPublished_ThenResultsAreSaved()
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
                            Value = 1,
                            Provider = new ProviderSummary
                            {
                                UKPRN = "1234"
                            }
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

            IPublishedProviderResultsAssemblerService assembler = CreateResultsAssembler();
            assembler
                .AssemblePublishedProviderResults(Arg.Any<IEnumerable<ProviderResult>>(), Arg.Any<Reference>(), Arg.Any<SpecificationCurrentVersion>())
                .Returns(publishedProviderResults);

            assembler
                .GeneratePublishedProviderResultsToSave(Arg.Any<IEnumerable<PublishedProviderResult>>(), Arg.Any<IEnumerable<PublishedProviderResultExisting>>())
                .Returns((publishedProviderResults, Enumerable.Empty<PublishedProviderResultExisting>()));

            IProviderVariationAssemblerService providerVariationAssemblerService = CreateProviderVariationAssemblerService();

            PublishedResultsService resultsService = InitialisePublishedResultsService(specificationsRepository, resultsRepository, assembler, publishedProviderResultsRepository, providerVariationAssemblerService);

            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            // Act
            await resultsService.PublishProviderResultsWithVariations(message);

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
        public async Task PublishProviderResultsWithVariations_WhenExisitingAllocationResultsShouldBeExcluded_ThenResultsAreSaved()
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
                .AssemblePublishedProviderResults(Arg.Any<IEnumerable<ProviderResult>>(), Arg.Any<Reference>(), Arg.Any<SpecificationCurrentVersion>())
                .Returns(publishedProviderResults);

            assembler
                .GeneratePublishedProviderResultsToSave(Arg.Any<IEnumerable<PublishedProviderResult>>(), Arg.Any<IEnumerable<PublishedProviderResultExisting>>())
                .Returns((publishedProviderResults, existingToRemove));

            publishedProviderResultsRepository
                .GetPublishedProviderResultForId("c3BlYy0xMTIzQUFBQUE=", "123")
                .Returns(existingProviderResultToRemove);

            IFeatureToggle featureToggle = InitialiseFeatureToggle();

            IJobsApiClient jobsApiClient = InitialiseJobsApiClient();

            PublishedResultsService resultsService = CreateResultsService(resultsRepository: resultsRepository,
                publishedProviderResultsRepository: publishedProviderResultsRepository,
                specificationsRepository: specificationsRepository,
                publishedProviderResultsAssemblerService: assembler,
                publishedProviderResultsVersionRepository: versionRepository,
                featureToggle: featureToggle, jobsApiClient: jobsApiClient);

            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            // Act
            await resultsService.PublishProviderResultsWithVariations(message);

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
        public void PublishProviderResultsWithVariations_WhenFailsToUpdateSpecificationRefreshDate_ThenNoExceptionThrown()
        {
            // Arrange
            IEnumerable<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult
                {
                    AllocationLineResults = new List<AllocationLineResult>
                    {
                        new AllocationLineResult { AllocationLine = new Reference { Id = "alloc1", Name = "Alloc 1" }, Value = 12 }
                    },
                    CalculationResults = new List<CalculationResult>
                    {
                        new CalculationResult
                        {
                            AllocationLine = new Reference { Id = "alloc1", Name = "Alloc 1" },
                            Calculation = new Reference { Id = "calc1", Name = "Calc 1" },
                            CalculationSpecification = new Reference { Id = "calcspec1", Name = "Calc 1" },
                            CalculationType = Models.Calcs.CalculationType.Funding,
                            Value = 12
                        }
                    },
                    Provider = new ProviderSummary { UKPRN = "prov1", Id = "prov1" },
                    SpecificationId = specificationId
                }
            };

            ICalculationResultsRepository calculationResultsRepository = CreateResultsRepository();
            calculationResultsRepository.GetProviderResultsBySpecificationId(Arg.Is(specificationId), Arg.Is(-1))
                .Returns(providerResults);

            ISpecificationsRepository specificationsRepository = InitialiseSpecificationRepository(null);
            specificationsRepository
                .UpdatePublishedRefreshedDate(Arg.Is(specificationId), Arg.Any<DateTimeOffset>())
                .Returns(HttpStatusCode.InternalServerError);

            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();
            ILogger logger = CreateLogger();
            IPublishedProviderResultsAssemblerService providerResultsAssembler = CreateRealResultsAssembler(specificationsRepository);
            IProviderVariationAssemblerService providerVariationAssemblerService = CreateProviderVariationAssemblerService();

            PublishedResultsService resultsService = InitialisePublishedResultsService(specificationsRepository, calculationResultsRepository, providerResultsAssembler, publishedProviderResultsRepository, providerVariationAssemblerService, logger);

            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            // Act
            Func<Task> test = () => resultsService.PublishProviderResultsWithVariations(message);

            // Assert
            test.Should().NotThrow();
            logger.Received(1).Error(Arg.Is($"Failed to update the published refresh date on the specification with id: {specificationId}. Failed with code: InternalServerError"));
            logger.DidNotReceive().Information(Arg.Is($"Updated the published refresh date on the specification with id: {specificationId}"));
        }

        [TestMethod]
        public async Task PublishProviderResultsWithVariations_WhenPublishedResultsToSave_ThenJobServiceCalled()
        {
            // Arrange
            string specificationId = "1";

            SpecificationCurrentVersion specificationCurrentVersion = new SpecificationCurrentVersion
            {
                Id = specificationId,
                LastCalculationUpdatedAt = DateTimeOffset.Parse("2019/02/12T10:42:00"),
                PublishedResultsRefreshedAt = DateTimeOffset.Parse("2019/02/11T11:00:00")
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

            List<PublishedProviderResult> publishedProviderResults = new List<PublishedProviderResult>()
            {
                new PublishedProviderResult()
                {
                    FundingStreamResult = new PublishedFundingStreamResult()
                    {
                        AllocationLineResult = new PublishedAllocationLineResult()
                        {
                            Current = new PublishedAllocationLineResultVersion()
                            {
                                Status = AllocationLineStatus.Updated,
                                Provider = new ProviderSummary { UKPRN = "prov1" },
                                Major = 1,
                                Minor = 2
                            },
                            AllocationLine = new AllocationLine()
                            {
                                Id = "alId",
                                Name = "Allocation Line",
                            }
                        },
                        FundingStream = new FundingStream { Id = "fs1", Name = "fs one", PeriodType = new PeriodType { Id = "pt1", Name = "pt" } }
                    },
                    FundingPeriod = new Period { Id = "fp1" }
                }
            };

            (IEnumerable<PublishedProviderResult> newOrUpdatedPublishedProviderResults, IEnumerable<PublishedProviderResultExisting> existingRecordsToZero) resultsToSave =
                (publishedProviderResults, Enumerable.Empty<PublishedProviderResultExisting>());

            IPublishedProviderResultsAssemblerService assembler = CreateResultsAssembler();
            assembler
                .GeneratePublishedProviderResultsToSave(Arg.Any<IEnumerable<PublishedProviderResult>>(), Arg.Any<IEnumerable<PublishedProviderResultExisting>>())
                .Returns(resultsToSave);

            ILogger logger = CreateLogger();

            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
                .IsJobServiceForPublishProviderResultsEnabled()
                .Returns(true);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(new ApiResponse<JobViewModel>(HttpStatusCode.OK, new JobViewModel { Id = jobId, Trigger = new Trigger { Message = "Refreshing" } }));
            jobsApiClient
                .AddJobLog(Arg.Is(jobId), Arg.Any<JobLogUpdateModel>())
                .Returns(new ApiResponse<JobLog>(HttpStatusCode.OK, new JobLog()));
            jobsApiClient
                .CreateJob(Arg.Is<JobCreateModel>(j => j.JobDefinitionId == JobConstants.DefinitionNames.FetchProviderProfileJob))
                .Returns(new Job { Id = "job-34" });

            PublishedResultsService resultsService = CreateResultsService(resultsRepository: resultsRepository,
                specificationsRepository: specificationsRepository,
                publishedProviderResultsAssemblerService: assembler,
                logger: logger,
                featureToggle: featureToggle,
                jobsApiClient: jobsApiClient);

            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            // Act
            await resultsService.PublishProviderResultsWithVariations(message);

            //Assert
            logger.DidNotReceive().Error(Arg.Any<string>());
            logger.Received(1).Information(Arg.Is($"Updated the published refresh date on the specification with id: {specificationId}"));

            await jobsApiClient.Received(1).AddJobLog(Arg.Is(jobId), Arg.Is<JobLogUpdateModel>(l => l.CompletedSuccessfully.HasValue == false && l.ItemsProcessed == 0));
            await jobsApiClient.Received(1).AddJobLog(Arg.Is(jobId), Arg.Is<JobLogUpdateModel>(l => l.CompletedSuccessfully.HasValue == false && l.ItemsProcessed == 5));
            await jobsApiClient.Received(1).AddJobLog(Arg.Is(jobId), Arg.Is<JobLogUpdateModel>(l => l.CompletedSuccessfully.HasValue == false && l.ItemsProcessed == 10));
            await jobsApiClient.Received(1).AddJobLog(Arg.Is(jobId), Arg.Is<JobLogUpdateModel>(l => l.CompletedSuccessfully.HasValue == false && l.ItemsProcessed == 23));
            await jobsApiClient.Received(1).AddJobLog(Arg.Is(jobId), Arg.Is<JobLogUpdateModel>(l => l.CompletedSuccessfully.HasValue == false && l.ItemsProcessed == 38));
            await jobsApiClient.Received(1).AddJobLog(Arg.Is(jobId), Arg.Is<JobLogUpdateModel>(l => l.CompletedSuccessfully.HasValue == false && l.ItemsProcessed == 53));
            await jobsApiClient.Received(1).AddJobLog(Arg.Is(jobId), Arg.Is<JobLogUpdateModel>(l => l.CompletedSuccessfully.HasValue == false && l.ItemsProcessed == 63));
            await jobsApiClient.Received(1).AddJobLog(Arg.Is(jobId), Arg.Is<JobLogUpdateModel>(l => l.CompletedSuccessfully.HasValue == false && l.ItemsProcessed == 73));
            await jobsApiClient.Received(0).AddJobLog(Arg.Is(jobId), Arg.Is<JobLogUpdateModel>(l => l.CompletedSuccessfully.HasValue && l.CompletedSuccessfully.Value && l.ItemsProcessed.Value == 100));
        }

        [TestMethod]
        public async Task PublishProviderResultsWithVariations_WhenPublishedResultsToSave_ThenFetchProfilePeriodIsUsingJobService()
        {
            // Arrange
            string specificationId = "1";

            SpecificationCurrentVersion specificationCurrentVersion = new SpecificationCurrentVersion
            {
                Id = specificationId,
                LastCalculationUpdatedAt = DateTimeOffset.Parse("2019/02/12T10:42:00"),
                PublishedResultsRefreshedAt = DateTimeOffset.Parse("2019/02/11T11:00:00")
            };

            IEnumerable<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult()
            };

            List<PublishedProviderResult> publishedProviderResults = new List<PublishedProviderResult>();

            for (int i = 0; i < 560; i++)
            {
                publishedProviderResults
                    .Add(new PublishedProviderResult()
                    {
                        FundingStreamResult = new PublishedFundingStreamResult()
                        {
                            AllocationLineResult = new PublishedAllocationLineResult()
                            {
                                Current = new PublishedAllocationLineResultVersion()
                                {
                                    Status = AllocationLineStatus.Updated,
                                    Provider = new ProviderSummary { UKPRN = "prov1" },
                                    Major = 1,
                                    Minor = 2
                                },
                                AllocationLine = new AllocationLine()
                                {
                                    Id = "alId",
                                    Name = "Allocation Line",
                                }
                            },
                            FundingStream = new FundingStream { Id = "fs1", Name = "fs one", PeriodType = new PeriodType { Id = "pt1", Name = "pt" } }
                        },
                        FundingPeriod = new Period { Id = "fp1" }
                    });
            }

            ICalculationResultsRepository resultsRepository = CreateResultsRepository();
            resultsRepository.GetProviderResultsBySpecificationId(Arg.Is(specificationId), Arg.Is(-1))
                .Returns(Task.FromResult(providerResults));

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository.GetCurrentSpecificationById(Arg.Is(specificationId))
                .Returns(specificationCurrentVersion);

            specificationsRepository.UpdatePublishedRefreshedDate(Arg.Is(specificationId), Arg.Any<DateTimeOffset>())
                .Returns(Task.FromResult(HttpStatusCode.OK));

            (IEnumerable<PublishedProviderResult> newOrUpdatedPublishedProviderResults, IEnumerable<PublishedProviderResultExisting> existingRecordsToZero) resultsToSave =
                (publishedProviderResults, Enumerable.Empty<PublishedProviderResultExisting>());

            IPublishedProviderResultsAssemblerService assembler = CreateResultsAssembler();
            assembler
                .GeneratePublishedProviderResultsToSave(Arg.Any<IEnumerable<PublishedProviderResult>>(), Arg.Any<IEnumerable<PublishedProviderResultExisting>>())
                .Returns(resultsToSave);

            ILogger logger = CreateLogger();

            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
                .IsJobServiceForPublishProviderResultsEnabled()
                .Returns(true);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(new ApiResponse<JobViewModel>(HttpStatusCode.OK, new JobViewModel { Id = jobId, Trigger = new Trigger { Message = "Refreshing" } }));
            jobsApiClient
                .AddJobLog(Arg.Is(jobId), Arg.Any<JobLogUpdateModel>())
                .Returns(new ApiResponse<JobLog>(HttpStatusCode.OK, new JobLog()));
            jobsApiClient
                .CreateJob(Arg.Is<JobCreateModel>(j => j.JobDefinitionId == JobConstants.DefinitionNames.FetchProviderProfileJob))
                .Returns(new Job { Id = "fpp-job" });

            PublishedResultsService resultsService = CreateResultsService(resultsRepository: resultsRepository,
                specificationsRepository: specificationsRepository,
                publishedProviderResultsAssemblerService: assembler,
                logger: logger,
                featureToggle: featureToggle,
                jobsApiClient: jobsApiClient);

            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            // Act
            await resultsService.PublishProviderResultsWithVariations(message);

            //Assert
            logger.DidNotReceive().Error(Arg.Any<string>());
            logger.Received(1).Information(Arg.Is($"Updated the published refresh date on the specification with id: {specificationId}"));

            await jobsApiClient.Received(6).CreateJob(Arg.Is<JobCreateModel>(j => j.JobDefinitionId == JobConstants.DefinitionNames.FetchProviderProfileJob && j.SpecificationId == specificationId && j.ParentJobId == jobId));
        }

        [TestMethod]
        public async Task PublishProviderResultsWithVariations_WhenLastPublishedDateAfterLastCalculatedDate_ThenDoesNotPublishResults()
        {
            // Arrange
            string specificationId = "1";

            SpecificationCurrentVersion specificationCurrentVersion = new SpecificationCurrentVersion
            {
                Id = specificationId,
                LastCalculationUpdatedAt = DateTimeOffset.Parse("2019/02/12T10:42:00"),
                PublishedResultsRefreshedAt = DateTimeOffset.Parse("2019/02/13T11:00:00")
            };

            IEnumerable<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult()
            };

            List<PublishedProviderResult> publishedProviderResults = new List<PublishedProviderResult>
            {
                new PublishedProviderResult()
                {
                    FundingStreamResult = new PublishedFundingStreamResult()
                    {
                        AllocationLineResult = new PublishedAllocationLineResult()
                        {
                            Current = new PublishedAllocationLineResultVersion()
                            {
                                Status = AllocationLineStatus.Updated,
                                Provider = new ProviderSummary { UKPRN = "prov1" },
                                Major = 1,
                                Minor = 2
                            },
                            AllocationLine = new AllocationLine()
                            {
                                Id = "alId",
                                Name = "Allocation Line",
                            }
                        },
                        FundingStream = new FundingStream { Id = "fs1", Name = "fs one", PeriodType = new PeriodType { Id = "pt1", Name = "pt" } }
                    },
                    FundingPeriod = new Period { Id = "fp1" }
                }
            };

            ICalculationResultsRepository resultsRepository = CreateResultsRepository();
            resultsRepository.GetProviderResultsBySpecificationId(Arg.Is(specificationId), Arg.Is(-1))
                .Returns(Task.FromResult(providerResults));

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository.GetCurrentSpecificationById(Arg.Is(specificationId))
                .Returns(specificationCurrentVersion);

            specificationsRepository.UpdatePublishedRefreshedDate(Arg.Is(specificationId), Arg.Any<DateTimeOffset>())
                .Returns(Task.FromResult(HttpStatusCode.OK));

            IPublishedProviderResultsAssemblerService assembler = CreateResultsAssembler();

            ILogger logger = CreateLogger();

            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
                .IsJobServiceForPublishProviderResultsEnabled()
                .Returns(true);

            IJobsApiClient jobsApiClient = InitialiseJobsApiClient();

            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();

            PublishedResultsService resultsService = CreateResultsService(resultsRepository: resultsRepository,
                specificationsRepository: specificationsRepository,
                publishedProviderResultsAssemblerService: assembler,
                publishedProviderResultsRepository: publishedProviderResultsRepository,
                logger: logger,
                featureToggle: featureToggle,
                jobsApiClient: jobsApiClient);

            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            // Act
            await resultsService.PublishProviderResultsWithVariations(message);

            //Assert
            await assembler
                .DidNotReceive()
                .GeneratePublishedProviderResultsToSave(Arg.Any<IEnumerable<PublishedProviderResult>>(), Arg.Any<IEnumerable<PublishedProviderResultExisting>>());

            await publishedProviderResultsRepository
                .DidNotReceive()
                .SavePublishedResults(Arg.Any<IEnumerable<PublishedProviderResult>>());
        }

        [TestMethod]
        // Applies to provider variation scenario VAR03
        public async Task PublishProviderResultsWithVariations_WhenAllocationResultHasNotChangedFromExisting_AndProviderClosedWithSuccessor_ThenAmendProfilePeriodsForBothProviders()
        {
            // Arrange
            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            DateTimeOffset specVariationDate = DateTimeOffset.Parse("2018-04-30T23:59:59");
            ISpecificationsRepository specificationsRepository = InitialiseSpecificationRepository(specVariationDate);

            List<ProviderResult> providerResults = CreateProviderResults(12, 24);

            ICalculationResultsRepository calculationResultsRepository = InitialiseCalculationResultsRepository(providerResults);

            IPublishedProviderResultsAssemblerService providerResultsAssembler = CreateRealResultsAssembler(specificationsRepository);

            IPublishedProviderResultsRepository publishedProviderResultsRepository = InitialisePublishedProviderResultsRepositoryWithExistingResults();

            IProviderVariationAssemblerService providerVariationAssembler = CreateProviderVariationAssemblerService();
            providerVariationAssembler
                .AssembleProviderVariationItems(Arg.Is(providerResults), Arg.Is(specificationId))
                .Returns(new List<ProviderChangeItem>
                {
                    new ProviderChangeItem
                    {
                        DoesProviderHaveSuccessor = true,
                        HasProviderClosed = true,
                        SuccessorProviderId = "prov2",
                        UpdatedProvider = new ProviderSummary
                        {
                            Id = "prov1",
                            UKPRN = "prov1u",
                            Status = "Closed",
                            ProviderSubType = "Academy sponsor led",
                            ProviderType = "Academies",
                            Successor = "prov2"
                        }
                    }
                });

            PublishedResultsService service = InitialisePublishedResultsService(calculationResultsRepository: calculationResultsRepository,
                specificationsRepository: specificationsRepository, providerResultsAssembler: providerResultsAssembler,
                publishedProviderResultsRepository: publishedProviderResultsRepository, providerVariationAssembler: providerVariationAssembler);

            // Setup saving results being saved - makes asserting easier
            IEnumerable<PublishedProviderResult> resultsBeingSaved = null;

            await publishedProviderResultsRepository
                .SavePublishedResults(Arg.Do<IEnumerable<PublishedProviderResult>>(r => resultsBeingSaved = r));

            // Act
            await service.PublishProviderResultsWithVariations(message);

            // Assert
            await publishedProviderResultsRepository
                .Received(1)
                .SavePublishedResults(Arg.Any<IEnumerable<PublishedProviderResult>>());

            // Should have results for both original and successor providers
            resultsBeingSaved.Should().HaveCount(2);

            AssertProviderAllocationLineCorrect(resultsBeingSaved, "prov1", specVariationDate, 7, 0, AllocationLineStatus.Held, 0, 2, "Alloc 1");

            AssertProviderAllocationLineCorrect(resultsBeingSaved, "prov2", specVariationDate, 14, 15, AllocationLineStatus.Held, 0, 2, "Alloc 1", new[] { "prov1u" });
        }

        [Ignore("Scenario not supported at the moment - needs clarification from BA")]
        [TestMethod]
        public async Task PublishProviderResultsWithVariations_WhenAllocationResultHasChangedFromExisting_AndProviderClosedWithSuccessor_ThenAmendProfilePeriodsForBothProviders()
        {
            // Arrange
            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            DateTimeOffset specVariationDate = DateTimeOffset.Parse("2018-04-30T23:59:59");
            ISpecificationsRepository specificationsRepository = InitialiseSpecificationRepository(specVariationDate);

            List<ProviderResult> providerResults = CreateProviderResults(13, 26);

            ICalculationResultsRepository calculationResultsRepository = InitialiseCalculationResultsRepository(providerResults);

            IPublishedProviderResultsAssemblerService providerResultsAssembler = CreateRealResultsAssembler(specificationsRepository);

            IPublishedProviderResultsRepository publishedProviderResultsRepository = InitialisePublishedProviderResultsRepositoryWithExistingResults();

            IProviderVariationAssemblerService providerVariationAssembler = CreateProviderVariationAssemblerService();
            providerVariationAssembler
                .AssembleProviderVariationItems(Arg.Is(providerResults), Arg.Is(specificationId))
                .Returns(new List<ProviderChangeItem>
                {
                    new ProviderChangeItem
                    {
                        DoesProviderHaveSuccessor = true,
                        HasProviderClosed = true,
                        SuccessorProviderId = "prov2",
                        UpdatedProvider = new ProviderSummary
                        {
                            Id = "prov1",
                            UKPRN = "prov1u",
                            Status = "Closed",
                            ProviderSubType = "Academy sponsor led",
                            ProviderType = "Academies",
                            Successor = "prov2"
                        }
                    }
                });

            PublishedResultsService service = InitialisePublishedResultsService(calculationResultsRepository: calculationResultsRepository,
                specificationsRepository: specificationsRepository, providerResultsAssembler: providerResultsAssembler,
                publishedProviderResultsRepository: publishedProviderResultsRepository, providerVariationAssembler: providerVariationAssembler);

            // Setup saving results being saved - makes asserting easier
            IEnumerable<PublishedProviderResult> resultsBeingSaved = null;

            await publishedProviderResultsRepository
                .SavePublishedResults(Arg.Do<IEnumerable<PublishedProviderResult>>(r => resultsBeingSaved = r));

            // Act
            await service.PublishProviderResultsWithVariations(message);

            // Assert
            await publishedProviderResultsRepository
                .Received(1)
                .SavePublishedResults(Arg.Any<IEnumerable<PublishedProviderResult>>());

            // Should have a result for the provider and the successor
            resultsBeingSaved.Should().HaveCount(2);

            AssertProviderAllocationLineCorrect(resultsBeingSaved, "prov1", specVariationDate, 7, 0, AllocationLineStatus.Held, 0, 2, "alloc1");

            AssertProviderAllocationLineCorrect(resultsBeingSaved, "prov2", specVariationDate, 16, 15, AllocationLineStatus.Held, 0, 2, "alloc1", new[] { "prov1" });
        }

        [TestMethod]
        // Applies to provider variation scenario VAR05 and VAR08
        public async Task PublishProviderResultsWithVariations_WhenProviderDataHasChanged_AndResultNotChanged_ThenAllocationLineMinorVersionUpdated()
        {
            // Arrange
            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            DateTimeOffset specVariationDate = DateTimeOffset.Parse("2018-04-30T23:59:59");
            ISpecificationsRepository specificationsRepository = InitialiseSpecificationRepository(specVariationDate);

            List<ProviderResult> providerResults = CreateProviderResults(12);

            ICalculationResultsRepository calculationResultsRepository = InitialiseCalculationResultsRepository(providerResults);

            IPublishedProviderResultsAssemblerService providerResultsAssembler = CreateRealResultsAssembler(specificationsRepository);

            List<PublishedProviderResultExisting> existingResults = new List<PublishedProviderResultExisting>()
            {
                new PublishedProviderResultExisting
                {
                    AllocationLineId = "alloc1",
                    Major = 0,
                    Minor = 1,
                    ProviderId = "prov1",
                    Status = AllocationLineStatus.Held,
                    Value = 12,
                    Version = 1,
                    ProfilePeriods = new List<ProfilingPeriod>
                    {
                        new ProfilingPeriod {  Period = "Apr", Year = 2018, Type = "Calendar", Value = 7 },
                        new ProfilingPeriod {  Period = "Jan", Year = 2019, Type = "Calendar", Value = 5 }
                    }
                }
            };
            IPublishedProviderResultsRepository publishedProviderResultsRepository = InitialisePublishedProviderResultsRepository(existingResults);

            IProviderVariationAssemblerService providerVariationAssembler = CreateProviderVariationAssemblerService();
            providerVariationAssembler
                .AssembleProviderVariationItems(Arg.Is(providerResults), Arg.Is(specificationId))
                .Returns(new List<ProviderChangeItem>
                {
                    new ProviderChangeItem
                    {
                        HasProviderDataChanged = true,
                        UpdatedProvider = new ProviderSummary
                        {
                            Authority = "updated auth",
                            Id = "prov1",
                            Status = "Open",
                            ProviderSubType = "x",
                            ProviderType = "y"
                        },
                        VariationReasons = new List<VariationReason>
                        {
                            VariationReason.AuthorityFieldUpdated
                        }
                    }
                });

            PublishedResultsService service = InitialisePublishedResultsService(calculationResultsRepository: calculationResultsRepository,
                specificationsRepository: specificationsRepository, providerResultsAssembler: providerResultsAssembler,
                publishedProviderResultsRepository: publishedProviderResultsRepository, providerVariationAssembler: providerVariationAssembler);

            // Store results to be saved to assert on them later
            IEnumerable<PublishedProviderResult> resultsBeingSaved = null;
            await publishedProviderResultsRepository
                .SavePublishedResults(Arg.Do<IEnumerable<PublishedProviderResult>>(r => resultsBeingSaved = r));

            // Act
            await service.PublishProviderResultsWithVariations(message);

            // Assert
            await publishedProviderResultsRepository
                .Received(1)
                .SavePublishedResults(Arg.Any<IEnumerable<PublishedProviderResult>>());

            resultsBeingSaved.Should().HaveCount(1);

            PublishedProviderResult prov1Result = resultsBeingSaved.FirstOrDefault(r => r.ProviderId == "prov1");

            // The provider details on the result should be updated
            prov1Result.FundingStreamResult.AllocationLineResult.Current.Provider.Authority.Should().Be("updated auth");

            // The status of the result should be updated
            prov1Result.FundingStreamResult.AllocationLineResult.Current.Status.Should().Be(AllocationLineStatus.Held);

            // The Minor version should be updated
            prov1Result.FundingStreamResult.AllocationLineResult.Current.Major.Should().Be(0);
            prov1Result.FundingStreamResult.AllocationLineResult.Current.Minor.Should().Be(2);
        }

        [TestMethod]
        // Applies to provider variation scenario VAR05 and VAR08
        public async Task PublishProviderResultsWithVariations_WhenProviderDataHasChanged_AndResultHasChangedFromExisting_ThenAllocationLineMinorVersionUpdated()
        {
            // Arrange
            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            DateTimeOffset specVariationDate = DateTimeOffset.Parse("2018-04-30T23:59:59");
            ISpecificationsRepository specificationsRepository = InitialiseSpecificationRepository(specVariationDate);

            List<ProviderResult> providerResults = CreateProviderResults(12);

            ICalculationResultsRepository calculationResultsRepository = InitialiseCalculationResultsRepository(providerResults);

            IPublishedProviderResultsAssemblerService providerResultsAssembler = CreateRealResultsAssembler(specificationsRepository);

            List<PublishedProviderResultExisting> existingResults = new List<PublishedProviderResultExisting>()
            {
                new PublishedProviderResultExisting
                {
                    AllocationLineId = "alloc1",
                    Major = 0,
                    Minor = 1,
                    ProviderId = "prov1",
                    Status = AllocationLineStatus.Held,
                    Value = 11,
                    Version = 1,
                    ProfilePeriods = new List<ProfilingPeriod>
                    {
                        new ProfilingPeriod {  Period = "Apr", Year = 2018, Type = "Calendar", Value = 6 },
                        new ProfilingPeriod {  Period = "Jan", Year = 2019, Type = "Calendar", Value = 5 }
                    }
                }
            };
            IPublishedProviderResultsRepository publishedProviderResultsRepository = InitialisePublishedProviderResultsRepository(existingResults);

            IProviderVariationAssemblerService providerVariationAssembler = CreateProviderVariationAssemblerService();
            providerVariationAssembler
                .AssembleProviderVariationItems(Arg.Is(providerResults), Arg.Is(specificationId))
                .Returns(new List<ProviderChangeItem>
                {
                    new ProviderChangeItem
                    {
                        HasProviderDataChanged = true,
                        UpdatedProvider = new ProviderSummary
                        {
                            Authority = "updated auth",
                            Id = "prov1",
                            Status = "Open",
                            ProviderSubType = "x",
                            ProviderType = "y"
                        },
                        VariationReasons = new List<VariationReason>
                        {
                            VariationReason.AuthorityFieldUpdated
                        }
                    }
                });

            PublishedResultsService service = InitialisePublishedResultsService(calculationResultsRepository: calculationResultsRepository,
                specificationsRepository: specificationsRepository, providerResultsAssembler: providerResultsAssembler,
                publishedProviderResultsRepository: publishedProviderResultsRepository, providerVariationAssembler: providerVariationAssembler);

            // Store results to be saved to assert on them later
            IEnumerable<PublishedProviderResult> resultsBeingSaved = null;
            await publishedProviderResultsRepository
                .SavePublishedResults(Arg.Do<IEnumerable<PublishedProviderResult>>(r => resultsBeingSaved = r));

            // Act
            await service.PublishProviderResultsWithVariations(message);

            // Assert
            await publishedProviderResultsRepository
                .Received(1)
                .SavePublishedResults(Arg.Any<IEnumerable<PublishedProviderResult>>());

            resultsBeingSaved.Should().HaveCount(1);

            PublishedProviderResult prov1Result = resultsBeingSaved.FirstOrDefault(r => r.ProviderId == "prov1");

            // The provider details on the result should be updated
            prov1Result.FundingStreamResult.AllocationLineResult.Current.Provider.Authority.Should().Be("updated auth");

            // The status of the result should be updated
            prov1Result.FundingStreamResult.AllocationLineResult.Current.Status.Should().Be(AllocationLineStatus.Held);

            // The minor version of the result should be incremented
            prov1Result.FundingStreamResult.AllocationLineResult.Current.Major.Should().Be(0);
            prov1Result.FundingStreamResult.AllocationLineResult.Current.Minor.Should().Be(2);
        }

        [TestMethod]
        public async Task PublishProviderResultsWithVariations_WhenProviderDataHasChanged_AndResultHasChangedFromExisting_ThenAllocationLineVariationReasonsUpdated()
        {
            // Arrange
            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            DateTimeOffset specVariationDate = DateTimeOffset.Parse("2018-04-30T23:59:59");
            ISpecificationsRepository specificationsRepository = InitialiseSpecificationRepository(specVariationDate);

            List<ProviderResult> providerResults = CreateProviderResults(12);

            ICalculationResultsRepository calculationResultsRepository = InitialiseCalculationResultsRepository(providerResults);

            IPublishedProviderResultsAssemblerService providerResultsAssembler = CreateRealResultsAssembler(specificationsRepository);

            List<PublishedProviderResultExisting> existingResults = new List<PublishedProviderResultExisting>()
            {
                new PublishedProviderResultExisting
                {
                    AllocationLineId = "alloc1",
                    Major = 0,
                    Minor = 1,
                    ProviderId = "prov1",
                    Status = AllocationLineStatus.Held,
                    Value = 11,
                    Version = 1,
                    ProfilePeriods = new List<ProfilingPeriod>
                    {
                        new ProfilingPeriod {  Period = "Apr", Year = 2018, Type = "Calendar", Value = 6 },
                        new ProfilingPeriod {  Period = "Jan", Year = 2019, Type = "Calendar", Value = 5 }
                    }
                }
            };
            IPublishedProviderResultsRepository publishedProviderResultsRepository = InitialisePublishedProviderResultsRepository(existingResults);

            IProviderVariationAssemblerService providerVariationAssembler = CreateProviderVariationAssemblerService();
            providerVariationAssembler
                .AssembleProviderVariationItems(Arg.Is(providerResults), Arg.Is(specificationId))
                .Returns(new List<ProviderChangeItem>
                {
                    new ProviderChangeItem
                    {
                        HasProviderDataChanged = true,
                        UpdatedProvider = new ProviderSummary
                        {
                            Authority = "updated auth",
                            Id = "prov1",
                            Status = "Open",
                            ProviderSubType = "x",
                            ProviderType = "y"
                        },
                        VariationReasons = new List<VariationReason>
                        {
                            VariationReason.AuthorityFieldUpdated
                        }
                    }
                });

            PublishedResultsService service = InitialisePublishedResultsService(calculationResultsRepository: calculationResultsRepository,
                specificationsRepository: specificationsRepository, providerResultsAssembler: providerResultsAssembler,
                publishedProviderResultsRepository: publishedProviderResultsRepository, providerVariationAssembler: providerVariationAssembler);

            // Store results to be saved to assert on them later
            IEnumerable<PublishedProviderResult> resultsBeingSaved = null;
            await publishedProviderResultsRepository
                .SavePublishedResults(Arg.Do<IEnumerable<PublishedProviderResult>>(r => resultsBeingSaved = r));

            // Act
            await service.PublishProviderResultsWithVariations(message);

            // Assert
            await publishedProviderResultsRepository
                .Received(1)
                .SavePublishedResults(Arg.Any<IEnumerable<PublishedProviderResult>>());

            resultsBeingSaved.Should().HaveCount(1);

            PublishedProviderResult prov1Result = resultsBeingSaved.FirstOrDefault(r => r.ProviderId == "prov1");

            // The provider details on the result should be updated
            prov1Result.FundingStreamResult.AllocationLineResult.Current.Provider.Authority.Should().Be("updated auth");

            // The status of the result should be updated
            prov1Result.FundingStreamResult.AllocationLineResult.Current.VariationReasons.Should().NotBeNullOrEmpty()
                .And.Contain(VariationReason.AuthorityFieldUpdated);
        }

        [TestMethod]
        // Applies to provider variation scenario VAR02
        public async Task PublishProviderResultsWithVariations_WhenProviderHasClosed_AndNoSuccessor_ThenProfileValuesUpdated()
        {
            // Arrange
            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            DateTimeOffset specVariationDate = DateTimeOffset.Parse("2018-04-30T23:59:59");
            ISpecificationsRepository specificationsRepository = InitialiseSpecificationRepository(specVariationDate);

            List<ProviderResult> providerResults = CreateProviderResults(12, 24);

            ICalculationResultsRepository calculationResultsRepository = InitialiseCalculationResultsRepository(providerResults);

            IPublishedProviderResultsAssemblerService providerResultsAssembler = CreateRealResultsAssembler(specificationsRepository);

            IPublishedProviderResultsRepository publishedProviderResultsRepository = InitialisePublishedProviderResultsRepositoryWithExistingResults();

            IProviderVariationAssemblerService providerVariationAssembler = CreateProviderVariationAssemblerService();
            providerVariationAssembler
                .AssembleProviderVariationItems(Arg.Is(providerResults), Arg.Is(specificationId))
                .Returns(new List<ProviderChangeItem>
                {
                    new ProviderChangeItem
                    {
                        DoesProviderHaveSuccessor = false,
                        HasProviderClosed = true,
                        UpdatedProvider = new ProviderSummary
                        {
                            Id = "prov1",
                            Status = "Closed",
                            ProviderSubType = "x",
                            ProviderType = "y",
                            Successor = "prov2"
                        }
                    }
                });

            PublishedResultsService service = InitialisePublishedResultsService(calculationResultsRepository: calculationResultsRepository,
                specificationsRepository: specificationsRepository, providerResultsAssembler: providerResultsAssembler,
                publishedProviderResultsRepository: publishedProviderResultsRepository, providerVariationAssembler: providerVariationAssembler);

            IEnumerable<PublishedProviderResult> resultsBeingSaved = null;
            await publishedProviderResultsRepository
                .SavePublishedResults(Arg.Do<IEnumerable<PublishedProviderResult>>(r => resultsBeingSaved = r));

            // Act
            await service.PublishProviderResultsWithVariations(message);

            // Assert
            await publishedProviderResultsRepository
                .Received(1)
                .SavePublishedResults(Arg.Any<IEnumerable<PublishedProviderResult>>());

            resultsBeingSaved.Should().HaveCount(1);

            PublishedProviderResult prov1Result = resultsBeingSaved.FirstOrDefault(r => r.ProviderId == "prov1");

            // The status of the result should be updated
            prov1Result.FundingStreamResult.AllocationLineResult.Current.Status
                .Should()
                .Be(AllocationLineStatus.Held);

            // The minor version of the result should be incremented
            prov1Result.FundingStreamResult.AllocationLineResult.Current.Major.Should().Be(0);
            prov1Result.FundingStreamResult.AllocationLineResult.Current.Minor.Should().Be(2);

            // The profiling periods after the variation date should be zero
            prov1Result.FundingStreamResult.AllocationLineResult.Current.ProfilingPeriods.Where(p => p.PeriodDate > specVariationDate)
                .Should()
                .Contain(p => p.Value == 0);
        }

        [TestMethod]
        // Applies to provider variation scenario VAR03
        public async Task PublishProviderResultsWithVariations_WhenMultipleProvidersMergeIntoOneSuccessor_ThenSuccessorVersionOnlyUpdatedOnce()
        {
            // Arrange
            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            DateTimeOffset specVariationDate = DateTimeOffset.Parse("2018-04-30T23:59:59");
            ISpecificationsRepository specificationsRepository = InitialiseSpecificationRepository(specVariationDate);

            List<ProviderResult> providerResults = CreateProviderResults(12, 24, 36);

            ICalculationResultsRepository calculationResultsRepository = InitialiseCalculationResultsRepository(providerResults);

            IPublishedProviderResultsAssemblerService providerResultsAssembler = CreateRealResultsAssembler(specificationsRepository);

            List<PublishedProviderResultExisting> existingResults = new List<PublishedProviderResultExisting>()
            {
                new PublishedProviderResultExisting
                {
                    AllocationLineId = "alloc1",
                    Major = 0,
                    Minor = 1,
                    ProviderId = "prov1",
                    Status = AllocationLineStatus.Held,
                    Value = 12,
                    Version = 1,
                    ProfilePeriods = new List<ProfilingPeriod>
                    {
                        new ProfilingPeriod { Period = "Apr", Type = "CalendarMonth", Value = 7, Year = 2018 },
                        new ProfilingPeriod { Period = "Jan", Type = "CalendarMonth", Value = 5, Year = 2019 }
                    },
                    ProviderLookups = new List<ProviderLookup>
                    {
                        new ProviderLookup { ProviderType = "Academies", ProviderSubType = "Academy sponsor led" }
                    }
                },
                new PublishedProviderResultExisting
                {
                    AllocationLineId = "alloc1",
                    Major = 0,
                    Minor = 1,
                    ProviderId = "prov2",
                    Status = AllocationLineStatus.Held,
                    Value = 24,
                    Version = 1,
                    ProfilePeriods = new List<ProfilingPeriod>
                    {
                        new ProfilingPeriod { Period = "Apr", Type = "CalendarMonth", Value = 14, Year = 2018 },
                        new ProfilingPeriod { Period = "Jan", Type = "CalendarMonth", Value = 10, Year = 2019 }
                    },
                    ProviderLookups = new List<ProviderLookup>
                    {
                        new ProviderLookup { ProviderType = "Academies", ProviderSubType = "Academy sponsor led" }
                    }
                },
                new PublishedProviderResultExisting
                {
                    AllocationLineId = "alloc1",
                    Major = 0,
                    Minor = 1,
                    ProviderId = "prov3",
                    Status = AllocationLineStatus.Held,
                    Value = 36,
                    Version = 1,
                    ProfilePeriods = new List<ProfilingPeriod>
                    {
                        new ProfilingPeriod { Period = "Apr", Type = "CalendarMonth", Value = 21, Year = 2018 },
                        new ProfilingPeriod { Period = "Jan", Type = "CalendarMonth", Value = 15, Year = 2019 }
                    },
                    ProviderLookups = new List<ProviderLookup>
                    {
                        new ProviderLookup { ProviderType = "Academies", ProviderSubType = "Academy sponsor led" }
                    }
                }
            };
            IPublishedProviderResultsRepository publishedProviderResultsRepository = InitialisePublishedProviderResultsRepository(existingResults);

            IProviderVariationAssemblerService providerVariationAssembler = CreateProviderVariationAssemblerService();
            providerVariationAssembler
                .AssembleProviderVariationItems(Arg.Is(providerResults), Arg.Is(specificationId))
                .Returns(new List<ProviderChangeItem>
                {
                    new ProviderChangeItem
                    {
                        DoesProviderHaveSuccessor = true,
                        HasProviderClosed = true,
                        SuccessorProviderId = "prov3",
                        UpdatedProvider = new ProviderSummary
                        {
                            Id = "prov1",
                            UKPRN = "prov1u",
                            Status = "Closed",
                            ProviderSubType = "Academy sponsor led",
                            ProviderType = "Academies",
                            Successor = "prov3"
                        }
                    },
                    new ProviderChangeItem
                    {
                        DoesProviderHaveSuccessor = true,
                        HasProviderClosed = true,
                        SuccessorProviderId = "prov3",
                        UpdatedProvider = new ProviderSummary
                        {
                            Id = "prov2",
                            UKPRN = "prov2u",
                            Status = "Closed",
                            ProviderSubType = "Academy sponsor led",
                            ProviderType = "Academies",
                            Successor = "prov3"
                        }
                    }
                });

            PublishedResultsService service = InitialisePublishedResultsService(specificationsRepository, calculationResultsRepository, providerResultsAssembler, publishedProviderResultsRepository, providerVariationAssembler);

            // Setup saving results being saved - makes asserting easier
            IEnumerable<PublishedProviderResult> resultsBeingSaved = null;

            await publishedProviderResultsRepository
                .SavePublishedResults(Arg.Do<IEnumerable<PublishedProviderResult>>(r => resultsBeingSaved = r));

            // Act
            await service.PublishProviderResultsWithVariations(message);

            // Assert
            await publishedProviderResultsRepository
                .Received(1)
                .SavePublishedResults(Arg.Any<IEnumerable<PublishedProviderResult>>());

            // Should have results for both original and successor providers
            resultsBeingSaved.Should().HaveCount(3);

            AssertProviderAllocationLineCorrect(resultsBeingSaved, "prov1", specVariationDate, 7, 0, AllocationLineStatus.Held, 0, 2, "Alloc 1");

            AssertProviderAllocationLineCorrect(resultsBeingSaved, "prov2", specVariationDate, 14, 0, AllocationLineStatus.Held, 0, 2, "Alloc 1");

            AssertProviderAllocationLineCorrect(resultsBeingSaved, "prov3", specVariationDate, 21, 30, AllocationLineStatus.Held, 0, 2, "Alloc 1", new[] { "prov1u", "prov2u" });
        }

        [TestMethod]
        // Applies to provider variation scenario VAR02 and VAR03
        public async Task PublishProviderResultsWithVariations_WhenProviderClosed_ThenNotProfiled()
        {
            // Arrange
            IFeatureToggle featureToggle = InitialiseFeatureToggle();

            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            IJobsApiClient jobsApiClient = InitialiseJobsApiClient();

            DateTimeOffset specVariationDate = DateTimeOffset.Parse("2018-04-30T23:59:59");
            ISpecificationsRepository specificationsRepository = InitialiseSpecificationRepository(specVariationDate);

            List<ProviderResult> providerResults = CreateProviderResults(12, 24);

            ICalculationResultsRepository calculationResultsRepository = InitialiseCalculationResultsRepository(providerResults);

            IPublishedProviderResultsAssemblerService providerResultsAssembler = CreateRealResultsAssembler(specificationsRepository);

            IPublishedProviderResultsRepository publishedProviderResultsRepository = InitialisePublishedProviderResultsRepositoryWithExistingResults();

            IProviderVariationAssemblerService providerVariationAssembler = CreateProviderVariationAssemblerService();
            providerVariationAssembler
                .AssembleProviderVariationItems(Arg.Is(providerResults), Arg.Is(specificationId))
                .Returns(new List<ProviderChangeItem>
                {
                    new ProviderChangeItem
                    {
                        DoesProviderHaveSuccessor = false,
                        HasProviderClosed = true,
                        UpdatedProvider = new ProviderSummary
                        {
                            Id = "prov1",
                            Status = "Closed",
                            ProviderSubType = "x",
                            ProviderType = "y",
                            Successor = "prov2"
                        }
                    }
                });

            IProviderVariationsService providerVariationsService = CreateProviderVariationsService(providerVariationAssembler, specificationsRepository);

            PublishedResultsService service = CreateResultsService(featureToggle: featureToggle, jobsApiClient: jobsApiClient, resultsRepository: calculationResultsRepository,
                specificationsRepository: specificationsRepository, publishedProviderResultsAssemblerService: providerResultsAssembler,
                publishedProviderResultsRepository: publishedProviderResultsRepository, providerVariationsService: providerVariationsService);

            // Act
            await service.PublishProviderResultsWithVariations(message);

            // Assert
            await jobsApiClient
                .DidNotReceive()
                .CreateJob(Arg.Is<JobCreateModel>(j => j.JobDefinitionId == JobConstants.DefinitionNames.FetchProviderProfileJob));
        }

        [TestMethod]
        // Applies to provider variation scenario VAR16 and VAR17
        public async Task PublishProviderResultsWithVariations_WhenProviderHasNotVaried_ThenNoUpdatesPerfomed()
        {
            // Arrange
            IFeatureToggle featureToggle = InitialiseFeatureToggle();

            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            IJobsApiClient jobsApiClient = InitialiseJobsApiClient();

            DateTimeOffset specVariationDate = DateTimeOffset.Parse("2019-04-30T23:59:59");
            ISpecificationsRepository specificationsRepository = InitialiseSpecificationRepository(specVariationDate);

            List<ProviderResult> providerResults = CreateProviderResults(12, 24);

            ICalculationResultsRepository calculationResultsRepository = InitialiseCalculationResultsRepository(providerResults);

            IPublishedProviderResultsAssemblerService providerResultsAssembler = CreateRealResultsAssembler(specificationsRepository);

            List<PublishedProviderResultExisting> existingResults = new List<PublishedProviderResultExisting>()
            {
                new PublishedProviderResultExisting { AllocationLineId = "alloc1", Major = 0, Minor = 1, ProviderId = "prov1", Status = AllocationLineStatus.Held, Value = 12, Version = 1 },
                new PublishedProviderResultExisting { AllocationLineId = "alloc1", Major = 0, Minor = 1, ProviderId = "prov2", Status = AllocationLineStatus.Held, Value = 24, Version = 1 }
            };
            IPublishedProviderResultsRepository publishedProviderResultsRepository = InitialisePublishedProviderResultsRepository(existingResults);

            IProviderVariationAssemblerService providerVariationAssembler = CreateProviderVariationAssemblerService();
            providerVariationAssembler
                .AssembleProviderVariationItems(Arg.Is(providerResults), Arg.Is(specificationId))
                .Returns(new List<ProviderChangeItem>());

            IProviderVariationsService providerVariationsService = CreateProviderVariationsService(providerVariationAssembler, specificationsRepository);

            PublishedResultsService service = CreateResultsService(featureToggle: featureToggle, jobsApiClient: jobsApiClient, resultsRepository: calculationResultsRepository,
                specificationsRepository: specificationsRepository, publishedProviderResultsAssemblerService: providerResultsAssembler,
                publishedProviderResultsRepository: publishedProviderResultsRepository, providerVariationsService: providerVariationsService);

            // Act
            await service.PublishProviderResultsWithVariations(message);

            // Assert
            await publishedProviderResultsRepository
                .DidNotReceive()
                .SavePublishedResults(Arg.Any<IEnumerable<PublishedProviderResult>>());
        }

        [TestMethod]
        // Applies to provider variation scenario VAR03
        public async Task PublishProviderResultsWithVariations_WhenAllocationResultHasNotChangedFromExisting_AndProviderClosedWithSuccessor_ThenCanOnlyBeVariedOnce()
        {
            // Arrange
            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            DateTimeOffset specVariationDate = DateTimeOffset.Parse("2018-04-30T23:59:59");
            ISpecificationsRepository specificationsRepository = InitialiseSpecificationRepository(specVariationDate);

            List<ProviderResult> providerResults = CreateProviderResults(12, 24);

            ICalculationResultsRepository calculationResultsRepository = InitialiseCalculationResultsRepository(providerResults);

            IPublishedProviderResultsAssemblerService providerResultsAssembler = CreateRealResultsAssembler(specificationsRepository);

            List<PublishedProviderResultExisting> existingResultsFirstTime = new List<PublishedProviderResultExisting>()
            {
                new PublishedProviderResultExisting
                {
                    AllocationLineId = "alloc1",
                    Major = 0,
                    Minor = 1,
                    ProviderId = "prov1",
                    Status = AllocationLineStatus.Held,
                    Value = 12,
                    Version = 1,
                    ProfilePeriods = new List<ProfilingPeriod>
                    {
                        new ProfilingPeriod { Period = "Apr", Type = "CalendarMonth", Value = 7, Year = 2018 },
                        new ProfilingPeriod { Period = "Jan", Type = "CalendarMonth", Value = 5, Year = 2019 }
                    },
                    ProviderLookups = new List<ProviderLookup>
                    {
                        new ProviderLookup { ProviderType = "Academies", ProviderSubType = "Academy sponsor led" }
                    }
                },
                new PublishedProviderResultExisting
                {
                    AllocationLineId = "alloc1",
                    Major = 0,
                    Minor = 1,
                    ProviderId = "prov2",
                    Status = AllocationLineStatus.Held,
                    Value = 24,
                    Version = 1,
                    ProfilePeriods = new List<ProfilingPeriod>
                    {
                        new ProfilingPeriod { Period = "Apr", Type = "CalendarMonth", Value = 14, Year = 2018 },
                        new ProfilingPeriod { Period = "Jan", Type = "CalendarMonth", Value = 10, Year = 2019 }
                    },
                    ProviderLookups = new List<ProviderLookup>
                    {
                        new ProviderLookup { ProviderType = "Academies", ProviderSubType = "Academy sponsor led" }
                    }
                }
            };

            List<PublishedProviderResultExisting> existingResultsSecondTime = new List<PublishedProviderResultExisting>()
            {
                new PublishedProviderResultExisting
                {
                    AllocationLineId = "alloc1",
                    Major = 0,
                    Minor = 1,
                    ProviderId = "prov1",
                    Status = AllocationLineStatus.Held,
                    Value = 12,
                    Version = 1,
                    ProfilePeriods = new List<ProfilingPeriod>
                    {
                        new ProfilingPeriod { Period = "Apr", Type = "CalendarMonth", Value = 7, Year = 2018 },
                        new ProfilingPeriod { Period = "Jan", Type = "CalendarMonth", Value = 5, Year = 2019 }
                    },
                    ProviderLookups = new List<ProviderLookup>
                    {
                        new ProviderLookup { ProviderType = "Academies", ProviderSubType = "Academy sponsor led" }
                    },
                    HasResultBeenVaried = true
                },
                new PublishedProviderResultExisting
                {
                    AllocationLineId = "alloc1",
                    Major = 0,
                    Minor = 1,
                    ProviderId = "prov2",
                    Status = AllocationLineStatus.Held,
                    Value = 24,
                    Version = 1,
                    ProfilePeriods = new List<ProfilingPeriod>
                    {
                        new ProfilingPeriod { Period = "Apr", Type = "CalendarMonth", Value = 14, Year = 2018 },
                        new ProfilingPeriod { Period = "Jan", Type = "CalendarMonth", Value = 10, Year = 2019 }
                    },
                    ProviderLookups = new List<ProviderLookup>
                    {
                        new ProviderLookup { ProviderType = "Academies", ProviderSubType = "Academy sponsor led" }
                    }
                }
            };

            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();
            publishedProviderResultsRepository
                .GetExistingPublishedProviderResultsForSpecificationId(Arg.Is(specificationId))
                .Returns(existingResultsFirstTime, existingResultsSecondTime);

            IProviderVariationAssemblerService providerVariationAssembler = CreateProviderVariationAssemblerService();
            providerVariationAssembler
                .AssembleProviderVariationItems(Arg.Is(providerResults), Arg.Is(specificationId))
                .Returns(new List<ProviderChangeItem>
                {
                    new ProviderChangeItem
                    {
                        DoesProviderHaveSuccessor = true,
                        HasProviderClosed = true,
                        SuccessorProviderId = "prov2",
                        UpdatedProvider = new ProviderSummary
                        {
                            Id = "prov1",
                            UKPRN = "prov1u",
                            Status = "Closed",
                            ProviderSubType = "Academy sponsor led",
                            ProviderType = "Academies",
                            Successor = "prov2"
                        }
                    }
                });

            PublishedResultsService service = InitialisePublishedResultsService(calculationResultsRepository: calculationResultsRepository,
                specificationsRepository: specificationsRepository, providerResultsAssembler: providerResultsAssembler,
                publishedProviderResultsRepository: publishedProviderResultsRepository, providerVariationAssembler: providerVariationAssembler);

            // Setup saving results being saved - makes asserting easier
            List<PublishedProviderResult> resultsBeingSaved = new List<PublishedProviderResult>();

            await publishedProviderResultsRepository
                .SavePublishedResults(Arg.Do<IEnumerable<PublishedProviderResult>>(r => resultsBeingSaved.AddRange(r)));

            // Act
            await service.PublishProviderResultsWithVariations(message); // 1st refresh

            await service.PublishProviderResultsWithVariations(message); // 2nd refresh

            // Assert
            await publishedProviderResultsRepository
                .Received(1)
                .SavePublishedResults(Arg.Any<IEnumerable<PublishedProviderResult>>());

            // Should have results for both original and successor providers
            resultsBeingSaved.Should().HaveCount(2);

            AssertProviderAllocationLineCorrect(resultsBeingSaved, "prov1", specVariationDate, 7, 0, AllocationLineStatus.Held, 0, 2, "Alloc 1");

            AssertProviderAllocationLineCorrect(resultsBeingSaved, "prov2", specVariationDate, 14, 15, AllocationLineStatus.Held, 0, 2, "Alloc 1", new[] { "prov1u" });
        }

        [TestMethod]
        // Applies to provider variation scenario VAR02
        public async Task PublishProviderResultsWithVariations_WhenProviderHasClosed_AndNoSuccessor_ThenCanOnlyBeVariedOnce()
        {
            // Arrange
            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            DateTimeOffset specVariationDate = DateTimeOffset.Parse("2018-04-30T23:59:59");
            ISpecificationsRepository specificationsRepository = InitialiseSpecificationRepository(specVariationDate);

            List<ProviderResult> providerResults = CreateProviderResults(12, 24);

            ICalculationResultsRepository calculationResultsRepository = InitialiseCalculationResultsRepository(providerResults);

            IPublishedProviderResultsAssemblerService providerResultsAssembler = CreateRealResultsAssembler(specificationsRepository);

            List<PublishedProviderResultExisting> existingResultsFirstTime = new List<PublishedProviderResultExisting>()
            {
                new PublishedProviderResultExisting
                {
                    AllocationLineId = "alloc1",
                    Major = 0,
                    Minor = 1,
                    ProviderId = "prov1",
                    Status = AllocationLineStatus.Held,
                    Value = 12,
                    Version = 1,
                    ProfilePeriods = new List<ProfilingPeriod>
                    {
                        new ProfilingPeriod { Period = "Apr", Type = "CalendarMonth", Value = 7, Year = 2018 },
                        new ProfilingPeriod { Period = "Jan", Type = "CalendarMonth", Value = 5, Year = 2019 }
                    },
                    ProviderLookups = new List<ProviderLookup>
                    {
                        new ProviderLookup { ProviderType = "Academies", ProviderSubType = "Academy sponsor led" }
                    }
                },
                new PublishedProviderResultExisting
                {
                    AllocationLineId = "alloc1",
                    Major = 0,
                    Minor = 1,
                    ProviderId = "prov2",
                    Status = AllocationLineStatus.Held,
                    Value = 24,
                    Version = 1,
                    ProfilePeriods = new List<ProfilingPeriod>
                    {
                        new ProfilingPeriod { Period = "Apr", Type = "CalendarMonth", Value = 14, Year = 2018 },
                        new ProfilingPeriod { Period = "Jan", Type = "CalendarMonth", Value = 10, Year = 2019 }
                    },
                    ProviderLookups = new List<ProviderLookup>
                    {
                        new ProviderLookup { ProviderType = "Academies", ProviderSubType = "Academy sponsor led" }
                    }
                }
            };

            List<PublishedProviderResultExisting> existingResultsSecondTime = new List<PublishedProviderResultExisting>()
            {
                new PublishedProviderResultExisting
                {
                    AllocationLineId = "alloc1",
                    Major = 0,
                    Minor = 1,
                    ProviderId = "prov1",
                    Status = AllocationLineStatus.Held,
                    Value = 12,
                    Version = 1,
                    ProfilePeriods = new List<ProfilingPeriod>
                    {
                        new ProfilingPeriod { Period = "Apr", Type = "CalendarMonth", Value = 7, Year = 2018 },
                        new ProfilingPeriod { Period = "Jan", Type = "CalendarMonth", Value = 5, Year = 2019 }
                    },
                    ProviderLookups = new List<ProviderLookup>
                    {
                        new ProviderLookup { ProviderType = "Academies", ProviderSubType = "Academy sponsor led" }
                    },
                    HasResultBeenVaried = true
                },
                new PublishedProviderResultExisting
                {
                    AllocationLineId = "alloc1",
                    Major = 0,
                    Minor = 1,
                    ProviderId = "prov2",
                    Status = AllocationLineStatus.Held,
                    Value = 24,
                    Version = 1,
                    ProfilePeriods = new List<ProfilingPeriod>
                    {
                        new ProfilingPeriod { Period = "Apr", Type = "CalendarMonth", Value = 14, Year = 2018 },
                        new ProfilingPeriod { Period = "Jan", Type = "CalendarMonth", Value = 10, Year = 2019 }
                    },
                    ProviderLookups = new List<ProviderLookup>
                    {
                        new ProviderLookup { ProviderType = "Academies", ProviderSubType = "Academy sponsor led" }
                    }
                }
            };

            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();
            publishedProviderResultsRepository
                .GetExistingPublishedProviderResultsForSpecificationId(Arg.Is(specificationId))
                .Returns(existingResultsFirstTime, existingResultsSecondTime);

            IProviderVariationAssemblerService providerVariationAssembler = CreateProviderVariationAssemblerService();
            providerVariationAssembler
                .AssembleProviderVariationItems(Arg.Is(providerResults), Arg.Is(specificationId))
                .Returns(new List<ProviderChangeItem>
                {
                    new ProviderChangeItem
                    {
                        DoesProviderHaveSuccessor = false,
                        HasProviderClosed = true,
                        UpdatedProvider = new ProviderSummary
                        {
                            Id = "prov1",
                            Status = "Closed",
                            ProviderSubType = "x",
                            ProviderType = "y",
                            Successor = "prov2"
                        }
                    }
                });

            PublishedResultsService service = InitialisePublishedResultsService(calculationResultsRepository: calculationResultsRepository,
                specificationsRepository: specificationsRepository, providerResultsAssembler: providerResultsAssembler,
                publishedProviderResultsRepository: publishedProviderResultsRepository, providerVariationAssembler: providerVariationAssembler);

            List<PublishedProviderResult> resultsBeingSaved = new List<PublishedProviderResult>();
            await publishedProviderResultsRepository
                .SavePublishedResults(Arg.Do<IEnumerable<PublishedProviderResult>>(r => resultsBeingSaved.AddRange(r)));

            // Act
            await service.PublishProviderResultsWithVariations(message); // 1st refresh

            await service.PublishProviderResultsWithVariations(message); // 2nd refresh

            // Assert
            await publishedProviderResultsRepository
                .Received(1)
                .SavePublishedResults(Arg.Any<IEnumerable<PublishedProviderResult>>());

            resultsBeingSaved.Should().HaveCount(1);

            PublishedProviderResult prov1Result = resultsBeingSaved.FirstOrDefault(r => r.ProviderId == "prov1");

            // The status of the result should be updated
            prov1Result.FundingStreamResult.AllocationLineResult.Current.Status
                .Should()
                .Be(AllocationLineStatus.Held);

            // The minor version of the result should be incremented
            prov1Result.FundingStreamResult.AllocationLineResult.Current.Major.Should().Be(0);
            prov1Result.FundingStreamResult.AllocationLineResult.Current.Minor.Should().Be(2);

            // The profiling periods after the variation date should be zero
            prov1Result.FundingStreamResult.AllocationLineResult.Current.ProfilingPeriods.Where(p => p.PeriodDate > specVariationDate)
                .Should()
                .Contain(p => p.Value == 0);
        }

        [TestMethod]
        // Applies to all provider variation scenarios
        public async Task PublishProviderResultsWithVariations_WhenRequestIsToChoose_ThenNoVariationsProcessed()
        {
            // Arrange
            IFeatureToggle featureToggle = InitialiseFeatureToggle();

            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            IJobsApiClient jobsApiClient = InitialiseJobsApiClient(false);

            ILogger logger = CreateLogger();

            DateTimeOffset specVariationDate = DateTimeOffset.Parse("2019-04-30T23:59:59");
            ISpecificationsRepository specificationsRepository = InitialiseSpecificationRepository(specVariationDate);

            List<ProviderResult> providerResults = CreateProviderResults(12, 24);

            ICalculationResultsRepository calculationResultsRepository = InitialiseCalculationResultsRepository(providerResults);

            IPublishedProviderResultsAssemblerService providerResultsAssembler = CreateRealResultsAssembler(specificationsRepository);

            List<PublishedProviderResultExisting> existingResults = new List<PublishedProviderResultExisting>();
            IPublishedProviderResultsRepository publishedProviderResultsRepository = InitialisePublishedProviderResultsRepository(existingResults);

            IProviderVariationAssemblerService providerVariationAssembler = CreateProviderVariationAssemblerService();

            IProviderVariationsService providerVariationsService = CreateProviderVariationsService(providerVariationAssembler, specificationsRepository, logger);

            PublishedResultsService service = CreateResultsService(featureToggle: featureToggle, jobsApiClient: jobsApiClient, resultsRepository: calculationResultsRepository,
                specificationsRepository: specificationsRepository, publishedProviderResultsAssemblerService: providerResultsAssembler,
                publishedProviderResultsRepository: publishedProviderResultsRepository, providerVariationsService: providerVariationsService, logger: logger);

            // Act
            await service.PublishProviderResultsWithVariations(message);

            // Assert
            await providerVariationAssembler
                .DidNotReceive()
                .AssembleProviderVariationItems(Arg.Any<IEnumerable<ProviderResult>>(), Arg.Any<string>());

            logger
                .Received(1)
                .Information(Arg.Is($"Not processing variations for specification '{specificationId}' as job '{jobId}' is not for Refresh."));
        }

        [TestMethod]
        // Applies to provider variation scenario VAR03
        public async Task PublishProviderResultsWithVariations_WhenProviderClosedWithSuccessor_AndSuccessorNotFound_ThenRefreshFails()
        {
            // Arrange
            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            DateTimeOffset specVariationDate = DateTimeOffset.Parse("2018-04-30T23:59:59");
            ISpecificationsRepository specificationsRepository = InitialiseSpecificationRepository(specVariationDate);

            List<ProviderResult> providerResults = CreateProviderResults(12);

            ICalculationResultsRepository calculationResultsRepository = InitialiseCalculationResultsRepository(providerResults);

            IPublishedProviderResultsAssemblerService providerResultsAssembler = CreateRealResultsAssembler(specificationsRepository);

            IPublishedProviderResultsRepository publishedProviderResultsRepository = InitialisePublishedProviderResultsRepositoryWithExistingResults();

            IProviderVariationAssemblerService providerVariationAssembler = CreateProviderVariationAssemblerService();
            providerVariationAssembler
                .AssembleProviderVariationItems(Arg.Is(providerResults), Arg.Is(specificationId))
                .Returns(new List<ProviderChangeItem>
                {
                    new ProviderChangeItem
                    {
                        DoesProviderHaveSuccessor = true,
                        HasProviderClosed = true,
                        SuccessorProviderId = "prov2",
                        UpdatedProvider = new ProviderSummary
                        {
                            Id = "prov1",
                            Status = "Closed",
                            ProviderSubType = "Academy sponsor led",
                            ProviderType = "Academies",
                            Successor = "prov2"
                        }
                    }
                });

            ILogger logger = CreateLogger();

            PublishedResultsService service = InitialisePublishedResultsService(calculationResultsRepository: calculationResultsRepository,
                specificationsRepository: specificationsRepository, providerResultsAssembler: providerResultsAssembler,
                publishedProviderResultsRepository: publishedProviderResultsRepository, providerVariationAssembler: providerVariationAssembler, logger: logger);

            // Act
            await service.PublishProviderResultsWithVariations(message);

            // Assert
            logger
                .Received(1)
                .Error($"Failed to process provider variations for specification: {specificationId}");

            await publishedProviderResultsRepository
                .DidNotReceive()
                .SavePublishedResults(Arg.Any<IEnumerable<PublishedProviderResult>>());
        }

        [TestMethod]
        // Applies to all provider variation scenarios
        public async Task PublishProviderResultsWithVariations_WhenProviderVariationAssemblerThrowsException_ThenRefreshFails()
        {
            // Arrange
            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            DateTimeOffset specVariationDate = DateTimeOffset.Parse("2018-04-30T23:59:59");
            ISpecificationsRepository specificationsRepository = InitialiseSpecificationRepository(specVariationDate);

            List<ProviderResult> providerResults = CreateProviderResults(12);

            ICalculationResultsRepository calculationResultsRepository = InitialiseCalculationResultsRepository(providerResults);

            IPublishedProviderResultsAssemblerService providerResultsAssembler = CreateRealResultsAssembler(specificationsRepository);

            IPublishedProviderResultsRepository publishedProviderResultsRepository = InitialisePublishedProviderResultsRepositoryWithExistingResults();

            IProviderVariationAssemblerService providerVariationAssembler = CreateProviderVariationAssemblerService();
            providerVariationAssembler
                .AssembleProviderVariationItems(Arg.Is(providerResults), Arg.Is(specificationId))
                .Returns<Task<IEnumerable<ProviderChangeItem>>>(x => { throw new NonRetriableException("Assembler error"); });

            ILogger logger = CreateLogger();

            PublishedResultsService service = InitialisePublishedResultsService(calculationResultsRepository: calculationResultsRepository,
                specificationsRepository: specificationsRepository, providerResultsAssembler: providerResultsAssembler,
                publishedProviderResultsRepository: publishedProviderResultsRepository, providerVariationAssembler: providerVariationAssembler, logger: logger);

            // Act
            await service.PublishProviderResultsWithVariations(message);

            // Assert
            logger
                .Received(1)
                .Error($"Failed to process provider variations for specification: {specificationId}");

            await publishedProviderResultsRepository
                .DidNotReceive()
                .SavePublishedResults(Arg.Any<IEnumerable<PublishedProviderResult>>());
        }

        [TestMethod]
        // Applies to provider variation scenario VAR03
        public async Task PublishProviderResultsWithVariations_WhenProviderClosedWithSuccessor_AndAffectedProviderHasNoExistingResult_ThenMessageLogged()
        {
            // Arrange
            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            DateTimeOffset specVariationDate = DateTimeOffset.Parse("2018-04-30T23:59:59");
            ISpecificationsRepository specificationsRepository = InitialiseSpecificationRepository(specVariationDate);

            List<ProviderResult> providerResults = CreateProviderResults(12, 24);

            ICalculationResultsRepository calculationResultsRepository = InitialiseCalculationResultsRepository(providerResults);

            IPublishedProviderResultsAssemblerService providerResultsAssembler = CreateRealResultsAssembler(specificationsRepository);

            List<PublishedProviderResultExisting> existingResults = new List<PublishedProviderResultExisting>()
            {
                new PublishedProviderResultExisting
                {
                    AllocationLineId = "alloc1",
                    Major = 0,
                    Minor = 1,
                    ProviderId = "prov2",
                    Status = AllocationLineStatus.Held,
                    Value = 24,
                    Version = 1,
                    ProfilePeriods = new List<ProfilingPeriod>
                    {
                        new ProfilingPeriod { Period = "Apr", Type = "CalendarMonth", Value = 14, Year = 2018 },
                        new ProfilingPeriod { Period = "Jan", Type = "CalendarMonth", Value = 10, Year = 2019 }
                    },
                    ProviderLookups = new List<ProviderLookup>
                    {
                        new ProviderLookup { ProviderType = "Academies", ProviderSubType = "Academy sponsor led" }
                    }
                }
            };
            IPublishedProviderResultsRepository publishedProviderResultsRepository = InitialisePublishedProviderResultsRepository(existingResults);

            IProviderVariationAssemblerService providerVariationAssembler = CreateProviderVariationAssemblerService();
            providerVariationAssembler
                .AssembleProviderVariationItems(Arg.Is(providerResults), Arg.Is(specificationId))
                .Returns(new List<ProviderChangeItem>
                {
                    new ProviderChangeItem
                    {
                        DoesProviderHaveSuccessor = true,
                        HasProviderClosed = true,
                        SuccessorProviderId = "prov2",
                        UpdatedProvider = new ProviderSummary
                        {
                            Id = "prov1",
                            Status = "Closed",
                            ProviderSubType = "Academy sponsor led",
                            ProviderType = "Academies",
                            Successor = "prov2"
                        }
                    }
                });

            ILogger logger = CreateLogger();

            PublishedResultsService service = InitialisePublishedResultsService(calculationResultsRepository: calculationResultsRepository,
                specificationsRepository: specificationsRepository, providerResultsAssembler: providerResultsAssembler,
                publishedProviderResultsRepository: publishedProviderResultsRepository, providerVariationAssembler: providerVariationAssembler, logger: logger);

            // Setup saving results being saved - makes asserting easier
            IEnumerable<PublishedProviderResult> resultsBeingSaved = null;

            await publishedProviderResultsRepository
                .SavePublishedResults(Arg.Do<IEnumerable<PublishedProviderResult>>(r => resultsBeingSaved = r));

            // Act
            await service.PublishProviderResultsWithVariations(message);

            // Assert
            logger
                .Received(1)
                .Information($"No existing result for provider prov1 and allocation line alloc1 to vary. Specification '{specificationId}'");
        }

        [TestMethod]
        // Applies to provider variation scenario VAR02
        public async Task PublishProviderResultsWithVariations_WhenProviderClosedWithoutSuccessor_AndAffectedProviderHasNoExistingResult_ThenMessageLogged()
        {
            // Arrange
            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            DateTimeOffset specVariationDate = DateTimeOffset.Parse("2018-04-30T23:59:59");
            ISpecificationsRepository specificationsRepository = InitialiseSpecificationRepository(specVariationDate);

            List<ProviderResult> providerResults = CreateProviderResults(12, 24);

            ICalculationResultsRepository calculationResultsRepository = InitialiseCalculationResultsRepository(providerResults);

            IPublishedProviderResultsAssemblerService providerResultsAssembler = CreateRealResultsAssembler(specificationsRepository);

            List<PublishedProviderResultExisting> existingResults = new List<PublishedProviderResultExisting>()
            {
                new PublishedProviderResultExisting
                {
                    AllocationLineId = "alloc1",
                    Major = 0,
                    Minor = 1,
                    ProviderId = "prov2",
                    Status = AllocationLineStatus.Held,
                    Value = 24,
                    Version = 1,
                    ProfilePeriods = new List<ProfilingPeriod>
                    {
                        new ProfilingPeriod { Period = "Apr", Type = "CalendarMonth", Value = 14, Year = 2018 },
                        new ProfilingPeriod { Period = "Jan", Type = "CalendarMonth", Value = 10, Year = 2019 }
                    },
                    ProviderLookups = new List<ProviderLookup>
                    {
                        new ProviderLookup { ProviderType = "Academies", ProviderSubType = "Academy sponsor led" }
                    }
                }
            };
            IPublishedProviderResultsRepository publishedProviderResultsRepository = InitialisePublishedProviderResultsRepository(existingResults);

            IProviderVariationAssemblerService providerVariationAssembler = CreateProviderVariationAssemblerService();
            providerVariationAssembler
                .AssembleProviderVariationItems(Arg.Is(providerResults), Arg.Is(specificationId))
                .Returns(new List<ProviderChangeItem>
                {
                    new ProviderChangeItem
                    {
                        HasProviderClosed = true,
                        UpdatedProvider = new ProviderSummary
                        {
                            Id = "prov1",
                            Status = "Closed",
                            ProviderSubType = "Academy sponsor led",
                            ProviderType = "Academies"
                        }
                    }
                });

            ILogger logger = CreateLogger();

            PublishedResultsService service = InitialisePublishedResultsService(calculationResultsRepository: calculationResultsRepository,
                specificationsRepository: specificationsRepository, providerResultsAssembler: providerResultsAssembler,
                publishedProviderResultsRepository: publishedProviderResultsRepository, providerVariationAssembler: providerVariationAssembler, logger: logger);

            // Setup saving results being saved - makes asserting easier
            IEnumerable<PublishedProviderResult> resultsBeingSaved = null;

            await publishedProviderResultsRepository
                .SavePublishedResults(Arg.Do<IEnumerable<PublishedProviderResult>>(r => resultsBeingSaved = r));

            // Act
            await service.PublishProviderResultsWithVariations(message);

            // Assert
            logger
                .Received(1)
                .Information($"Provider 'prov1' has closed without successor but has no existing result. Specification '{specificationId}' and allocation line 'alloc1'");
        }

        [TestMethod]
        // Applies to provider variation scenario VAR03
        public async Task PublishProviderResultsWithVariations_WhenProviderClosedWithSuccessor_AndNoResultGenerated_ThenRefreshFails()
        {
            // Arrange
            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            DateTimeOffset specVariationDate = DateTimeOffset.Parse("2018-04-30T23:59:59");
            ISpecificationsRepository specificationsRepository = InitialiseSpecificationRepository(specVariationDate);

            List<ProviderResult> providerResults = CreateProviderResults(12, 24);
            providerResults.Remove(providerResults.First(r => r.Provider.Id == "prov1"));

            ICalculationResultsRepository calculationResultsRepository = InitialiseCalculationResultsRepository(providerResults);

            IPublishedProviderResultsAssemblerService providerResultsAssembler = CreateRealResultsAssembler(specificationsRepository);

            IPublishedProviderResultsRepository publishedProviderResultsRepository = InitialisePublishedProviderResultsRepositoryWithExistingResults();

            IProviderVariationAssemblerService providerVariationAssembler = CreateProviderVariationAssemblerService();
            providerVariationAssembler
                .AssembleProviderVariationItems(Arg.Is(providerResults), Arg.Is(specificationId))
                .Returns(new List<ProviderChangeItem>
                {
                    new ProviderChangeItem
                    {
                        DoesProviderHaveSuccessor = true,
                        HasProviderClosed = true,
                        SuccessorProviderId = "prov2",
                        UpdatedProvider = new ProviderSummary
                        {
                            Id = "prov1",
                            Status = "Closed",
                            ProviderSubType = "Academy sponsor led",
                            ProviderType = "Academies",
                            Successor = "prov2"
                        }
                    }
                });

            ILogger logger = CreateLogger();

            PublishedResultsService service = InitialisePublishedResultsService(calculationResultsRepository: calculationResultsRepository,
                specificationsRepository: specificationsRepository, providerResultsAssembler: providerResultsAssembler,
                publishedProviderResultsRepository: publishedProviderResultsRepository, providerVariationAssembler: providerVariationAssembler, logger: logger);

            // Setup saving results being saved - makes asserting easier
            IEnumerable<PublishedProviderResult> resultsBeingSaved = null;

            await publishedProviderResultsRepository
                .SavePublishedResults(Arg.Do<IEnumerable<PublishedProviderResult>>(r => resultsBeingSaved = r));

            // Act
            await service.PublishProviderResultsWithVariations(message);

            // Assert
            logger
                .Received(1)
                .Error($"Failed to process provider variations for specification: {specificationId}");

            await publishedProviderResultsRepository
                .DidNotReceive()
                .SavePublishedResults(Arg.Any<IEnumerable<PublishedProviderResult>>());
        }

        [TestMethod]
        // Applies to provider variation scenario VAR02
        public async Task PublishProviderResultsWithVariations_WhenProviderClosedWithoutSuccessor_AndNoResultGenerated_ThenRefreshFails()
        {
            // Arrange
            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            DateTimeOffset specVariationDate = DateTimeOffset.Parse("2018-04-30T23:59:59");
            ISpecificationsRepository specificationsRepository = InitialiseSpecificationRepository(specVariationDate);

            List<ProviderResult> providerResults = CreateProviderResults(12, 24);
            providerResults.Remove(providerResults.First(r => r.Provider.Id == "prov1"));

            ICalculationResultsRepository calculationResultsRepository = InitialiseCalculationResultsRepository(providerResults);

            IPublishedProviderResultsAssemblerService providerResultsAssembler = CreateRealResultsAssembler(specificationsRepository);

            IPublishedProviderResultsRepository publishedProviderResultsRepository = InitialisePublishedProviderResultsRepositoryWithExistingResults();

            IProviderVariationAssemblerService providerVariationAssembler = CreateProviderVariationAssemblerService();
            providerVariationAssembler
                .AssembleProviderVariationItems(Arg.Is(providerResults), Arg.Is(specificationId))
                .Returns(new List<ProviderChangeItem>
                {
                    new ProviderChangeItem
                    {
                        HasProviderClosed = true,
                        UpdatedProvider = new ProviderSummary
                        {
                            Id = "prov1",
                            Status = "Closed",
                            ProviderSubType = "Academy sponsor led",
                            ProviderType = "Academies"
                        }
                    }
                });

            ILogger logger = CreateLogger();

            PublishedResultsService service = InitialisePublishedResultsService(
                calculationResultsRepository: calculationResultsRepository,
                specificationsRepository: specificationsRepository,
                providerResultsAssembler: providerResultsAssembler,
                publishedProviderResultsRepository: publishedProviderResultsRepository,
                providerVariationAssembler: providerVariationAssembler,
                logger: logger);

            // Setup saving results being saved - makes asserting easier
            IEnumerable<PublishedProviderResult> resultsBeingSaved = null;

            await publishedProviderResultsRepository
                .SavePublishedResults(Arg.Do<IEnumerable<PublishedProviderResult>>(r => resultsBeingSaved = r));

            // Act
            await service.PublishProviderResultsWithVariations(message);

            // Assert
            logger
                .Received(1)
                .Error($"Failed to process provider variations for specification: {specificationId}");

            await publishedProviderResultsRepository
                .DidNotReceive()
                .SavePublishedResults(Arg.Any<IEnumerable<PublishedProviderResult>>());
        }

        [TestMethod]
        // Applies to all provider variation scenarios
        public async Task PublishProviderResults_WhenProviderChangesAreGenerated_ThenChangesAreSaved()
        {
            // Arrange
            string specificationId = "spec-1";

            SpecificationCurrentVersion specificationCurrentVersion = new SpecificationCurrentVersion
            {
                Id = specificationId
            };

            IEnumerable<PublishedProviderResult> publishedProviderResults = CreatePublishedProviderResults();

            ICalculationResultsRepository resultsRepository = CreateResultsRepository();
            resultsRepository.GetProviderResultsBySpecificationId(Arg.Is(specificationId), Arg.Is(-1))
                .Returns(CreateProviderResults(3));

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
                .AssemblePublishedProviderResults(Arg.Any<IEnumerable<ProviderResult>>(), Arg.Any<Reference>(), Arg.Any<SpecificationCurrentVersion>())
                .Returns(publishedProviderResults);

            assembler
                .GeneratePublishedProviderResultsToSave(Arg.Any<IEnumerable<PublishedProviderResult>>(), Arg.Any<IEnumerable<PublishedProviderResultExisting>>())
                .Returns((publishedProviderResults, Enumerable.Empty<PublishedProviderResultExisting>()));

            IPublishedProviderCalculationResultsRepository publishedProviderCalculationResultsRepository = CreatePublishedProviderCalculationResultsRepository();

            IProviderChangesRepository providerChangesRepository = CreateProviderChangesRepository();

            IProviderVariationsService providerVariationsService = CreateProviderVariationsService();
            IEnumerable<ProviderChangeItem> changeItems = CreateProviderChangeResults();

            ProcessProviderVariationsResult processProviderVariationsResult = new ProcessProviderVariationsResult()
            {
                ProviderChanges = changeItems,
            };

            providerVariationsService
                .ProcessProviderVariations(
                Arg.Any<JobViewModel>(),
                Arg.Any<SpecificationCurrentVersion>(),
                Arg.Any<IEnumerable<ProviderResult>>(),
                Arg.Any<IEnumerable<PublishedProviderResultExisting>>(),
                Arg.Any<IEnumerable<PublishedProviderResult>>(),
                Arg.Any<List<PublishedProviderResult>>(),
                Arg.Any<Reference>())
                .Returns(processProviderVariationsResult);

            IFeatureToggle featureToggles = InitialiseFeatureToggle(true);

            IJobsApiClient jobsApiClient = InitialiseJobsApiClient(true);

            PublishedResultsService resultsService = CreateResultsService(
                resultsRepository: resultsRepository,
                publishedProviderResultsRepository: publishedProviderResultsRepository,
                specificationsRepository: specificationsRepository,
                publishedProviderCalculationResultsRepository: publishedProviderCalculationResultsRepository,
                publishedProviderResultsAssemblerService: assembler,
                providerChangesRepository: providerChangesRepository,
                providerVariationsService: providerVariationsService,
                jobsApiClient: jobsApiClient,
                featureToggle: featureToggles);

            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            // Act
            await resultsService.PublishProviderResultsWithVariations(message);

            //Assert
            await providerChangesRepository
                .Received(1)
                .AddProviderChanges(Arg.Is<IEnumerable<ProviderChangeRecord>>(c => c.Count() == 3));
        }

        [TestMethod]
        // Applies to all provider variation scenarios
        public async Task PublishProviderResults_WhenProviderChangesReturnedAreNull_ThenErrorsReturned()
        {
            // Arrange
            string specificationId = "spec-1";

            SpecificationCurrentVersion specificationCurrentVersion = new SpecificationCurrentVersion
            {
                Id = specificationId
            };

            IEnumerable<PublishedProviderResult> publishedProviderResults = CreatePublishedProviderResults();

            ICalculationResultsRepository resultsRepository = CreateResultsRepository();
            resultsRepository.GetProviderResultsBySpecificationId(Arg.Is(specificationId), Arg.Is(-1))
                .Returns(CreateProviderResults(3));

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
                .AssemblePublishedProviderResults(Arg.Any<IEnumerable<ProviderResult>>(), Arg.Any<Reference>(), Arg.Any<SpecificationCurrentVersion>())
                .Returns(publishedProviderResults);

            assembler
                .GeneratePublishedProviderResultsToSave(Arg.Any<IEnumerable<PublishedProviderResult>>(), Arg.Any<IEnumerable<PublishedProviderResultExisting>>())
                .Returns((publishedProviderResults, Enumerable.Empty<PublishedProviderResultExisting>()));

            IPublishedProviderCalculationResultsRepository publishedProviderCalculationResultsRepository = CreatePublishedProviderCalculationResultsRepository();

            IProviderChangesRepository providerChangesRepository = CreateProviderChangesRepository();

            IProviderVariationsService providerVariationsService = CreateProviderVariationsService();
            IEnumerable<ProviderChangeItem> changeItems = CreateProviderChangeResults();

            ProcessProviderVariationsResult processProviderVariationsResult = null;

            providerVariationsService
                .ProcessProviderVariations(
                Arg.Any<JobViewModel>(),
                Arg.Any<SpecificationCurrentVersion>(),
                Arg.Any<IEnumerable<ProviderResult>>(),
                Arg.Any<IEnumerable<PublishedProviderResultExisting>>(),
                Arg.Any<IEnumerable<PublishedProviderResult>>(),
                Arg.Any<List<PublishedProviderResult>>(),
                Arg.Any<Reference>())
                .Returns(processProviderVariationsResult);

            IFeatureToggle featureToggles = InitialiseFeatureToggle(true);

            IJobsApiClient jobsApiClient = InitialiseJobsApiClient(true);

            ILogger logger = CreateLogger();

            ICacheProvider cacheProvider = CreateCacheProvider();

            PublishedResultsService resultsService = CreateResultsService(
                resultsRepository: resultsRepository,
                publishedProviderResultsRepository: publishedProviderResultsRepository,
                specificationsRepository: specificationsRepository,
                publishedProviderCalculationResultsRepository: publishedProviderCalculationResultsRepository,
                publishedProviderResultsAssemblerService: assembler,
                providerChangesRepository: providerChangesRepository,
                providerVariationsService: providerVariationsService,
                jobsApiClient: jobsApiClient,
                featureToggle: featureToggles,
                logger: logger,
                cacheProvider: cacheProvider);

            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            // Act
            await resultsService.PublishProviderResultsWithVariations(message);

            //Assert
            logger
                .Received(1)
                .Error(Arg.Is<string>("Provider changes returned null for specification '{specificationId}'"), Arg.Is(specificationCurrentVersion.Id));
        }

        private static IEnumerable<ProviderChangeItem> CreateProviderChangeResults()
        {
            List<ProviderChangeItem> changeItems = new List<ProviderChangeItem>();
            changeItems.Add(new ProviderChangeItem()
            {
                DoesProviderHaveSuccessor = false,
                HasProviderClosed = true,
                HasProviderDataChanged = false,
                HasProviderOpened = false,
                PriorProviderState = new ProviderSummary()
                {
                    Id = "provider1",
                },
                ProviderReasonCode = "ProviderReason",
                SuccessorProviderId = null,
                UpdatedProvider = new ProviderSummary()
                {
                    Id = "provider1",
                },
                VariationReasons = Enumerable.Empty<VariationReason>(),
            });

            changeItems.Add(new ProviderChangeItem()
            {
                DoesProviderHaveSuccessor = false,
                HasProviderClosed = true,
                HasProviderDataChanged = false,
                HasProviderOpened = false,
                PriorProviderState = new ProviderSummary()
                {
                    Id = "provider2",
                },
                ProviderReasonCode = "ProviderReason",
                SuccessorProviderId = null,
                UpdatedProvider = new ProviderSummary()
                {
                    Id = "provider2",
                },
                VariationReasons = Enumerable.Empty<VariationReason>(),
            });

            changeItems.Add(new ProviderChangeItem()
            {
                DoesProviderHaveSuccessor = false,
                HasProviderClosed = true,
                HasProviderDataChanged = false,
                HasProviderOpened = false,
                PriorProviderState = new ProviderSummary()
                {
                    Id = "provider3",
                },
                ProviderReasonCode = "ProviderReason",
                SuccessorProviderId = null,
                UpdatedProvider = new ProviderSummary()
                {
                    Id = "provider3",
                },
                VariationReasons = Enumerable.Empty<VariationReason>(),
            });
            return changeItems;
        }

        [TestMethod]
        // Applies to provider variation scenario VAR03
        public async Task PublishProviderResultsWithVariations_WhenProviderClosedWithSuccessor__AndNoAffectedPeriods_ThenNoResultsSaved()
        {
            // Arrange
            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            DateTimeOffset specVariationDate = DateTimeOffset.Parse("2019-02-01T23:59:59");
            ISpecificationsRepository specificationsRepository = InitialiseSpecificationRepository(specVariationDate);

            List<ProviderResult> providerResults = CreateProviderResults(12, 24);

            ICalculationResultsRepository calculationResultsRepository = InitialiseCalculationResultsRepository(providerResults);

            IPublishedProviderResultsAssemblerService providerResultsAssembler = CreateRealResultsAssembler(specificationsRepository);

            IPublishedProviderResultsRepository publishedProviderResultsRepository = InitialisePublishedProviderResultsRepositoryWithExistingResults();

            IProviderVariationAssemblerService providerVariationAssembler = CreateProviderVariationAssemblerService();
            providerVariationAssembler
                .AssembleProviderVariationItems(Arg.Is(providerResults), Arg.Is(specificationId))
                .Returns(new List<ProviderChangeItem>
                {
                    new ProviderChangeItem
                    {
                        DoesProviderHaveSuccessor = true,
                        HasProviderClosed = true,
                        SuccessorProviderId = "prov2",
                        UpdatedProvider = new ProviderSummary
                        {
                            Id = "prov1",
                            Status = "Closed",
                            ProviderSubType = "Academy sponsor led",
                            ProviderType = "Academies",
                            Successor = "prov2"
                        }
                    }
                });

            PublishedResultsService service = InitialisePublishedResultsService(calculationResultsRepository: calculationResultsRepository,
                specificationsRepository: specificationsRepository, providerResultsAssembler: providerResultsAssembler,
                publishedProviderResultsRepository: publishedProviderResultsRepository, providerVariationAssembler: providerVariationAssembler);

            // Act
            await service.PublishProviderResultsWithVariations(message);

            // Assert
            await publishedProviderResultsRepository
                .DidNotReceive()
                .SavePublishedResults(Arg.Any<IEnumerable<PublishedProviderResult>>());
        }

        [TestMethod]
        // Applies to provider variation scenario VAR02
        public async Task PublishProviderResultsWithVariations_WhenProviderHasClosedWithNoSuccessor_AndNoAffectedPeriods_ThenNoResultsSaved()
        {
            // Arrange
            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            DateTimeOffset specVariationDate = DateTimeOffset.Parse("2019-02-01T23:59:59");
            ISpecificationsRepository specificationsRepository = InitialiseSpecificationRepository(specVariationDate);

            List<ProviderResult> providerResults = CreateProviderResults(12, 24);

            ICalculationResultsRepository calculationResultsRepository = InitialiseCalculationResultsRepository(providerResults);

            IPublishedProviderResultsAssemblerService providerResultsAssembler = CreateRealResultsAssembler(specificationsRepository);

            IPublishedProviderResultsRepository publishedProviderResultsRepository = InitialisePublishedProviderResultsRepositoryWithExistingResults();

            IProviderVariationAssemblerService providerVariationAssembler = CreateProviderVariationAssemblerService();
            providerVariationAssembler
                .AssembleProviderVariationItems(Arg.Is(providerResults), Arg.Is(specificationId))
                .Returns(new List<ProviderChangeItem>
                {
                    new ProviderChangeItem
                    {
                        DoesProviderHaveSuccessor = false,
                        HasProviderClosed = true,
                        UpdatedProvider = new ProviderSummary
                        {
                            Id = "prov1",
                            Status = "Closed",
                            ProviderSubType = "x",
                            ProviderType = "y",
                            Successor = "prov2"
                        }
                    }
                });

            PublishedResultsService service = InitialisePublishedResultsService(calculationResultsRepository: calculationResultsRepository,
                specificationsRepository: specificationsRepository, providerResultsAssembler: providerResultsAssembler,
                publishedProviderResultsRepository: publishedProviderResultsRepository, providerVariationAssembler: providerVariationAssembler);

            // Act
            await service.PublishProviderResultsWithVariations(message);

            // Assert
            await publishedProviderResultsRepository
                .DidNotReceive()
                .SavePublishedResults(Arg.Any<IEnumerable<PublishedProviderResult>>());
        }

        [TestMethod]
        // Applies to all provider variation scenario
        public async Task PublishProviderResultsWithVariations_WhenVariationsPresent_AndNoSpecificationVariationDate_ThenRefreshFails()
        {
            // Arrange
            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            ISpecificationsRepository specificationsRepository = InitialiseSpecificationRepository(null);

            List<ProviderResult> providerResults = CreateProviderResults(12, 24);

            ICalculationResultsRepository calculationResultsRepository = InitialiseCalculationResultsRepository(providerResults);

            IPublishedProviderResultsAssemblerService providerResultsAssembler = CreateRealResultsAssembler(specificationsRepository);

            IPublishedProviderResultsRepository publishedProviderResultsRepository = InitialisePublishedProviderResultsRepositoryWithExistingResults();

            IProviderVariationAssemblerService providerVariationAssembler = CreateProviderVariationAssemblerService();
            providerVariationAssembler
                .AssembleProviderVariationItems(Arg.Is(providerResults), Arg.Is(specificationId))
                .Returns(new List<ProviderChangeItem>
                {
                    new ProviderChangeItem
                    {
                        DoesProviderHaveSuccessor = true,
                        HasProviderClosed = true,
                        SuccessorProviderId = "prov2",
                        UpdatedProvider = new ProviderSummary
                        {
                            Id = "prov1",
                            Status = "Closed",
                            ProviderSubType = "Academy sponsor led",
                            ProviderType = "Academies",
                            Successor = "prov2"
                        }
                    }
                });

            ILogger logger = CreateLogger();

            PublishedResultsService service = InitialisePublishedResultsService(calculationResultsRepository: calculationResultsRepository,
                specificationsRepository: specificationsRepository, providerResultsAssembler: providerResultsAssembler,
                publishedProviderResultsRepository: publishedProviderResultsRepository, providerVariationAssembler: providerVariationAssembler, logger: logger);

            // Act
            await service.PublishProviderResultsWithVariations(message);

            // Assert
            logger
                .Received(1)
                .Error($"Failed to process provider variations for specification: {specificationId}");

            await publishedProviderResultsRepository
                .DidNotReceive()
                .SavePublishedResults(Arg.Any<IEnumerable<PublishedProviderResult>>());
        }

        [TestMethod]
        // Applies to provider variation scenario VAR01
        public async Task PublishProviderResultsWithVariations_WhenTwoSchoolsMergeToFormNewSchool_ThenCreateNewResultForSuccessor()
        {
            // Arrange
            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            DateTimeOffset specVariationDate = DateTimeOffset.Parse("2018-04-30T23:59:59");
            ISpecificationsRepository specificationsRepository = InitialiseSpecificationRepository(specVariationDate);

            List<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult
                {
                    AllocationLineResults = new List<AllocationLineResult>
                    {
                        new AllocationLineResult { AllocationLine = new Reference { Id = "alloc2", Name = "Alloc 2" }, Value = 12 }
                    },
                    CalculationResults = new List<CalculationResult>
                    {
                        new CalculationResult
                        {
                            AllocationLine = new Reference { Id = "alloc2", Name = "Alloc 2" },
                            Calculation = new Reference { Id = "calc1", Name = "Calc 1" },
                            CalculationSpecification = new Reference { Id = "calcspec1", Name = "Calc 1" },
                            CalculationType = Models.Calcs.CalculationType.Funding,
                            Value = 12
                        }
                    },
                    Id = "provresult1",
                    Provider = new ProviderSummary { Id = "prov1" }
                },
                new ProviderResult
                {
                    AllocationLineResults = new List<AllocationLineResult>
                    {
                        new AllocationLineResult { AllocationLine = new Reference { Id = "alloc2", Name = "Alloc 2" }, Value = 24 }
                    },
                    CalculationResults = new List<CalculationResult>
                    {
                        new CalculationResult
                        {
                            AllocationLine = new Reference { Id = "alloc2", Name = "Alloc 2" },
                            Calculation = new Reference { Id = "calc1", Name = "Calc 1" },
                            CalculationSpecification = new Reference { Id = "calcspec1", Name = "Calc 1" },
                            CalculationType = Models.Calcs.CalculationType.Funding,
                            Value = 24
                        }
                    },
                    Id = "provresult2",
                    Provider = new ProviderSummary { Id = "prov2" }
                }
            };

            ICalculationResultsRepository calculationResultsRepository = InitialiseCalculationResultsRepository(providerResults);

            IPublishedProviderResultsAssemblerService providerResultsAssembler = CreateRealResultsAssembler(specificationsRepository);

            List<PublishedProviderResultExisting> existingResults = new List<PublishedProviderResultExisting>()
            {
                new PublishedProviderResultExisting
                {
                    AllocationLineId = "alloc2",
                    Major = 0,
                    Minor = 1,
                    ProviderId = "prov1",
                    Status = AllocationLineStatus.Held,
                    Value = 12,
                    Version = 1,
                    ProfilePeriods = new List<ProfilingPeriod>
                    {
                        new ProfilingPeriod { Period = "Apr", Type = "CalendarMonth", Value = 7, Year = 2018 },
                        new ProfilingPeriod { Period = "Jan", Type = "CalendarMonth", Value = 5, Year = 2019 }
                    },
                    ProviderLookups = new List<ProviderLookup>
                    {
                        new ProviderLookup { ProviderType = "Free schools", ProviderSubType = "Free schools special" }
                    }
                },
                new PublishedProviderResultExisting
                {
                    AllocationLineId = "alloc2",
                    Major = 0,
                    Minor = 1,
                    ProviderId = "prov2",
                    Status = AllocationLineStatus.Held,
                    Value = 24,
                    Version = 1,
                    ProfilePeriods = new List<ProfilingPeriod>
                    {
                        new ProfilingPeriod { Period = "Apr", Type = "CalendarMonth", Value = 14, Year = 2018 },
                        new ProfilingPeriod { Period = "Jan", Type = "CalendarMonth", Value = 10, Year = 2019 }
                    },
                    ProviderLookups = new List<ProviderLookup>
                    {
                        new ProviderLookup { ProviderType = "Free schools", ProviderSubType = "Free schools special" }
                    }
                }
            };
            IPublishedProviderResultsRepository publishedProviderResultsRepository = InitialisePublishedProviderResultsRepository(existingResults);

            IProviderVariationAssemblerService providerVariationAssembler = CreateProviderVariationAssemblerService();
            providerVariationAssembler
                .AssembleProviderVariationItems(Arg.Is(providerResults), Arg.Is(specificationId))
                .Returns(new List<ProviderChangeItem>
                {
                    new ProviderChangeItem
                    {
                        DoesProviderHaveSuccessor = true,
                        HasProviderClosed = true,
                        SuccessorProviderId = "prov3",
                        SuccessorProvider = new ProviderSummary
                        {
                            Id = "prov3",
                            UKPRN = "prov3u",
                            Status = "Open",
                            ProviderSubType = "Free schools special",
                            ProviderType = "Free Schools"
                        },
                        UpdatedProvider = new ProviderSummary
                        {
                            Id = "prov1",
                            UKPRN = "prov1u",
                            Status = "Closed",
                            ProviderSubType = "Free schools special",
                            ProviderType = "Free Schools",
                            Successor = "prov3"
                        }
                    },
                    new ProviderChangeItem
                    {
                        DoesProviderHaveSuccessor = true,
                        HasProviderClosed = true,
                        SuccessorProviderId = "prov3",
                        SuccessorProvider = new ProviderSummary
                        {
                            Id = "prov3",
                            UKPRN = "prov3u",
                            Status = "Open",
                            ProviderSubType = "Free schools special",
                            ProviderType = "Free Schools"
                        },
                        UpdatedProvider = new ProviderSummary
                        {
                            Id = "prov2",
                            UKPRN = "prov2u",
                            Status = "Closed",
                            ProviderSubType = "Free schools special",
                            ProviderType = "Free Schools",
                            Successor = "prov3"
                        }
                    }
                });

            PublishedResultsService service = InitialisePublishedResultsService(specificationsRepository, calculationResultsRepository, providerResultsAssembler, publishedProviderResultsRepository, providerVariationAssembler);

            // Setup saving results being saved - makes asserting easier
            IEnumerable<PublishedProviderResult> resultsBeingSaved = null;

            await publishedProviderResultsRepository
                .SavePublishedResults(Arg.Do<IEnumerable<PublishedProviderResult>>(r => resultsBeingSaved = r));

            // Act
            await service.PublishProviderResultsWithVariations(message);

            // Assert
            await publishedProviderResultsRepository
                .Received(1)
                .SavePublishedResults(Arg.Any<IEnumerable<PublishedProviderResult>>());

            // Should have results for both original and successor providers
            resultsBeingSaved.Should().HaveCount(3);

            AssertProviderAllocationLineCorrect(resultsBeingSaved, "prov1", specVariationDate, 7, 0, AllocationLineStatus.Held, 0, 2, "Alloc 2");

            AssertProviderAllocationLineCorrect(resultsBeingSaved, "prov2", specVariationDate, 14, 0, AllocationLineStatus.Held, 0, 2, "Alloc 2");

            AssertProviderAllocationLineCorrect(resultsBeingSaved, "prov3", specVariationDate, 0, 15, AllocationLineStatus.Held, 0, 1, "Alloc 2", new[] { "prov1u", "prov2u" });
        }

        [TestMethod]
        // Applies to provider variation scenario VAR04
        public async Task PublishProviderResultsWithVariations_WhenTwoSchoolsMergeToFormNewAcademy_ThenCreateNewResultForSuccessor()
        {
            // Arrange
            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            DateTimeOffset specVariationDate = DateTimeOffset.Parse("2018-04-30T23:59:59");
            ISpecificationsRepository specificationsRepository = InitialiseSpecificationRepository(specVariationDate);

            List<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult
                {
                    AllocationLineResults = new List<AllocationLineResult>
                    {
                        new AllocationLineResult { AllocationLine = new Reference { Id = "alloc2", Name = "Alloc 2" }, Value = 12 }
                    },
                    CalculationResults = new List<CalculationResult>
                    {
                        new CalculationResult
                        {
                            AllocationLine = new Reference { Id = "alloc2", Name = "Alloc 2" },
                            Calculation = new Reference { Id = "calc1", Name = "Calc 1" },
                            CalculationSpecification = new Reference { Id = "calcspec1", Name = "Calc 1" },
                            CalculationType = Models.Calcs.CalculationType.Funding,
                            Value = 12
                        }
                    },
                    Id = "provresult1",
                    Provider = new ProviderSummary { Id = "prov1" }
                },
                new ProviderResult
                {
                    AllocationLineResults = new List<AllocationLineResult>
                    {
                        new AllocationLineResult { AllocationLine = new Reference { Id = "alloc2", Name = "Alloc 2" }, Value = 24 }
                    },
                    CalculationResults = new List<CalculationResult>
                    {
                        new CalculationResult
                        {
                            AllocationLine = new Reference { Id = "alloc2", Name = "Alloc 2" },
                            Calculation = new Reference { Id = "calc1", Name = "Calc 1" },
                            CalculationSpecification = new Reference { Id = "calcspec1", Name = "Calc 1" },
                            CalculationType = Models.Calcs.CalculationType.Funding,
                            Value = 24
                        }
                    },
                    Id = "provresult2",
                    Provider = new ProviderSummary { Id = "prov2" }
                }
            };

            ICalculationResultsRepository calculationResultsRepository = InitialiseCalculationResultsRepository(providerResults);

            IPublishedProviderResultsAssemblerService providerResultsAssembler = CreateRealResultsAssembler(specificationsRepository);

            List<PublishedProviderResultExisting> existingResults = new List<PublishedProviderResultExisting>()
            {
                new PublishedProviderResultExisting
                {
                    AllocationLineId = "alloc2",
                    Major = 0,
                    Minor = 1,
                    ProviderId = "prov1",
                    Status = AllocationLineStatus.Held,
                    Value = 12,
                    Version = 1,
                    ProfilePeriods = new List<ProfilingPeriod>
                    {
                        new ProfilingPeriod { Period = "Apr", Type = "CalendarMonth", Value = 7, Year = 2018 },
                        new ProfilingPeriod { Period = "Jan", Type = "CalendarMonth", Value = 5, Year = 2019 }
                    },
                    ProviderLookups = new List<ProviderLookup>
                    {
                        new ProviderLookup { ProviderType = "Free schools", ProviderSubType = "Free schools special" }
                    }
                },
                new PublishedProviderResultExisting
                {
                    AllocationLineId = "alloc2",
                    Major = 0,
                    Minor = 1,
                    ProviderId = "prov2",
                    Status = AllocationLineStatus.Held,
                    Value = 24,
                    Version = 1,
                    ProfilePeriods = new List<ProfilingPeriod>
                    {
                        new ProfilingPeriod { Period = "Apr", Type = "CalendarMonth", Value = 14, Year = 2018 },
                        new ProfilingPeriod { Period = "Jan", Type = "CalendarMonth", Value = 10, Year = 2019 }
                    },
                    ProviderLookups = new List<ProviderLookup>
                    {
                        new ProviderLookup { ProviderType = "Free schools", ProviderSubType = "Free schools special" }
                    }
                }
            };
            IPublishedProviderResultsRepository publishedProviderResultsRepository = InitialisePublishedProviderResultsRepository(existingResults);

            IProviderVariationAssemblerService providerVariationAssembler = CreateProviderVariationAssemblerService();
            providerVariationAssembler
                .AssembleProviderVariationItems(Arg.Is(providerResults), Arg.Is(specificationId))
                .Returns(new List<ProviderChangeItem>
                {
                    new ProviderChangeItem
                    {
                        DoesProviderHaveSuccessor = true,
                        HasProviderClosed = true,
                        SuccessorProviderId = "prov3",
                        SuccessorProvider = new ProviderSummary
                        {
                            Id = "prov3",
                            UKPRN = "prov3u",
                            Status = "Open",
                            ProviderSubType = "Academy sponsor led",
                            ProviderType = "Academies"
                        },
                        UpdatedProvider = new ProviderSummary
                        {
                            Id = "prov1",
                            UKPRN = "prov1u",
                            Status = "Closed",
                            ProviderSubType = "Free schools special",
                            ProviderType = "Free Schools",
                            Successor = "prov3"
                        }
                    },
                    new ProviderChangeItem
                    {
                        DoesProviderHaveSuccessor = true,
                        HasProviderClosed = true,
                        SuccessorProviderId = "prov3",
                        SuccessorProvider = new ProviderSummary
                        {
                            Id = "prov3",
                            UKPRN = "prov3u",
                            Status = "Open",
                            ProviderSubType = "Academy sponsor led",
                            ProviderType = "Academies"
                        },
                        UpdatedProvider = new ProviderSummary
                        {
                            Id = "prov2",
                            UKPRN = "prov2u",
                            Status = "Closed",
                            ProviderSubType = "Free schools special",
                            ProviderType = "Free Schools",
                            Successor = "prov3"
                        }
                    }
                });

            PublishedResultsService service = InitialisePublishedResultsService(specificationsRepository, calculationResultsRepository, providerResultsAssembler, publishedProviderResultsRepository, providerVariationAssembler);

            // Setup saving results being saved - makes asserting easier
            IEnumerable<PublishedProviderResult> resultsBeingSaved = null;

            await publishedProviderResultsRepository
                .SavePublishedResults(Arg.Do<IEnumerable<PublishedProviderResult>>(r => resultsBeingSaved = r));

            // Act
            await service.PublishProviderResultsWithVariations(message);

            // Assert
            await publishedProviderResultsRepository
                .Received(1)
                .SavePublishedResults(Arg.Any<IEnumerable<PublishedProviderResult>>());

            // Should have results for both original and successor providers
            resultsBeingSaved.Should().HaveCount(3);

            AssertProviderAllocationLineCorrect(resultsBeingSaved, "prov1", specVariationDate, 7, 0, AllocationLineStatus.Held, 0, 2, "Alloc 2");

            AssertProviderAllocationLineCorrect(resultsBeingSaved, "prov2", specVariationDate, 14, 0, AllocationLineStatus.Held, 0, 2, "Alloc 2");

            AssertProviderAllocationLineCorrect(resultsBeingSaved, "prov3", specVariationDate, 0, 15, AllocationLineStatus.Held, 0, 1, "Alloc 1", new[] { "prov1u", "prov2u" });
        }

        [TestMethod]
        // Applies to provider variation scenario VAR21
        public async Task PublishProviderResultsWithVariations_WhenTwoSchoolsMergeToFormNewSchool_AndHaveNoExistingResult_ThenNoActionTaken()
        {
            // Arrange
            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            DateTimeOffset specVariationDate = DateTimeOffset.Parse("2018-04-30T23:59:59");
            ISpecificationsRepository specificationsRepository = InitialiseSpecificationRepository(specVariationDate);

            List<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult
                {
                    AllocationLineResults = new List<AllocationLineResult>
                    {
                        new AllocationLineResult { AllocationLine = new Reference { Id = "alloc2", Name = "Alloc 2" }, Value = 12 }
                    },
                    CalculationResults = new List<CalculationResult>
                    {
                        new CalculationResult
                        {
                            AllocationLine = new Reference { Id = "alloc2", Name = "Alloc 2" },
                            Calculation = new Reference { Id = "calc1", Name = "Calc 1" },
                            CalculationSpecification = new Reference { Id = "calcspec1", Name = "Calc 1" },
                            CalculationType = Models.Calcs.CalculationType.Funding,
                            Value = 12
                        }
                    },
                    Id = "provresult1",
                    Provider = new ProviderSummary { Id = "prov1" }
                },
                new ProviderResult
                {
                    AllocationLineResults = new List<AllocationLineResult>
                    {
                        new AllocationLineResult { AllocationLine = new Reference { Id = "alloc2", Name = "Alloc 2" }, Value = 24 }
                    },
                    CalculationResults = new List<CalculationResult>
                    {
                        new CalculationResult
                        {
                            AllocationLine = new Reference { Id = "alloc2", Name = "Alloc 2" },
                            Calculation = new Reference { Id = "calc1", Name = "Calc 1" },
                            CalculationSpecification = new Reference { Id = "calcspec1", Name = "Calc 1" },
                            CalculationType = Models.Calcs.CalculationType.Funding,
                            Value = 24
                        }
                    },
                    Id = "provresult2",
                    Provider = new ProviderSummary { Id = "prov2" }
                }
            };

            ICalculationResultsRepository calculationResultsRepository = InitialiseCalculationResultsRepository(providerResults);

            IPublishedProviderResultsAssemblerService providerResultsAssembler = CreateRealResultsAssembler(specificationsRepository);

            List<PublishedProviderResultExisting> existingResults = new List<PublishedProviderResultExisting>
            {
                new PublishedProviderResultExisting
                {
                    AllocationLineId = "alloc2",
                    Id = "prov4-result1",
                    ProviderId = "prov4",
                    Status = AllocationLineStatus.Held,
                    Value = 12
                }
            };

            IPublishedProviderResultsRepository publishedProviderResultsRepository = InitialisePublishedProviderResultsRepository(existingResults);

            IProviderVariationAssemblerService providerVariationAssembler = CreateProviderVariationAssemblerService();
            providerVariationAssembler
                .AssembleProviderVariationItems(Arg.Is(providerResults), Arg.Is(specificationId))
                .Returns(new List<ProviderChangeItem>
                {
                    new ProviderChangeItem
                    {
                        DoesProviderHaveSuccessor = true,
                        HasProviderClosed = true,
                        SuccessorProviderId = "prov3",
                        SuccessorProvider = new ProviderSummary
                        {
                            Id = "prov3",
                            UKPRN = "prov3u",
                            Status = "Open",
                            ProviderSubType = "Free schools special",
                            ProviderType = "Free Schools"
                        },
                        UpdatedProvider = new ProviderSummary
                        {
                            Id = "prov1",
                            UKPRN = "prov1u",
                            Status = "Closed",
                            ProviderSubType = "Free schools special",
                            ProviderType = "Free Schools",
                            Successor = "prov3"
                        }
                    },
                    new ProviderChangeItem
                    {
                        DoesProviderHaveSuccessor = true,
                        HasProviderClosed = true,
                        SuccessorProviderId = "prov3",
                        SuccessorProvider = new ProviderSummary
                        {
                            Id = "prov3",
                            UKPRN = "prov3u",
                            Status = "Open",
                            ProviderSubType = "Free schools special",
                            ProviderType = "Free Schools"
                        },
                        UpdatedProvider = new ProviderSummary
                        {
                            Id = "prov2",
                            UKPRN = "prov2u",
                            Status = "Closed",
                            ProviderSubType = "Free schools special",
                            ProviderType = "Free Schools",
                            Successor = "prov3"
                        }
                    }
                });

            ILogger logger = CreateLogger();

            PublishedResultsService service = InitialisePublishedResultsService(specificationsRepository, calculationResultsRepository, providerResultsAssembler, publishedProviderResultsRepository, providerVariationAssembler, logger);

            // Act
            await service.PublishProviderResultsWithVariations(message);

            // Assert
            logger
                .Received(1)
                .Information($"No existing result for provider prov1 and allocation line alloc1 to vary. Specification '{specificationId}'");

            logger
                .Received(1)
                .Information($"No existing result for provider prov1 and allocation line alloc2 to vary. Specification '{specificationId}'");

            logger
                .Received(1)
                .Information($"No existing result for provider prov2 and allocation line alloc1 to vary. Specification '{specificationId}'");

            logger
                .Received(1)
                .Information($"No existing result for provider prov1 and allocation line alloc2 to vary. Specification '{specificationId}'");
        }

        [TestMethod]
        // Applies to provider variation scenario VAR21
        public async Task PublishProviderResultsWithVariations_WhenTwoSchoolsMergeToFormNewSchool_AndAlreadyVaried_ThenNoActionTaken()
        {
            // Arrange
            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            DateTimeOffset specVariationDate = DateTimeOffset.Parse("2018-04-30T23:59:59");
            ISpecificationsRepository specificationsRepository = InitialiseSpecificationRepository(specVariationDate);

            List<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult
                {
                    AllocationLineResults = new List<AllocationLineResult>
                    {
                        new AllocationLineResult { AllocationLine = new Reference { Id = "alloc2", Name = "Alloc 2" }, Value = 12 }
                    },
                    CalculationResults = new List<CalculationResult>
                    {
                        new CalculationResult
                        {
                            AllocationLine = new Reference { Id = "alloc2", Name = "Alloc 2" },
                            Calculation = new Reference { Id = "calc1", Name = "Calc 1" },
                            CalculationSpecification = new Reference { Id = "calcspec1", Name = "Calc 1" },
                            CalculationType = Models.Calcs.CalculationType.Funding,
                            Value = 12
                        }
                    },
                    Id = "provresult1",
                    Provider = new ProviderSummary { Id = "prov1" }
                },
                new ProviderResult
                {
                    AllocationLineResults = new List<AllocationLineResult>
                    {
                        new AllocationLineResult { AllocationLine = new Reference { Id = "alloc2", Name = "Alloc 2" }, Value = 24 }
                    },
                    CalculationResults = new List<CalculationResult>
                    {
                        new CalculationResult
                        {
                            AllocationLine = new Reference { Id = "alloc2", Name = "Alloc 2" },
                            Calculation = new Reference { Id = "calc1", Name = "Calc 1" },
                            CalculationSpecification = new Reference { Id = "calcspec1", Name = "Calc 1" },
                            CalculationType = Models.Calcs.CalculationType.Funding,
                            Value = 24
                        }
                    },
                    Id = "provresult2",
                    Provider = new ProviderSummary { Id = "prov2" }
                }
            };

            ICalculationResultsRepository calculationResultsRepository = InitialiseCalculationResultsRepository(providerResults);

            IPublishedProviderResultsAssemblerService providerResultsAssembler = CreateRealResultsAssembler(specificationsRepository);

            List<PublishedProviderResultExisting> existingResults = new List<PublishedProviderResultExisting>()
            {
                new PublishedProviderResultExisting
                {
                    AllocationLineId = "alloc2",
                    Major = 0,
                    Minor = 1,
                    ProviderId = "prov1",
                    Status = AllocationLineStatus.Held,
                    Value = 12,
                    Version = 1,
                    ProfilePeriods = new List<ProfilingPeriod>
                    {
                        new ProfilingPeriod { Period = "Apr", Type = "CalendarMonth", Value = 7, Year = 2018 },
                        new ProfilingPeriod { Period = "Jan", Type = "CalendarMonth", Value = 5, Year = 2019 }
                    },
                    ProviderLookups = new List<ProviderLookup>
                    {
                        new ProviderLookup { ProviderType = "Free schools", ProviderSubType = "Free schools special" }
                    },
                    HasResultBeenVaried = true
                },
                new PublishedProviderResultExisting
                {
                    AllocationLineId = "alloc2",
                    Major = 0,
                    Minor = 1,
                    ProviderId = "prov2",
                    Status = AllocationLineStatus.Held,
                    Value = 24,
                    Version = 1,
                    ProfilePeriods = new List<ProfilingPeriod>
                    {
                        new ProfilingPeriod { Period = "Apr", Type = "CalendarMonth", Value = 14, Year = 2018 },
                        new ProfilingPeriod { Period = "Jan", Type = "CalendarMonth", Value = 10, Year = 2019 }
                    },
                    ProviderLookups = new List<ProviderLookup>
                    {
                        new ProviderLookup { ProviderType = "Free schools", ProviderSubType = "Free schools special" }
                    },
                    HasResultBeenVaried = true
                }
            };
            IPublishedProviderResultsRepository publishedProviderResultsRepository = InitialisePublishedProviderResultsRepository(existingResults);

            IProviderVariationAssemblerService providerVariationAssembler = CreateProviderVariationAssemblerService();
            providerVariationAssembler
                .AssembleProviderVariationItems(Arg.Is(providerResults), Arg.Is(specificationId))
                .Returns(new List<ProviderChangeItem>
                {
                    new ProviderChangeItem
                    {
                        DoesProviderHaveSuccessor = true,
                        HasProviderClosed = true,
                        SuccessorProviderId = "prov3",
                        SuccessorProvider = new ProviderSummary
                        {
                            Id = "prov3",
                            UKPRN = "prov3u",
                            Status = "Open",
                            ProviderSubType = "Free schools special",
                            ProviderType = "Free Schools"
                        },
                        UpdatedProvider = new ProviderSummary
                        {
                            Id = "prov1",
                            UKPRN = "prov1u",
                            Status = "Closed",
                            ProviderSubType = "Free schools special",
                            ProviderType = "Free Schools",
                            Successor = "prov3"
                        }
                    },
                    new ProviderChangeItem
                    {
                        DoesProviderHaveSuccessor = true,
                        HasProviderClosed = true,
                        SuccessorProviderId = "prov3",
                        SuccessorProvider = new ProviderSummary
                        {
                            Id = "prov3",
                            UKPRN = "prov3u",
                            Status = "Open",
                            ProviderSubType = "Free schools special",
                            ProviderType = "Free Schools"
                        },
                        UpdatedProvider = new ProviderSummary
                        {
                            Id = "prov2",
                            UKPRN = "prov2u",
                            Status = "Closed",
                            ProviderSubType = "Free schools special",
                            ProviderType = "Free Schools",
                            Successor = "prov3"
                        }
                    }
                });

            ILogger logger = CreateLogger();

            PublishedResultsService service = InitialisePublishedResultsService(specificationsRepository, calculationResultsRepository, providerResultsAssembler, publishedProviderResultsRepository, providerVariationAssembler, logger);

            // Act
            await service.PublishProviderResultsWithVariations(message);

            // Assert
            await publishedProviderResultsRepository
                .DidNotReceive()
                .SavePublishedResults(Arg.Any<IEnumerable<PublishedProviderResult>>());

            logger
                .Received(1)
                .Information($"Result for provider prov1 and allocation line alloc2 has already been varied. Specification '{specificationId}'");

            logger
                .Received(1)
                .Information($"Result for provider prov2 and allocation line alloc2 has already been varied. Specification '{specificationId}'");
        }

        [TestMethod]
        // Applies to provider variation scenario VAR21
        public async Task PublishProviderResultsWithVariations_WhenTwoSchoolsMergeToFormNewSchool_AndNoAffectedPeriods_ThenNoActionTaken()
        {
            // Arrange
            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            DateTimeOffset specVariationDate = DateTimeOffset.Parse("2019-02-28T23:59:59");
            ISpecificationsRepository specificationsRepository = InitialiseSpecificationRepository(specVariationDate);

            List<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult
                {
                    AllocationLineResults = new List<AllocationLineResult>
                    {
                        new AllocationLineResult { AllocationLine = new Reference { Id = "alloc2", Name = "Alloc 2" }, Value = 12 }
                    },
                    CalculationResults = new List<CalculationResult>
                    {
                        new CalculationResult
                        {
                            AllocationLine = new Reference { Id = "alloc2", Name = "Alloc 2" },
                            Calculation = new Reference { Id = "calc1", Name = "Calc 1" },
                            CalculationSpecification = new Reference { Id = "calcspec1", Name = "Calc 1" },
                            CalculationType = Models.Calcs.CalculationType.Funding,
                            Value = 12
                        }
                    },
                    Id = "provresult1",
                    Provider = new ProviderSummary { Id = "prov1" }
                },
                new ProviderResult
                {
                    AllocationLineResults = new List<AllocationLineResult>
                    {
                        new AllocationLineResult { AllocationLine = new Reference { Id = "alloc2", Name = "Alloc 2" }, Value = 24 }
                    },
                    CalculationResults = new List<CalculationResult>
                    {
                        new CalculationResult
                        {
                            AllocationLine = new Reference { Id = "alloc2", Name = "Alloc 2" },
                            Calculation = new Reference { Id = "calc1", Name = "Calc 1" },
                            CalculationSpecification = new Reference { Id = "calcspec1", Name = "Calc 1" },
                            CalculationType = Models.Calcs.CalculationType.Funding,
                            Value = 24
                        }
                    },
                    Id = "provresult2",
                    Provider = new ProviderSummary { Id = "prov2" }
                }
            };

            ICalculationResultsRepository calculationResultsRepository = InitialiseCalculationResultsRepository(providerResults);

            IPublishedProviderResultsAssemblerService providerResultsAssembler = CreateRealResultsAssembler(specificationsRepository);

            List<PublishedProviderResultExisting> existingResults = new List<PublishedProviderResultExisting>()
            {
                new PublishedProviderResultExisting
                {
                    AllocationLineId = "alloc2",
                    Major = 0,
                    Minor = 1,
                    ProviderId = "prov1",
                    Status = AllocationLineStatus.Held,
                    Value = 12,
                    Version = 1,
                    ProfilePeriods = new List<ProfilingPeriod>
                    {
                        new ProfilingPeriod { Period = "Apr", Type = "CalendarMonth", Value = 7, Year = 2018 },
                        new ProfilingPeriod { Period = "Jan", Type = "CalendarMonth", Value = 5, Year = 2019 }
                    },
                    ProviderLookups = new List<ProviderLookup>
                    {
                        new ProviderLookup { ProviderType = "Free schools", ProviderSubType = "Free schools special" }
                    }
                },
                new PublishedProviderResultExisting
                {
                    AllocationLineId = "alloc2",
                    Major = 0,
                    Minor = 1,
                    ProviderId = "prov2",
                    Status = AllocationLineStatus.Held,
                    Value = 24,
                    Version = 1,
                    ProfilePeriods = new List<ProfilingPeriod>
                    {
                        new ProfilingPeriod { Period = "Apr", Type = "CalendarMonth", Value = 14, Year = 2018 },
                        new ProfilingPeriod { Period = "Jan", Type = "CalendarMonth", Value = 10, Year = 2019 }
                    },
                    ProviderLookups = new List<ProviderLookup>
                    {
                        new ProviderLookup { ProviderType = "Free schools", ProviderSubType = "Free schools special" }
                    }
                }
            };
            IPublishedProviderResultsRepository publishedProviderResultsRepository = InitialisePublishedProviderResultsRepository(existingResults);

            IProviderVariationAssemblerService providerVariationAssembler = CreateProviderVariationAssemblerService();
            providerVariationAssembler
                .AssembleProviderVariationItems(Arg.Is(providerResults), Arg.Is(specificationId))
                .Returns(new List<ProviderChangeItem>
                {
                    new ProviderChangeItem
                    {
                        DoesProviderHaveSuccessor = true,
                        HasProviderClosed = true,
                        SuccessorProviderId = "prov3",
                        SuccessorProvider = new ProviderSummary
                        {
                            Id = "prov3",
                            UKPRN = "prov3u",
                            Status = "Open",
                            ProviderSubType = "Free schools special",
                            ProviderType = "Free Schools"
                        },
                        UpdatedProvider = new ProviderSummary
                        {
                            Id = "prov1",
                            UKPRN = "prov1u",
                            Status = "Closed",
                            ProviderSubType = "Free schools special",
                            ProviderType = "Free Schools",
                            Successor = "prov3"
                        }
                    },
                    new ProviderChangeItem
                    {
                        DoesProviderHaveSuccessor = true,
                        HasProviderClosed = true,
                        SuccessorProviderId = "prov3",
                        SuccessorProvider = new ProviderSummary
                        {
                            Id = "prov3",
                            UKPRN = "prov3u",
                            Status = "Open",
                            ProviderSubType = "Free schools special",
                            ProviderType = "Free Schools"
                        },
                        UpdatedProvider = new ProviderSummary
                        {
                            Id = "prov2",
                            UKPRN = "prov2u",
                            Status = "Closed",
                            ProviderSubType = "Free schools special",
                            ProviderType = "Free Schools",
                            Successor = "prov3"
                        }
                    }
                });

            ILogger logger = CreateLogger();

            PublishedResultsService service = InitialisePublishedResultsService(specificationsRepository, calculationResultsRepository, providerResultsAssembler, publishedProviderResultsRepository, providerVariationAssembler, logger);

            // Act
            await service.PublishProviderResultsWithVariations(message);

            // Assert
            await publishedProviderResultsRepository
                .DidNotReceive()
                .SavePublishedResults(Arg.Any<IEnumerable<PublishedProviderResult>>());

            logger
                .Received(1)
                .Information($"There are no affected profiling periods for the allocation line result alloc2 and provider prov1");

            logger
                .Received(1)
                .Information($"There are no affected profiling periods for the allocation line result alloc2 and provider prov2");
        }

        [TestMethod]
        // Applies to provider variation scenario VAR19
        public async Task PublishProviderResultsWithVariations_WhenSchoolClosesAndReOpensAsANewSchool_ThenCreateNewResultForNewSchool()
        {
            // Arrange
            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            DateTimeOffset specVariationDate = DateTimeOffset.Parse("2018-04-30T23:59:59");
            ISpecificationsRepository specificationsRepository = InitialiseSpecificationRepository(specVariationDate);

            List<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult
                {
                    AllocationLineResults = new List<AllocationLineResult>
                    {
                        new AllocationLineResult { AllocationLine = new Reference { Id = "alloc2", Name = "Alloc 2" }, Value = 12 }
                    },
                    CalculationResults = new List<CalculationResult>
                    {
                        new CalculationResult
                        {
                            AllocationLine = new Reference { Id = "alloc2", Name = "Alloc 2" },
                            Calculation = new Reference { Id = "calc1", Name = "Calc 1" },
                            CalculationSpecification = new Reference { Id = "calcspec1", Name = "Calc 1" },
                            CalculationType = Models.Calcs.CalculationType.Funding,
                            Value = 12
                        }
                    },
                    Id = "provresult1",
                    Provider = new ProviderSummary { Id = "prov1" }
                }
            };

            ICalculationResultsRepository calculationResultsRepository = InitialiseCalculationResultsRepository(providerResults);

            IPublishedProviderResultsAssemblerService providerResultsAssembler = CreateRealResultsAssembler(specificationsRepository);

            List<PublishedProviderResultExisting> existingResults = new List<PublishedProviderResultExisting>()
            {
                new PublishedProviderResultExisting
                {
                    AllocationLineId = "alloc2",
                    Major = 0,
                    Minor = 1,
                    ProviderId = "prov1",
                    Status = AllocationLineStatus.Held,
                    Value = 12,
                    Version = 1,
                    ProfilePeriods = new List<ProfilingPeriod>
                    {
                        new ProfilingPeriod { Period = "Apr", Type = "CalendarMonth", Value = 7, Year = 2018 },
                        new ProfilingPeriod { Period = "Jan", Type = "CalendarMonth", Value = 5, Year = 2019 }
                    },
                    ProviderLookups = new List<ProviderLookup>
                    {
                        new ProviderLookup { ProviderType = "Free schools", ProviderSubType = "Free schools special" }
                    }
                }
            };
            IPublishedProviderResultsRepository publishedProviderResultsRepository = InitialisePublishedProviderResultsRepository(existingResults);

            IProviderVariationAssemblerService providerVariationAssembler = CreateProviderVariationAssemblerService();
            providerVariationAssembler
                .AssembleProviderVariationItems(Arg.Is(providerResults), Arg.Is(specificationId))
                .Returns(new List<ProviderChangeItem>
                {
                    new ProviderChangeItem
                    {
                        DoesProviderHaveSuccessor = true,
                        HasProviderClosed = true,
                        SuccessorProviderId = "prov3",
                        SuccessorProvider = new ProviderSummary
                        {
                            Id = "prov3",
                            UKPRN = "prov3u",
                            Status = "Open",
                            ProviderSubType = "Free schools special",
                            ProviderType = "Free Schools"
                        },
                        UpdatedProvider = new ProviderSummary
                        {
                            Id = "prov1",
                            UKPRN = "prov1u",
                            Status = "Closed",
                            ProviderSubType = "Free schools special",
                            ProviderType = "Free Schools",
                            Successor = "prov3"
                        }
                    }
                });

            PublishedResultsService service = InitialisePublishedResultsService(specificationsRepository, calculationResultsRepository, providerResultsAssembler, publishedProviderResultsRepository, providerVariationAssembler);

            // Setup saving results being saved - makes asserting easier
            IEnumerable<PublishedProviderResult> resultsBeingSaved = null;

            await publishedProviderResultsRepository
                .SavePublishedResults(Arg.Do<IEnumerable<PublishedProviderResult>>(r => resultsBeingSaved = r));

            // Act
            await service.PublishProviderResultsWithVariations(message);

            // Assert
            await publishedProviderResultsRepository
                .Received(1)
                .SavePublishedResults(Arg.Any<IEnumerable<PublishedProviderResult>>());

            // Should have results for both original and successor providers
            resultsBeingSaved.Should().HaveCount(2);

            AssertProviderAllocationLineCorrect(resultsBeingSaved, "prov1", specVariationDate, 7, 0, AllocationLineStatus.Held, 0, 2, "Alloc 2");

            AssertProviderAllocationLineCorrect(resultsBeingSaved, "prov3", specVariationDate, 0, 5, AllocationLineStatus.Held, 0, 1, "Alloc 2");
        }

        [TestMethod]
        // Applies to provider variation scenario VAR19
        public async Task PublishProviderResultsWithVariations_WhenSchoolClosesAndReOpensAsAcademy_ThenCreateNewResultForAcademy()
        {
            // Arrange
            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            DateTimeOffset specVariationDate = DateTimeOffset.Parse("2018-04-30T23:59:59");
            ISpecificationsRepository specificationsRepository = InitialiseSpecificationRepository(specVariationDate);

            List<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult
                {
                    AllocationLineResults = new List<AllocationLineResult>
                    {
                        new AllocationLineResult { AllocationLine = new Reference { Id = "alloc2", Name = "Alloc 2" }, Value = 12 }
                    },
                    CalculationResults = new List<CalculationResult>
                    {
                        new CalculationResult
                        {
                            AllocationLine = new Reference { Id = "alloc2", Name = "Alloc 2" },
                            Calculation = new Reference { Id = "calc1", Name = "Calc 1" },
                            CalculationSpecification = new Reference { Id = "calcspec1", Name = "Calc 1" },
                            CalculationType = Models.Calcs.CalculationType.Funding,
                            Value = 12
                        }
                    },
                    Id = "provresult1",
                    Provider = new ProviderSummary { Id = "prov1" }
                }
            };

            ICalculationResultsRepository calculationResultsRepository = InitialiseCalculationResultsRepository(providerResults);

            IPublishedProviderResultsAssemblerService providerResultsAssembler = CreateRealResultsAssembler(specificationsRepository);

            List<PublishedProviderResultExisting> existingResults = new List<PublishedProviderResultExisting>()
            {
                new PublishedProviderResultExisting
                {
                    AllocationLineId = "alloc2",
                    Major = 0,
                    Minor = 1,
                    ProviderId = "prov1",
                    Status = AllocationLineStatus.Held,
                    Value = 12,
                    Version = 1,
                    ProfilePeriods = new List<ProfilingPeriod>
                    {
                        new ProfilingPeriod { Period = "Apr", Type = "CalendarMonth", Value = 7, Year = 2018 },
                        new ProfilingPeriod { Period = "Jan", Type = "CalendarMonth", Value = 5, Year = 2019 }
                    },
                    ProviderLookups = new List<ProviderLookup>
                    {
                        new ProviderLookup { ProviderType = "Free schools", ProviderSubType = "Free schools special" }
                    }
                }
            };
            IPublishedProviderResultsRepository publishedProviderResultsRepository = InitialisePublishedProviderResultsRepository(existingResults);

            IProviderVariationAssemblerService providerVariationAssembler = CreateProviderVariationAssemblerService();
            providerVariationAssembler
                .AssembleProviderVariationItems(Arg.Is(providerResults), Arg.Is(specificationId))
                .Returns(new List<ProviderChangeItem>
                {
                    new ProviderChangeItem
                    {
                        DoesProviderHaveSuccessor = true,
                        HasProviderClosed = true,
                        SuccessorProviderId = "prov3",
                        SuccessorProvider = new ProviderSummary
                        {
                            Id = "prov3",
                            UKPRN = "prov3u",
                            Status = "Open",
                            ProviderSubType = "Academy sponsor led",
                            ProviderType = "Academies"
                        },
                        UpdatedProvider = new ProviderSummary
                        {
                            Id = "prov1",
                            UKPRN = "prov1u",
                            Status = "Closed",
                            ProviderSubType = "Free schools special",
                            ProviderType = "Free Schools",
                            Successor = "prov3"
                        }
                    }
                });

            PublishedResultsService service = InitialisePublishedResultsService(specificationsRepository, calculationResultsRepository, providerResultsAssembler, publishedProviderResultsRepository, providerVariationAssembler);

            // Setup saving results being saved - makes asserting easier
            IEnumerable<PublishedProviderResult> resultsBeingSaved = null;

            await publishedProviderResultsRepository
                .SavePublishedResults(Arg.Do<IEnumerable<PublishedProviderResult>>(r => resultsBeingSaved = r));

            // Act
            await service.PublishProviderResultsWithVariations(message);

            // Assert
            await publishedProviderResultsRepository
                .Received(1)
                .SavePublishedResults(Arg.Any<IEnumerable<PublishedProviderResult>>());

            // Should have results for both original and successor providers
            resultsBeingSaved.Should().HaveCount(2);

            AssertProviderAllocationLineCorrect(resultsBeingSaved, "prov1", specVariationDate, 7, 0, AllocationLineStatus.Held, 0, 2, "Alloc 2");

            AssertProviderAllocationLineCorrect(resultsBeingSaved, "prov3", specVariationDate, 0, 5, AllocationLineStatus.Held, 0, 1, "Alloc 1");
        }

        [TestMethod]
        // Applies to provider variation scenario VAR19
        public async Task PublishProviderResultsWithVariations_WhenSchoolClosesAndReOpens_AndOriginalStatusIsPublished_ThenNewResultIsUpdated()
        {
            // Arrange
            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            DateTimeOffset specVariationDate = DateTimeOffset.Parse("2018-04-30T23:59:59");
            ISpecificationsRepository specificationsRepository = InitialiseSpecificationRepository(specVariationDate);

            List<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult
                {
                    AllocationLineResults = new List<AllocationLineResult>
                    {
                        new AllocationLineResult { AllocationLine = new Reference { Id = "alloc2", Name = "Alloc 2" }, Value = 12 }
                    },
                    CalculationResults = new List<CalculationResult>
                    {
                        new CalculationResult
                        {
                            AllocationLine = new Reference { Id = "alloc2", Name = "Alloc 2" },
                            Calculation = new Reference { Id = "calc1", Name = "Calc 1" },
                            CalculationSpecification = new Reference { Id = "calcspec1", Name = "Calc 1" },
                            CalculationType = Models.Calcs.CalculationType.Funding,
                            Value = 12
                        }
                    },
                    Id = "provresult1",
                    Provider = new ProviderSummary { Id = "prov1" }
                }
            };

            ICalculationResultsRepository calculationResultsRepository = InitialiseCalculationResultsRepository(providerResults);

            IPublishedProviderResultsAssemblerService providerResultsAssembler = CreateRealResultsAssembler(specificationsRepository);

            List<PublishedProviderResultExisting> existingResults = new List<PublishedProviderResultExisting>()
            {
                new PublishedProviderResultExisting
                {
                    AllocationLineId = "alloc2",
                    Major = 1,
                    Minor = 0,
                    ProviderId = "prov1",
                    Status = AllocationLineStatus.Published,
                    Value = 12,
                    Version = 1,
                    ProfilePeriods = new List<ProfilingPeriod>
                    {
                        new ProfilingPeriod { Period = "Apr", Type = "CalendarMonth", Value = 7, Year = 2018 },
                        new ProfilingPeriod { Period = "Jan", Type = "CalendarMonth", Value = 5, Year = 2019 }
                    },
                    ProviderLookups = new List<ProviderLookup>
                    {
                        new ProviderLookup { ProviderType = "Free schools", ProviderSubType = "Free schools special" }
                    }
                }
            };
            IPublishedProviderResultsRepository publishedProviderResultsRepository = InitialisePublishedProviderResultsRepository(existingResults);

            IProviderVariationAssemblerService providerVariationAssembler = CreateProviderVariationAssemblerService();
            providerVariationAssembler
                .AssembleProviderVariationItems(Arg.Is(providerResults), Arg.Is(specificationId))
                .Returns(new List<ProviderChangeItem>
                {
                    new ProviderChangeItem
                    {
                        DoesProviderHaveSuccessor = true,
                        HasProviderClosed = true,
                        SuccessorProviderId = "prov3",
                        SuccessorProvider = new ProviderSummary
                        {
                            Id = "prov3",
                            UKPRN = "prov3u",
                            Status = "Open",
                            ProviderSubType = "Academy sponsor led",
                            ProviderType = "Academies"
                        },
                        UpdatedProvider = new ProviderSummary
                        {
                            Id = "prov1",
                            UKPRN = "prov1u",
                            Status = "Closed",
                            ProviderSubType = "Free schools special",
                            ProviderType = "Free Schools",
                            Successor = "prov3"
                        }
                    }
                });

            PublishedResultsService service = InitialisePublishedResultsService(specificationsRepository, calculationResultsRepository, providerResultsAssembler, publishedProviderResultsRepository, providerVariationAssembler);

            // Setup saving results being saved - makes asserting easier
            IEnumerable<PublishedProviderResult> resultsBeingSaved = null;

            await publishedProviderResultsRepository
                .SavePublishedResults(Arg.Do<IEnumerable<PublishedProviderResult>>(r => resultsBeingSaved = r));

            // Act
            await service.PublishProviderResultsWithVariations(message);

            // Assert
            await publishedProviderResultsRepository
                .Received(1)
                .SavePublishedResults(Arg.Any<IEnumerable<PublishedProviderResult>>());

            // Should have results for both original and successor providers
            resultsBeingSaved.Should().HaveCount(2);

            AssertProviderAllocationLineCorrect(resultsBeingSaved, "prov1", specVariationDate, 7, 0, AllocationLineStatus.Updated, 1, 1, "Alloc 2");

            AssertProviderAllocationLineCorrect(resultsBeingSaved, "prov3", specVariationDate, 0, 5, AllocationLineStatus.Held, 0, 1, "Alloc 1");
        }

        [TestMethod]
        // Applies to provider variation scenario VAR03
        public async Task PublishProviderResultsWithVariations_WhenProviderClosedWithSuccessor_AndOriginalStatusIsPublished_ThenNewResultIsUpdated()
        {
            // Arrange
            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            DateTimeOffset specVariationDate = DateTimeOffset.Parse("2018-04-30T23:59:59");
            ISpecificationsRepository specificationsRepository = InitialiseSpecificationRepository(specVariationDate);

            List<ProviderResult> providerResults = CreateProviderResults(12, 24);

            ICalculationResultsRepository calculationResultsRepository = InitialiseCalculationResultsRepository(providerResults);

            IPublishedProviderResultsAssemblerService providerResultsAssembler = CreateRealResultsAssembler(specificationsRepository);

            IPublishedProviderResultsRepository publishedProviderResultsRepository = InitialisePublishedProviderResultsRepositoryWithExistingResults(AllocationLineStatus.Published);

            IProviderVariationAssemblerService providerVariationAssembler = CreateProviderVariationAssemblerService();
            providerVariationAssembler
                .AssembleProviderVariationItems(Arg.Is(providerResults), Arg.Is(specificationId))
                .Returns(new List<ProviderChangeItem>
                {
                    new ProviderChangeItem
                    {
                        DoesProviderHaveSuccessor = true,
                        HasProviderClosed = true,
                        SuccessorProviderId = "prov2",
                        UpdatedProvider = new ProviderSummary
                        {
                            Id = "prov1",
                            UKPRN = "prov1u",
                            Status = "Closed",
                            ProviderSubType = "Academy sponsor led",
                            ProviderType = "Academies",
                            Successor = "prov2"
                        }
                    }
                });

            PublishedResultsService service = InitialisePublishedResultsService(calculationResultsRepository: calculationResultsRepository,
                specificationsRepository: specificationsRepository, providerResultsAssembler: providerResultsAssembler,
                publishedProviderResultsRepository: publishedProviderResultsRepository, providerVariationAssembler: providerVariationAssembler);

            // Setup saving results being saved - makes asserting easier
            IEnumerable<PublishedProviderResult> resultsBeingSaved = null;

            await publishedProviderResultsRepository
                .SavePublishedResults(Arg.Do<IEnumerable<PublishedProviderResult>>(r => resultsBeingSaved = r));

            // Act
            await service.PublishProviderResultsWithVariations(message);

            // Assert
            await publishedProviderResultsRepository
                .Received(1)
                .SavePublishedResults(Arg.Any<IEnumerable<PublishedProviderResult>>());

            // Should have results for both original and successor providers
            resultsBeingSaved.Should().HaveCount(2);

            AssertProviderAllocationLineCorrect(resultsBeingSaved, "prov1", specVariationDate, 7, 0, AllocationLineStatus.Updated, 1, 1, "Alloc 1");

            AssertProviderAllocationLineCorrect(resultsBeingSaved, "prov2", specVariationDate, 14, 15, AllocationLineStatus.Updated, 1, 1, "Alloc 1", new[] { "prov1u" });
        }

        [TestMethod]
        // Applies to provider variation scenario VAR19
        public async Task PublishProviderResultsWithVariations_WhenSchoolClosesAndReOpensAsAcademy_AndAlreadyVaried_ThenNoActionTaken()
        {
            // Arrange
            Message message = new Message();
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            DateTimeOffset specVariationDate = DateTimeOffset.Parse("2018-04-30T23:59:59");
            ISpecificationsRepository specificationsRepository = InitialiseSpecificationRepository(specVariationDate);

            List<ProviderResult> providerResults = new List<ProviderResult>
            {
                new ProviderResult
                {
                    AllocationLineResults = new List<AllocationLineResult>
                    {
                        new AllocationLineResult { AllocationLine = new Reference { Id = "alloc2", Name = "Alloc 2" }, Value = 12 }
                    },
                    CalculationResults = new List<CalculationResult>
                    {
                        new CalculationResult
                        {
                            AllocationLine = new Reference { Id = "alloc2", Name = "Alloc 2" },
                            Calculation = new Reference { Id = "calc1", Name = "Calc 1" },
                            CalculationSpecification = new Reference { Id = "calcspec1", Name = "Calc 1" },
                            CalculationType = Models.Calcs.CalculationType.Funding,
                            Value = 12
                        }
                    },
                    Id = "provresult1",
                    Provider = new ProviderSummary { Id = "prov1" }
                }
            };

            ICalculationResultsRepository calculationResultsRepository = InitialiseCalculationResultsRepository(providerResults);

            IPublishedProviderResultsAssemblerService providerResultsAssembler = CreateRealResultsAssembler(specificationsRepository);

            List<PublishedProviderResultExisting> existingResults = new List<PublishedProviderResultExisting>()
            {
                new PublishedProviderResultExisting
                {
                    AllocationLineId = "alloc2",
                    Major = 1,
                    Minor = 0,
                    ProviderId = "prov1",
                    Status = AllocationLineStatus.Published,
                    Value = 7,
                    Version = 3,
                    ProfilePeriods = new List<ProfilingPeriod>
                    {
                        new ProfilingPeriod { Period = "Apr", Type = "CalendarMonth", Value = 7, Year = 2018 },
                        new ProfilingPeriod { Period = "Jan", Type = "CalendarMonth", Value = 0, Year = 2019 }
                    },
                    ProviderLookups = new List<ProviderLookup>
                    {
                        new ProviderLookup { ProviderType = "Free schools", ProviderSubType = "Free schools special" }
                    },
                    HasResultBeenVaried = true
                },
                new PublishedProviderResultExisting
                {
                    AllocationLineId = "alloc1",
                    Major = 0,
                    Minor = 2,
                    ProviderId = "prov3",
                    Status = AllocationLineStatus.Approved,
                    Value = 5,
                    Version = 2,
                    ProfilePeriods = new List<ProfilingPeriod>
                    {
                        new ProfilingPeriod { Period = "Apr", Type = "CalendarMonth", Value = 0, Year = 2018 },
                        new ProfilingPeriod { Period = "Jan", Type = "CalendarMonth", Value = 5, Year = 2019 }
                    },
                    ProviderLookups = new List<ProviderLookup>
                    {
                        new ProviderLookup { ProviderType = "Academies", ProviderSubType = "Academy sponsor led" }
                    },
                    HasResultBeenVaried = true
                }
            };

            IPublishedProviderResultsRepository publishedProviderResultsRepository = InitialisePublishedProviderResultsRepository(existingResults);

            IProviderVariationAssemblerService providerVariationAssembler = CreateProviderVariationAssemblerService();
            providerVariationAssembler
                .AssembleProviderVariationItems(Arg.Is(providerResults), Arg.Is(specificationId))
                .Returns(new List<ProviderChangeItem>
                {
                    new ProviderChangeItem
                    {
                        DoesProviderHaveSuccessor = true,
                        HasProviderClosed = true,
                        SuccessorProviderId = "prov3",
                        SuccessorProvider = new ProviderSummary
                        {
                            Id = "prov3",
                            UKPRN = "prov3u",
                            Status = "Open",
                            ProviderSubType = "Academy sponsor led",
                            ProviderType = "Academies"
                        },
                        UpdatedProvider = new ProviderSummary
                        {
                            Id = "prov1",
                            UKPRN = "prov1u",
                            Status = "Closed",
                            ProviderSubType = "Free schools special",
                            ProviderType = "Free Schools",
                            Successor = "prov3"
                        }
                    }
                });

            ILogger logger = CreateLogger();

            PublishedResultsService service = InitialisePublishedResultsService(specificationsRepository, calculationResultsRepository, providerResultsAssembler, publishedProviderResultsRepository, providerVariationAssembler, logger);

            // Act
            await service.PublishProviderResultsWithVariations(message);

            // Assert
            await publishedProviderResultsRepository
                            .DidNotReceive()
                            .SavePublishedResults(Arg.Any<IEnumerable<PublishedProviderResult>>());

            logger
                .Received(1)
                .Information($"Result for provider prov1 and allocation line alloc2 has already been varied. Specification '{specificationId}'");
        }

        private static void AssertProviderAllocationLineCorrect(
            IEnumerable<PublishedProviderResult> resultsToSave,
            string providerId,
            DateTimeOffset specVariationDate,
            decimal expectedValueBeforeVariation,
            decimal expectedValueAfterVariation,
            AllocationLineStatus expectedStatus,
            int expectedMajorVersion,
            int expectedMinorVersion,
            string expectedAllocationLineName,
            string[] expectedPredecessors = null)
        {
            PublishedProviderResult provResult = resultsToSave.FirstOrDefault(r => r.ProviderId == providerId);

            provResult.FundingStreamResult.AllocationLineResult.HasResultBeenVaried
                .Should()
                .BeTrue("The result should have been varied");

            // The profile periods before the variation date should match the expected value
            provResult.FundingStreamResult.AllocationLineResult.Current.ProfilingPeriods.Where(p => p.PeriodDate < specVariationDate)
                .Should()
                .Contain(p => p.Value == expectedValueBeforeVariation, because: $"Profile Period before variation should be {expectedValueBeforeVariation}");

            // The profile periods after the variation date should match the expected value
            provResult.FundingStreamResult.AllocationLineResult.Current.ProfilingPeriods.Where(p => p.PeriodDate > specVariationDate)
                .Should()
                .Contain(p => p.Value == expectedValueAfterVariation, because: $"Profile Period after variation should be {expectedValueAfterVariation}");

            // The total of the allocation should match the total of the profile periods
            provResult.FundingStreamResult.AllocationLineResult.Current.Value
                .Should()
                .Be(provResult.FundingStreamResult.AllocationLineResult.Current.ProfilingPeriods.Sum(p => p.Value), because: $"Allocation value must match sum of profile periods");

            provResult.FundingStreamResult.AllocationLineResult.Current.Status
                .Should()
                .Be(expectedStatus, "Allocation Status");

            provResult.FundingStreamResult.AllocationLineResult.Current.Major
                .Should()
                .Be(expectedMajorVersion, "Major version");

            provResult.FundingStreamResult.AllocationLineResult.Current.Minor
                .Should()
                .Be(expectedMinorVersion, "Minor version");

            provResult.FundingStreamResult.AllocationLineResult.AllocationLine.Name
                .Should()
                .Be(expectedAllocationLineName, "Allocation Line Name");

            if (expectedPredecessors.AnyWithNullCheck())
            {
                foreach (string predecessor in expectedPredecessors)
                {
                    provResult.FundingStreamResult.AllocationLineResult.Current.Predecessors.Should().Contain(predecessor);
                }
            }
        }

        private PublishedResultsService InitialisePublishedResultsService(
            ISpecificationsRepository specificationsRepository,
            ICalculationResultsRepository calculationResultsRepository,
            IPublishedProviderResultsAssemblerService providerResultsAssembler,
            IPublishedProviderResultsRepository publishedProviderResultsRepository,
            IProviderVariationAssemblerService providerVariationAssembler,
            ILogger logger = null,
            ICacheProvider cacheProvider = null)
        {
            IFeatureToggle featureToggle = InitialiseFeatureToggle();
            IJobsApiClient jobsApiClient = InitialiseJobsApiClient();
            IPublishedAllocationLineLogicalResultVersionService versionService = CreateRealPublishedAllocationLineLogicalResultVersionService();
            IProviderVariationsService providerVariationsService = CreateProviderVariationsService(providerVariationAssembler, specificationsRepository, logger);

            return CreateResultsService(featureToggle: featureToggle, jobsApiClient: jobsApiClient, resultsRepository: calculationResultsRepository,
                specificationsRepository: specificationsRepository, publishedProviderResultsAssemblerService: providerResultsAssembler,
                publishedProviderResultsRepository: publishedProviderResultsRepository, providerVariationsService: providerVariationsService,
                publishedAllocationLineLogicalResultVersionService: versionService, logger: logger ?? CreateLogger(),
                cacheProvider: cacheProvider ?? CreateCacheProvider());
        }

        private static IPublishedProviderResultsRepository InitialisePublishedProviderResultsRepositoryWithExistingResults(AllocationLineStatus status = AllocationLineStatus.Held)
        {
            List<PublishedProviderResultExisting> existingResults = new List<PublishedProviderResultExisting>()
            {
                new PublishedProviderResultExisting
                {
                    AllocationLineId = "alloc1",
                    Major = status == AllocationLineStatus.Held ? 0 : 1,
                    Minor = status == AllocationLineStatus.Held ? 1 : 0,
                    ProviderId = "prov1",
                    Status = status,
                    Value = 12,
                    Version = 1,
                    ProfilePeriods = new List<ProfilingPeriod>
                    {
                        new ProfilingPeriod { Period = "Apr", Type = "CalendarMonth", Value = 7, Year = 2018 },
                        new ProfilingPeriod { Period = "Jan", Type = "CalendarMonth", Value = 5, Year = 2019 }
                    },
                    ProviderLookups = new List<ProviderLookup>
                    {
                        new ProviderLookup { ProviderType = "Academies", ProviderSubType = "Academy sponsor led" }
                    }
                },
                new PublishedProviderResultExisting
                {
                    AllocationLineId = "alloc1",
                    Major = status == AllocationLineStatus.Held ? 0 : 1,
                    Minor = status == AllocationLineStatus.Held ? 1 : 0,
                    ProviderId = "prov2",
                    Status = status,
                    Value = 24,
                    Version = 1,
                    ProfilePeriods = new List<ProfilingPeriod>
                    {
                        new ProfilingPeriod { Period = "Apr", Type = "CalendarMonth", Value = 14, Year = 2018 },
                        new ProfilingPeriod { Period = "Jan", Type = "CalendarMonth", Value = 10, Year = 2019 }
                    },
                    ProviderLookups = new List<ProviderLookup>
                    {
                        new ProviderLookup { ProviderType = "Academies", ProviderSubType = "Academy sponsor led" }
                    }
                }
            };
            IPublishedProviderResultsRepository publishedProviderResultsRepository = InitialisePublishedProviderResultsRepository(existingResults);
            return publishedProviderResultsRepository;
        }

        private static IPublishedProviderResultsRepository InitialisePublishedProviderResultsRepository(List<PublishedProviderResultExisting> existingResults)
        {
            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();
            publishedProviderResultsRepository
                .GetExistingPublishedProviderResultsForSpecificationId(Arg.Is(specificationId))
                .Returns(existingResults);
            return publishedProviderResultsRepository;
        }

        private static ICalculationResultsRepository InitialiseCalculationResultsRepository(List<ProviderResult> providerResults)
        {
            ICalculationResultsRepository calculationResultsRepository = CreateResultsRepository();
            calculationResultsRepository
                .GetProviderResultsBySpecificationId(Arg.Is(specificationId), Arg.Any<int>())
                .Returns(providerResults);
            return calculationResultsRepository;
        }

        private static List<ProviderResult> CreateProviderResults(params decimal[] providerResults)
        {
            List<ProviderResult> results = new List<ProviderResult>();

            for (int i = 0; i < providerResults.Length; i++)
            {
                decimal value = providerResults[i];

                results.Add(
                new ProviderResult
                {
                    AllocationLineResults = new List<AllocationLineResult>
                    {
                        new AllocationLineResult { AllocationLine = new Reference { Id = "alloc1", Name = "Alloc 1" }, Value = value }
                    },
                    CalculationResults = new List<CalculationResult>
                    {
                        new CalculationResult
                        {
                            AllocationLine = new Reference { Id = "alloc1", Name = "Alloc 1" },
                            Calculation = new Reference { Id = "calc1", Name = "Calc 1" },
                            CalculationSpecification = new Reference { Id = "calcspec1", Name = "Calc 1" },
                            CalculationType = Models.Calcs.CalculationType.Funding,
                            Value = value
                        }
                    },
                    Id = "provresult" + (i + 1),
                    Provider = new ProviderSummary { Id = "prov" + (i + 1) }
                });
            }

            return results;
        }

        private static ISpecificationsRepository InitialiseSpecificationRepository(DateTimeOffset? specVariationDate)
        {
            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCurrentSpecificationById(Arg.Is(specificationId))
                .Returns(new SpecificationCurrentVersion
                {
                    Id = specificationId,
                    VariationDate = specVariationDate,
                    FundingPeriod = new Reference { Id = "1819", Name = "AY1819" },
                    FundingStreams = new List<FundingStream>
                    {
                        new FundingStream
                        {
                            AllocationLines = new List<AllocationLine>
                            {
                                new AllocationLine
                                {
                                    Id = "alloc1",
                                    Name = "Alloc 1",
                                    ProviderLookups = new List<ProviderLookup>
                                    {
                                        new ProviderLookup { ProviderType = "Academies", ProviderSubType = "Academy sponsor led" }
                                    }
                                },
                                new AllocationLine
                                {
                                    Id = "alloc2",
                                    Name = "Alloc 2",
                                    ProviderLookups = new List<ProviderLookup>
                                    {
                                        new ProviderLookup { ProviderType = "Free Schools", ProviderSubType = "Free schools special" }
                                    }
                                }
                            },
                            Id = "PSG",
                            Name = "PE & Sport"
                        }
                    },
                    Policies = new List<Policy>
                    {
                        new Policy
                        {
                            Calculations = new List<Calculation>
                            {
                                new Calculation { CalculationType = CalculationType.Funding, Id = "calcspec1" }
                            }
                        }
                    }
                });
            specificationsRepository
                .GetFundingPeriodById(Arg.Is("1819"))
                .Returns(new Period { EndDate = DateTimeOffset.Parse("2019-08-31T23:59:59"), Id = "1819", Name = "AY1819", StartDate = DateTimeOffset.Parse("2018-09-01T00:00:00") });
            specificationsRepository
                .GetFundingStreams()
                .Returns(new List<FundingStream>
                {
                    new FundingStream
                    {
                        Id = "PSG",
                        Name = "PE & Sport",
                        PeriodType = new PeriodType { Id = "PT1" },
                        AllocationLines = new List<AllocationLine>
                        {
                            new AllocationLine
                            {
                                Id = "alloc1",
                                Name = "Alloc 1",
                                ProviderLookups = new List<ProviderLookup>
                                {
                                    new ProviderLookup { ProviderType = "Academies", ProviderSubType = "Academy sponsor led"}
                                }
                            },
                            new AllocationLine
                            {
                                Id = "alloc2",
                                Name = "Alloc 2",
                                ProviderLookups = new List<ProviderLookup>
                                {
                                    new ProviderLookup { ProviderType = "Free Schools", ProviderSubType = "Free schools special" }
                                }
                            }
                        }
                    }
                });
            return specificationsRepository;
        }

        private IPublishedProviderResultsAssemblerService CreateRealResultsAssembler(ISpecificationsRepository specificationsRepository)
        {
            return new PublishedProviderResultsAssemblerService(specificationsRepository, CreateLogger(), CreatePublishedProviderResultsVersionRepository());
        }

        private IPublishedAllocationLineLogicalResultVersionService CreateRealPublishedAllocationLineLogicalResultVersionService()
        {
            return new PublishedAllocationLineLogicalResultVersionService();
        }

        private static IFeatureToggle InitialiseFeatureToggle(bool enableProviderVariations = true)
        {
            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
                .IsJobServiceForPublishProviderResultsEnabled()
                .Returns(true);
            featureToggle
                .IsProviderVariationsEnabled()
                .Returns(enableProviderVariations);

            return featureToggle;
        }

        private static IJobsApiClient InitialiseJobsApiClient(bool isRefreshJob = true)
        {
            string message = isRefreshJob ? "Refreshing published provider results for specification" : "Selecting specification for funding";

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(new ApiResponse<JobViewModel>(HttpStatusCode.OK, new JobViewModel { Id = jobId, Trigger = new Trigger { Message = message } }));
            jobsApiClient
                .AddJobLog(Arg.Is(jobId), Arg.Any<JobLogUpdateModel>())
                .Returns(new ApiResponse<JobLog>(HttpStatusCode.OK, new JobLog()));
            jobsApiClient
                .CreateJob(Arg.Any<JobCreateModel>())
                .Returns(new Job { Id = Guid.NewGuid().ToString() });

            return jobsApiClient;
        }
    }
}
