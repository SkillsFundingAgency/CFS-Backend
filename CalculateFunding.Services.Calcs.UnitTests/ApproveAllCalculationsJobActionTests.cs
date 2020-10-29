using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Services.Calcs;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Tests.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using Job = CalculateFunding.Common.ApiClient.Jobs.Models.Job;
using JobCreateModel = CalculateFunding.Common.ApiClient.Jobs.Models.JobCreateModel;

namespace CalculateFunding.Services.Calcs.UnitTests
{
    [TestClass]
    public class ApproveAllCalculationsJobActionTests
    {
        private const string ApproveAllCalculationsJob = JobConstants.DefinitionNames.ApproveAllCalculationsJob;
        private const string SpecificationId = "specification-id";
        
        private IJobManagement _jobs;
        private ApproveAllCalculationsJobAction _action;
        private Reference _user;
        private string _userId;
        private string _userName;
        private string _correlationId;

        [TestInitialize]
        public void SetUp()
        {
            _jobs = Substitute.For<IJobManagement>();

            _userId = NewRandomString();
            _userName = NewRandomString();

            _user = NewReference(_ => _.WithId(_userId)
                .WithName(_userName));

            _correlationId = NewRandomString();

            _action = new ApproveAllCalculationsJobAction(
                _jobs,
                Substitute.For<ILogger>());

            _jobs.QueueJob(Arg.Any<JobCreateModel>())
                .Returns(new Job());//default instance as we assert was called but have null checks in the test now
        }

        [TestMethod]
        public async Task QueuesJobAndAssignCalculationsJobs()
        {
            string specificationId = NewRandomString();
            string expectedParentJobId = NewRandomString();
            SetupApproveAllCalculationsJob(expectedParentJobId, specificationId);
            var approveAllCalculationsJob = CreateJobModelMatching(_ =>
                _.JobDefinitionId == ApproveAllCalculationsJob &&
                _.ParentJobId == null &&
                _.ItemCount == null &&
                HasProperty(_, SpecificationId, specificationId)
            );

            await WhenTheApproveAllCalculationsJobActionIsRun(specificationId, _user, _correlationId);

            await ThenTheApproveAllCalculationsJobIsCreated(approveAllCalculationsJob);
        }

        private void SetupApproveAllCalculationsJob(string expectedParentJobId, string specificationId)
        {
            Job createSpecificationJob = NewJob(jobBuilder => jobBuilder.WithId(expectedParentJobId));
            WhenJobIsCreateForARequestModelMatching(
                CreateJobModelMatching(jobCreateModel =>
                    jobCreateModel.JobDefinitionId == ApproveAllCalculationsJob &&
                    HasProperty(jobCreateModel, SpecificationId, specificationId) &&
                    jobCreateModel.ParentJobId == null),
                createSpecificationJob);
        }

        private async Task ThenTheApproveAllCalculationsJobIsCreated(Expression<Predicate<JobCreateModel>> expectedJob)
        {
            await _jobs.Received(1).QueueJob(
                Arg.Is(expectedJob));
        }

        private Expression<Predicate<JobCreateModel>> CreateJobModelMatching(Predicate<JobCreateModel> extraChecks)
        {
            return _ => _.CorrelationId == _correlationId &&
                        _.InvokerUserId == _userId &&
                        _.InvokerUserDisplayName == _userName &&
                        extraChecks(_);
        }

        private async Task WhenTheApproveAllCalculationsJobActionIsRun(
            string specificationId, 
            Reference user, 
            string correlationId)
        {
            await _action.Run(specificationId, user, correlationId);
        }

        private void WhenJobIsCreateForARequestModelMatching(Expression<Predicate<JobCreateModel>> jobCreateModelMatching, Job job)
        {
            _jobs.QueueJob(Arg.Is(jobCreateModelMatching))
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