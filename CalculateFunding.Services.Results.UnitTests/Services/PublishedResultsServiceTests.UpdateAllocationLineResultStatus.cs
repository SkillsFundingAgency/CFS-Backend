﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Results.Search;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Results.Interfaces;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Results.UnitTests.Services
{
    public partial class PublishedResultsServiceTests
    {
        [TestMethod]
        public void UpdateAllocationLineResultStatus_GivenNoJobId_LogsErrorAndThrowsException()
        {
            // Arrange
            Message message = new Message();

            ILogger logger = CreateLogger();

            IJobsApiClient jobsApiClient = CreateJobsApiClient();

            PublishedResultsService publishedResultsService = CreateResultsService(logger, jobsApiClient: jobsApiClient);

            // Act
            Func<Task> test = async () => await publishedResultsService.UpdateAllocationLineResultStatus(message);

            // Assert
            test
                .Should()
                .ThrowExactly<Exception>()
                .Which
                .Message
                .Should()
                .Be("Missing parent job id to update allocation line result status");

            logger
                .Received(1)
                .Error(Arg.Is("Missing parent job id to update allocation line result status"));
        }

        [TestMethod]
        public async Task UpdateAllocationLineResultStatus_GivenNullModel_LogsErrorCreatesFailedJobLog()
        {
            // Arrange
            Message message = new Message();
            message.UserProperties.Add("jobId", jobId);

            ILogger logger = CreateLogger();

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(new ApiResponse<JobViewModel>(HttpStatusCode.OK, new JobViewModel { Id = jobId }));

            PublishedResultsService publishedResultsService = CreateResultsService(logger, jobsApiClient: jobsApiClient);

            // Act
            await publishedResultsService.UpdateAllocationLineResultStatus(message);

            // Assert
            logger
                .Received(1)
                .Error(Arg.Is($"A null allocation line result status update model was provided for job id  '{jobId}'"));

            await
                jobsApiClient
                    .Received(1)
                    .AddJobLog(Arg.Is(jobId), Arg.Is<JobLogUpdateModel>(m =>
                       m.CompletedSuccessfully == false &&
                       m.Outcome == "Failed to update allocation line result status - null update model provided"
                    ));
        }

        [TestMethod]
        public async Task UpdateAllocationLineResultStatus_GivenParentJobResponseNotFound_LogsErrorAndStopsProcessing()
        {
            // Arrange
            UpdatePublishedAllocationLineResultStatusModel model = new UpdatePublishedAllocationLineResultStatusModel();
            string json = JsonConvert.SerializeObject(model);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties.Add("jobId", jobId);

            ILogger logger = CreateLogger();

            ApiResponse<JobViewModel> jobResponse = new ApiResponse<JobViewModel>(HttpStatusCode.NotFound);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(jobResponse);

            PublishedResultsService publishedResultsService = CreateResultsService(logger, jobsApiClient: jobsApiClient);

            // Act
            await publishedResultsService.UpdateAllocationLineResultStatus(message);

            // Assert
            logger
                .Received(1)
                .Error(Arg.Is($"Could not find the job with id: '{jobId}'"));
        }

        [TestMethod]
        public async Task UpdateAllocationLineResultStatus_GivenParentJobResponseIsNull_LogsErrorAndStopsProcessing()
        {
            // Arrange
            UpdatePublishedAllocationLineResultStatusModel model = new UpdatePublishedAllocationLineResultStatusModel();
            string json = JsonConvert.SerializeObject(model);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties.Add("jobId", jobId);

            ILogger logger = CreateLogger();

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns((ApiResponse<JobViewModel>)null);

            PublishedResultsService publishedResultsService = CreateResultsService(logger, jobsApiClient: jobsApiClient);

            // Act
            await publishedResultsService.UpdateAllocationLineResultStatus(message);

            // Assert
            logger
                .Received(1)
                .Error(Arg.Is($"Could not find the job with id: '{jobId}'"));
        }

        [TestMethod]
        public async Task UpdateAllocationLineResultStatus_GivenParentJobAlreadyCancelled_LogsAndDoesntCreateStartingLog()
        {
            // Arrange
            UpdatePublishedAllocationLineResultStatusModel model = new UpdatePublishedAllocationLineResultStatusModel();
            string json = JsonConvert.SerializeObject(model);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties.Add("jobId", jobId);

            ILogger logger = CreateLogger();

            JobViewModel jobViewModel = new JobViewModel
            {
                Id = jobId,
                CompletionStatus = CompletionStatus.Cancelled
            };

            ApiResponse<JobViewModel> jobResponse = new ApiResponse<JobViewModel>(HttpStatusCode.OK, jobViewModel);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(jobResponse);

            PublishedResultsService publishedResultsService = CreateResultsService(logger, jobsApiClient: jobsApiClient);

            // Act
            await publishedResultsService.UpdateAllocationLineResultStatus(message);

            // Assert
            logger
                .Received(1)
                .Information(Arg.Is($"Received job with id: '{jobId}' is already in a completed state with status {jobViewModel.CompletionStatus.ToString()}"));

            await
                jobsApiClient
                    .DidNotReceive()
                    .AddJobLog(Arg.Any<string>(), Arg.Any<JobLogUpdateModel>());
        }

        [TestMethod]
        public async Task UpdateAllocationLineResultStatus_GivenUpdateModelButPublishedProviderResultsCannotBeFound_LogsAndAddsJobLog()
        {
            // Arrange
            IEnumerable<UpdatePublishedAllocationLineResultStatusProviderModel> Providers = new[]
            {
                new UpdatePublishedAllocationLineResultStatusProviderModel
                {
                    ProviderId = "1111",
                    AllocationLineIds = new[] { "AAAAA" }
                },
                new UpdatePublishedAllocationLineResultStatusProviderModel
                {
                    ProviderId = "1111-1",
                    AllocationLineIds = new[] { "AAAAA" }
                },
                new UpdatePublishedAllocationLineResultStatusProviderModel
                {
                    ProviderId = "1111-2",
                    AllocationLineIds = new[] { "AAAAA" }
                }
            };

            UpdatePublishedAllocationLineResultStatusModel model = new UpdatePublishedAllocationLineResultStatusModel
            {
                Providers = Providers,
                Status = AllocationLineStatus.Approved
            };

            string json = JsonConvert.SerializeObject(model);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties.Add("jobId", jobId);

            ILogger logger = CreateLogger();

            JobViewModel jobViewModel = new JobViewModel
            {
                Id = jobId,
                SpecificationId = specificationId
            };

            ApiResponse<JobViewModel> jobResponse = new ApiResponse<JobViewModel>(HttpStatusCode.OK, jobViewModel);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(jobResponse);

            IPublishedProviderResultsRepository resultsRepository = CreatePublishedProviderResultsRepository();
            resultsRepository
                .GetPublishedProviderResultsForSpecificationIdAndProviderId(Arg.Is(specificationId), Arg.Any<IEnumerable<string>>())
                .Returns((IEnumerable<PublishedProviderResult>)null);

            PublishedResultsService publishedResultsService = CreateResultsService(logger, jobsApiClient: jobsApiClient, publishedProviderResultsRepository: resultsRepository);

            // Act
            await publishedResultsService.UpdateAllocationLineResultStatus(message);

            // Assert
            await
                jobsApiClient
                    .Received(1)
                    .AddJobLog(Arg.Is(jobId), Arg.Is<JobLogUpdateModel>(m => m.CompletedSuccessfully == true && m.Outcome == "No provider results found to update"));

            logger
                .Received(1)
                .Error(Arg.Is($"No provider results to update for specification id: {specificationId}"));
        }

        [TestMethod]
        public async Task UpdateAllocationLineResultStatus_GivenUpdateModelAndCompletesSuccessfully_AddsJobLog()
        {
            // Arrange
            IEnumerable<UpdatePublishedAllocationLineResultStatusProviderModel> Providers = new[]
            {
                new UpdatePublishedAllocationLineResultStatusProviderModel
                {
                    ProviderId = "1111",
                    AllocationLineIds = new[] { "AAAAA" }
                }
            };

            UpdatePublishedAllocationLineResultStatusModel model = new UpdatePublishedAllocationLineResultStatusModel
            {
                Providers = Providers,
                Status = AllocationLineStatus.Approved
            };

            string json = JsonConvert.SerializeObject(model);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties.Add("jobId", jobId);

            ILogger logger = CreateLogger();

            JobViewModel jobViewModel = new JobViewModel
            {
                Id = jobId,
                SpecificationId = specificationId,
                Properties = new Dictionary<string, string>()
            };

            ApiResponse<JobViewModel> jobResponse = new ApiResponse<JobViewModel>(HttpStatusCode.OK, jobViewModel);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(jobResponse);

            IEnumerable<PublishedProviderResult> publishedProviderResults = CreatePublishedProviderResults();
            publishedProviderResults
                .First()
                .FundingStreamResult
                .AllocationLineResult
                .Current
                .Status = AllocationLineStatus.Approved;

            foreach (PublishedProviderResult publishedProviderResult in publishedProviderResults)
            {
                publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.ProfilingPeriods = new[] { new ProfilingPeriod() };
            }

            PublishedAllocationLineResultVersion newVersion = publishedProviderResults.First().FundingStreamResult.AllocationLineResult.Current as PublishedAllocationLineResultVersion;
            newVersion.Version = 2;

            IVersionRepository<PublishedAllocationLineResultVersion> versionRepository = CreatePublishedProviderResultsVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<PublishedAllocationLineResultVersion>(), Arg.Any<PublishedAllocationLineResultVersion>(), Arg.Is("1111"), Arg.Is(true))
                .Returns(newVersion);

            SpecificationCurrentVersion specification = CreateSpecification(specificationId);

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCurrentSpecificationById(Arg.Is(specificationId))
                .Returns(specification);

            IPublishedProviderResultsRepository resultsRepository = CreatePublishedProviderResultsRepository();
            resultsRepository
                .GetPublishedProviderResultsForSpecificationIdAndProviderId(Arg.Is(specificationId), Arg.Any<IEnumerable<string>>())
                .Returns(publishedProviderResults);

            PublishedResultsService publishedResultsService = CreateResultsService(
                logger,
                jobsApiClient: jobsApiClient,
                publishedProviderResultsRepository: resultsRepository,
                specificationsRepository: specificationsRepository,
                publishedProviderResultsVersionRepository: versionRepository);

            // Act
            await publishedResultsService.UpdateAllocationLineResultStatus(message);

            // Assert
            await
                jobsApiClient
                .Received(1)
                .AddJobLog(Arg.Is(jobId), Arg.Is<JobLogUpdateModel>(m =>
                        m.CompletedSuccessfully == true &&
                        m.Outcome == "Allocation line results were successfully updated"
                    ));

        }

        [TestMethod]
        public async Task UpdateAllocationLineResultStatus_GivenUpdateModelWithThreeResultsButOnlyOneCompletedSuccessfully_AddsJobLog()
        {
            // Arrange
            IEnumerable<UpdatePublishedAllocationLineResultStatusProviderModel> Providers = new[]
            {
                new UpdatePublishedAllocationLineResultStatusProviderModel
                {
                    ProviderId = "1111",
                    AllocationLineIds = new[] { "AAAAA" }
                },
                new UpdatePublishedAllocationLineResultStatusProviderModel
                {
                    ProviderId = "1111-1",
                    AllocationLineIds = new[] { "AAAAA" }
                },
                new UpdatePublishedAllocationLineResultStatusProviderModel
                {
                    ProviderId = "1111-2",
                    AllocationLineIds = new[] { "AAAAA" }
                }
            };

            UpdatePublishedAllocationLineResultStatusModel model = new UpdatePublishedAllocationLineResultStatusModel
            {
                Providers = Providers,
                Status = AllocationLineStatus.Approved
            };

            string json = JsonConvert.SerializeObject(model);
            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties.Add("jobId", jobId);

            ILogger logger = CreateLogger();

            JobViewModel jobViewModel = new JobViewModel
            {
                Id = jobId,
                SpecificationId = specificationId,
                Properties = new Dictionary<string, string>()
            };

            ApiResponse<JobViewModel> jobResponse = new ApiResponse<JobViewModel>(HttpStatusCode.OK, jobViewModel);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(jobResponse);

            IEnumerable<PublishedProviderResult> publishedProviderResults = CreatePublishedProviderResults();

            foreach (PublishedProviderResult publishedProviderResult in publishedProviderResults)
            {
                publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.ProfilingPeriods = new[] { new ProfilingPeriod() };
            }

            PublishedAllocationLineResultVersion newVersion1 = publishedProviderResults.ElementAt(0).FundingStreamResult.AllocationLineResult.Current.Clone() as PublishedAllocationLineResultVersion;
            newVersion1.Version = 2;
            newVersion1.Status = AllocationLineStatus.Approved;

            PublishedAllocationLineResultVersion newVersion2 = publishedProviderResults.ElementAt(0).FundingStreamResult.AllocationLineResult.Current.Clone() as PublishedAllocationLineResultVersion;
            newVersion2.Version = 2;
            newVersion2.Status = AllocationLineStatus.Approved;

            PublishedAllocationLineResultVersion newVersion3 = publishedProviderResults.ElementAt(0).FundingStreamResult.AllocationLineResult.Current.Clone() as PublishedAllocationLineResultVersion;
            newVersion3.Version = 2;
            newVersion3.Status = AllocationLineStatus.Approved;

            IVersionRepository<PublishedAllocationLineResultVersion> versionRepository = CreatePublishedProviderResultsVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<PublishedAllocationLineResultVersion>(), Arg.Any<PublishedAllocationLineResultVersion>(), Arg.Any<string>(), Arg.Is(true))
                .Returns(newVersion1, newVersion2, newVersion3);


            IPublishedProviderResultsRepository resultsProviderRepository = CreatePublishedProviderResultsRepository();
            resultsProviderRepository
                .GetPublishedProviderResultsForSpecificationIdAndProviderId(Arg.Is(specificationId), Arg.Any<IEnumerable<string>>())
                .Returns(publishedProviderResults);

            SpecificationCurrentVersion specification = CreateSpecification(specificationId);

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCurrentSpecificationById(Arg.Is(specificationId))
                .Returns(specification);


            PublishedResultsService publishedResultsService = CreateResultsService(
                logger,
                jobsApiClient: jobsApiClient,
                publishedProviderResultsRepository: resultsProviderRepository,
                specificationsRepository: specificationsRepository,
                publishedProviderResultsVersionRepository: versionRepository);

            // Act
            await publishedResultsService.UpdateAllocationLineResultStatus(message);

            // Assert
            await
                jobsApiClient
                .Received(1)
                .AddJobLog(Arg.Is(jobId), Arg.Is<JobLogUpdateModel>(m =>
                        m.CompletedSuccessfully == true &&
                        m.Outcome == "Allocation line results were successfully updated"
                    ));
        }


        [TestMethod]
        public void UpdateAllocationLineResultStatus_GivenUpdateCauseExceptionWhenSavingToCosomos_LogsAndThrowsRetriableException()
        {
            // Arrange
            IEnumerable<UpdatePublishedAllocationLineResultStatusProviderModel> Providers = new[]
            {
                new UpdatePublishedAllocationLineResultStatusProviderModel
                {
                    ProviderId = "1111",
                    AllocationLineIds = new[] { "AAAAA" }
                }
            };

            UpdatePublishedAllocationLineResultStatusModel model = new UpdatePublishedAllocationLineResultStatusModel
            {
                Providers = Providers,
                Status = AllocationLineStatus.Approved
            };

            string json = JsonConvert.SerializeObject(model);
            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties.Add("jobId", jobId);

            ILogger logger = CreateLogger();

            JobViewModel jobViewModel = new JobViewModel
            {
                Id = jobId,
                SpecificationId = specificationId,
                Properties = new Dictionary<string, string>()
            };

            ApiResponse<JobViewModel> jobResponse = new ApiResponse<JobViewModel>(HttpStatusCode.OK, jobViewModel);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(jobResponse);

            IEnumerable<PublishedProviderResult> publishedProviderResults = CreatePublishedProviderResults();
            publishedProviderResults
                .First()
                .FundingStreamResult
                .AllocationLineResult
                .Current
                .Status = AllocationLineStatus.Approved;

            foreach (PublishedProviderResult publishedProviderResult in publishedProviderResults)
            {
                publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.ProfilingPeriods = new[] { new ProfilingPeriod() };
            }

            PublishedAllocationLineResultVersion newVersion = publishedProviderResults.First().FundingStreamResult.AllocationLineResult.Current as PublishedAllocationLineResultVersion;
            newVersion.Version = 2;

            IVersionRepository<PublishedAllocationLineResultVersion> versionRepository = CreatePublishedProviderResultsVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<PublishedAllocationLineResultVersion>(), Arg.Any<PublishedAllocationLineResultVersion>(), Arg.Is("1111"), Arg.Is(true))
                .Returns(newVersion);

            SpecificationCurrentVersion specification = CreateSpecification(specificationId);

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCurrentSpecificationById(Arg.Is(specificationId))
                .Returns(specification);

            IPublishedProviderResultsRepository resultsRepository = CreatePublishedProviderResultsRepository();
            resultsRepository
                .GetPublishedProviderResultsForSpecificationIdAndProviderId(Arg.Is(specificationId), Arg.Any<IEnumerable<string>>())
                .Returns(publishedProviderResults);

            resultsRepository
                .When(x => x.SavePublishedResults(Arg.Any<List<PublishedProviderResult>>()))
                .Do(x => { throw new Exception(); });

            PublishedResultsService publishedResultsService = CreateResultsService(
                logger,
                jobsApiClient: jobsApiClient,
                publishedProviderResultsRepository: resultsRepository,
                specificationsRepository: specificationsRepository,
                publishedProviderResultsVersionRepository: versionRepository);

            // Act
            Func<Task> test = async () => await publishedResultsService.UpdateAllocationLineResultStatus(message);

            // Assert
            test
                .Should()
                .ThrowExactly<RetriableException>()
                .Which
                .Message
                .Should()
                .Be("Failed when updating allocation line results");

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Is("Failed when updating allocation line results"));
        }

        [TestMethod]
        public async Task UpdateAllocationLineResultStatus_GivenUpdateModel_ThenNoProfilingMessageSent()
        {
            // Arrange
            IEnumerable<UpdatePublishedAllocationLineResultStatusProviderModel> Providers = new[]
            {
                new UpdatePublishedAllocationLineResultStatusProviderModel
                {
                    ProviderId = "1111",
                    AllocationLineIds = new[] { "AAAAA" }
                }
            };

            UpdatePublishedAllocationLineResultStatusModel model = new UpdatePublishedAllocationLineResultStatusModel
            {
                Providers = Providers,
                Status = AllocationLineStatus.Approved
            };

            string json = JsonConvert.SerializeObject(model);
            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties.Add("jobId", jobId);

            ILogger logger = CreateLogger();

            string parentJobId = "parent-job-id-1";
            JobViewModel jobViewModel = new JobViewModel
            {
                Id = jobId,
                SpecificationId = specificationId,
                Properties = new Dictionary<string, string>(),
                ParentJobId = parentJobId
            };

            ApiResponse<JobViewModel> jobResponse = new ApiResponse<JobViewModel>(HttpStatusCode.OK, jobViewModel);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(jobResponse);
            jobsApiClient
                .CreateJob(Arg.Is<JobCreateModel>(j => j.JobDefinitionId == JobConstants.DefinitionNames.FetchProviderProfileJob))
                .Returns(new Job { Id = "fpp-job" });

            IEnumerable<PublishedProviderResult> publishedProviderResults = CreatePublishedProviderResults();
            publishedProviderResults
                .First()
                .FundingStreamResult
                .AllocationLineResult
                .Current
                .Status = AllocationLineStatus.Approved;

            foreach (PublishedProviderResult publishedProviderResult in publishedProviderResults)
            {
                publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.ProfilingPeriods = new[] { new ProfilingPeriod() };
            }

            PublishedAllocationLineResultVersion newVersion = publishedProviderResults.First().FundingStreamResult.AllocationLineResult.Current as PublishedAllocationLineResultVersion;
            newVersion.Version = 2;

            IVersionRepository<PublishedAllocationLineResultVersion> versionRepository = CreatePublishedProviderResultsVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<PublishedAllocationLineResultVersion>(), Arg.Any<PublishedAllocationLineResultVersion>(), Arg.Is("1111"), Arg.Is(true))
                .Returns(newVersion);

            SpecificationCurrentVersion specification = CreateSpecification(specificationId);

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCurrentSpecificationById(Arg.Is(specificationId))
                .Returns(specification);

            IPublishedProviderResultsRepository resultsRepository = CreatePublishedProviderResultsRepository();
            resultsRepository
                .GetPublishedProviderResultsForSpecificationIdAndProviderId(Arg.Is(specificationId), Arg.Any<IEnumerable<string>>())
                .Returns(publishedProviderResults);

            IMessengerService messengerService = CreateMessengerService();

            PublishedResultsService publishedResultsService = CreateResultsService(
                logger,
                jobsApiClient: jobsApiClient,
                publishedProviderResultsRepository: resultsRepository,
                specificationsRepository: specificationsRepository,
                publishedProviderResultsVersionRepository: versionRepository,
                messengerService: messengerService);

            // Act
            await publishedResultsService.UpdateAllocationLineResultStatus(message);

            // Assert
            await
                jobsApiClient
                    .DidNotReceive()
                    .CreateJob(Arg.Is<JobCreateModel>(j => j.JobDefinitionId == JobConstants.DefinitionNames.FetchProviderProfileJob && j.SpecificationId == specificationId && j.ParentJobId == parentJobId));
        }

        [TestMethod]
        public async Task UpdateAllocationLineResultStatus_GivenAllResultsAreHeldAndAttemptToApproved_UpdatesSearchDoesNotUpdatePublishedField()
        {
            // Arrange
            string specificationId = "spec-1";
            string providerId = "1111";

            IEnumerable<UpdatePublishedAllocationLineResultStatusProviderModel> Providers = new[]
            {
                new UpdatePublishedAllocationLineResultStatusProviderModel
                {
                    ProviderId = providerId,
                    AllocationLineIds = new[] { "AAAAA" }
                }
            };

            JobViewModel jobViewModel = new JobViewModel
            {
                Id = jobId,
                SpecificationId = specificationId,
                Properties = new Dictionary<string, string>()
            };

            UpdatePublishedAllocationLineResultStatusModel model = new UpdatePublishedAllocationLineResultStatusModel
            {
                Providers = Providers,
                Status = AllocationLineStatus.Approved
            };

            string json = JsonConvert.SerializeObject(model);
            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties.Add("jobId", jobId);

            IEnumerable<PublishedProviderResult> publishedProviderResults = CreatePublishedProviderResultsWithDifferentProviders();

            foreach (PublishedProviderResult publishedProviderResult in publishedProviderResults)
            {
                publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.ProfilingPeriods = new[] { new ProfilingPeriod() };
            }

            IPublishedProviderResultsRepository resultsProviderRepository = CreatePublishedProviderResultsRepository();

            resultsProviderRepository
                .GetPublishedProviderResultsForSpecificationIdAndProviderId(Arg.Is(specificationId), Arg.Any<IEnumerable<string>>())
                .Returns(publishedProviderResults);

            PublishedAllocationLineResultVersion newVersion = publishedProviderResults.ElementAt(0).FundingStreamResult.AllocationLineResult.Current.Clone() as PublishedAllocationLineResultVersion;
            newVersion.Version = 2;
            newVersion.Status = AllocationLineStatus.Approved;

            IVersionRepository<PublishedAllocationLineResultVersion> versionRepository = CreatePublishedProviderResultsVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<PublishedAllocationLineResultVersion>(), Arg.Any<PublishedAllocationLineResultVersion>(), Arg.Is(providerId), Arg.Is(true))
                .Returns(newVersion);

            ISearchRepository<AllocationNotificationFeedIndex> searchRepository = CreateAllocationNotificationFeedSearchRepository();

            SpecificationCurrentVersion specification = CreateSpecification(specificationId);

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCurrentSpecificationById(Arg.Is(specificationId))
                .Returns(specification);

            ApiResponse<JobViewModel> jobResponse = new ApiResponse<JobViewModel>(HttpStatusCode.OK, jobViewModel);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(jobResponse);

            PublishedResultsService resultsService = CreateResultsService(
                publishedProviderResultsRepository: resultsProviderRepository,
                allocationNotificationFeedSearchRepository: searchRepository,
                specificationsRepository: specificationsRepository,
                publishedProviderResultsVersionRepository: versionRepository,
                jobsApiClient: jobsApiClient);

            // Act
            await resultsService.UpdateAllocationLineResultStatus(message);

            // Assert
            await searchRepository
                    .Received(1)
                    .Index(Arg.Is<IEnumerable<AllocationNotificationFeedIndex>>(m =>
                        m.First().ProviderId == providerId &&
                        m.First().Title == "Allocation test allocation line 1 was Approved" &&
                        m.First().Summary == "UKPRN: 1111, version 0.1" &&
                        m.First().DatePublished.HasValue == false &&
                        m.First().FundingStreamId == "fs-1" &&
                        m.First().FundingStreamName == "funding stream 1" &&
                        m.First().FundingPeriodId == "1819" &&
                        m.First().ProviderUkPrn == "1111" &&
                        m.First().ProviderUpin == "2222" &&
                        m.First().ProviderOpenDate.HasValue &&
                        m.First().AllocationLineId == "AAAAA" &&
                        m.First().AllocationLineName == "test allocation line 1" &&
                        m.First().AllocationVersionNumber == 2 &&
                        m.First().AllocationStatus == "Approved" &&
                        m.First().AllocationAmount == 50.0 &&
                        m.First().ProviderProfiling == "[{\"period\":null,\"occurrence\":0,\"periodYear\":0,\"periodType\":null,\"profileValue\":0.0,\"distributionPeriod\":null}]" &&
                        m.First().ProviderName == "test provider name 1" &&
                        m.First().LaCode == "77777" &&
                        m.First().Authority == "London" &&
                        m.First().ProviderType == "test type" &&
                        m.First().SubProviderType == "test sub type" &&
                        m.First().EstablishmentNumber == "es123"
            ));

            await
                versionRepository
                    .Received(1)
                    .SaveVersions(Arg.Is<IEnumerable<KeyValuePair<string, PublishedAllocationLineResultVersion>>>(m => m.Count() == 1 && m.First().Key == newVersion.ProviderId && m.First().Value == newVersion));

            publishedProviderResults
                .First()
                .FundingStreamResult.AllocationLineResult.Published
                .Should()
                .BeNull();
        }


        [TestMethod]
        public async Task UpdateAllocationLineResultStatus_GivenAllResultsAreAprrovedAndAttemptToPublish_SetsPublishedField()
        {
            // Arrange
            string specificationId = "spec-1";
            string providerId = "1111";

            IEnumerable<UpdatePublishedAllocationLineResultStatusProviderModel> Providers = new[]
            {
                new UpdatePublishedAllocationLineResultStatusProviderModel
                {
                    ProviderId = providerId,
                    AllocationLineIds = new[] { "AAAAA" }
                }
            };

            JobViewModel jobViewModel = new JobViewModel
            {
                Id = jobId,
                SpecificationId = specificationId,
                Properties = new Dictionary<string, string>()
            };

            UpdatePublishedAllocationLineResultStatusModel model = new UpdatePublishedAllocationLineResultStatusModel
            {
                Providers = Providers,
                Status = AllocationLineStatus.Published
            };

            string json = JsonConvert.SerializeObject(model);
            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties.Add("jobId", jobId);

            IEnumerable<PublishedProviderResult> publishedProviderResults = CreatePublishedProviderResultsWithDifferentProviders();

            foreach (PublishedProviderResult publishedProviderResult in publishedProviderResults)
            {
                publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.ProfilingPeriods = new[] { new ProfilingPeriod { Period = "Apr", Year = 2019 } };

                publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Status = AllocationLineStatus.Approved;
            }

            IPublishedProviderResultsRepository resultsProviderRepository = CreatePublishedProviderResultsRepository();

            resultsProviderRepository
                .GetPublishedProviderResultsForSpecificationIdAndProviderId(Arg.Is(specificationId), Arg.Any<IEnumerable<string>>())
                .Returns(publishedProviderResults);

            PublishedAllocationLineResultVersion newVersion = publishedProviderResults.ElementAt(0).FundingStreamResult.AllocationLineResult.Current.Clone() as PublishedAllocationLineResultVersion;
            newVersion.Version = 3;
            newVersion.Status = AllocationLineStatus.Published;

            IVersionRepository<PublishedAllocationLineResultVersion> versionRepository = CreatePublishedProviderResultsVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<PublishedAllocationLineResultVersion>(), Arg.Any<PublishedAllocationLineResultVersion>(), Arg.Is(providerId), Arg.Is(true))
                .Returns(newVersion);

            ISearchRepository<AllocationNotificationFeedIndex> searchRepository = CreateAllocationNotificationFeedSearchRepository();

            SpecificationCurrentVersion specification = CreateSpecification(specificationId);

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCurrentSpecificationById(Arg.Is(specificationId))
                .Returns(specification);

            ApiResponse<JobViewModel> jobResponse = new ApiResponse<JobViewModel>(HttpStatusCode.OK, jobViewModel);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(jobResponse);

            PublishedResultsService resultsService = CreateResultsService(
                publishedProviderResultsRepository: resultsProviderRepository,
                allocationNotificationFeedSearchRepository: searchRepository,
                specificationsRepository: specificationsRepository,
                publishedProviderResultsVersionRepository: versionRepository,
                jobsApiClient: jobsApiClient);

            // Act
            await resultsService.UpdateAllocationLineResultStatus(message);

            // Assert
            await
                versionRepository
                    .Received(1)
                    .SaveVersions(Arg.Is<IEnumerable<KeyValuePair<string, PublishedAllocationLineResultVersion>>>(m => m.Count() == 1 && m.First().Key == newVersion.ProviderId && m.First().Value == newVersion));

            publishedProviderResults
                .First()
                .FundingStreamResult.AllocationLineResult.Published
                .Should()
                .NotBeNull();

            publishedProviderResults
                .First()
                .FundingStreamResult.AllocationLineResult.Published
                .Should()
                .BeEquivalentTo(publishedProviderResults.First().FundingStreamResult.AllocationLineResult.Current);
        }

        [TestMethod]
        public async Task UpdateAllocationLineResultStatus_GivenPublishResultsReturnsButNoAllocationLinesSpecified_AddJobLog()
        {
            // Arrange
            IEnumerable<UpdatePublishedAllocationLineResultStatusProviderModel> Providers = new[]
            {
                new UpdatePublishedAllocationLineResultStatusProviderModel
                {
                    ProviderId = "1111",
                }
            };

            string specificationId = "spec-1";

            JobViewModel jobViewModel = new JobViewModel
            {
                Id = jobId,
                SpecificationId = specificationId,
                Properties = new Dictionary<string, string>()
            };

            UpdatePublishedAllocationLineResultStatusModel model = new UpdatePublishedAllocationLineResultStatusModel
            {
                Providers = Providers,
                Status = AllocationLineStatus.Approved
            };

            ApiResponse<JobViewModel> jobResponse = new ApiResponse<JobViewModel>(HttpStatusCode.OK, jobViewModel);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(jobResponse);

            string json = JsonConvert.SerializeObject(model);
            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties.Add("jobId", jobId);

            IPublishedProviderResultsRepository resultsProviderRepository = CreatePublishedProviderResultsRepository();
            resultsProviderRepository
                .GetPublishedProviderResultsForSpecificationIdAndProviderId(Arg.Is(specificationId), Arg.Any<IEnumerable<string>>())
                .Returns(CreatePublishedProviderResults());

            ISearchRepository<AllocationNotificationFeedIndex> searchRepository = CreateAllocationNotificationFeedSearchRepository();

            SpecificationCurrentVersion specification = CreateSpecification(specificationId);

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCurrentSpecificationById(Arg.Is(specificationId))
                .Returns(specification);

            PublishedResultsService resultsService = CreateResultsService(
                publishedProviderResultsRepository: resultsProviderRepository,
                allocationNotificationFeedSearchRepository: searchRepository,
                specificationsRepository: specificationsRepository,
                jobsApiClient: jobsApiClient);

            // Act
            await resultsService.UpdateAllocationLineResultStatus(message);

            // Assert
            await
                jobsApiClient
                .Received(1)
                .AddJobLog(Arg.Is(jobId), Arg.Is<JobLogUpdateModel>(m =>
                        m.CompletedSuccessfully == true &&
                        m.Outcome == "Allocation line results were successfully updated"
                    ));
        }

        [TestMethod]
        public async Task UpdateAllocationLineResultStatus_GivenAllResultsAreHeldAndAttemptToApproved_ThenEnsureHistoryAdded()
        {
            // Arrange
            string specificationId = "spec-1";
            string providerId = "1111";

            IEnumerable<UpdatePublishedAllocationLineResultStatusProviderModel> Providers = new[]
            {
                new UpdatePublishedAllocationLineResultStatusProviderModel
                {
                    ProviderId = providerId,
                    AllocationLineIds = new[] { "AAAAA" }
                }
            };

            UpdatePublishedAllocationLineResultStatusModel model = new UpdatePublishedAllocationLineResultStatusModel
            {
                Providers = Providers,
                Status = AllocationLineStatus.Approved
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);

            Message message = new Message(byteArray);
            message.UserProperties["jobId"] = jobId;

            IEnumerable<PublishedProviderResult> publishedProviderResults = CreatePublishedProviderResultsWithDifferentProviders();

            foreach (PublishedProviderResult publishedProviderResult in publishedProviderResults)
            {
                publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.ProfilingPeriods = new[] { new ProfilingPeriod() };
            }

            IPublishedProviderResultsRepository resultsProviderRepository = CreatePublishedProviderResultsRepository();

            resultsProviderRepository
                .GetPublishedProviderResultsForSpecificationIdAndProviderId(Arg.Is(specificationId), Arg.Any<IEnumerable<string>>())
                .Returns(publishedProviderResults);

            PublishedAllocationLineResultVersion newVersion = publishedProviderResults.ElementAt(0).FundingStreamResult.AllocationLineResult.Current.Clone() as PublishedAllocationLineResultVersion;
            newVersion.Version = 2;
            newVersion.Status = AllocationLineStatus.Approved;

            IVersionRepository<PublishedAllocationLineResultVersion> versionRepository = CreatePublishedProviderResultsVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<PublishedAllocationLineResultVersion>(), Arg.Any<PublishedAllocationLineResultVersion>(), Arg.Is(providerId), Arg.Is(true))
                .Returns(newVersion);

            ISearchRepository<AllocationNotificationFeedIndex> searchRepository = CreateAllocationNotificationFeedSearchRepository();

            SpecificationCurrentVersion specification = CreateSpecification(specificationId);

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCurrentSpecificationById(Arg.Is(specificationId))
                .Returns(specification);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(new ApiResponse<JobViewModel>(HttpStatusCode.OK, new JobViewModel { Id = jobId, SpecificationId = specificationId }));
            jobsApiClient
                .CreateJob(Arg.Any<JobCreateModel>())
                .Returns(new Job { Id = "newJobId" });

            PublishedResultsService resultsService = CreateResultsService(
                publishedProviderResultsRepository: resultsProviderRepository,
                allocationNotificationFeedSearchRepository: searchRepository,
                specificationsRepository: specificationsRepository,
                publishedProviderResultsVersionRepository: versionRepository,
                jobsApiClient: jobsApiClient);

            // Act
            await resultsService.UpdateAllocationLineResultStatus(message);

            // Assert
            await searchRepository
                    .Received(1)
                    .Index(Arg.Is<IEnumerable<AllocationNotificationFeedIndex>>(m =>
                        m.First().ProviderId == providerId &&
                        m.First().Title == "Allocation test allocation line 1 was Approved" &&
                        m.First().Summary == "UKPRN: 1111, version 0.1" &&
                        m.First().DatePublished.HasValue == false &&
                        m.First().FundingStreamId == "fs-1" &&
                        m.First().FundingStreamName == "funding stream 1" &&
                        m.First().FundingPeriodId == "1819" &&
                        m.First().ProviderUkPrn == "1111" &&
                        m.First().ProviderUpin == "2222" &&
                        m.First().ProviderOpenDate.HasValue &&
                        m.First().AllocationLineId == "AAAAA" &&
                        m.First().AllocationLineName == "test allocation line 1" &&
                        m.First().AllocationVersionNumber == 2 &&
                        m.First().AllocationStatus == "Approved" &&
                        m.First().AllocationAmount == 50.0 &&
                        m.First().ProviderProfiling == "[{\"period\":null,\"occurrence\":0,\"periodYear\":0,\"periodType\":null,\"profileValue\":0.0,\"distributionPeriod\":null}]" &&
                        m.First().ProviderName == "test provider name 1" &&
                        m.First().LaCode == "77777" &&
                        m.First().Authority == "London" &&
                        m.First().ProviderType == "test type" &&
                        m.First().SubProviderType == "test sub type" &&
                        m.First().EstablishmentNumber == "es123"
            ));

            await
                versionRepository
                    .Received(1)
                    .SaveVersions(Arg.Is<IEnumerable<KeyValuePair<string, PublishedAllocationLineResultVersion>>>(m => m.Count() == 1 && m.First().Key == newVersion.ProviderId && m.First().Value == newVersion));
        }

        [TestMethod]
        public async Task UpdateAllocationLineResultStatus_GivenThreeProvidersToPublish_ThenCreatesThreeHistoryItems()
        {
            // Arrange
            IEnumerable<UpdatePublishedAllocationLineResultStatusProviderModel> Providers = new[]
            {
                new UpdatePublishedAllocationLineResultStatusProviderModel
                {
                    ProviderId = "1111",
                    AllocationLineIds = new[] { "AAAAA" }
                },
                new UpdatePublishedAllocationLineResultStatusProviderModel
                {
                    ProviderId = "1111-1",
                    AllocationLineIds = new[] { "AAAAA" }
                },
                new UpdatePublishedAllocationLineResultStatusProviderModel
                {
                    ProviderId = "1111-2",
                    AllocationLineIds = new[] { "AAAAA" }
                }
            };

            UpdatePublishedAllocationLineResultStatusModel model = new UpdatePublishedAllocationLineResultStatusModel
            {
                Providers = Providers,
                Status = AllocationLineStatus.Approved
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);

            Message message = new Message(byteArray);
            message.UserProperties["jobId"] = jobId;

            IEnumerable<PublishedProviderResult> publishedProviderResults = CreatePublishedProviderResultsWithDifferentProviders();

            foreach (PublishedProviderResult publishedProviderResult in publishedProviderResults)
            {
                publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.ProfilingPeriods = new[] { new ProfilingPeriod() };
            }

            IPublishedProviderResultsRepository resultsProviderRepository = CreatePublishedProviderResultsRepository();
            resultsProviderRepository
                .GetPublishedProviderResultsForSpecificationIdAndProviderId(Arg.Is(specificationId), Arg.Any<IEnumerable<string>>())
                .Returns(publishedProviderResults);

            PublishedAllocationLineResultVersion newVersion1 = publishedProviderResults.ElementAt(0).FundingStreamResult.AllocationLineResult.Current.Clone() as PublishedAllocationLineResultVersion;
            newVersion1.Version = 2;
            newVersion1.Status = AllocationLineStatus.Approved;

            PublishedAllocationLineResultVersion newVersion2 = publishedProviderResults.ElementAt(0).FundingStreamResult.AllocationLineResult.Current.Clone() as PublishedAllocationLineResultVersion;
            newVersion2.Version = 2;
            newVersion2.Status = AllocationLineStatus.Approved;

            PublishedAllocationLineResultVersion newVersion3 = publishedProviderResults.ElementAt(0).FundingStreamResult.AllocationLineResult.Current.Clone() as PublishedAllocationLineResultVersion;
            newVersion3.Version = 2;
            newVersion3.Status = AllocationLineStatus.Approved;

            IVersionRepository<PublishedAllocationLineResultVersion> versionRepository = CreatePublishedProviderResultsVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<PublishedAllocationLineResultVersion>(), Arg.Any<PublishedAllocationLineResultVersion>(), Arg.Any<string>(), Arg.Any<bool>())
                .Returns(newVersion1, newVersion2, newVersion3);

            SpecificationCurrentVersion specification = CreateSpecification(specificationId);

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCurrentSpecificationById(Arg.Is(specificationId))
                .Returns(specification);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(new ApiResponse<JobViewModel>(HttpStatusCode.OK, new JobViewModel { Id = jobId, SpecificationId = specificationId }));

            PublishedResultsService resultsService = CreateResultsService(
                publishedProviderResultsRepository: resultsProviderRepository,
                specificationsRepository: specificationsRepository,
                publishedProviderResultsVersionRepository: versionRepository,
                jobsApiClient: jobsApiClient);

            // Act
            await resultsService.UpdateAllocationLineResultStatus(message);

            // Assert
            await
               versionRepository
                   .Received(1)
                   .SaveVersions(Arg.Is<IEnumerable<KeyValuePair<string, PublishedAllocationLineResultVersion>>>(m => m.Count() == 3));
        }

        [TestMethod]
        public async Task UpdateAllocationLineResultStatus_GivenThreeProvidersToApprove_ThenCreatesThreeHistoryItems()
        {
            // Arrange
            IEnumerable<UpdatePublishedAllocationLineResultStatusProviderModel> Providers = new[]
            {
                new UpdatePublishedAllocationLineResultStatusProviderModel
                {
                    ProviderId = "1111",
                    AllocationLineIds = new[] { "AAAAA" }
                },
                new UpdatePublishedAllocationLineResultStatusProviderModel
                {
                    ProviderId = "1111-1",
                    AllocationLineIds = new[] { "AAAAA" }
                },
                new UpdatePublishedAllocationLineResultStatusProviderModel
                {
                    ProviderId = "1111-2",
                    AllocationLineIds = new[] { "AAAAA" }
                }
            };

            UpdatePublishedAllocationLineResultStatusModel model = new UpdatePublishedAllocationLineResultStatusModel
            {
                Providers = Providers,
                Status = AllocationLineStatus.Approved
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);

            Message message = new Message(byteArray);
            message.UserProperties["jobId"] = jobId;

            IEnumerable<PublishedProviderResult> publishedProviderResults = CreatePublishedProviderResultsWithDifferentProviders();

            foreach (PublishedProviderResult publishedProviderResult in publishedProviderResults)
            {
                publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.ProfilingPeriods = new[] { new ProfilingPeriod() };
            }

            PublishedAllocationLineResultVersion newVersion1 = publishedProviderResults.ElementAt(0).FundingStreamResult.AllocationLineResult.Current.Clone() as PublishedAllocationLineResultVersion;
            newVersion1.Version = 2;
            newVersion1.Status = AllocationLineStatus.Approved;

            PublishedAllocationLineResultVersion newVersion2 = publishedProviderResults.ElementAt(0).FundingStreamResult.AllocationLineResult.Current.Clone() as PublishedAllocationLineResultVersion;
            newVersion2.Version = 2;
            newVersion2.Status = AllocationLineStatus.Approved;

            PublishedAllocationLineResultVersion newVersion3 = publishedProviderResults.ElementAt(0).FundingStreamResult.AllocationLineResult.Current.Clone() as PublishedAllocationLineResultVersion;
            newVersion3.Version = 2;
            newVersion3.Status = AllocationLineStatus.Approved;

            IVersionRepository<PublishedAllocationLineResultVersion> versionRepository = CreatePublishedProviderResultsVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<PublishedAllocationLineResultVersion>(), Arg.Any<PublishedAllocationLineResultVersion>(), Arg.Any<string>(), Arg.Any<bool>())
                .Returns(newVersion1, newVersion2, newVersion3);

            IPublishedProviderResultsRepository resultsProviderRepository = CreatePublishedProviderResultsRepository();
            resultsProviderRepository
                .GetPublishedProviderResultsForSpecificationIdAndProviderId(Arg.Is(specificationId), Arg.Any<IEnumerable<string>>())
                .Returns(publishedProviderResults);

            SpecificationCurrentVersion specification = CreateSpecification(specificationId);

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCurrentSpecificationById(Arg.Is(specificationId))
                .Returns(specification);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(new ApiResponse<JobViewModel>(HttpStatusCode.OK, new JobViewModel { Id = jobId, SpecificationId = specificationId }));

            PublishedResultsService resultsService = CreateResultsService(
                publishedProviderResultsRepository: resultsProviderRepository,
                specificationsRepository: specificationsRepository,
                publishedProviderResultsVersionRepository: versionRepository,
                jobsApiClient: jobsApiClient);

            // Act
            await resultsService.UpdateAllocationLineResultStatus(message);

            // Assert
            await
               versionRepository
                   .Received(1)
                   .SaveVersions(Arg.Is<IEnumerable<KeyValuePair<string, PublishedAllocationLineResultVersion>>>(m => m.Count() == 3));
        }

        [TestMethod]
        public async Task UpdateAllocationLineResultStatus_GivenPublishResultsReturnsAllocationLinesSpecifiedOnlyOneStatusChanged_ThenCreatesHistoryItems()
        {
            // Arrange
            IEnumerable<UpdatePublishedAllocationLineResultStatusProviderModel> Providers = new[]
            {
                new UpdatePublishedAllocationLineResultStatusProviderModel
                {
                    ProviderId = "1111",
                    AllocationLineIds = new[] { "AAAAA" }
                }
            };

            UpdatePublishedAllocationLineResultStatusModel model = new UpdatePublishedAllocationLineResultStatusModel
            {
                Providers = Providers,
                Status = AllocationLineStatus.Approved
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);

            Message message = new Message(byteArray);
            message.UserProperties["jobId"] = jobId;

            IEnumerable<PublishedProviderResult> publishedProviderResults = CreatePublishedProviderResults();
            publishedProviderResults
                .First()
                .FundingStreamResult
                .AllocationLineResult
                .Current
                .Status = AllocationLineStatus.Approved;

            foreach (PublishedProviderResult publishedProviderResult in publishedProviderResults)
            {
                publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.ProfilingPeriods = new[] { new ProfilingPeriod() };
            }

            PublishedAllocationLineResultVersion newVersion = publishedProviderResults.First().FundingStreamResult.AllocationLineResult.Current as PublishedAllocationLineResultVersion;
            newVersion.Version = 2;

            IVersionRepository<PublishedAllocationLineResultVersion> versionRepository = CreatePublishedProviderResultsVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<PublishedAllocationLineResultVersion>(), Arg.Any<PublishedAllocationLineResultVersion>(), Arg.Is("1111"), Arg.Is(true))
                .Returns(newVersion);

            SpecificationCurrentVersion specification = CreateSpecification(specificationId);

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCurrentSpecificationById(Arg.Is(specificationId))
                .Returns(specification);

            IPublishedProviderResultsRepository resultsProviderRepository = CreatePublishedProviderResultsRepository();
            resultsProviderRepository
                .GetPublishedProviderResultsForSpecificationIdAndProviderId(Arg.Is(specificationId), Arg.Any<IEnumerable<string>>())
                .Returns(publishedProviderResults);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(new ApiResponse<JobViewModel>(HttpStatusCode.OK, new JobViewModel { Id = jobId, SpecificationId = specificationId }));

            PublishedResultsService resultsService = CreateResultsService(
                publishedProviderResultsRepository: resultsProviderRepository,
                specificationsRepository: specificationsRepository,
                publishedProviderResultsVersionRepository: versionRepository,
                jobsApiClient: jobsApiClient);

            // Act
            await resultsService.UpdateAllocationLineResultStatus(message);

            // Assert
            await
               versionRepository
                   .Received(1)
                   .SaveVersions(Arg.Is<IEnumerable<KeyValuePair<string, PublishedAllocationLineResultVersion>>>(m => m.First().Key == newVersion.ProviderId && m.First().Value.Version == 2));
        }

        [TestMethod]
        public void UpdateAllocationLineResultStatus_GivenPublishResultsReturnsButNoAllocationLinesSpecified_ThenCreatesNoItems()
        {
            // Arrange
            IEnumerable<UpdatePublishedAllocationLineResultStatusProviderModel> Providers = new[]
            {
                new UpdatePublishedAllocationLineResultStatusProviderModel
                {
                    ProviderId = "1111",
                }
            };

            UpdatePublishedAllocationLineResultStatusModel model = new UpdatePublishedAllocationLineResultStatusModel
            {
                Providers = Providers,
                Status = AllocationLineStatus.Approved
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);

            Message message = new Message(byteArray);
            message.UserProperties["jobId"] = jobId;

            IPublishedProviderResultsRepository resultsProviderRepository = CreatePublishedProviderResultsRepository();
            resultsProviderRepository
                .GetPublishedProviderResultsForSpecificationIdAndProviderId(Arg.Is(specificationId), Arg.Any<IEnumerable<string>>())
                .Returns(CreatePublishedProviderResults());

            IVersionRepository<PublishedAllocationLineResultVersion> versionRepository = CreatePublishedProviderResultsVersionRepository();

            PublishedResultsService resultsService = CreateResultsService(publishedProviderResultsRepository: resultsProviderRepository, publishedProviderResultsVersionRepository: versionRepository);

            // Act
            Func<Task> action = async () => await resultsService.UpdateAllocationLineResultStatus(message);

            // Assert
            action.Should().NotThrow();

            versionRepository
                .DidNotReceive()
                .CreateVersion(Arg.Any<PublishedAllocationLineResultVersion>(), Arg.Any<PublishedAllocationLineResultVersion>(), Arg.Is("1111"), Arg.Any<bool>());
        }

        [TestMethod]
        public void UpdateAllocationLineResultStatus_GivenPublishResultsReturnsButUpdatingThrowsException_LogsErrorAndThrowsRetriableException()
        {
            // Arrange
            IEnumerable<UpdatePublishedAllocationLineResultStatusProviderModel> Providers = new[]
            {
                new UpdatePublishedAllocationLineResultStatusProviderModel
                {
                    ProviderId = "1111",
                    AllocationLineIds = new[] { "AAAAA" }
                }
            };

            UpdatePublishedAllocationLineResultStatusModel model = new UpdatePublishedAllocationLineResultStatusModel
            {
                Providers = Providers,
                Status = AllocationLineStatus.Approved
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);

            Message message = new Message(byteArray);
            message.UserProperties["jobId"] = jobId;

            IEnumerable<PublishedProviderResult> publishedProviderResults = CreatePublishedProviderResults();

            IPublishedProviderResultsRepository resultsProviderRepository = CreatePublishedProviderResultsRepository();
            resultsProviderRepository
                .GetPublishedProviderResultsForSpecificationIdAndProviderId(Arg.Is(specificationId), Arg.Any<IEnumerable<string>>())
                .Returns(publishedProviderResults);
            resultsProviderRepository
                .When(x => x.SavePublishedResults(Arg.Any<IEnumerable<PublishedProviderResult>>()))
                .Do(x => { throw new Exception(); });

            PublishedAllocationLineResultVersion newVersion1 = publishedProviderResults.ElementAt(0).FundingStreamResult.AllocationLineResult.Current.Clone() as PublishedAllocationLineResultVersion;
            newVersion1.Version = 2;
            newVersion1.Status = AllocationLineStatus.Approved;

            PublishedAllocationLineResultVersion newVersion2 = publishedProviderResults.ElementAt(0).FundingStreamResult.AllocationLineResult.Current.Clone() as PublishedAllocationLineResultVersion;
            newVersion2.Version = 2;
            newVersion2.Status = AllocationLineStatus.Approved;

            PublishedAllocationLineResultVersion newVersion3 = publishedProviderResults.ElementAt(0).FundingStreamResult.AllocationLineResult.Current.Clone() as PublishedAllocationLineResultVersion;
            newVersion3.Version = 2;
            newVersion3.Status = AllocationLineStatus.Approved;

            IVersionRepository<PublishedAllocationLineResultVersion> publishedProviderResultsVersionRepository = CreatePublishedProviderResultsVersionRepository();
            publishedProviderResultsVersionRepository
                .CreateVersion(Arg.Any<PublishedAllocationLineResultVersion>(), Arg.Any<PublishedAllocationLineResultVersion>(), Arg.Any<string>(), Arg.Is(true))
                .Returns(newVersion1, newVersion2, newVersion3);

            ILogger logger = CreateLogger();

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(new ApiResponse<JobViewModel>(HttpStatusCode.OK, new JobViewModel { Id = jobId, SpecificationId = specificationId }));

            PublishedResultsService resultsService = CreateResultsService(logger, publishedProviderResultsRepository: resultsProviderRepository, jobsApiClient: jobsApiClient, publishedProviderResultsVersionRepository: publishedProviderResultsVersionRepository);

            // Act
            Func<Task> action = async () => await resultsService.UpdateAllocationLineResultStatus(message);

            // Assert
            action
                .Should()
                .ThrowExactly<RetriableException>();

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Is("Failed when updating allocation line results"));
        }

        [TestMethod]
        public async Task UpdateAllocationLineResultStatus_GivenNoPublishResultsReturns_LogsError()
        {
            // Arrange
            IEnumerable<UpdatePublishedAllocationLineResultStatusProviderModel> Providers = new[]
            {
                new UpdatePublishedAllocationLineResultStatusProviderModel
                {
                    ProviderId = "1234"
                }
            };

            UpdatePublishedAllocationLineResultStatusModel model = new UpdatePublishedAllocationLineResultStatusModel
            {
                Providers = Providers
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);

            Message message = new Message(byteArray);
            message.UserProperties["jobId"] = jobId;

            IPublishedProviderResultsRepository resultsProviderRepository = CreatePublishedProviderResultsRepository();
            resultsProviderRepository
                .GetPublishedProviderResultsForSpecificationId(Arg.Is(specificationId))
                .Returns((IEnumerable<PublishedProviderResult>)null);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(new ApiResponse<JobViewModel>(HttpStatusCode.OK, new JobViewModel { Id = jobId, SpecificationId = specificationId }));
            jobsApiClient
                .CreateJob(Arg.Any<JobCreateModel>())
                .Returns(new Job { Id = "newJobId" });

            ILogger logger = CreateLogger();

            PublishedResultsService resultsService = CreateResultsService(publishedProviderResultsRepository: resultsProviderRepository, jobsApiClient: jobsApiClient, logger: logger);

            // Act
            await resultsService.UpdateAllocationLineResultStatus(message);

            // Assert
            logger
                .Received(1)
                .Error(Arg.Is($"No provider results to update for specification id: {specificationId}"));
        }
    }
}
