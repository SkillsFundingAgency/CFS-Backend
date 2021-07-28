using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Core.Constants;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs.Services
{
    public partial class CalculationServiceTests
    {
        [TestMethod]
        public async Task QueueCalculationRun_GivenCalled_MakesSendInstructAllocationsToJobServiceCall()
        {
            // Arrange
            IJobManagement jobManagement = CreateJobManagement();
            jobManagement
                .QueueJob(Arg.Any<JobCreateModel>())
                .Returns(new Job { Id = "job-id-1" });

            Reference author = CreateAuthor();
            string triggerEntityId = NewRandomString();
            string triggerEntityType = NewRandomString();
            string triggerMessage = NewRandomString();

            QueueCalculationRunModel queueCalculationRunModel = new QueueCalculationRunModel
            {
                Author = author,
                CorrelationId = NewRandomString(),
                Trigger = new TriggerModel
                {
                    EntityId = triggerEntityId,
                    EntityType = triggerEntityType,
                    Message = triggerMessage
                }
            };

            CalculationService service = CreateCalculationService(
                jobManagement: jobManagement);

            // Act
            IActionResult result = await service.QueueCalculationRun(
                SpecificationId, queueCalculationRunModel);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            await
                jobManagement
                    .Received(1)
                    .QueueJob(Arg.Is<JobCreateModel>(
                        m =>
                            m.InvokerUserDisplayName == author.Name &&
                            m.InvokerUserId == author.Id &&
                            m.JobDefinitionId == JobConstants.DefinitionNames.CreateInstructAllocationJob &&
                            m.Properties["specification-id"] == SpecificationId &&
                            m.Trigger.EntityId == triggerEntityId &&
                            m.Trigger.EntityType == triggerEntityType &&
                            m.Trigger.Message == triggerMessage
                        ));
        }
    }
}
