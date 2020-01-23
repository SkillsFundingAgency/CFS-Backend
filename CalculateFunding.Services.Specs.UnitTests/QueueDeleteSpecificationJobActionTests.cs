using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Messages;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Tests.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;
using Serilog;
using Job = CalculateFunding.Common.ApiClient.Jobs.Models.Job;
using JobCreateModel = CalculateFunding.Common.ApiClient.Jobs.Models.JobCreateModel;

namespace CalculateFunding.Services.Specs.UnitTests
{
    [TestClass]
    public class QueueDeleteSpecificationJobActionTests
    {
        private const string DeleteSpecificationJob = JobConstants.DefinitionNames.DeleteSpecificationJob;
        private const string SpecificationId = "specification-id";
        private const string Deletion = "deletion-type";
        private readonly Dictionary<string, string> _specificationChildJobDefinitions = new Dictionary<string, string>
        {
            [JobConstants.DefinitionNames.DeleteCalculationResultsJob] = "Deleting Calculation Results",
            [JobConstants.DefinitionNames.DeleteCalculationsJob] = "Deleting Calculations",
            [JobConstants.DefinitionNames.DeleteDatasetsJob] = "Deleting Datasets",
            [JobConstants.DefinitionNames.DeleteTestResultsJob] = "Deleting Test Results",
            [JobConstants.DefinitionNames.DeleteTestsJob] = "Deleting Tests",
            [JobConstants.DefinitionNames.DeleteJobsJob] = "Deleting Jobs"
        };
        private IJobsApiClient _jobs;
        private QueueDeleteSpecificationJobAction _action;
        private Reference _user;
        private string _userId;
        private string _userName;
        private string _correlationId;

        [TestInitialize]
        public void SetUp()
        {
            _jobs = Substitute.For<IJobsApiClient>();

            _userId = NewRandomString();
            _userName = NewRandomString();

            _user = NewReference(_ => _.WithId(_userId)
                .WithName(_userName));

            _correlationId = NewRandomString();

            _action = new QueueDeleteSpecificationJobAction(
                _jobs,
                new SpecificationsResiliencePolicies
                {
                    JobsApiClient = Policy.NoOpAsync(),
                    PoliciesApiClient = Policy.NoOpAsync()
                },
                Substitute.For<ILogger>());

            _jobs.CreateJob(Arg.Any<JobCreateModel>())
                .Returns(new Job());//default instance as we assert was called but have null checks in the test now
        }

        [TestMethod]
        [DataRow(DeletionType.SoftDelete)]
        [DataRow(DeletionType.PermanentDelete)]
        public async Task QueuesParentJobAndAssignCalculationsJobsWhereConfigurationHasADefaultTemplateVersion(DeletionType deletionType)
        {
            string specificationId = NewRandomString();
            string expectedParentJobId = NewRandomString();
            string deletionTypeValue = deletionType.ToString("D");
            SetupDeleteSpecificationJob(expectedParentJobId, specificationId);
            SetupDeleteSpecificationChildJobs(specificationId, expectedParentJobId);
            var deleteSpecificationParentJob = CreateJobModelMatching(_ =>
                _.JobDefinitionId == DeleteSpecificationJob &&
                _.ParentJobId == null &&
                _.ItemCount == null &&
                HasProperty(_, SpecificationId, specificationId) &&
                HasProperty(_, Deletion, deletionTypeValue)
            );

            await WhenTheQueueDeleteSpecificationJobActionIsRun(specificationId, _user, _correlationId, deletionType);

            await ThenTheDeleteSpecificationJobIsCreated(deleteSpecificationParentJob);
            await AndThenTheSpecificationChildJobsWereCreated(specificationId, deletionTypeValue, expectedParentJobId);
        }

        private void SetupDeleteSpecificationJob(string expectedParentJobId, string specificationId)
        {
            Job createSpecificationJob = NewJob(jobBuilder => jobBuilder.WithId(expectedParentJobId));
            WhenJobIsCreateForARequestModelMatching(
                CreateJobModelMatching(jobCreateModel =>
                    jobCreateModel.JobDefinitionId == DeleteSpecificationJob &&
                    HasProperty(jobCreateModel, SpecificationId, specificationId) &&
                    jobCreateModel.ParentJobId == null),
                createSpecificationJob);
        }

        private void SetupDeleteSpecificationChildJobs(string specificationId, string parentJobId)
        {
            foreach (var specificationChildJobDefinition in _specificationChildJobDefinitions)
            {
                Job job = NewJob(jobBuilder => jobBuilder.WithId(NewRandomString()));
                job.ParentJobId = parentJobId;

                WhenJobIsCreateForARequestModelMatching(
                    CreateJobModelMatching(jobCreateModel =>
                        jobCreateModel.JobDefinitionId == specificationChildJobDefinition.Key &&
                        jobCreateModel.ParentJobId == parentJobId &&
                        HasProperty(jobCreateModel, SpecificationId, specificationId)),
                    job);
            }
        }

        private async Task ThenTheDeleteSpecificationJobIsCreated(Expression<Predicate<JobCreateModel>> expectedJob)
        {
            await _jobs.Received(1).CreateJob(
                Arg.Is(expectedJob));
        }

        private async Task AndThenTheSpecificationChildJobsWereCreated(string specificationId, string deletionTypeValue, string expectedParentJobId)
        {
            foreach (var specificationChildJobDefinition in _specificationChildJobDefinitions)
            {
                await ThenTheDeleteSpecificationJobIsCreated(
                    CreateJobModelMatching(_ => _.JobDefinitionId == specificationChildJobDefinition.Key &&
                                                _.ParentJobId == expectedParentJobId &&
                                                _.ItemCount == null &&
                                                HasProperty(_, SpecificationId, specificationId) &&
                                                HasProperty(_, Deletion, deletionTypeValue)
                    ));
            }
        }

        private Expression<Predicate<JobCreateModel>> CreateJobModelMatching(Predicate<JobCreateModel> extraChecks)
        {
            return _ => _.CorrelationId == _correlationId &&
                        _.InvokerUserId == _userId &&
                        _.InvokerUserDisplayName == _userName &&
                        extraChecks(_);
        }

        private async Task WhenTheQueueDeleteSpecificationJobActionIsRun(
            string specificationId, 
            Reference user, 
            string correlationId, 
            DeletionType deletionType)
        {
            await _action.Run(specificationId, user, correlationId, deletionType);
        }

        private void WhenJobIsCreateForARequestModelMatching(Expression<Predicate<JobCreateModel>> jobCreateModelMatching, Job job)
        {
            _jobs.CreateJob(Arg.Is(jobCreateModelMatching))
                .Returns(job);
        }

        private Job NewJob(Action<JobBuilder> setUp = null)
        {
            JobBuilder jobBuilder = new JobBuilder();

            setUp?.Invoke(jobBuilder);

            return jobBuilder.Build();
        }

        private Reference NewReference(Action<ReferenceBuilder> setUp = null)
        {
            ReferenceBuilder referenceBuilder = new ReferenceBuilder();

            setUp?.Invoke(referenceBuilder);

            return referenceBuilder.Build();
        }

        private static string NewRandomString() => new RandomString();

        private bool HasProperty(JobCreateModel jobCreateModel, string key, string value) => 
            jobCreateModel.Properties.TryGetValue(key, out string matchValue1) && matchValue1 == value;
    }
}