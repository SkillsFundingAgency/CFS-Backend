using CalculateFunding.Common.ApiClient.Jobs;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NSubstitute;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using FluentAssertions;
using System.Net;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Core.Constants;
using System.Linq;
using CalculateFunding.Common.Caching;
using CalculateFunding.Services.Results.Interfaces;

namespace CalculateFunding.Services.Results.Services
{
    public partial class PublishedResultsServiceTests
    {
        [TestMethod]
        public async Task CreateAllocationLineResultStatusUpdateJobs_GivenNoJobId_LogsErrorAndDoesntFetchJob()
        {
            //Arrange
            Message message = new Message();

            ILogger logger = CreateLogger();

            IJobsApiClient jobsApiClient = CreateJobsApiClient();

            PublishedResultsService publishedResultsService = CreateResultsService(logger, jobsApiClient: jobsApiClient);

            //Act
            await publishedResultsService.CreateAllocationLineResultStatusUpdateJobs(message);

            //Assert
            logger
                .Received(1)
                .Error(Arg.Is("Missing parent job id to instruct allocation line status updates"));

            await
                jobsApiClient
                    .DidNotReceive()
                    .GetJobById(Arg.Any<string>());
        }

        [TestMethod]
        public void CreateAllocationLineResultStatusUpdateJobs_GivenJobIdButJobResponseIsNull_LogsAndThrowsException()
        {
            //Arrange
            Message message = new Message();
            message.UserProperties.Add("jobId", jobId);

            ILogger logger = CreateLogger();

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(jobId)
                .Returns((ApiResponse<JobViewModel>)null);

            PublishedResultsService publishedResultsService = CreateResultsService(logger, jobsApiClient: jobsApiClient);

            //Act
            Func<Task> test = async () => await publishedResultsService.CreateAllocationLineResultStatusUpdateJobs(message);

            //Assert
            test
                .Should()
                .ThrowExactly<Exception>()
                .Which
                .Message
                .Should()
                .Be($"Could not find the parent job with job id: '{jobId}'");

            logger
                .Received(1)
                .Error(Arg.Is($"Could not find the parent job with job id: '{jobId}'"));
        }

        [TestMethod]
        public void CreateAllocationLineResultStatusUpdateJobs_GivenJobIdButJobResponseIsNotFound_LogsAndThrowsException()
        {
            //Arrange
            Message message = new Message();
            message.UserProperties.Add("jobId", jobId);

            ILogger logger = CreateLogger();

            ApiResponse<JobViewModel> apiResponse = new ApiResponse<JobViewModel>(HttpStatusCode.NotFound);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(jobId)
                .Returns(apiResponse);

            PublishedResultsService publishedResultsService = CreateResultsService(logger, jobsApiClient: jobsApiClient);

            //Act
            Func<Task> test = async () => await publishedResultsService.CreateAllocationLineResultStatusUpdateJobs(message);

            //Assert
            test
                .Should()
                .ThrowExactly<Exception>()
                .Which
                .Message
                .Should()
                .Be($"Could not find the parent job with job id: '{jobId}'");

            logger
                .Received(1)
                .Error(Arg.Is($"Could not find the parent job with job id: '{jobId}'"));
        }

        [TestMethod]
        public async Task CreateAllocationLineResultStatusUpdateJobs_GivenJobFoundButAlreadyCompleted_LogsAndReturnsDoesNotAddJobLog()
        {
            //Arrange
            Message message = new Message();
            message.UserProperties.Add("jobId", jobId);

            ILogger logger = CreateLogger();

            JobViewModel job = new JobViewModel
            {
                Id = jobId,
                CompletionStatus = CompletionStatus.Cancelled
            };

            ApiResponse<JobViewModel> apiResponse = new ApiResponse<JobViewModel>(HttpStatusCode.OK, job);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(jobId)
                .Returns(apiResponse);

            PublishedResultsService publishedResultsService = CreateResultsService(logger, jobsApiClient: jobsApiClient);

            //Act
            await publishedResultsService.CreateAllocationLineResultStatusUpdateJobs(message);

            //Assert
            logger
                .Received(1)
                .Information(Arg.Is($"Received job with id: '{job.Id}' is already in a completed state with status {job.CompletionStatus.ToString()}"));

            await
                jobsApiClient
                    .DidNotReceive()
                    .AddJobLog(Arg.Any<string>(), Arg.Any<JobLogUpdateModel>());
        }

