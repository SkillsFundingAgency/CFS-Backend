using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
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

        private SpecificationTemplateVersionChangedHandler _changedHandler;

        [TestInitialize]
        public void SetUp()
        {
            _jobs = new Mock<IJobManagement>();
            _calculations = new Mock<ICalculationsApiClient>();

            _changedHandler = new SpecificationTemplateVersionChangedHandler(_jobs.Object,
                _calculations.Object,
                new SpecificationsResiliencePolicies
                {
                    CalcsApiClient = Policy.NoOpAsync()
                },
                Logger.None);
        }

        private void GivenAllTheAssociateTemplateIdWithSpecificationCallsSucceed()
        {
            _calculations.Setup(_ => _.AssociateTemplateIdWithSpecification(It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(new ApiResponse<TemplateMapping>(HttpStatusCode.OK));
        }

        [TestMethod]
        public void GuardsAgainstMissingSpecificationVersion()
        {
            Func<Task> invocation = () => WhenTemplateVersionChangeIsHandled(null,
                null,
                null,
                null);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("specificationVersion");
        }

        [TestMethod]
        public async Task ExitsEarlyIfNoAssignedTemplateIdsSupplied()
        {
            await WhenTemplateVersionChangeIsHandled(NewSpecificationVersion(), 
                null, 
                NewUser(), 
                NewRandomString());
            
            ThenNoTemplateVersionsWereAssigned();
            AndNoAssignTemplateCalculationJobsWereCreated();
        }

        [TestMethod]
        public void ExistsEarlyIfAssociateTemplateIdWithSpecificationCallFails()
        {
            string fundingStream = NewRandomString();
            string existingTemplateId = NewRandomString();
            string changedTemplateId = NewRandomString();

            IDictionary<string, string> assignedTemplateIds = NewAssignTemplateIds((fundingStream, changedTemplateId));

            string specificationId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            
            SpecificationVersion specificationVersion = NewSpecificationVersion(_ => _.WithSpecificationId(specificationId)
                .WithFundingPeriodId(fundingPeriodId)
                .WithTemplateIds((fundingStream, existingTemplateId)));

            Reference user = NewUser();
            
            string correlationId = NewRandomString();
            

            Func<Task> invocation = () => WhenTemplateVersionChangeIsHandled(specificationVersion,
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
         
            AndTheAssignTemplateCalculationJobWasNotCreated(user, correlationId, specificationId, fundingStream,  fundingPeriodId, existingTemplateId);            
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
            
            SpecificationVersion specificationVersion = NewSpecificationVersion(_ => _.WithSpecificationId(specificationId)
                .WithFundingPeriodId(fundingPeriodId)
                .WithTemplateIds((fundingStreamOne, existingTemplateIdOne),
                (fundingStreamTwo, existingTemplateIdTwo),
                (fundingStreamThree, existingTemplateIdThree)));

            Reference user = NewUser();
            
            string correlationId = NewRandomString();
            
            GivenAllTheAssociateTemplateIdWithSpecificationCallsSucceed();

            await WhenTemplateVersionChangeIsHandled(specificationVersion,
                assignedTemplateIds,
                user,
                correlationId);
            
            ThenTheTemplateVersionWasAssigned(fundingStreamTwo, changedTemplateIdTwo, specificationId);
            AndTheTemplateVersionWasNotAssigned(fundingStreamTwo, existingTemplateIdTwo, specificationId);
            AndTheTemplateVersionWasNotAssigned(fundingStreamOne, existingTemplateIdOne, specificationId);
            AndTheTemplateVersionWasNotAssigned(fundingStreamThree, existingTemplateIdThree, specificationId);
            AndTheAssignTemplateCalculationJobWasCreated(user, correlationId, specificationId, fundingStreamTwo,  fundingPeriodId, changedTemplateIdTwo);            
            AndTheAssignTemplateCalculationJobWasNotCreated(user, correlationId, specificationId, fundingStreamTwo,  fundingPeriodId, existingTemplateIdTwo);            
            AndTheAssignTemplateCalculationJobWasNotCreated(user, correlationId, specificationId, fundingStreamOne,  fundingPeriodId, existingTemplateIdOne);            
            AndTheAssignTemplateCalculationJobWasNotCreated(user, correlationId, specificationId, fundingStreamOne,  fundingPeriodId, existingTemplateIdThree);            
        }

        [TestMethod]
        public async Task AssignsTemplateVersionAndQueuesAssignTemplateCalculationJobIfTheSuppliedTemplateVersionsDiffersToTheSpecificationVersion()
        {
            string fundingStream = NewRandomString();
            string existingTemplateId = NewRandomString();
            string changedTemplateId = NewRandomString();
            string specificationId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            
            SpecificationVersion specificationVersion = NewSpecificationVersion(_ => _.WithSpecificationId(specificationId)
                .WithFundingPeriodId(fundingPeriodId)
                .WithTemplateIds((fundingStream, existingTemplateId)));
            
            GivenAllTheAssociateTemplateIdWithSpecificationCallsSucceed();
            
            await WhenTemplateVersionChangeIsHandled(specificationVersion,
                fundingStream,
                changedTemplateId);

            ThenTheTemplateVersionWasAssigned(fundingStream, changedTemplateId, specificationId);     
            AndTheAssignTemplateCalculationJobWasNotCreated(specificationId, fundingStream,  fundingPeriodId, existingTemplateId);               
        }

        private void ThenTheTemplateVersionWasAssigned(string fundingStreamId,
            string templateVersion,
            string specificationId)
        {
            AndTheTemplateVersionWasAssignedXTimes(fundingStreamId,
                templateVersion,
                specificationId,
                Times.Once());
        }
        
        private void AndTheTemplateVersionWasNotAssigned(string fundingStreamId,
            string templateVersion,
            string specificationId)
        {
            AndTheTemplateVersionWasAssignedXTimes(fundingStreamId,
                templateVersion,
                specificationId,
                Times.Never());
        }
        
        private void AndTheTemplateVersionWasAssignedXTimes(string fundingStreamId,
            string templateVersion,
            string specificationId,
            Times times)
        {
            _calculations.Verify(_ => _.AssociateTemplateIdWithSpecification(specificationId,
                    templateVersion,
                    fundingStreamId),
                times);    
        }

        private void ThenNoTemplateVersionsWereAssigned()
        {
            _calculations.Verify(_ => _.AssociateTemplateIdWithSpecification(It.IsAny<string>(),
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
        
        private Reference NewUser() => new ReferenceBuilder()
            .Build();
        
        private string NewRandomString() => new RandomString();
        
        private async Task WhenTemplateVersionChangeIsHandled(SpecificationVersion specificationVersion,
            string fundingStreamId,
            string templateVersionId)
            => await _changedHandler.HandleTemplateVersionChanged(specificationVersion, fundingStreamId, templateVersionId);

        private async Task WhenTemplateVersionChangeIsHandled(SpecificationVersion specificationVersion,
            IDictionary<string, string> assignedTemplateIds,
            Reference user,
            string correlationId)
            => await _changedHandler.HandleTemplateVersionChanged(specificationVersion, assignedTemplateIds, user, correlationId);

        private SpecificationVersion NewSpecificationVersion(Action<SpecificationVersionBuilder> setUp = null)
        {
            SpecificationVersionBuilder specificationVersionBuilder = new SpecificationVersionBuilder();

            setUp?.Invoke(specificationVersionBuilder);

            return specificationVersionBuilder.Build();
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