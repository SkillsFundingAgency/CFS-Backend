using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Services.Core.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace CalculateFunding.Services.Calcs.Services
{
    public partial class CalculationServiceTests
    {
        [TestMethod]
        public async Task QueueApproveAllSpecificationCalculations_JobActionThrowsException_ReturnsInternalServerErrorResult()
        {
            string correlationId = "any-id";

            Reference author = new Reference();

            IApproveAllCalculationsJobAction approveAllCalculationsJobAction = CreateApproveAllCalculationsJobAction();

            approveAllCalculationsJobAction
                .Run(Arg.Is(SpecificationId), Arg.Is(author), Arg.Is(correlationId))
                .Throws<Exception>();

            CalculationService calculationService = CreateCalculationService(approveAllCalculationsJobAction: approveAllCalculationsJobAction);

            IActionResult actionResult = await calculationService.QueueApproveAllSpecificationCalculations(SpecificationId, author, correlationId);

            actionResult
                .Should()
                .BeAssignableTo<InternalServerErrorResult>();
        }

        [TestMethod]
        public async Task QueueApproveAllSpecificationCalculations_JobActionQueuesJob_ReturnsOKJobDetails()
        {
            string correlationId = "any-id";
            string jobId = "any-id";


            Reference author = new Reference();
            Job job = new Job { Id = jobId };

            IApproveAllCalculationsJobAction approveAllCalculationsJobAction = CreateApproveAllCalculationsJobAction();

            approveAllCalculationsJobAction
                .Run(Arg.Is(SpecificationId), Arg.Is(author), Arg.Is(correlationId))
                .Returns(job);

            CalculationService calculationService = CreateCalculationService(approveAllCalculationsJobAction: approveAllCalculationsJobAction);

            IActionResult actionResult = await calculationService.QueueApproveAllSpecificationCalculations(SpecificationId, author, correlationId);

            actionResult
                .Should()
                .BeAssignableTo<OkObjectResult>()
                .And
                .NotBeNull();

            OkObjectResult okObjectResult = actionResult as OkObjectResult;

            okObjectResult.Value.Should().NotBeNull().And.BeAssignableTo<Job>();

            Job actualJob = okObjectResult.Value as Job;

            actualJob.Id.Should().Be(jobId);
        }

    }
}