        [TestMethod]
        public void CreateAllocationLineResultStatusUpdateJobs_GivenJobFoundButModelNotFoundInCache_LogsAndThrowsException()
        {
            //Arrange
            const string cacheKey = "cache-key";

            Message message = new Message();
            message.UserProperties.Add("jobId", jobId);

            ILogger logger = CreateLogger();

            JobViewModel job = new JobViewModel
            {
                Id = jobId,
                Properties = new Dictionary<string, string>
                {
                    {"cache-key", cacheKey }
                }
            };

            ApiResponse<JobViewModel> apiResponse = new ApiResponse<JobViewModel>(HttpStatusCode.OK, job);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(jobId)
                .Returns(apiResponse);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<UpdatePublishedAllocationLineResultStatusModel>(Arg.Is(cacheKey))
                .Returns((UpdatePublishedAllocationLineResultStatusModel)null);

            PublishedResultsService publishedResultsService = CreateResultsService(logger, jobsApiClient: jobsApiClient, cacheProvider: cacheProvider);

            //Act
            Func<Task> test = async () => await publishedResultsService.CreateAllocationLineResultStatusUpdateJobs(message);

            //Assert
            test
                .Should()
                .ThrowExactly<Exception>()
                .Which
                .Message
                .Should()
                .Be($"Could not find the update model in cache with cache key: '{cacheKey}'");

            logger
                .Received(1)
                .Error(Arg.Is($"Could not find the update model in cache with cache key: '{cacheKey}'"));
        }

        [TestMethod]
        public async Task CreateAllocationLineResultStatusUpdateJobs_GivenUpateModelWith10ProvidersAndDefaultMaxPartitionSize_CreatesOneChildJob()
        {
            //Arrange
            const string cacheKey = "cache-key";

            Message message = new Message();
            message.UserProperties.Add("jobId", jobId);

            ILogger logger = CreateLogger();

            JobViewModel job = new JobViewModel
            {
                Id = jobId,
                SpecificationId = specificationId,
                InvokerUserDisplayName = "user-name",
                InvokerUserId = "user-id",
                CorrelationId = "coorelation-id",
                Properties = new Dictionary<string, string>
                {
                    {"cache-key", cacheKey }
                },
                JobDefinitionId = JobConstants.DefinitionNames.CreateInstructAllocationLineResultStatusUpdateJob
            };

            UpdatePublishedAllocationLineResultStatusModel updateModel = new UpdatePublishedAllocationLineResultStatusModel
            {
                Status = AllocationLineStatus.Approved,
                Providers = CreateProviderAllocationLineResults()
            };

            ApiResponse<JobViewModel> apiResponse = new ApiResponse<JobViewModel>(HttpStatusCode.OK, job);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(jobId)
                .Returns(apiResponse);

            IEnumerable<Job> newJobs = new[]
            {
                new Job()
            };

            jobsApiClient
                .CreateJobs(Arg.Any<IEnumerable<JobCreateModel>>())
                .Returns(newJobs);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<UpdatePublishedAllocationLineResultStatusModel>(Arg.Is(cacheKey))
                .Returns(updateModel);

            PublishedResultsService publishedResultsService = CreateResultsService(logger, jobsApiClient: jobsApiClient, cacheProvider: cacheProvider);

            //Act
            await publishedResultsService.CreateAllocationLineResultStatusUpdateJobs(message);

            //Assert
            await
                jobsApiClient
                    .Received(1)
                    .CreateJobs(Arg.Is<IEnumerable<JobCreateModel>>(
                            m => m.First().Trigger.EntityId == jobId &&
                                 m.First().Trigger.EntityType == "Job" &&
                                 m.First().Trigger.Message == $"Triggered by parent job" &&
                                 m.First().SpecificationId == specificationId &&
                                 m.First().ParentJobId == jobId &&
                                 m.First().InvokerUserId == job.InvokerUserId &&
                                 m.First().InvokerUserDisplayName == job.InvokerUserDisplayName &&
                                 m.First().CorrelationId == job.CorrelationId &&
                                 !string.IsNullOrWhiteSpace(m.First().MessageBody)));

            await
                cacheProvider
                    .Received(1)
                    .RemoveAsync<UpdatePublishedAllocationLineResultStatusModel>(Arg.Is(cacheKey));
        }

