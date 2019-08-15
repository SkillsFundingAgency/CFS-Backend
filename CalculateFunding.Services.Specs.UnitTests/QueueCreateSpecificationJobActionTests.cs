using System;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Tests.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;
using Serilog;

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
        private IJobsApiClient _jobs;

        private QueueCreateSpecificationJobAction _action;
        private Reference _user;
        private string _userId;
        private string _userName;
        private string _correlationId;

        [TestInitialize]
        public void SetUp()
        {
            _policies = Substitute.For<IPoliciesApiClient>();
            _jobs = Substitute.For<IJobsApiClient>();

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

            _jobs.CreateJob(Arg.Any<JobCreateModel>())
                .Returns(new Job());//default instance as we assert was called but have null checks in the test now
        }

        [TestMethod]
        public async Task QueuesParentJobAndAssignCalculationsJobsWhereConfigurationHasADefaultTemplateVersion()
        {
            string fundingSteam1 = NewRandomString();
            string fundingSteam2 = NewRandomString();
            string fundingSteam3 = NewRandomString();

            string specificationId = NewRandomString();
            string[] fundingStreamIds =
            {
                fundingSteam1,
                fundingSteam2,
                fundingSteam3
            };

            string templateVersion1 = NewRandomString();
            string templateVersion2 = NewRandomString();

            string fundingPeriodId = NewRandomString();

            SpecificationVersion specificationVersion = NewSpecificationVersion(_ => _.WithSpecificationId(specificationId)
                .WithFundingStreamsIds(fundingStreamIds)
                .WithFundingPeriodId(fundingPeriodId));

            string expectedParentJobId = NewRandomString();

            Job createSpecificationJob = NewJob(_ => _.WithId(expectedParentJobId));

            GivenTheFundingConfigurationForPeriodAndStream(fundingPeriodId, fundingSteam1,
                NewFundingConfiguration(_ => _.WithDefaultTemplateVersion(templateVersion1)
                    .WithFundingStreamId(fundingSteam1)
                    .WithFundingPeriodId(fundingPeriodId)),
                NewTemplateMetadataContents(_ => _.WithFundingLines(
                    NewFundingLine(fl => fl.WithCalculations(NewCalculation(cal => cal.WithReferenceData(NewReferenceData(), NewReferenceData())))),
                    NewFundingLine(fl => fl.WithCalculations(NewCalculation(), NewCalculation()))))); //item count 5
            AndTheFundingConfigurationForPeriodAndStream(fundingPeriodId, fundingSteam2, NewFundingConfiguration(), NewTemplateMetadataContents());
            AndTheFundingConfigurationForPeriodAndStream(fundingPeriodId, fundingSteam3,
                NewFundingConfiguration(_ => _.WithDefaultTemplateVersion(templateVersion2)
                    .WithFundingStreamId(fundingSteam3)
                    
                    .WithFundingPeriodId(fundingPeriodId)),
                NewTemplateMetadataContents(_ => _.WithFundingLines(
                    NewFundingLine(fl => fl.WithCalculations(NewCalculation(cal => cal.WithReferenceData(NewReferenceData())))),
                    NewFundingLine(fl => fl.WithCalculations(NewCalculation()))))); //item count 3
            AndTheJobIsCreateForARequestModelMatching(CreateJobModelMatching(_ => _.JobDefinitionId == JobConstants.DefinitionNames.CreateSpecificationJob &&
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
                                            HasProperty(_, FundingStreamId, fundingSteam1) &&
                                            HasProperty(_, FundingPeriodId, fundingPeriodId)));
            await AndTheAssignTemplateCalculationJobWasCreated(
                CreateJobModelMatching(_ => _.JobDefinitionId == AssignTemplateCalculationsJob &&
                                            _.ParentJobId == expectedParentJobId &&
                                            _.ItemCount == 3 &&
                                            HasProperty(_, TemplateVersion, templateVersion2) && 
                                            HasProperty(_, SpecificationId, specificationId) &&
                                            HasProperty(_, FundingStreamId, fundingSteam3) &&
                                            HasProperty(_, FundingPeriodId, fundingPeriodId)));
            await AndTheAssignTemplateCalculationJobWasNotCreated(
                CreateJobModelMatching(_ => _.JobDefinitionId == AssignTemplateCalculationsJob &&
                                            _.ParentJobId == expectedParentJobId &&
                                            HasProperty(_, SpecificationId, specificationId) &&
                                            HasProperty(_, FundingStreamId, fundingSteam2) &&
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

        private void GivenTheFundingConfigurationForPeriodAndStream(string fundingPeriodId,
            string fundingStreamId,
            FundingConfiguration fundingConfiguration,
            TemplateMetadataContents templateContents)
        {
            _policies.GetFundingConfiguration(fundingStreamId, fundingPeriodId)
                .Returns(new ApiResponse<FundingConfiguration>(HttpStatusCode.OK, fundingConfiguration));
            _policies.GetFundingTemplateContents(fundingStreamId, fundingConfiguration.DefaultTemplateVersion)
                .Returns(new ApiResponse<TemplateMetadataContents>(HttpStatusCode.OK, templateContents));
        }

        private void AndTheFundingConfigurationForPeriodAndStream(string fundingPeriodId,
            string fundingStreamId,
            FundingConfiguration fundingConfiguration,
            TemplateMetadataContents templateContents)
        {
            GivenTheFundingConfigurationForPeriodAndStream(fundingPeriodId, fundingStreamId, fundingConfiguration, templateContents);
        }

        private void AndTheJobIsCreateForARequestModelMatching(Expression<Predicate<JobCreateModel>> jobCreateModelMatching, Job job)
        {
            _jobs.CreateJob(Arg.Is(jobCreateModelMatching))
                .Returns(job);
        }

        private async Task ThenTheAssignTemplateCalculationJobWasCreated(Expression<Predicate<JobCreateModel>> expectedJob)
        {
            await _jobs.Received(1).CreateJob(
                Arg.Is(expectedJob));
        }

        private async Task AndTheAssignTemplateCalculationJobWasCreated(Expression<Predicate<JobCreateModel>> expectedJob)
        {
            await ThenTheAssignTemplateCalculationJobWasCreated(expectedJob);
        }

        private async Task AndTheAssignTemplateCalculationJobWasNotCreated(Expression<Predicate<JobCreateModel>> expectedJob)
        {
            await _jobs.Received(0).CreateJob(
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