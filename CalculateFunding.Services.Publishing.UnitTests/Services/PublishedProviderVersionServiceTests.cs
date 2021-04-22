using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Tests.Common.Helpers;
using CalculateFunding.UnitTests.ApiClientHelpers.Jobs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage.Blob;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Publishing.UnitTests.Services
{
    [TestClass]
    public class PublishedProviderVersionServiceTests
    {
        private const string publishedProviderVersionId = "id1";
        private const string publishedProviderSpecificationId = "specId1";
        private const string body = "just a string";

        private readonly string blobName = $"{publishedProviderVersionId}.json";

        private PublishedProviderVersionService _service;
        private IBlobClient _blobClient;
        private IJobManagement _jobManagement;
        private ILogger _logger;
        private Job _job;

        [TestInitialize]
        public void SetUp()
        {
            _blobClient = Substitute.For<IBlobClient>();
            _jobManagement = Substitute.For<IJobManagement>();
            _logger = Substitute.For<ILogger>();

            _service = new PublishedProviderVersionService(_logger,
                _blobClient,
                new ResiliencePolicies
                {
                    BlobClient = Policy.NoOpAsync(),
                },
                _jobManagement);
        }

        [TestMethod]
        public async Task GetPublishedProviderVersionBody_GivenNullOrEmptyId_ReturnsBadRequest()
        {
            //Arrange
            string id = "";

            //Act
            IActionResult result = await _service.GetPublishedProviderVersionBody(id);

            //Assert
            result
                .Should()
                .BeAssignableTo<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Null or empty id provided.");
        }

        [TestMethod]
        public async Task GetPublishedProviderVersionBody_GivenBlobDoesNotExists_ReturnsNotFound()
        {
            //Act
            IActionResult result = await _service.GetPublishedProviderVersionBody(publishedProviderVersionId);

            //Assert
            result
                .Should()
                .BeAssignableTo<NotFoundResult>();

            _logger
                .Received(1)
                .Error(Arg.Is($"Blob '{blobName}' does not exist."));
        }

        [TestMethod]
        public async Task GetPublishedProviderVersionBody_GivenGetBlobReferenceCausesException_LogsAndReturnsInternalServerError()
        {
            //Arrange
            _blobClient
                .BlobExistsAsync(Arg.Is(blobName))
                .Returns(true);

            _blobClient
                .When(x => x.GetBlobReferenceFromServerAsync(Arg.Is(blobName)))
                .Do(x => { throw new Exception(); });

            string errorMessage = $"Failed to fetch blob '{blobName}' from azure storage";

            //Act
            IActionResult result = await _service.GetPublishedProviderVersionBody(publishedProviderVersionId);

            //Assert
            result
                .Should()
                .BeAssignableTo<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be(errorMessage);

            _logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Is(errorMessage));
        }

        [TestMethod]
        public async Task GetPublishedProviderVersionBody_GivenReturnsFromBlobStorage_returnsOK()
        {
            //Arrange
            byte[] bytes = Encoding.UTF8.GetBytes(body);

            Stream memoryStream = new MemoryStream(bytes);

            ICloudBlob cloudBlob = CreateBlob();

            _blobClient
                .BlobExistsAsync(Arg.Is(blobName))
                .Returns(true);

            _blobClient
                .GetBlobReferenceFromServerAsync(Arg.Is(blobName))
                .Returns(cloudBlob);

            _blobClient
                .DownloadToStreamAsync(Arg.Is(cloudBlob))
                .Returns(memoryStream);

            //Act
            IActionResult result = await _service.GetPublishedProviderVersionBody(publishedProviderVersionId);

            //Assert
            result
                .Should()
                .BeAssignableTo<OkObjectResult>()
                .Which
                .Value
                .Should()
                .Be(body);
        }

        [TestMethod]
        public void SavePublishedProviderVersionBody_GivenGetBlobReferenceFromServerAsyncThrowsException_ThrowsNewException()
        {
            _blobClient
                .When(x => x.GetBlockBlobReference(Arg.Is(blobName)))
                .Do(x => { throw new Exception("Failed to get blob reference"); });

            string errorMessage = $"Failed to save blob '{blobName}' to azure storage";

            //Act
            Func<Task> test = async () => await _service.SavePublishedProviderVersionBody(publishedProviderVersionId, body, publishedProviderSpecificationId);

            //Assert
            test
                .Should()
                .ThrowExactly<Exception>()
                .And
                .Message
                .Should()
                .Be(errorMessage);

            _logger
                .Received(1)
                .Error(Arg.Is<Exception>(m => m.Message == "Failed to get blob reference"), Arg.Is(errorMessage));
        }

        [TestMethod]
        public void SavePublishedProviderVersionBody_GivenUploadAsyncThrowsException_ThrowsNewException()
        {
            _blobClient
                .When(x => x.UploadAsync(Arg.Any<ICloudBlob>(), Arg.Is(body)))
                .Do(x => { throw new Exception("Failed to upload blob"); });

            string errorMessage = $"Failed to save blob '{blobName}' to azure storage";

            //Act
            Func<Task> test = async () => await _service.SavePublishedProviderVersionBody(publishedProviderVersionId, body, publishedProviderSpecificationId);

            //Assert
            test
                .Should()
                .ThrowExactly<Exception>()
                .And
                .Message
                .Should()
                .Be(errorMessage);

            _logger
                .Received(1)
                .Error(Arg.Is<Exception>(m => m.Message == "Failed to upload blob"), Arg.Is(errorMessage));
        }

        [TestMethod]
        public async Task SavePublishedProviderVersionBody_GivenUploadAsyncSuccessful_EnsuresCalledWithCorrectParameters()
        {
            //Arrange
            ICloudBlob blob = Substitute.For<ICloudBlob>();

            _blobClient
                .GetBlockBlobReference(Arg.Is(blobName))
                .Returns(blob);

            //Act
            await _service.SavePublishedProviderVersionBody(publishedProviderVersionId, body, publishedProviderSpecificationId);

            //Assert
            await
                _blobClient
                    .Received(1)
                    .UploadAsync(Arg.Is(blob), Arg.Is(body));
            await
                _blobClient
                    .Received(1)
                    .AddMetadataAsync(
                        Arg.Is(blob),
                        Arg.Is<IDictionary<string, string>>(_ => _.ContainsKey("specification-id") && _["specification-id"] == publishedProviderSpecificationId));
        }

        [TestMethod]
        public void ReIndex_ThrowsExceptionIfNoUserSupplied()
        {
            Func<Task<ActionResult<Job>>> invocation = () => WhenThePublishedProviderVersionsAreReIndexed(correlationId: NewRandomString());

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("user");
        }

        [TestMethod]
        public void ReIndex_ThrowsExceptionIfNoCorrelationIdSupplied()
        {
            Func<Task<ActionResult<Job>>> invocation = () => WhenThePublishedProviderVersionsAreReIndexed(NewUser());

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("correlationId");
        }

        [TestMethod]
        public async Task ReIndex_CreatesReIndexPublishedProviderJob()
        {
            Reference user = NewUser(_ => _.WithId(NewRandomString())
                .WithName(NewRandomString()));
            string correlationId = NewRandomString();

            AndJobIsCreated();

            ActionResult<Job> result = await WhenThePublishedProviderVersionsAreReIndexed(user, correlationId);

            result
                .Result
                .Should()
                .BeNull();

            result
               .Value
               .Should()
               .NotBeNull();

            await _jobManagement
                .Received(1)
                .QueueJob(Arg.Is<JobCreateModel>(_ =>
                    _.CorrelationId == correlationId &&
                    _.InvokerUserId == user.Id &&
                    _.InvokerUserDisplayName == user.Name &&
                    _.JobDefinitionId == JobConstants.DefinitionNames.ReIndexPublishedProvidersJob));
        }

        private void AndJobIsCreated()
        {
            _job = NewJob();

            _jobManagement.QueueJob(Arg.Any<JobCreateModel>()).Returns(_job);
        }

        private Job NewJob(Action<JobBuilder> setUp = null)
        {
            JobBuilder jobBuilder = new JobBuilder();

            setUp?.Invoke(jobBuilder);

            return jobBuilder.Build();
        }

        private async Task<ActionResult<Job>> WhenThePublishedProviderVersionsAreReIndexed(Reference user = null,
            string correlationId = null)
        {
            return await _service.ReIndex(user, correlationId);
        }

        private Reference NewUser(Action<ReferenceBuilder> setUp = null)
        {
            ReferenceBuilder userBuilder = new ReferenceBuilder();

            setUp?.Invoke(userBuilder);

            return userBuilder.Build();
        }

        private string NewRandomString() => new RandomString();

        private static ICloudBlob CreateBlob()
        {
            return Substitute.For<ICloudBlob>();
        }
    }
}