        [TestMethod]
        public async Task CreateAllocationLineResultStatusUpdateJobs_GivenUpateModelWith10ProvidersAndMaxPartitionSizeOf2_CreatesFiveChildJobs()
        {
            //Arrange
            const string cacheKey = "cache-key";

            Message message = new Message();
            message.UserProperties.Add("jobId", jobId);

            ILogger logger = CreateLogger();

            JobViewModel job = new JobViewModel
            {
                Id = jobId,
                SpecificationId = specificationId,
                InvokerUserDisplayName = "user-name",
                InvokerUserId = "user-id",
                CorrelationId = "coorelation-id",
                Properties = new Dictionary<string, string>
                {
                    {"cache-key", cacheKey }
                },
                JobDefinitionId = JobConstants.DefinitionNames.CreateInstructAllocationLineResultStatusUpdateJob
            };

            UpdatePublishedAllocationLineResultStatusModel updateModel = new UpdatePublishedAllocationLineResultStatusModel
            {
                Status = AllocationLineStatus.Approved,
                Providers = CreateProviderAllocationLineResults()
            };

            ApiResponse<JobViewModel> apiResponse = new ApiResponse<JobViewModel>(HttpStatusCode.OK, job);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(jobId)
                .Returns(apiResponse);

            IEnumerable<Job> newJobs = new[]
            {
                new Job(),
                new Job(),
                new Job(),
                new Job(),
                new Job(),
            };

            jobsApiClient
                .CreateJobs(Arg.Any<IEnumerable<JobCreateModel>>())
                .Returns(newJobs);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<UpdatePublishedAllocationLineResultStatusModel>(Arg.Is(cacheKey))
                .Returns(updateModel);

            IPublishedProviderResultsSettings settings = CreatePublishedProviderResultsSettings();
            settings
                .UpdateAllocationLineResultStatusBatchCount
                .Returns(2);

            PublishedResultsService publishedResultsService = CreateResultsService(logger, jobsApiClient: jobsApiClient, cacheProvider: cacheProvider, publishedProviderResultsSettings: settings);

            //Act
            await publishedResultsService.CreateAllocationLineResultStatusUpdateJobs(message);

            //Assert
            await
                jobsApiClient
                    .Received(1)
                    .CreateJobs(Arg.Is<IEnumerable<JobCreateModel>>(m => m.Count() == 5));

            await
                cacheProvider
                    .Received(1)
                    .RemoveAsync<UpdatePublishedAllocationLineResultStatusModel>(Arg.Is(cacheKey));
        }

