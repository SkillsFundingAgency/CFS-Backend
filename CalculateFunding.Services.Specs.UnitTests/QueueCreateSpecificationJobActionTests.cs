using System;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Tests.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;
using Serilog;
using Calculation = CalculateFunding.Common.TemplateMetadata.Models.Calculation;

namespace CalculateFunding.Services.Specs.UnitTests
{
    [TestClass]
    public class QueueCreateSpecificationJobActionTests
    {
        private const string AssignTemplateCalculationsJob = JobConstants.DefinitionNames.AssignTemplateCalculationsJob;
        private const string SpecificationId = "specification-id";
        private const string FundingStreamId = "fundingstream-id";
        private const string FundingPeriodId = "fundingperiod-id";
        private const string TemplateVersion = "template-version";
        
        private IPoliciesApiClient _policies;
        private IJobManagement _jobs;

        private QueueCreateSpecificationJobAction _action;
        private Reference _user;
        private string _userId;
        private string _userName;
        private string _correlationId;

        [TestInitialize]
        public void SetUp()
        {
            _policies = Substitute.For<IPoliciesApiClient>();
            _jobs = Substitute.For<IJobManagement>();

            _userId = NewRandomString();
            _userName = NewRandomString();

            _user = NewReference(_ => _.WithId(_userId)
                .WithName(_userName));

            _correlationId = NewRandomString();

            _action = new QueueCreateSpecificationJobAction(_policies,
                _jobs,
                new SpecificationsResiliencePolicies
                {
                    JobsApiClient = Policy.NoOpAsync(),
                    PoliciesApiClient = Policy.NoOpAsync()
                },
                Substitute.For<ILogger>());

            _jobs.QueueJob(Arg.Any<JobCreateModel>())
                .Returns(new Job());//default instance as we assert was called but have null checks in the test now
        }

        [TestMethod]
        public async Task QueuesParentJobAndAssignCalculationsJobsWhereConfigurationHasADefaultTemplateVersion()
        {
            string fundingStream1 = NewRandomString();
            string fundingStream2 = NewRandomString();
            string fundingStream3 = NewRandomString();

            string specificationId = NewRandomString();
            string[] fundingStreamIds =
            {
                fundingStream1,
                fundingStream2,
                fundingStream3
            };

            string templateVersion1 = NewRandomString();
            string templateVersion2 = NewRandomString();

            string fundingPeriodId = NewRandomString();

            uint templateCalculationId1 = NewRandomUint();
            uint templateCalculationId2 = NewRandomUint();
            uint templateCalculationId3 = NewRandomUint();
            uint templateCalculationId4 = NewRandomUint();
            uint templateCalculationId5 = NewRandomUint();

            SpecificationVersion specificationVersion = NewSpecificationVersion(_ => _.WithSpecificationId(specificationId)
                .WithFundingStreamsIds(fundingStreamIds)
                .WithFundingPeriodId(fundingPeriodId)
                .WithTemplateIds((fundingStream1, templateVersion1), (fundingStream3, templateVersion2)));

            string expectedParentJobId = NewRandomString();

            Job createSpecificationJob = NewJob(_ => _.WithId(expectedParentJobId));

            GivenTheFundingTemplateContentsForPeriodAndStream(fundingPeriodId, fundingStream1,
                templateVersion1,
                 NewTemplateMetadataContents(_ => _.WithFundingLines(
                     NewFundingLine(fl => fl.WithCalculations(NewCalculation(cal => cal.WithReferenceData(NewReferenceData(), NewReferenceData()).WithTemplateCalculationId(templateCalculationId1)))),
                     NewFundingLine(fl => fl.WithCalculations(NewCalculation(cal => cal.WithTemplateCalculationId(templateCalculationId2)), NewCalculation(cal => cal.WithTemplateCalculationId(templateCalculationId3))))))); //item count 5
            AndTheFundingTemplateContentsForPeriodAndStream(fundingPeriodId, fundingStream2, string.Empty, NewTemplateMetadataContents());
            AndTheFundingTemplateContentsForPeriodAndStream(fundingPeriodId, fundingStream3,
                templateVersion2,
                NewTemplateMetadataContents(_ => _.WithFundingLines(
                    NewFundingLine(fl => fl.WithCalculations(NewCalculation(cal => cal.WithReferenceData(NewReferenceData()).WithTemplateCalculationId(templateCalculationId4)))),
                    NewFundingLine(fl => fl.WithCalculations(NewCalculation(cal => cal.WithTemplateCalculationId(templateCalculationId5))))))); //item count 3


            AndTheJobIsCreatedForARequestModelMatching(CreateJobModelMatching(_ => _.JobDefinitionId == JobConstants.DefinitionNames.CreateSpecificationJob &&
                                                                                  HasProperty(_, SpecificationId, specificationId) &&
                                                                                  _.ParentJobId == null),
                createSpecificationJob);

            await WhenTheQueueCreateSpecificationJobActionIsRun(specificationVersion, _user, _correlationId);

            await ThenTheAssignTemplateCalculationJobWasCreated(
                CreateJobModelMatching(_ => _.JobDefinitionId == AssignTemplateCalculationsJob &&
                                            _.ParentJobId == expectedParentJobId &&
                                            _.ItemCount == 5 &&
                                            HasProperty(_, TemplateVersion, templateVersion1) && 
                                            HasProperty(_, SpecificationId, specificationId) &&
                                            HasProperty(_, FundingStreamId, fundingStream1) &&
                                            HasProperty(_, FundingPeriodId, fundingPeriodId)));
            await AndTheAssignTemplateCalculationJobWasCreated(
                CreateJobModelMatching(_ => _.JobDefinitionId == AssignTemplateCalculationsJob &&
                                            _.ParentJobId == expectedParentJobId &&
                                            _.ItemCount == 3 &&
                                            HasProperty(_, TemplateVersion, templateVersion2) && 
                                            HasProperty(_, SpecificationId, specificationId) &&
                                            HasProperty(_, FundingStreamId, fundingStream3) &&
                                            HasProperty(_, FundingPeriodId, fundingPeriodId)));
            await AndTheAssignTemplateCalculationJobWasNotCreated(
                CreateJobModelMatching(_ => _.JobDefinitionId == AssignTemplateCalculationsJob &&
                                            _.ParentJobId == expectedParentJobId &&
                                            HasProperty(_, SpecificationId, specificationId) &&
                                            HasProperty(_, FundingStreamId, fundingStream2) &&
                                            HasProperty(_, FundingPeriodId, fundingPeriodId)));
        }

