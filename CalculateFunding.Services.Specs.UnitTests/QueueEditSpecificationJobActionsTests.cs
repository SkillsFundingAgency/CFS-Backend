﻿using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Tests.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Specs.UnitTests
{
    [TestClass]
    public class QueueEditSpecificationJobActionsTests
    {
        private const string SpecificationIdKey = "specification-id";
        private const string FundingStreamIdKey = "fundingstream-id";
        private const string ProviderSnapshotIdKey = "providerSanpshot-id";

        private QueueEditSpecificationJobActions _action;
        private IJobManagement _jobManagement;
        private ILogger _logger;
        private Reference _user;
        private string _userId;
        private string _userName;
        private string _correlationId;

        [TestInitialize]
        public void SetUp()
        {
            _jobManagement = Substitute.For<IJobManagement>();
            _logger = Substitute.For<ILogger>();

            _userId = NewRandomString();
            _userName = NewRandomString();

            _user = NewReference(_ => _.WithId(_userId).WithName(_userName));
            _correlationId = NewRandomString();

            _action = new QueueEditSpecificationJobActions(_jobManagement, _logger);

            _jobManagement.QueueJob(Arg.Any<JobCreateModel>())
                .Returns(new Job());//default instance as we assert was called but have null checks in the test now
        }

        [TestMethod]
        public async Task ShouldQueueProviderSnapshotDataLoadJobWhenSpecificationProviverSoruceIsFDZ()
        {
            string fundingStreamId = NewRandomString();
            string specificationId = NewRandomString();
            int providerSnapshotId = NewRandomInt();
            SpecificationVersion specificationVersion = NewSpecificationVersion(_ => _.WithFundingStreamsIds(fundingStreamId)
                                                                                      .WithSpecificationId(specificationId)
                                                                                      .WithProviderSource(Models.Providers.ProviderSource.FDZ)
                                                                                      .WithProviderSnapshotId(providerSnapshotId));

            await WhenTheQueueEditSpecificationJobActionsIsRun(specificationVersion, _user, _correlationId);

            await ThenProviderSnapshotDataLoadJobWasCreated(
                CreateJobModelMatching(_ => _.JobDefinitionId == JobConstants.DefinitionNames.ProviderSnapshotDataLoadJob &&
                                            HasProperty(_, SpecificationIdKey, specificationId) &&
                                            HasProperty(_, FundingStreamIdKey, fundingStreamId) &&
                                            HasProperty(_, ProviderSnapshotIdKey, providerSnapshotId.ToString()))
                );
        }

        [TestMethod]
        public async Task ShouldNotQueueProviderSnapshotDataLoadJobWhenSpecificationProviverSoruceIsCFS()
        {
            string fundingStreamId = NewRandomString();
            string specificationId = NewRandomString();
            int providerSnapshotId = NewRandomInt();
            SpecificationVersion specificationVersion = NewSpecificationVersion(_ => _.WithFundingStreamsIds(fundingStreamId)
                                                                                      .WithSpecificationId(specificationId)
                                                                                      .WithProviderSource(Models.Providers.ProviderSource.CFS)
                                                                                      .WithProviderSnapshotId(providerSnapshotId));

            await WhenTheQueueEditSpecificationJobActionsIsRun(specificationVersion, _user, _correlationId);

            await ThenProviderSnapshotDataLoadJobWasNotCreated(
                CreateJobModelMatching(_ => _.JobDefinitionId == JobConstants.DefinitionNames.ProviderSnapshotDataLoadJob &&
                                            HasProperty(_, SpecificationIdKey, specificationId) &&
                                            HasProperty(_, FundingStreamIdKey, fundingStreamId) &&
                                            HasProperty(_, ProviderSnapshotIdKey, providerSnapshotId.ToString()))
                );
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
            string correlationId)
        {
            await _action.Run(specificationVersion, user, correlationId);
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