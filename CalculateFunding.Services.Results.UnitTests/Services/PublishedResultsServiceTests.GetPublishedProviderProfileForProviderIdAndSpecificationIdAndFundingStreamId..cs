using CalculateFunding.Models.Results;
using CalculateFunding.Services.Results.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.UnitTests.Services
{
    public partial class PublishedResultsServiceTests
    {
        [TestMethod]
        [DataRow("", "", "", "providerId", "No Provider ID provided")]
        [DataRow(" ", "", "", "providerId", "No Provider ID provided")]
        [DataRow("p", "", "", "specificationId", "No Specification ID provided")]
        [DataRow("p", " ", "", "specificationId", "No Specification ID provided")]
        [DataRow("p", "s", "", "fundingStreamId", "No Funding Stream ID provided")]
        [DataRow("p", "s", " ", "fundingStreamId", "No Funding Stream ID provided")]
        public void GetPublishedProviderProfileForProviderIdAndSpecificationIdAndFundingStreamId_MissingData_LogsAndThrowsException(
            string providerId,
            string specificationId,
            string fundingStreamId,
            string parameterName,
            string message)
        {
            //Arrange
            ILogger logger = Substitute.For<ILogger>();
            PublishedResultsService service = CreateResultsService(logger);

            //Act
            Func<Task> action = async () =>
                await service.GetPublishedProviderProfileForProviderIdAndSpecificationIdAndFundingStreamId(providerId, specificationId, fundingStreamId);

            //Assert
            action
                .Should().Throw<ArgumentNullException>()
                .WithMessage($"{message}{Environment.NewLine}Parameter name: {parameterName}");

            logger
                .Received(1)
                .Error(message);
        }

        [TestMethod]
        public async Task GetPublishedProviderProfileForProviderIdAndSpecificationIdAndFundingStreamId_ValidData_ReturnsData()
        {
            string providerId = "123";
            string specificationId = "456";
            string fundingStreamId = "789";

            IPublishedProviderResultsRepository publishedProviderResultsRepository = Substitute.For<IPublishedProviderResultsRepository>();
            IEnumerable<PublishedProviderProfileViewModel> returnData = new[] { new PublishedProviderProfileViewModel() };

            publishedProviderResultsRepository
                .GetPublishedProviderProfileForProviderIdAndSpecificationIdAndFundingStreamId(providerId, specificationId, fundingStreamId)
                .Returns(returnData);

            PublishedResultsService service = CreateResultsService(publishedProviderResultsRepository: publishedProviderResultsRepository);
            IActionResult result = await service.GetPublishedProviderProfileForProviderIdAndSpecificationIdAndFundingStreamId(providerId, specificationId, fundingStreamId);

            await publishedProviderResultsRepository
                .Received(1)
                .GetPublishedProviderProfileForProviderIdAndSpecificationIdAndFundingStreamId(providerId, specificationId, fundingStreamId);

            result.Should().BeOfType<OkObjectResult>();
            (result as OkObjectResult).Value.Should().Be(returnData);
        }

        [TestMethod]
        public async Task GetPublishedProviderProfileForProviderIdAndSpecificationIdAndFundingStreamId_RepoReturnsEmpty_ReturnsNotFoundResult()
        {
            string providerId = "123";
            string specificationId = "456";
            string fundingStreamId = "789";

            IPublishedProviderResultsRepository publishedProviderResultsRepository = Substitute.For<IPublishedProviderResultsRepository>();

            PublishedResultsService service = CreateResultsService(publishedProviderResultsRepository: publishedProviderResultsRepository);
            IActionResult result = await service.GetPublishedProviderProfileForProviderIdAndSpecificationIdAndFundingStreamId(providerId, specificationId, fundingStreamId);

            await publishedProviderResultsRepository
                .Received(1)
                .GetPublishedProviderProfileForProviderIdAndSpecificationIdAndFundingStreamId(providerId, specificationId, fundingStreamId);

            result.Should().BeOfType<NotFoundResult>();
        }
    }
}