        private Expression<Predicate<JobCreateModel>> CreateJobModelMatching(Predicate<JobCreateModel> extraChecks)
        {
            return _ => _.CorrelationId == _correlationId &&
                        _.InvokerUserId == _userId &&
                        _.InvokerUserDisplayName == _userName &&
                        extraChecks(_);
        }

        private static string NewRandomString()
        {
            return new RandomString();
        }

        private static uint NewRandomUint()
        {
            return (uint) new RandomNumberBetween(0, int.MaxValue);
        }

        private bool HasProperty(JobCreateModel jobCreateModel,
            string key,
            string value)
        {
            return jobCreateModel.Properties.TryGetValue(key, out string matchValue1)
                   && matchValue1 == value;
        }

        private async Task WhenTheQueueCreateSpecificationJobActionIsRun(SpecificationVersion specificationVersion,
            Reference user,
            string correlationId)
        {
            await _action.Run(specificationVersion, user, correlationId);
        }
        private void GivenTheFundingTemplateContentsForPeriodAndStream(string fundingPeriodId,
           string fundingStreamId,
           string templateVersionId,
           TemplateMetadataContents templateContents)
        {
            _policies.GetFundingTemplateContents(fundingStreamId, fundingPeriodId, templateVersionId)
                .Returns(new ApiResponse<TemplateMetadataContents>(HttpStatusCode.OK, templateContents));
        }

        private void AndTheFundingTemplateContentsForPeriodAndStream(string fundingPeriodId,
            string fundingStreamId,
           string templateVersionId,
            TemplateMetadataContents templateContents)
        {
            GivenTheFundingTemplateContentsForPeriodAndStream(fundingPeriodId, fundingStreamId, templateVersionId, templateContents);
        }

        private void AndTheJobIsCreatedForARequestModelMatching(Expression<Predicate<JobCreateModel>> jobCreateModelMatching, Job job)
        {
            _jobs.QueueJob(Arg.Is(jobCreateModelMatching))
                .Returns(job);
        }

        private async Task ThenTheAssignTemplateCalculationJobWasCreated(Expression<Predicate<JobCreateModel>> expectedJob)
        {
            await _jobs.Received(1).QueueJob(
                Arg.Is(expectedJob));
        }

        private async Task AndTheAssignTemplateCalculationJobWasCreated(Expression<Predicate<JobCreateModel>> expectedJob)
        {
            await ThenTheAssignTemplateCalculationJobWasCreated(expectedJob);
        }

        private async Task AndTheAssignTemplateCalculationJobWasNotCreated(Expression<Predicate<JobCreateModel>> expectedJob)
        {
            await _jobs.Received(0).QueueJob(
                Arg.Is(expectedJob));
        }

        private Job NewJob(Action<JobBuilder> setUp = null)
        {
            JobBuilder jobBuilder = new JobBuilder();

            setUp?.Invoke(jobBuilder);

            return jobBuilder.Build();
        }

        private FundingConfiguration NewFundingConfiguration(Action<FundingConfigurationBuilder> setUp = null)
        {
            FundingConfigurationBuilder fundingConfigurationBuilder = new FundingConfigurationBuilder();

            setUp?.Invoke(fundingConfigurationBuilder);

            return fundingConfigurationBuilder.Build();
        }

        private SpecificationVersion NewSpecificationVersion(Action<SpecificationVersionBuilder> setUp = null)
        {
            SpecificationVersionBuilder specificationVersionBuilder = new SpecificationVersionBuilder();

            setUp?.Invoke(specificationVersionBuilder);

            return specificationVersionBuilder.Build();
        }

        private Reference NewReference(Action<ReferenceBuilder> setUp = null)
        {
            ReferenceBuilder referenceBuilder = new ReferenceBuilder();

            setUp?.Invoke(referenceBuilder);

            return referenceBuilder.Build();
        }

        private TemplateMetadataContents NewTemplateMetadataContents(Action<TemplateMetadataContentsBuilder> setUp = null)
        {
            TemplateMetadataContentsBuilder contentsBuilder = new TemplateMetadataContentsBuilder();

            setUp?.Invoke(contentsBuilder);

            return contentsBuilder.Build();
        }

        private FundingLine NewFundingLine(Action<FundingLineBuilder> setUp = null)
        {
            FundingLineBuilder fundingLineBuilder = new FundingLineBuilder();

            setUp?.Invoke(fundingLineBuilder);

            return fundingLineBuilder.Build();
        }

        private Calculation NewCalculation(Action<CalculationBuilder> setUp = null)
        {
            CalculationBuilder calculationBuilder = new CalculationBuilder();

            setUp?.Invoke(calculationBuilder);

            return calculationBuilder.Build();
        }

        private ReferenceData NewReferenceData()
        {
            return new ReferenceDataBuilder()
                .Build();
        }
    }
}