        [TestMethod]
        public void CreateAllocationLineResultStatusUpdateJobs_GivenUpateModelWith10ProvidersAndMaxPartitionSizeOf2ButOnlyThreeJobsCreatedFromFive_ThrowsException()
        {
            //Arrange
            const string cacheKey = "cache-key";

            Message message = new Message();
            message.UserProperties.Add("jobId", jobId);

            ILogger logger = CreateLogger();

            JobViewModel job = new JobViewModel
            {
                Id = jobId,
                SpecificationId = specificationId,
                InvokerUserDisplayName = "user-name",
                InvokerUserId = "user-id",
                CorrelationId = "coorelation-id",
                Properties = new Dictionary<string, string>
                {
                    {"cache-key", cacheKey }
                },
                JobDefinitionId = JobConstants.DefinitionNames.CreateInstructAllocationLineResultStatusUpdateJob
            };

            UpdatePublishedAllocationLineResultStatusModel updateModel = new UpdatePublishedAllocationLineResultStatusModel
            {
                Status = AllocationLineStatus.Approved,
                Providers = CreateProviderAllocationLineResults()
            };

            ApiResponse<JobViewModel> apiResponse = new ApiResponse<JobViewModel>(HttpStatusCode.OK, job);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(jobId)
                .Returns(apiResponse);

            IEnumerable<Job> newJobs = new[]
            {
                new Job(),
                new Job(),
                new Job()
            };

            jobsApiClient
                .CreateJobs(Arg.Any<IEnumerable<JobCreateModel>>())
                .Returns(newJobs);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<UpdatePublishedAllocationLineResultStatusModel>(Arg.Is(cacheKey))
                .Returns(updateModel);

            IPublishedProviderResultsSettings settings = CreatePublishedProviderResultsSettings();
            settings
                .UpdateAllocationLineResultStatusBatchCount
                .Returns(2);

            PublishedResultsService publishedResultsService = CreateResultsService(logger, jobsApiClient: jobsApiClient, cacheProvider: cacheProvider, publishedProviderResultsSettings: settings);

            //Act
            Func<Task> test = async () => await publishedResultsService.CreateAllocationLineResultStatusUpdateJobs(message);

            //Assert
            test
                .Should()
                .ThrowExactly<Exception>()
                .Which
                .Message
                .Should()
                .Be($"Only 3 jobs were created from 5 childJobs for parent job: '{job.Id}'");

            logger
                .Received(1)
                .Error(Arg.Is($"Only 3 jobs were created from 5 childJobs for parent job: '{job.Id}'"));
        }

        private static IEnumerable<UpdatePublishedAllocationLineResultStatusProviderModel> CreateProviderAllocationLineResults()
        {
            return new[]
                {
                    new UpdatePublishedAllocationLineResultStatusProviderModel
                    {
                        ProviderId = "111",
                        AllocationLineIds = new string[]{ "1"}
                    },
                    new UpdatePublishedAllocationLineResultStatusProviderModel
                    {
                        ProviderId = "222",
                        AllocationLineIds = new string[]{ "1"}
                    },
                    new UpdatePublishedAllocationLineResultStatusProviderModel
                    {
                        ProviderId = "333",
                        AllocationLineIds = new string[]{ "1"}
                    },
                    new UpdatePublishedAllocationLineResultStatusProviderModel
                    {
                        ProviderId = "444",
                        AllocationLineIds = new string[]{ "1"}
                    },
                    new UpdatePublishedAllocationLineResultStatusProviderModel
                    {
                        ProviderId = "555",
                        AllocationLineIds = new string[]{ "1"}
                    },
                    new UpdatePublishedAllocationLineResultStatusProviderModel
                    {
                        ProviderId = "666",
                        AllocationLineIds = new string[]{ "1"}
                    },
                    new UpdatePublishedAllocationLineResultStatusProviderModel
                    {
                        ProviderId = "777",
                        AllocationLineIds = new string[]{ "1"}
                    },
                    new UpdatePublishedAllocationLineResultStatusProviderModel
                    {
                        ProviderId = "888",
                        AllocationLineIds = new string[]{ "1"}
                    },
                    new UpdatePublishedAllocationLineResultStatusProviderModel
                    {
                        ProviderId = "999",
                        AllocationLineIds = new string[]{ "1"}
                    },
                    new UpdatePublishedAllocationLineResultStatusProviderModel
                    {
                        ProviderId = "000",
                        AllocationLineIds = new string[]{ "1"}
                    }

                };
            }
    }
}
