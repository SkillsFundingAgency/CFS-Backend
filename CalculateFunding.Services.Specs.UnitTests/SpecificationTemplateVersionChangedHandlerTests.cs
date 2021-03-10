using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.Extensions;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;
using Serilog.Core;

namespace CalculateFunding.Services.Specs.UnitTests
{
    [TestClass]
    public class SpecificationTemplateVersionChangedHandlerTests
    {
        private Mock<IJobManagement> _jobs;
        private Mock<ICalculationsApiClient> _calculations;
        private Mock<IPoliciesApiClient> _policiesApiClient;

        private SpecificationTemplateVersionChangedHandler _changedHandler;

        [TestInitialize]
        public void SetUp()
        {
            _jobs = new Mock<IJobManagement>();
            _calculations = new Mock<ICalculationsApiClient>();
            _policiesApiClient = new Mock<IPoliciesApiClient>();

            _changedHandler = new SpecificationTemplateVersionChangedHandler(_jobs.Object,
                _calculations.Object,
                new SpecificationsResiliencePolicies
                {
                    CalcsApiClient = Policy.NoOpAsync(),
                    PoliciesApiClient = Policy.NoOpAsync()
                },
                Logger.None);
        }

        private void GivenAllTheAssociateTemplateIdWithSpecificationCallsSucceed()
        {
            _calculations.Setup(_ => _.ProcessTemplateMappings(It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(new ApiResponse<TemplateMapping>(HttpStatusCode.OK));
        }

        [TestMethod]
        public void GuardsAgainstMissingPreviousSpecificationVersion()
        {
            Func<Task> invocation = () => WhenTemplateVersionChangeIsHandled(null,
                null,
                null,
                null,
                null);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("previousSpecificationVersion");
        }

        [TestMethod]
        public async Task ExitsEarlyIfNoAssignedTemplateIdsSupplied()
        {
            await WhenTemplateVersionChangeIsHandled(NewSpecificationVersion(),
                null,
                null,
                NewUser(),
                NewRandomString());

            ThenNoProcessMappingsCalled();
            AndNoAssignTemplateCalculationJobsWereCreated();
        }

        [TestMethod]
        public void ExitEarlyIfAssociateTemplateIdWithSpecificationCallFails()
        {
            string fundingStream = NewRandomString();
            string existingTemplateId = NewRandomString();
            string changedTemplateId = NewRandomString();

            IDictionary<string, string> assignedTemplateIds = NewAssignTemplateIds((fundingStream, changedTemplateId));

            string specificationId = NewRandomString();
            string fundingPeriodId = NewRandomString();

            SpecificationVersion previousSpecificationVersion = NewSpecificationVersion(_ => _.WithSpecificationId(specificationId)
                .WithFundingPeriodId(fundingPeriodId)
                .WithTemplateIds((fundingStream, existingTemplateId)));

            SpecificationVersion specificationVersion = NewSpecificationVersion(_ => _.WithSpecificationId(specificationId)
                .WithFundingPeriodId(fundingPeriodId));

            Reference user = NewUser();

            string correlationId = NewRandomString();
            AndMetadataForTemplateVersion(fundingStream, fundingPeriodId, existingTemplateId, NewTemplateMetadataDistinctContents());
            AndMetadataForTemplateVersion(fundingStream, fundingPeriodId, changedTemplateId, NewTemplateMetadataDistinctContents());


            Func<Task> invocation = () => WhenTemplateVersionChangeIsHandled(previousSpecificationVersion,
                specificationVersion,
                assignedTemplateIds,
                user,
                correlationId);

            invocation
                .Should()
                .Throw<InvalidOperationException>()
                .Which
                .Message
                .Should()
                .Be($"Unable to associate template version {changedTemplateId} for funding stream {fundingStream} and period {fundingPeriodId} on specification {specificationId}");

            AndTheAssignTemplateCalculationJobWasNotCreated(user, correlationId, specificationId, fundingStream, fundingPeriodId, existingTemplateId);
        }

        [TestMethod]
        public async Task AssignsTemplateVersionAndQueuesAssignTemplateCalculationJobForAnyChangedTemplateVersionsInTheSpecificationVersion()
        {
            string fundingStreamOne = NewRandomString();
            string fundingStreamTwo = NewRandomString();
            string fundingStreamThree = NewRandomString();

            string existingTemplateIdOne = NewRandomString();
            string existingTemplateIdTwo = NewRandomString();
            string existingTemplateIdThree = NewRandomString();

            string changedTemplateIdTwo = NewRandomString();

            IDictionary<string, string> assignedTemplateIds = NewAssignTemplateIds((fundingStreamOne, existingTemplateIdOne),
                (fundingStreamTwo, changedTemplateIdTwo));

            string specificationId = NewRandomString();
            string fundingPeriodId = NewRandomString();

            SpecificationVersion previousSpecificationVersion = NewSpecificationVersion(_ => _.WithSpecificationId(specificationId)
                .WithFundingPeriodId(fundingPeriodId)
                .WithTemplateIds((fundingStreamOne, existingTemplateIdOne),
                (fundingStreamTwo, existingTemplateIdTwo),
                (fundingStreamThree, existingTemplateIdThree)));

            SpecificationVersion specificationVersion = previousSpecificationVersion.DeepCopy();

            Reference user = NewUser();

            string correlationId = NewRandomString();

            GivenAllTheAssociateTemplateIdWithSpecificationCallsSucceed();
            AndMetadataForTemplateVersion(fundingStreamTwo, fundingPeriodId, existingTemplateIdTwo, NewTemplateMetadataDistinctContents());
            AndMetadataForTemplateVersion(fundingStreamTwo, fundingPeriodId, changedTemplateIdTwo, NewTemplateMetadataDistinctContents());

            await WhenTemplateVersionChangeIsHandled(previousSpecificationVersion,
                specificationVersion,
                assignedTemplateIds,
                user,
                correlationId);

            ThenTheProcessMappingsWasCalled(fundingStreamTwo, changedTemplateIdTwo, specificationId);
            AndTheProcessMappingsWasNotCalled(fundingStreamTwo, existingTemplateIdTwo, specificationId);
            AndTheProcessMappingsWasNotCalled(fundingStreamOne, existingTemplateIdOne, specificationId);
            AndTheProcessMappingsWasNotCalled(fundingStreamThree, existingTemplateIdThree, specificationId);
            AndTheDetectObsoleteFundingLinesJobWasCreated(user, correlationId, specificationId, fundingStreamTwo, fundingPeriodId, existingTemplateIdTwo, changedTemplateIdTwo);
            AndTheAssignTemplateCalculationJobWasCreated(user, correlationId, specificationId, fundingStreamTwo, fundingPeriodId, changedTemplateIdTwo);
            AndTheAssignTemplateCalculationJobWasNotCreated(user, correlationId, specificationId, fundingStreamTwo, fundingPeriodId, existingTemplateIdTwo);
            AndTheAssignTemplateCalculationJobWasNotCreated(user, correlationId, specificationId, fundingStreamOne, fundingPeriodId, existingTemplateIdOne);
            AndTheAssignTemplateCalculationJobWasNotCreated(user, correlationId, specificationId, fundingStreamOne, fundingPeriodId, existingTemplateIdThree);
            AndTheSpecVersionTemplateVersionsAssigned(specificationVersion, fundingStreamTwo, changedTemplateIdTwo);
        }

        [TestMethod]
        public async Task AssignsTemplateVersionAndQueuesAssignTemplateCalculationJobIfTheSuppliedTemplateVersionsDiffersToTheSpecificationVersion()
        {
            string fundingStream = NewRandomString();
            string existingTemplateId = NewRandomString();
            string changedTemplateId = NewRandomString();
            string specificationId = NewRandomString();
            string fundingPeriodId = NewRandomString();

            SpecificationVersion previousSpecificationVersion = NewSpecificationVersion(_ => _.WithSpecificationId(specificationId)
                .WithFundingPeriodId(fundingPeriodId)
                .WithTemplateIds((fundingStream, existingTemplateId)));

            SpecificationVersion specificationVersion = previousSpecificationVersion.DeepCopy();

            Reference user = NewUser();

            string correlationId = NewRandomString();

            GivenAllTheAssociateTemplateIdWithSpecificationCallsSucceed();
            AndMetadataForTemplateVersion(fundingStream, fundingPeriodId, existingTemplateId, NewTemplateMetadataDistinctContents());
            AndMetadataForTemplateVersion(fundingStream, fundingPeriodId, changedTemplateId, NewTemplateMetadataDistinctContents());

            await WhenTemplateVersionChangeIsHandled(previousSpecificationVersion,
                specificationVersion,
                new Dictionary<string, string>
                {
                    { fundingStream , changedTemplateId }
                },
                user,
                correlationId);

            ThenTheProcessMappingsWasCalled(fundingStream, changedTemplateId, specificationId);
            AndTheDetectObsoleteFundingLinesJobWasNotCreated(specificationId, fundingStream, fundingPeriodId, existingTemplateId, changedTemplateId);
            AndTheAssignTemplateCalculationJobWasNotCreated(specificationId, fundingStream, fundingPeriodId, existingTemplateId);
            AndTheAssignTemplateCalculationJobWasNotCreated(specificationId, fundingStream, fundingPeriodId, changedTemplateId);
        }

        private void AndTheSpecVersionTemplateVersionsAssigned(SpecificationVersion specificationVersion, string fundingStreamId, string expectedVersion)
        {
            specificationVersion.TemplateIds[fundingStreamId]
                .Should()
                .Be(expectedVersion);
        }

        private void ThenTheProcessMappingsWasCalled(string fundingStreamId,
            string templateVersion,
            string specificationId)
        {
            AndTheProcessMappingsWasCalledXTimes(fundingStreamId,
                templateVersion,
                specificationId,
                Times.Once());
        }

        private void AndTheProcessMappingsWasNotCalled(string fundingStreamId,
            string templateVersion,
            string specificationId)
        {
            AndTheProcessMappingsWasCalledXTimes(fundingStreamId,
                templateVersion,
                specificationId,
                Times.Never());
        }

        private void AndTheProcessMappingsWasCalledXTimes(string fundingStreamId,
            string templateVersion,
            string specificationId,
            Times times)
        {
            _calculations.Verify(_ => _.ProcessTemplateMappings(specificationId,
                    templateVersion,
                    fundingStreamId),
                times);
        }

        private void ThenNoProcessMappingsCalled()
        {
            _calculations.Verify(_ => _.ProcessTemplateMappings(It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
                Times.Never);
        }

        private void AndTheAssignTemplateCalculationJobWasNotCreated(string specificationId,
            string fundingStreamId,
            string fundingPeriodId,
            string templateVersionId)
        {
            AndTheAssignTemplateCalculationJobWasNotCreated(null,
                null,
                specificationId,
                fundingStreamId,
                fundingPeriodId,
                templateVersionId);
        }

        private void AndTheAssignTemplateCalculationJobWasNotCreated(Reference user,
            string correlationId,
            string specificationId,
            string fundingStreamId,
            string fundingPeriodId,
            string templateVersionId)
        {
            AndTheAssignTemplateCalculationJobWasCreatedXTimes(user,
                           correlationId,
                           specificationId,
                           fundingStreamId,
                           fundingPeriodId,
                           templateVersionId,
                           Times.Never());
        }

        private void AndTheDetectObsoleteFundingLinesJobWasNotCreated(string specificationId,
            string fundingStreamId,
            string fundingPeriodId,
            string previousTemplateVersionId,
            string templateVersionId)
        {
            AndTheDetectObsoleteFundingLinesJobWasCreatedXTimes(null,
                           null,
                           specificationId,
                           fundingStreamId,
                           fundingPeriodId,
                           previousTemplateVersionId,
                           templateVersionId,
                           Times.Never());
        }

        private void AndTheDetectObsoleteFundingLinesJobWasCreated(Reference user,
            string correlationId,
            string specificationId,
            string fundingStreamId,
            string fundingPeriodId,
            string previousTemplateVersionId,
            string templateVersionId)
        {
            AndTheDetectObsoleteFundingLinesJobWasCreatedXTimes(user,
                correlationId,
                specificationId,
                fundingStreamId,
                fundingPeriodId,
                previousTemplateVersionId,
                templateVersionId,
                Times.Once());
        }

        private void AndTheAssignTemplateCalculationJobWasCreated(Reference user,
            string correlationId,
            string specificationId,
            string fundingStreamId,
            string fundingPeriodId,
            string templateVersionId)
        {
            AndTheAssignTemplateCalculationJobWasCreatedXTimes(user,
                correlationId,
                specificationId,
                fundingStreamId,
                fundingPeriodId,
                templateVersionId,
                Times.Once());
        }

        private void AndTheDetectObsoleteFundingLinesJobWasCreatedXTimes(Reference user,
            string correlationId,
            string specificationId,
            string fundingStreamId,
            string fundingPeriodId,
            string previousTemplateVersionId,
            string templateVersionId,
            Times times)
        {
            string userId = user?.Id;
            string userName = user?.Name;

            _jobs.Verify(_ => _.QueueJob(It.Is<JobCreateModel>(job =>
                    job.JobDefinitionId == JobConstants.DefinitionNames.DetectObsoleteFundingLinesJob &&
                    job.InvokerUserId == userId &&
                    job.InvokerUserDisplayName == userName &&
                    job.CorrelationId == correlationId &&
                    HasUserProperties(job.Properties,
                        "specification-id", specificationId,
                        "funding-stream-id", fundingStreamId,
                        "funding-period-id", fundingPeriodId,
                        "previous-template-version-id", previousTemplateVersionId,
                        "template-version-id", templateVersionId))),
                times);
        }

        private void AndTheAssignTemplateCalculationJobWasCreatedXTimes(Reference user,
            string correlationId,
            string specificationId,
            string fundingStreamId,
            string fundingPeriodId,
            string templateVersionId,
            Times times)
        {
            string userId = user?.Id;
            string userName = user?.Name;

            _jobs.Verify(_ => _.QueueJob(It.Is<JobCreateModel>(job =>
                    job.JobDefinitionId == JobConstants.DefinitionNames.AssignTemplateCalculationsJob &&
                    job.InvokerUserId == userId &&
                    job.InvokerUserDisplayName == userName &&
                    job.CorrelationId == correlationId &&
                    HasUserProperties(job.Properties,
                        "specification-id", specificationId,
                        "fundingstream-id", fundingStreamId,
                        "fundingperiod-id", fundingPeriodId,
                        "template-version", templateVersionId))),
                times);
        }

        private void AndNoAssignTemplateCalculationJobsWereCreated()
        {
            _jobs.Verify(_ => _.QueueJob(It.IsAny<JobCreateModel>()),
                Times.Never);
        }

        private void AndMetadataForTemplateVersion(string fundingStreamId, string fundingPeriodId, string templateVersion, TemplateMetadataDistinctContents templateContents = null)
        {
            _policiesApiClient.Setup(x => x.GetDistinctTemplateMetadataContents(fundingStreamId, fundingPeriodId, templateVersion))
                .ReturnsAsync(new ApiResponse<TemplateMetadataDistinctContents>(HttpStatusCode.OK, templateContents));
        }

        private Reference NewUser() => new ReferenceBuilder()
            .Build();

        private string NewRandomString() => new RandomString();

        private async Task WhenTemplateVersionChangeIsHandled(SpecificationVersion previousSpecificationVersion,
            SpecificationVersion specificationVersion,
            IDictionary<string, string> assignedTemplateIds,
            Reference user,
            string correlationId)
            => await _changedHandler.HandleTemplateVersionChanged(previousSpecificationVersion, specificationVersion, assignedTemplateIds, user, correlationId);

        private SpecificationVersion NewSpecificationVersion(Action<SpecificationVersionBuilder> setUp = null)
        {
            SpecificationVersionBuilder specificationVersionBuilder = new SpecificationVersionBuilder();

            setUp?.Invoke(specificationVersionBuilder);

            return specificationVersionBuilder.Build();
        }

        private TemplateMetadataDistinctContents NewTemplateMetadataDistinctContents(Action<TemplateMetadataDistinctContentsBuilder> setup = null)
        {
            TemplateMetadataDistinctContentsBuilder builder = new TemplateMetadataDistinctContentsBuilder();
            setup?.Invoke(builder);

            return builder.Build();
        }

        private IDictionary<string, string> NewAssignTemplateIds(params (string fundingStreamId, string templateVersionId)[] assignedTemplateIds)
            => assignedTemplateIds.ToDictionary(_ => _.fundingStreamId, _ => _.templateVersionId);

        private bool HasUserProperties(IDictionary<string, string> properties,
            params string[] expectedPropertyPairs)
        {
            Dictionary<string, string> expectedProperties = new Dictionary<string, string>();

            for (int propertyName = 0; propertyName < expectedPropertyPairs.Length; propertyName += 2)
            {
                expectedProperties[expectedPropertyPairs[propertyName]] = expectedPropertyPairs[propertyName + 1];
            }

            return expectedProperties.Count == properties.Count &&
                   properties.All(_ => expectedProperties.Contains(_));
        }
    }
}