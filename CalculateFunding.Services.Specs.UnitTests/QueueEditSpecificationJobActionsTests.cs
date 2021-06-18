using CalculateFunding.Common.ApiClient.DataSets;
using CalculateFunding.Common.ApiClient.DataSets.Models;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Specs.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Specs.UnitTests
{
    [TestClass]
    public class QueueEditSpecificationJobActionsTests
    {
        private const string SpecificationIdKey = "specification-id";
        private const string FundingStreamIdKey = "fundingstream-id";
        private const string ProviderSnapshotIdKey = "providerSanpshot-id";
        private const string ProviderCacheKeyKey = "provider-cache-key";
        private const string SpecificationSummaryCacheKeyKey = "specification-summary-cache-key";
        private const string DisableQueueCalculationJobKey = "disableQueueCalculationJob";

        private QueueEditSpecificationJobActions _action;
        private IJobManagement _jobManagement;
        private IDatasetsApiClient _datasetsApiClient;
        private ISpecificationTemplateVersionChangedHandler _templateVersionChangedHandler;
        private ISpecificationsResiliencePolicies _specificationsResiliencePolicies;
        private ILogger _logger;
        private Reference _user;
        private string _userId;
        private string _userName;
        private string _correlationId;

        [TestInitialize]
        public void SetUp()
        {
            _jobManagement = Substitute.For<IJobManagement>();
            _datasetsApiClient = Substitute.For<IDatasetsApiClient>();
            _templateVersionChangedHandler = Substitute.For<ISpecificationTemplateVersionChangedHandler>();
            _specificationsResiliencePolicies = new SpecificationsResiliencePolicies
            {
                DatasetsApiClient = Policy.NoOpAsync()
            };
            _logger = Substitute.For<ILogger>();

            _userId = NewRandomString();
            _userName = NewRandomString();

            _user = NewReference(_ => _.WithId(_userId).WithName(_userName));
            _correlationId = NewRandomString();

            _action = new QueueEditSpecificationJobActions(_jobManagement, _datasetsApiClient, _specificationsResiliencePolicies, _templateVersionChangedHandler, _logger);

            _jobManagement.QueueJob(Arg.Any<JobCreateModel>())
                .Returns(new Job());//default instance as we assert was called but have null checks in the test now
        }

        [TestMethod]
        public async Task ShouldQueueProviderSnapshotDataLoadJobWhenSpecificationProviderSourceIsFDZ()
        {
            string fundingStreamId = NewRandomString();
            string specificationId = NewRandomString();
            int providerSnapshotId = NewRandomInt();
            bool disableQueueCalculationJob = NewRandomBoolean();

            SpecificationVersion specificationVersion = NewSpecificationVersion(_ => _.WithFundingStreamsIds(fundingStreamId)
                                                                                      .WithSpecificationId(specificationId)
                                                                                      .WithProviderSource(Models.Providers.ProviderSource.FDZ)
                                                                                      .WithProviderSnapshotId(providerSnapshotId));
            
            SpecificationVersion previousspecificationVersion = NewSpecificationVersion(_ => _.WithFundingStreamsIds(fundingStreamId)
                                                                                      .WithSpecificationId(specificationId)
                                                                                      .WithProviderSource(Models.Providers.ProviderSource.FDZ)
                                                                                      .WithProviderSnapshotId(providerSnapshotId));

            SpecificationEditModel editModel = new SpecificationEditModel();

            string editSpecificationJobId = NewRandomString();

            GivenEditSpecificationJobQueued(editSpecificationJobId);

            await WhenTheQueueEditSpecificationJobActionsIsRun(specificationVersion, previousspecificationVersion, editModel, _user, _correlationId, false, !disableQueueCalculationJob);

            await ThenEditSpecificationJobWasCreated(CreateJobModelMatching(_ => _.JobDefinitionId == JobConstants.DefinitionNames.EditSpecificationJob &&
                                            HasProperty(_, SpecificationIdKey, specificationId))
                );

            await ThenProviderSnapshotDataLoadJobWasCreated(
                CreateJobModelMatching(_ => _.JobDefinitionId == JobConstants.DefinitionNames.ProviderSnapshotDataLoadJob &&
                                            _.ParentJobId == editSpecificationJobId &&
                                            HasProperty(_, SpecificationIdKey, specificationId) &&
                                            HasProperty(_, FundingStreamIdKey, fundingStreamId) &&
                                            HasProperty(_, ProviderSnapshotIdKey, providerSnapshotId.ToString()) &&
                                            HasProperty(_, DisableQueueCalculationJobKey, disableQueueCalculationJob.ToString()))
                );
        }

        [TestMethod]
        public async Task ShouldQueueProviderSnapshotDataLoadJobWhenSpecificationSetLatestProviderVersionUpdatesChangeFromManualToUseLatest()
        {
            string fundingStreamId = NewRandomString();
            string specificationId = NewRandomString();
            int providerSnapshotId = NewRandomInt();
            bool disableQueueCalculationJob = NewRandomBoolean();

            SpecificationVersion specificationVersion = NewSpecificationVersion(_ => _.WithFundingStreamsIds(fundingStreamId)
                                                                                      .WithSpecificationId(specificationId)
                                                                                      .WithProviderSource(Models.Providers.ProviderSource.CFS)
                                                                                      .WithProviderSnapshotId(providerSnapshotId));

            SpecificationVersion previousspecificationVersion = NewSpecificationVersion(_ => _.WithFundingStreamsIds(fundingStreamId)
                                                                                      .WithSpecificationId(specificationId)
                                                                                      .WithProviderSource(Models.Providers.ProviderSource.FDZ)
                                                                                      .WithProviderSnapshotId(providerSnapshotId));

            SpecificationEditModel editModel = new SpecificationEditModel();

            string editSpecificationJobId = NewRandomString();

            GivenEditSpecificationJobQueued(editSpecificationJobId);


            await WhenTheQueueEditSpecificationJobActionsIsRun(specificationVersion, previousspecificationVersion, editModel, _user, _correlationId, true, !disableQueueCalculationJob);
            
            await ThenEditSpecificationJobWasCreated(CreateJobModelMatching(_ => _.JobDefinitionId == JobConstants.DefinitionNames.EditSpecificationJob &&
                                            HasProperty(_, SpecificationIdKey, specificationId))
                );
            
            await ThenProviderSnapshotDataLoadJobWasCreated(
                CreateJobModelMatching(_ => _.JobDefinitionId == JobConstants.DefinitionNames.ProviderSnapshotDataLoadJob &&
                                            _.ParentJobId == editSpecificationJobId &&
                                            HasProperty(_, SpecificationIdKey, specificationId) &&
                                            HasProperty(_, FundingStreamIdKey, fundingStreamId) &&
                                            HasProperty(_, ProviderSnapshotIdKey, providerSnapshotId.ToString()) &&
                                            HasProperty(_, DisableQueueCalculationJobKey, disableQueueCalculationJob.ToString()))
                );
        }

        [TestMethod]
        public async Task ShouldNotQueueProviderSnapshotDataLoadJobWhenSpecificationProviderSoruceIsCFS()
        {
            string fundingStreamId = NewRandomString();
            string specificationId = NewRandomString();
            int providerSnapshotId = NewRandomInt();
            bool disableQueueCalculationJob = NewRandomBoolean();

            SpecificationVersion specificationVersion = NewSpecificationVersion(_ => _.WithFundingStreamsIds(fundingStreamId)
                                                                                      .WithSpecificationId(specificationId)
                                                                                      .WithProviderSource(Models.Providers.ProviderSource.CFS)
                                                                                      .WithProviderSnapshotId(providerSnapshotId));

            SpecificationVersion previousspecificationVersion = NewSpecificationVersion(_ => _.WithFundingStreamsIds(fundingStreamId)
                                                                                      .WithSpecificationId(specificationId)
                                                                                      .WithProviderSource(Models.Providers.ProviderSource.FDZ)
                                                                                      .WithProviderSnapshotId(providerSnapshotId));

            SpecificationEditModel editModel = new SpecificationEditModel();

            string editSpecificationJobId = NewRandomString();

            GivenEditSpecificationJobQueued(editSpecificationJobId);

            await WhenTheQueueEditSpecificationJobActionsIsRun(specificationVersion, previousspecificationVersion, editModel, _user, _correlationId, false, !disableQueueCalculationJob);
            
            await ThenEditSpecificationJobWasCreated(CreateJobModelMatching(_ => _.JobDefinitionId == JobConstants.DefinitionNames.EditSpecificationJob &&
                                            HasProperty(_, SpecificationIdKey, specificationId))
                );

            await ThenProviderSnapshotDataLoadJobWasNotCreated(
                CreateJobModelMatching(_ => _.JobDefinitionId == JobConstants.DefinitionNames.ProviderSnapshotDataLoadJob &&
                                            _.ParentJobId == editSpecificationJobId &&
                                            HasProperty(_, SpecificationIdKey, specificationId) &&
                                            HasProperty(_, FundingStreamIdKey, fundingStreamId) &&
                                            HasProperty(_, ProviderSnapshotIdKey, providerSnapshotId.ToString()) &&
                                            HasProperty(_, DisableQueueCalculationJobKey, disableQueueCalculationJob.ToString()))
                );
        }

        [TestMethod]
        public async Task ShouldQueueMapScopedDatasetJobWhenSpecificationProviderSourceIsCFSAndHaveProviderVersionId()
        {
            string fundingStreamId = NewRandomString();
            string specificationId = NewRandomString();
            string providerVersionId = NewRandomString();
            string datasetId = NewRandomString();
            bool disableQueueCalculationJob = NewRandomBoolean();

            SpecificationVersion specificationVersion = NewSpecificationVersion(_ => _.WithFundingStreamsIds(fundingStreamId)
                                                                                      .WithSpecificationId(specificationId)
                                                                                      .WithProviderSource(Models.Providers.ProviderSource.CFS)
                                                                                      .WithProviderVersionId(providerVersionId));

            SpecificationVersion previousspecificationVersion = NewSpecificationVersion(_ => _.WithFundingStreamsIds(fundingStreamId)
                                                                                      .WithSpecificationId(specificationId)
                                                                                      .WithProviderSource(Models.Providers.ProviderSource.FDZ));

            SpecificationEditModel editModel = new SpecificationEditModel();

            GivenTheDatasetSpecificationRelationship(specificationId, datasetId); 

            AndTheDataset(datasetId);
            await WhenTheQueueEditSpecificationJobActionsIsRun(specificationVersion, previousspecificationVersion, editModel, _user, _correlationId, false, !disableQueueCalculationJob);

            await ThenEditSpecificationJobWasCreated(CreateJobModelMatching(_ => _.JobDefinitionId == JobConstants.DefinitionNames.EditSpecificationJob &&
                                            HasProperty(_, SpecificationIdKey, specificationId))
                );

            await ThenProviderSnapshotDataLoadJobWasCreated(
                CreateJobModelMatching(_ => _.JobDefinitionId == JobConstants.DefinitionNames.MapScopedDatasetJob &&
                                            HasProperty(_, SpecificationIdKey, specificationId) &&
                                            HasProperty(_, ProviderCacheKeyKey, $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}") &&
                                            HasProperty(_, SpecificationSummaryCacheKeyKey, $"{CacheKeys.SpecificationSummaryById}{specificationId}") &&
                                            HasProperty(_, DisableQueueCalculationJobKey, disableQueueCalculationJob.ToString()))
                );
        }

        private void GivenTheDatasetSpecificationRelationship(string specificationId, string datasetId)
        {
            _datasetsApiClient
                .GetRelationshipsBySpecificationId(specificationId)
                .Returns(new ApiResponse<IEnumerable<DatasetSpecificationRelationshipViewModel>>(HttpStatusCode.OK, new[] {new DatasetSpecificationRelationshipViewModel {
                    IsProviderData = true,
                    DatasetId = datasetId
                } }));
        }

        private void AndTheDataset(string datasetId)
        {
            _datasetsApiClient
                .GetDatasetByDatasetId(datasetId)
                .Returns(new ApiResponse<DatasetViewModel>(HttpStatusCode.OK, new DatasetViewModel
                {
                   Id = datasetId
                }));
        }

        private async Task ThenEditSpecificationJobWasCreated(Expression<Predicate<JobCreateModel>> expectedJob)
        {
            await _jobManagement.Received(1).QueueJob(
                Arg.Is(expectedJob));
        }

        private async Task ThenProviderSnapshotDataLoadJobWasCreated(Expression<Predicate<JobCreateModel>> expectedJob)
        {
            await _jobManagement.Received(1).QueueJob(
                Arg.Is(expectedJob));
        }

        private async Task ThenProviderSnapshotDataLoadJobWasNotCreated(Expression<Predicate<JobCreateModel>> expectedJob)
        {
            await _jobManagement.Received(0).QueueJob(
                Arg.Is(expectedJob));
        }

        private Expression<Predicate<JobCreateModel>> CreateJobModelMatching(Predicate<JobCreateModel> extraChecks)
        {
            return _ => _.CorrelationId == _correlationId &&
                        _.InvokerUserId == _userId &&
                        _.InvokerUserDisplayName == _userName &&
                        extraChecks(_);
        }

        private bool HasProperty(JobCreateModel jobCreateModel,
            string key,
            string value)
        {
            return jobCreateModel.Properties.TryGetValue(key, out string matchValue1)
                   && matchValue1 == value;
        }

        private void AndEditSpecificationJobQueued(string jobId)
        {
            GivenEditSpecificationJobQueued(jobId);
        }

        private void GivenEditSpecificationJobQueued(string jobId)
        {
            _jobManagement
                .QueueJob(Arg.Is<JobCreateModel>(_ => _.JobDefinitionId == JobConstants.DefinitionNames.EditSpecificationJob))
                .Returns(new Job { Id = jobId });
        }

        private async Task WhenTheQueueEditSpecificationJobActionsIsRun(SpecificationVersion specificationVersion,
            SpecificationVersion previousSpecificationVersion,
            SpecificationEditModel editModel,
            Reference user,
            string correlationId,
            bool triggerProviderSnapshotDataLoadJob,
            bool triggerCalculationEngineRunJob)
        {
            await _action.Run(specificationVersion,
                previousSpecificationVersion,
                editModel,
                user,
                correlationId,
                triggerProviderSnapshotDataLoadJob,
                triggerCalculationEngineRunJob);
        }

        private string NewRandomString() => new RandomString();
        private int NewRandomInt() => new RandomNumberBetween(1, 10000);
        protected bool NewRandomBoolean() => new RandomBoolean();

        private Reference NewReference(Action<ReferenceBuilder> setUp = null)
        {
            ReferenceBuilder referenceBuilder = new ReferenceBuilder();

            setUp?.Invoke(referenceBuilder);

            return referenceBuilder.Build();
        }

        private SpecificationVersion NewSpecificationVersion(Action<SpecificationVersionBuilder> setUp = null)
        {
            SpecificationVersionBuilder specificationVersionBuilder = new SpecificationVersionBuilder();

            setUp?.Invoke(specificationVersionBuilder);

            return specificationVersionBuilder.Build();
        }
    }
}
