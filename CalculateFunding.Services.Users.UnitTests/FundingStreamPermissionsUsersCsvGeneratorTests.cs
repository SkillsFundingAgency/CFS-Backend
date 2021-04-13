using CalculateFunding.Common.Storage;
using CalculateFunding.Models.Users;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Users.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.Azure.Storage.Blob;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;
using Serilog;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Users
{
    [TestClass]
    public class FundingStreamPermissionsUsersCsvGeneratorTests
    {
        private const string UsersReportContainerName = "userreports";

        private FundingStreamPermissionsUsersCsvGenerator _service;

        private Mock<IFileSystemAccess> _fileSystemAccess;
        private Mock<IBlobClient> _blobClient;
        private Mock<ICsvUtils> _csvUtils;
        private Mock<IFileSystemCacheSettings> _fileSystemCacheSettings;
        private Mock<IUsersCsvTransformServiceLocator> _usersCsvTransformServiceLocator;
        
        private Mock<ICloudBlob> _cloudBlob;
        private Mock<IUsersCsvTransform> _transformation;
        private Mock<IUserRepository> _userRepository;
        private Mock<ILogger> _logger;

        private BlobProperties _blobProperties;

        private string _rootPath;

        private UserPermissionCsvGenerationMessage _message;



        [TestInitialize]
        public void SetUp()
        {
            _usersCsvTransformServiceLocator = new Mock<IUsersCsvTransformServiceLocator>();
            _userRepository = new Mock<IUserRepository>();
            _blobClient = new Mock<IBlobClient>();
            _csvUtils = new Mock<ICsvUtils>();
            _transformation = new Mock<IUsersCsvTransform>();
            _cloudBlob = new Mock<ICloudBlob>();
            _fileSystemAccess = new Mock<IFileSystemAccess>();
            _fileSystemCacheSettings = new Mock<IFileSystemCacheSettings>();
            _logger = new Mock<ILogger>();

            _service = new FundingStreamPermissionsUsersCsvGenerator(
                _fileSystemAccess.Object,
                _blobClient.Object,
                _csvUtils.Object,
                _fileSystemCacheSettings.Object,
                new UsersResiliencePolicies
                {
                    BlobClient = Policy.NoOpAsync(),
                    UserRepositoryPolicy = Policy.NoOpAsync()
                },
                _usersCsvTransformServiceLocator.Object,
                _logger.Object,
                _userRepository.Object
                );

            _rootPath = NewRandomString();
            _fileSystemCacheSettings.Setup(_ => _.Path)
                .Returns(_rootPath);

            _fileSystemAccess.Setup(_ => _.Append(It.IsAny<string>(),
                    It.IsAny<string>(), default))
                .Returns(Task.CompletedTask);

            _blobProperties = new BlobProperties();
            _cloudBlob.Setup(_ => _.Properties)
                .Returns(_blobProperties);

            _message = new UserPermissionCsvGenerationMessage();
        }

        [TestMethod]
        public async Task ThrowsNonRetriableExceptionWhenGetUsersWithFundingStreamPermissionsReturnsNull()
        {
            string environment = NewRandomString();
            string fundingStreamId = NewRandomString();
            DateTimeOffset dateTime = NewDateTimeOffset();

            _message = NewUserPermissionCsvGenerationMessage(_ => _.WithEnvironment(environment).WithFundingStreamId(fundingStreamId).WithReportRunTime(dateTime));

            AndGetUsersWithFundingStreamPermissions(fundingStreamId, null);

            string errorMessage = $"Unable to generate CSV for {JobConstants.DefinitionNames.GenerateFundingStreamPermissionsCsvJob} " +
                    $"for funding stream ID {fundingStreamId}. Failed to retrieve funding stream permissions items from repository";

            Func<Task> test = async () => await WhenTheCsvIsGenerated();

            test
                .Should()
                .ThrowExactly<NonRetriableException>()
                .Which
                .Message
                .Should()
                .Be(errorMessage);
        }

        [TestMethod]
        public async Task TransformsFundingStreamPermissionsForUsersAndCreatesCsvWithResults()
        {
            string environment = NewRandomString();
            string fundingStreamId = NewRandomString();
            DateTimeOffset dateTime = NewDateTimeOffset();

            string userId = NewRandomString();
            string jobDefinitionName = JobConstants.DefinitionNames.GenerateFundingStreamPermissionsCsvJob;

            _message = NewUserPermissionCsvGenerationMessage(_ => _.WithEnvironment(environment).WithFundingStreamId(fundingStreamId).WithReportRunTime(dateTime));

            string fileName = $"permissions-funding-stream-{_message.Environment}-{_message.FundingStreamId}-{_message.ReportRunTime:yyyy-MM-dd-HH-mm-ss}.csv";
            string expectedInterimFilePath = Path.Combine(_rootPath, fileName);

            IEnumerable<User> users = new List<User>
            {
                new User
                {
                    UserId = userId
                }
            };
            GivenGetAllUsers(users);

            IEnumerable<FundingStreamPermission> fundingStreamPermissions = new List<FundingStreamPermission>
            {
                new FundingStreamPermission
                {
                    UserId = userId,
                    FundingStreamId = fundingStreamId,
                    CanAdministerFundingStream = true
                }
            };
            AndGetUsersWithFundingStreamPermissions(fundingStreamId, fundingStreamPermissions);

            ExpandoObject[] transformedRowsOne = {
                new ExpandoObject()
            };
            string expectedCsvOne = NewRandomString();

            MemoryStream incrementalFileStream = new MemoryStream();

            AndTheCsvRowTransformation(transformedRowsOne, expectedCsvOne, false);
            AndTheCloudBlobForUserId(fileName, UsersReportContainerName);
            AndTheFileStream(expectedInterimFilePath, incrementalFileStream);
            AndTheFileExists(expectedInterimFilePath);
            AndTheTransformForJobDefinition(jobDefinitionName);

            await WhenTheCsvIsGenerated();

            _fileSystemAccess
                .Verify(_ => _.Delete(expectedInterimFilePath),
                    Times.Once);

            _fileSystemAccess
                .Verify(_ => _.Append(expectedInterimFilePath,
                        It.IsAny<string>(),
                        default),
                    Times.Exactly(1));

            _blobClient
                .Verify(_ => _.UploadFileAsync(_cloudBlob.Object, incrementalFileStream),
                    Times.Once);

            AndBlobMetadataSet(fundingStreamId);
        }

        private void AndBlobMetadataSet(string fundingStreamId)
        {
            _blobClient
            .Verify(_ => _.AddMetadataAsync(
                _cloudBlob.Object,
                It.Is<IDictionary<string, string>>(d =>
                    d.ContainsKey("funding-stream-id") && d["funding-stream-id"] == fundingStreamId)),
                Times.Once);
        }

        private void AndTheCloudBlobForUserId(string fileName, string containerName)
        {
            _blobClient
                .Setup(_ => _.GetBlockBlobReference(fileName, containerName))
                .Returns(_cloudBlob.Object);
        }

        private void AndTheFileStream(string path, Stream stream)
        {
            _fileSystemAccess.Setup(_ => _.OpenRead(path))
                .Returns(stream);
        }

        private void AndTheFileExists(string path)
        {
            _fileSystemAccess.Setup(_ => _.Exists(path))
                .Returns(true);
        }

        private void AndTheCsvRowTransformation(IEnumerable<ExpandoObject> transformedRows, string csv, bool outputHeaders)
        {
            _transformation
                .Setup(_ => _.Transform(It.IsAny<IEnumerable<dynamic>>()))
                .Returns(transformedRows);

            _csvUtils
                .Setup(_ => _.AsCsv(transformedRows, outputHeaders))
                .Returns(csv);
        }

        private void AndTheTransformForJobDefinition(string jobDefinitionName)
        {
            _usersCsvTransformServiceLocator.Setup(_ => _.GetService(jobDefinitionName))
                .Returns(_transformation.Object);
        }

        private void GivenGetAllUsers(IEnumerable<User> users)
        {
            _userRepository
                .Setup(_ => _.GetAllUsers())
                .ReturnsAsync(users);
        }

        private void AndGetUsersWithFundingStreamPermissions(
            string fundingStreamId,
            IEnumerable<FundingStreamPermission> fundingStreamPermissions)
        {
            _userRepository
                .Setup(_ => _.GetUsersWithFundingStreamPermissions(fundingStreamId))
                .ReturnsAsync(fundingStreamPermissions);
        }

        private async Task WhenTheCsvIsGenerated()
        {
            await _service.Generate(_message);
        }


        private UserPermissionCsvGenerationMessage NewUserPermissionCsvGenerationMessage(Action<UserPermissionCsvGenerationMessageBuilder> setUp = null)
        {
            UserPermissionCsvGenerationMessageBuilder userPermissionCsvGenerationMessageBuilder = new UserPermissionCsvGenerationMessageBuilder();

            setUp?.Invoke(userPermissionCsvGenerationMessageBuilder);

            return userPermissionCsvGenerationMessageBuilder.Build();
        }

        private static RandomString NewRandomString() => new RandomString();

        private static RandomDateTime NewDateTimeOffset() => new RandomDateTime();

    }
}
