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

        private QueueEditSpecificationJobActions _action;
        private IJobManagement _jobManagement;
        private IDatasetsApiClient _datasetsApiClient;
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
            _specificationsResiliencePolicies = new SpecificationsResiliencePolicies
            {
                DatasetsApiClient = Policy.NoOpAsync()
            };
            _logger = Substitute.For<ILogger>();

            _userId = NewRandomString();
            _userName = NewRandomString();

            _user = NewReference(_ => _.WithId(_userId).WithName(_userName));
            _correlationId = NewRandomString();

            _action = new QueueEditSpecificationJobActions(_jobManagement, _datasetsApiClient, _specificationsResiliencePolicies, _logger);

            _jobManagement.QueueJob(Arg.Any<JobCreateModel>())
                .Returns(new Job());//default instance as we assert was called but have null checks in the test now
        }

        [TestMethod]
        public async Task ShouldQueueProviderSnapshotDataLoadJobWhenSpecificationProviderSoruceIsFDZ()
        {
            string fundingStreamId = NewRandomString();
            string specificationId = NewRandomString();
            int providerSnapshotId = NewRandomInt();
            SpecificationVersion specificationVersion = NewSpecificationVersion(_ => _.WithFundingStreamsIds(fundingStreamId)
                                                                                      .WithSpecificationId(specificationId)
                                                                                      .WithProviderSource(Models.Providers.ProviderSource.FDZ)
                                                                                      .WithProviderSnapshotId(providerSnapshotId));

            await WhenTheQueueEditSpecificationJobActionsIsRun(specificationVersion, _user, _correlationId, false);

            await ThenProviderSnapshotDataLoadJobWasCreated(
                CreateJobModelMatching(_ => _.JobDefinitionId == JobConstants.DefinitionNames.ProviderSnapshotDataLoadJob &&
                                            HasProperty(_, SpecificationIdKey, specificationId) &&
                                            HasProperty(_, FundingStreamIdKey, fundingStreamId) &&
                                            HasProperty(_, ProviderSnapshotIdKey, providerSnapshotId.ToString()))
                );
        }

        [TestMethod]
        public async Task ShouldQueueProviderSnapshotDataLoadJobWhenSpecificationSetLatestProviderVersionUpdatesChangeFromManualToUseLatest()
        {
            string fundingStreamId = NewRandomString();
            string specificationId = NewRandomString();
            int providerSnapshotId = NewRandomInt();
            SpecificationVersion specificationVersion = NewSpecificationVersion(_ => _.WithFundingStreamsIds(fundingStreamId)
                                                                                      .WithSpecificationId(specificationId)
                                                                                      .WithProviderSource(Models.Providers.ProviderSource.CFS)
                                                                                      .WithProviderSnapshotId(providerSnapshotId));

            await WhenTheQueueEditSpecificationJobActionsIsRun(specificationVersion, _user, _correlationId, true);

            await ThenProviderSnapshotDataLoadJobWasCreated(
                CreateJobModelMatching(_ => _.JobDefinitionId == JobConstants.DefinitionNames.ProviderSnapshotDataLoadJob &&
                                            HasProperty(_, SpecificationIdKey, specificationId) &&
                                            HasProperty(_, FundingStreamIdKey, fundingStreamId) &&
                                            HasProperty(_, ProviderSnapshotIdKey, providerSnapshotId.ToString()))
                );
        }

        [TestMethod]
        public async Task ShouldNotQueueProviderSnapshotDataLoadJobWhenSpecificationProviderSoruceIsCFS()
        {
            string fundingStreamId = NewRandomString();
            string specificationId = NewRandomString();
            int providerSnapshotId = NewRandomInt();
            SpecificationVersion specificationVersion = NewSpecificationVersion(_ => _.WithFundingStreamsIds(fundingStreamId)
                                                                                      .WithSpecificationId(specificationId)
                                                                                      .WithProviderSource(Models.Providers.ProviderSource.CFS)
                                                                                      .WithProviderSnapshotId(providerSnapshotId));

            await WhenTheQueueEditSpecificationJobActionsIsRun(specificationVersion, _user, _correlationId, false);

            await ThenProviderSnapshotDataLoadJobWasNotCreated(
                CreateJobModelMatching(_ => _.JobDefinitionId == JobConstants.DefinitionNames.ProviderSnapshotDataLoadJob &&
                                            HasProperty(_, SpecificationIdKey, specificationId) &&
                                            HasProperty(_, FundingStreamIdKey, fundingStreamId) &&
                                            HasProperty(_, ProviderSnapshotIdKey, providerSnapshotId.ToString()))
                );
        }

        [TestMethod]
        public async Task ShouldQueueMapScopedDatasetJobWhenSpecificationProviverSoruceIsCFSAndHaveProviderVersionId()
        {
            string fundingStreamId = NewRandomString();
            string specificationId = NewRandomString();
            string providerVersionId = NewRandomString();
            string datasetId = NewRandomString();
            SpecificationVersion specificationVersion = NewSpecificationVersion(_ => _.WithFundingStreamsIds(fundingStreamId)
                                                                                      .WithSpecificationId(specificationId)
                                                                                      .WithProviderSource(Models.Providers.ProviderSource.CFS)
                                                                                      .WithProviderVersionId(providerVersionId));

            GivenTheDatasetSpecificationRelationship(specificationId, datasetId);
            AndTheDataset(datasetId);
            await WhenTheQueueEditSpecificationJobActionsIsRun(specificationVersion, _user, _correlationId, false);

            await ThenProviderSnapshotDataLoadJobWasCreated(
                CreateJobModelMatching(_ => _.JobDefinitionId == JobConstants.DefinitionNames.MapScopedDatasetJob &&
                                            HasProperty(_, SpecificationIdKey, specificationId) &&
                                            HasProperty(_, ProviderCacheKeyKey, $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}") &&
                                            HasProperty(_, SpecificationSummaryCacheKeyKey, $"{CacheKeys.SpecificationSummaryById}{specificationId}"))
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

        private async Task WhenTheQueueEditSpecificationJobActionsIsRun(SpecificationVersion specificationVersion,
            Reference user,
            string correlationId,
            bool triggerProviderSnapshotDataLoadJob)
        {
            await _action.Run(specificationVersion, user, correlationId, triggerProviderSnapshotDataLoadJob);
        }

        private string NewRandomString() => new RandomString();
        private int NewRandomInt() => new RandomNumberBetween(1, 10000);

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
