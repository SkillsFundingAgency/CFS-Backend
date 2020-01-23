using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Messages;
using CalculateFunding.Services.Jobs.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Jobs.Services
{
    public partial class JobManagementServiceTests
    {
        [TestMethod]
        [DataRow("SpecId1", DeletionType.SoftDelete)]
        [DataRow("SpecId1", DeletionType.PermanentDelete)]
        public async Task DeleteJobs_Deletes_Dependencies_Using_Correct_SpecificationId_And_DeletionType(string specificationId, DeletionType deletionType)
        {
            Message message = new Message
            {
                UserProperties =
                {
                    new KeyValuePair<string, object>("specification-id", specificationId),
                    new KeyValuePair<string, object>("deletion-type", (int)deletionType)
                }
            };
            IJobRepository jobRepository = CreateJobRepository();
            JobManagementService jobManagementService = CreateJobManagementService(jobRepository);

            IActionResult actionResult = await jobManagementService.DeleteJobs(message);

            await jobRepository.Received(1).DeleteJobsBySpecificationId(specificationId, deletionType);
            actionResult.Should().BeOfType<OkResult>();
        }
    }
}
