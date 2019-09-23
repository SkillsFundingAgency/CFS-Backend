using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.Caching;
using CalculateFunding.Models.Providers;
using CalculateFunding.Models.Providers.ViewModels;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Providers.Interfaces;
using CalculateFunding.Services.Providers.Validators;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage.Blob;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Providers.UnitTests
{
    [TestClass]
    public class ProviderVersionServiceTests
    {
        [TestMethod]
        public async Task UploadProviderVersion_WhenAllPropertiesPopulated()
        {
            // Arrange
            string id = "1";
            string providerVersionId = "2";
            string actionName = "Action";
            string controller = "Fat";
            string description = "Hello, world!";
            string inputProviderVersionId = "3";

            ProviderVersionViewModel providerVersionViewModel = CreateProviderVersion();

            ProviderVersion providerVersion = new ProviderVersion
            {
                Id = id,
                ProviderVersionId = providerVersionId,
                Description = description
            };

            ICacheProvider cacheProvider = CreateCacheProvider();
            providerVersionViewModel.VersionType = ProviderVersionType.Custom;

            UploadProviderVersionValidator uploadProviderVersionValidator = new UploadProviderVersionValidator();

            ICloudBlob cloudBlob = CreateCloudBlob();

            IBlobClient blobClient = CreateBlobClient();
            blobClient.GetBlockBlobReference(Arg.Any<string>()).Returns(cloudBlob);

            IMapper mapper = Substitute.For<IMapper>();
            mapper
                .Map<ProviderVersion>(providerVersionViewModel)
                .Returns(providerVersion);

            IProviderVersionsMetadataRepository providerVersionsMetadataRepository = CreateProviderVersionMetadataRepository();

            IProviderVersionService providerService = CreateProviderVersionService(blobClient: blobClient,
                providerVersionModelValidator: uploadProviderVersionValidator,
                cacheProvider: cacheProvider,
                mapper: mapper,
                providerVersionMetadataRepository: providerVersionsMetadataRepository);

            // Act
            IActionResult result = await providerService.UploadProviderVersion(actionName,
                controller,
                inputProviderVersionId,
                providerVersionViewModel);

            //Assert
            result.Should().BeOfType<CreatedAtActionResult>();

            CreatedAtActionResult typedResult = result as CreatedAtActionResult;
            typedResult.ActionName
                .Should()
                .Be(actionName);

            typedResult.ControllerName
                .Should()
                .Be(controller);

            typedResult.RouteValues.Count.Should().Be(1);
            typedResult.RouteValues.Single().Key
                .Should()
                .Be("providerVersionId");

            typedResult.RouteValues.Single().Value
                .Should()
                .Be(inputProviderVersionId);

            typedResult.Value
                .Should()
                .Be(inputProviderVersionId);

            blobClient
                .Received(1)
                .GetBlockBlobReference(Arg.Any<string>());

            await cloudBlob
                .Received(1)
                .UploadFromStreamAsync(Arg.Any<MemoryStream>());

            await providerVersionsMetadataRepository
                .Received(1)
                .CreateProviderVersion(Arg.Is<ProviderVersionMetadata>(x =>
                    x.ProviderVersionId == providerVersionId
                    && x.Description == description));
        }

        [TestMethod]
        public void UploadProviderVersion_WhenVersionIdEmpty_UploadFails()
        {
            // Arrange
            ProviderVersionViewModel providerVersionViewModel = CreateProviderVersion();

            providerVersionViewModel.ProviderVersionId = null;
            providerVersionViewModel.VersionType = ProviderVersionType.Custom;

            ICacheProvider cacheProvider = CreateCacheProvider();

            UploadProviderVersionValidator uploadProviderVersionValidator = new UploadProviderVersionValidator();

            IBlobClient blobClient = CreateBlobClient();

            IProviderVersionService providerService = CreateProviderVersionService(blobClient: blobClient,
                providerVersionModelValidator: uploadProviderVersionValidator, cacheProvider: cacheProvider);

            // Act
            Func<Task> result = async () =>
                await providerService.UploadProviderVersion("Action", "Controller", providerVersionViewModel.ProviderVersionId, providerVersionViewModel);

            result
                .Should()
                .Throw<ArgumentNullException>();
        }

        [TestMethod]
        public async Task UploadProviderVersion_WhenDescriptionEmpty_UploadFails()
        {
            // Arrange
            ProviderVersionViewModel providerVersionViewModel = CreateProviderVersion();

            providerVersionViewModel.Description = null;
            providerVersionViewModel.VersionType = ProviderVersionType.Custom;

            ICacheProvider cacheProvider = CreateCacheProvider();

            UploadProviderVersionValidator uploadProviderVersionValidator = new UploadProviderVersionValidator();

            IBlobClient blobClient = CreateBlobClient();

            IProviderVersionService providerService = CreateProviderVersionService(blobClient: blobClient, providerVersionModelValidator: uploadProviderVersionValidator, cacheProvider: cacheProvider);

            // Act
            IActionResult badRequest = await providerService.UploadProviderVersion("Action", "Controller", providerVersionViewModel.ProviderVersionId, providerVersionViewModel);

            badRequest
                .Should()
                .BeOfType<BadRequestObjectResult>();

            BadRequestObjectResult badRequestObject = badRequest as BadRequestObjectResult;
            SerializableError validationErrors = badRequestObject.Value as SerializableError;

            validationErrors
                .Count
                .Should()
                .Be(1);

            ((string[])validationErrors["Description"])[0]
                .Should()
                .Be("No provider description was provided to UploadProviderVersion");
        }

        [TestMethod]
        public async Task UploadProviderVersion_WhenVersionExists_UploadFails()
        {
            // Arrange
            ProviderVersionViewModel providerVersionViewModel = CreateProviderVersion();
            providerVersionViewModel.VersionType = ProviderVersionType.Custom;

            UploadProviderVersionValidator uploadProviderVersionValidator = new UploadProviderVersionValidator();

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .BlobExistsAsync(providerVersionViewModel.ProviderVersionId + ".json")
                .Returns(true);

            IProviderVersionService providerService = CreateProviderVersionService(blobClient: blobClient, providerVersionModelValidator: uploadProviderVersionValidator);

            // Act
            IActionResult conflictResponse = await providerService.UploadProviderVersion("Action", "Controller", providerVersionViewModel.ProviderVersionId, providerVersionViewModel);

            // Assert
            conflictResponse
                .Should()
                .BeOfType<ConflictResult>();
        }

        [TestMethod]
        public async Task UploadProviderVersion_WhenNameProviderTypeVersionFundingStreamExists_UploadFails()
        {
            // Arrange
            ProviderVersionViewModel providerVersionViewModel = CreateProviderVersion();

            IValidator<ProviderVersionViewModel> validator = Substitute.For<IValidator<ProviderVersionViewModel>>();
            validator
                .ValidateAsync(Arg.Any<ProviderVersionViewModel>())
                .Returns(new ValidationResult());

            IProviderVersionsMetadataRepository repository = Substitute.For<IProviderVersionsMetadataRepository>();
            repository
                .Exists(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>())
                .Returns(true);

            IProviderVersionService providerService = CreateProviderVersionService(providerVersionModelValidator: validator,
                providerVersionMetadataRepository: repository);

            // Act
            IActionResult conflictResponse = await providerService.UploadProviderVersion("Action", "Controller", providerVersionViewModel.ProviderVersionId, providerVersionViewModel);

            // Assert
            conflictResponse
                .Should()
                .BeOfType<ConflictResult>();
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task Exists_CallsCorrectly(bool exists)
        {
            string name = "A";
            string providerVersion = "B";
            int version = 42;
            string fundingStream = "C";

            IProviderVersionsMetadataRepository repository = Substitute.For<IProviderVersionsMetadataRepository>();
            repository
                .Exists(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>())
                .Returns(exists);

            IProviderVersionService providerService = CreateProviderVersionService(providerVersionMetadataRepository: repository);

            ProviderVersionViewModel providerVersionViewModel = new ProviderVersionViewModel
            {
                Name = name,
                ProviderVersionTypeString = providerVersion,
                Version = version,
                FundingStream = fundingStream
            };

            bool result = await providerService.Exists(providerVersionViewModel);

            Assert.AreEqual(exists, result);

            await repository
                .Received(1)
                .Exists(name, providerVersion, version, fundingStream);
        }

        [TestMethod]
        public async Task UpdateProviderData_WhenProvidersEmpty_UploadFails()
        {
            // Arrange
            ProviderVersionViewModel providerVersionViewModel = CreateProviderVersion();

            providerVersionViewModel.Providers = new List<Provider>();
            providerVersionViewModel.VersionType = ProviderVersionType.Custom;

            ICacheProvider cacheProvider = CreateCacheProvider();

            UploadProviderVersionValidator uploadProviderVersionValidator = new UploadProviderVersionValidator();

            IBlobClient blobClient = CreateBlobClient();

            IProviderVersionService providerService = CreateProviderVersionService(blobClient: blobClient, providerVersionModelValidator: uploadProviderVersionValidator, cacheProvider: cacheProvider);

            // Act
            IActionResult badRequest = await providerService.UploadProviderVersion("Action", "Controller", providerVersionViewModel.ProviderVersionId, providerVersionViewModel);

            badRequest
                .Should()
                .BeOfType<BadRequestObjectResult>();

            BadRequestObjectResult badRequestObject = badRequest as BadRequestObjectResult;
            SerializableError validationErrors = badRequestObject.Value as SerializableError;

            validationErrors
                .Count
                .Should()
                .Be(1);

            ((string[])validationErrors["Providers"])[0]
                .Should()
                .Be("No providers were provided to UploadProviderVersion");
        }

        [TestMethod]
        public async Task UpdateProviderData_WhenVersionTypeEmpty_UploadFails()
        {
            // Arrange
            ProviderVersionViewModel providerVersionViewModel = CreateProviderVersion();

            ICacheProvider cacheProvider = CreateCacheProvider();

            UploadProviderVersionValidator uploadProviderVersionValidator = new UploadProviderVersionValidator();

            IBlobClient blobClient = CreateBlobClient();

            IProviderVersionService providerService = CreateProviderVersionService(blobClient: blobClient, providerVersionModelValidator: uploadProviderVersionValidator, cacheProvider: cacheProvider);

            // Act
            IActionResult badRequest = await providerService.UploadProviderVersion("Action", "Controller", providerVersionViewModel.ProviderVersionId, providerVersionViewModel);

            badRequest
                .Should()
                .BeOfType<BadRequestObjectResult>();

            BadRequestObjectResult badRequestObject = badRequest as BadRequestObjectResult;
            SerializableError validationErrors = badRequestObject.Value as SerializableError;

            validationErrors
                .Count
                .Should()
                .Be(1);

            ((string[])validationErrors["VersionType"])[0]
                .Should()
                .Be("No provider version type provided to UploadProviderVersion");
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(int.MinValue)]
        public async Task UpdateProviderData_WhenVersionBelow1_UploadFails(int version)
        {
            // Arrange
            ProviderVersionViewModel providerVersionViewModel = CreateProviderVersion();
            providerVersionViewModel.VersionType = ProviderVersionType.Custom;
            providerVersionViewModel.Version = version;

            ICacheProvider cacheProvider = CreateCacheProvider();

            UploadProviderVersionValidator uploadProviderVersionValidator = new UploadProviderVersionValidator();

            IBlobClient blobClient = CreateBlobClient();

            IProviderVersionService providerService = CreateProviderVersionService(blobClient: blobClient, providerVersionModelValidator: uploadProviderVersionValidator, cacheProvider: cacheProvider);

            // Act
            IActionResult badRequest = await providerService.UploadProviderVersion("Action", "Controller", providerVersionViewModel.ProviderVersionId, providerVersionViewModel);

            badRequest
                .Should()
                .BeOfType<BadRequestObjectResult>();

            BadRequestObjectResult badRequestObject = badRequest as BadRequestObjectResult;
            SerializableError validationErrors = badRequestObject.Value as SerializableError;

            validationErrors
                .Count
                .Should()
                .Be(1);

            ((string[])validationErrors["Version"])[0]
                .Should()
                .Be("Version number must be greater than zero");
        }

        [TestMethod]
        public async Task UpdateProviderData_WhenNoTargetDate_UploadFails()
        {
            // Arrange
            ProviderVersionViewModel providerVersionViewModel = CreateProviderVersion();
            providerVersionViewModel.VersionType = ProviderVersionType.Custom;
            providerVersionViewModel.TargetDate = DateTimeOffset.MinValue;

            ICacheProvider cacheProvider = CreateCacheProvider();

            UploadProviderVersionValidator uploadProviderVersionValidator = new UploadProviderVersionValidator();

            IBlobClient blobClient = CreateBlobClient();

            IProviderVersionService providerService = CreateProviderVersionService(blobClient: blobClient, providerVersionModelValidator: uploadProviderVersionValidator, cacheProvider: cacheProvider);

            // Act
            IActionResult badRequest = await providerService.UploadProviderVersion("Action", "Controller", providerVersionViewModel.ProviderVersionId, providerVersionViewModel);

            badRequest
                .Should()
                .BeOfType<BadRequestObjectResult>();

            BadRequestObjectResult badRequestObject = badRequest as BadRequestObjectResult;
            SerializableError validationErrors = badRequestObject.Value as SerializableError;

            validationErrors
                .Count
                .Should()
                .Be(1);

            ((string[])validationErrors["TargetDate"])[0]
                .Should()
                .Be("No target date provided to UploadProviderVersion");
        }

        [TestMethod]
        public async Task UpdateProviderData_WhenNoFundingStreamAndVersionTypeCustom_UploadFails()
        {
            // Arrange
            ProviderVersionViewModel providerVersionViewModel = CreateProviderVersion();
            providerVersionViewModel.VersionType = ProviderVersionType.Custom;
            providerVersionViewModel.FundingStream = "";

            ICacheProvider cacheProvider = CreateCacheProvider();

            UploadProviderVersionValidator uploadProviderVersionValidator = new UploadProviderVersionValidator();

            IBlobClient blobClient = CreateBlobClient();

            IProviderVersionService providerService = CreateProviderVersionService(blobClient: blobClient, providerVersionModelValidator: uploadProviderVersionValidator, cacheProvider: cacheProvider);

            // Act
            IActionResult badRequest = await providerService.UploadProviderVersion("Action", "Controller", providerVersionViewModel.ProviderVersionId, providerVersionViewModel);

            badRequest
                .Should()
                .BeOfType<BadRequestObjectResult>();

            BadRequestObjectResult badRequestObject = badRequest as BadRequestObjectResult;
            SerializableError validationErrors = badRequestObject.Value as SerializableError;

            validationErrors
                .Count
                .Should()
                .Be(1);

            ((string[])validationErrors["FundingStream"])[0]
                .Should()
                .Be("No funding stream provided to UploadProviderVersion with a custom provider version");
        }

        [TestMethod]
        public async Task UpdateProviderData_WhenNoFundingStreamAndVersionTypeNotCustom_UploadSucceeds()
        {
            // Arrange
            ProviderVersionViewModel providerVersionViewModel = CreateProviderVersion();
            providerVersionViewModel.VersionType = ProviderVersionType.SystemImported;
            providerVersionViewModel.FundingStream = "";

            ICacheProvider cacheProvider = CreateCacheProvider();

            UploadProviderVersionValidator uploadProviderVersionValidator = new UploadProviderVersionValidator();

            IBlobClient blobClient = CreateBlobClient();

            IProviderVersionService providerService = CreateProviderVersionService(blobClient: blobClient, providerVersionModelValidator: uploadProviderVersionValidator, cacheProvider: cacheProvider);

            // Act
            IActionResult request = await providerService.UploadProviderVersion("Action", "Controller", providerVersionViewModel.ProviderVersionId, providerVersionViewModel);

            request
                .Should()
                .BeOfType<CreatedAtActionResult>();
        }

        [TestMethod]
        public async Task UpdateProviderData_WhenAllRequiredProviderPropertiesEmpty_UploadFails()
        {
            // Arrange
            ProviderVersionViewModel providerVersionViewModel = CreateProviderVersion();

            providerVersionViewModel.VersionType = ProviderVersionType.Custom;

            providerVersionViewModel.Providers.ToList().ForEach(x =>
            {
                x.UKPRN = null;
                x.LACode = null;
                x.Name = null;
                x.Status = null;
                x.TrustStatusViewModelString = null;
            });

            ICacheProvider cacheProvider = CreateCacheProvider();

            UploadProviderVersionValidator uploadProviderVersionValidator = new UploadProviderVersionValidator();

            IBlobClient blobClient = CreateBlobClient();

            IProviderVersionService providerService = CreateProviderVersionService(blobClient: blobClient, providerVersionModelValidator: uploadProviderVersionValidator, cacheProvider: cacheProvider);

            // Act
            IActionResult badRequest = await providerService.UploadProviderVersion("Action", "Controller", providerVersionViewModel.ProviderVersionId, providerVersionViewModel);

            badRequest
                .Should()
                .BeOfType<BadRequestObjectResult>();

            BadRequestObjectResult badRequestObject = badRequest as BadRequestObjectResult;
            SerializableError validationErrors = badRequestObject.Value as SerializableError;

            ((string[])validationErrors["Providers"])[0]
                .Should()
                .Be($"No UKPRN specified for '{providerVersionViewModel.Providers.First().Name}' was provided to UploadProviderVersion");

            ((string[])validationErrors["Providers"])[1]
                .Should()
                .Be($"No LACode specified for '{providerVersionViewModel.Providers.First().Name}' was provided to UploadProviderVersion");

            ((string[])validationErrors["Providers"])[2]
                .Should()
                .Be($"No establishment name specified for '{providerVersionViewModel.Providers.First().Name}' was provided to UploadProviderVersion");

            ((string[])validationErrors["Providers"])[3]
                .Should()
                .Be($"No status specified for '{providerVersionViewModel.Providers.First().Name}' was provided to UploadProviderVersion");

            ((string[])validationErrors["Providers"])[4]
                .Should()
                .Be($"No trust status specified for '{providerVersionViewModel.Providers.First().Name}' was provided to UploadProviderVersion");
        }

        [TestMethod]
        public async Task UpdateProviderData_WhenDuplicateUKPRN_UploadFails()
        {
            // Arrange
            ProviderVersionViewModel providerVersionViewModel = CreateProviderVersion();

            providerVersionViewModel.Providers = providerVersionViewModel.Providers.Concat(new[] { GetProviderViewModel() });
            providerVersionViewModel.VersionType = ProviderVersionType.Custom;

            ICacheProvider cacheProvider = CreateCacheProvider();

            UploadProviderVersionValidator uploadProviderVersionValidator = new UploadProviderVersionValidator();

            IBlobClient blobClient = CreateBlobClient();

            IProviderVersionService providerService = CreateProviderVersionService(blobClient: blobClient, providerVersionModelValidator: uploadProviderVersionValidator, cacheProvider: cacheProvider);

            // Act
            IActionResult badRequest = await providerService.UploadProviderVersion("Action", "Controller", providerVersionViewModel.ProviderVersionId, providerVersionViewModel);

            badRequest
                .Should()
                .BeOfType<BadRequestObjectResult>();

            BadRequestObjectResult badRequestObject = badRequest as BadRequestObjectResult;
            SerializableError validationErrors = badRequestObject.Value as SerializableError;

            ((string[])validationErrors["Providers"])[0]
                .Should()
                .Be($"Duplicate UKPRN specified for {providerVersionViewModel.Providers.First().UKPRN} was provided to UploadProviderVersion");
        }

        [TestMethod]
        public async Task GetAllProviders_WhenProviderVersionDoesntExist_BlobNotFoundReturned()
        {
            // Arrange
            ProviderVersionViewModel providerVersionViewModel = CreateProviderVersion();

            providerVersionViewModel.Providers = providerVersionViewModel.Providers.Concat(new[] { GetProviderViewModel() });
            providerVersionViewModel.VersionType = ProviderVersionType.Custom;

            ICacheProvider cacheProvider = CreateCacheProvider();

            UploadProviderVersionValidator uploadProviderVersionValidator = new UploadProviderVersionValidator();

            ICloudBlob cloudBlob = CreateCloudBlob();
            cloudBlob.Exists().Returns(false);

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .GetBlockBlobReference(Arg.Any<string>())
                .Returns(cloudBlob);

            IProviderVersionService providerService = CreateProviderVersionService(blobClient: blobClient, providerVersionModelValidator: uploadProviderVersionValidator, cacheProvider: cacheProvider);

            // Act
            IActionResult notFoundRequest = await providerService.GetAllProviders(providerVersionViewModel.ProviderVersionId, true);

            blobClient
                .Received(1)
                .GetBlockBlobReference(Arg.Any<string>());

            notFoundRequest
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task DoesProviderVersionExist_WhenProviderVersionExists_NoContentReturned()
        {
            // Arrange
            IBlobClient blobClient = CreateBlobClient();

            blobClient
                .BlobExistsAsync(Arg.Any<string>())
                .Returns(true);

            IProviderVersionService providerService = CreateProviderVersionService(blobClient: blobClient);

            // Act
            IActionResult noContentResult = await providerService.DoesProviderVersionExist(Guid.NewGuid().ToString());

            noContentResult
                .Should()
                .BeOfType<NoContentResult>();
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(true, true)]
        public async Task Exists_ReturnsAsExpected(bool exists, bool output)
        {
            // Arrange
            IBlobClient blobClient = CreateBlobClient();

            blobClient
                .BlobExistsAsync(Arg.Any<string>())
                .Returns(exists);

            IProviderVersionService providerService = CreateProviderVersionService(blobClient: blobClient);

            // Act
            bool result = await providerService.Exists(Guid.NewGuid().ToString());

            exists
                .Should()
                .Be(output);
        }

        [TestMethod]
        public async Task DoesProviderVersionExist_WhenProviderVersionDoesntExist_NotFoundReturned()
        {
            // Arrange
            IBlobClient blobClient = CreateBlobClient();

            blobClient
                .BlobExistsAsync(Arg.Any<string>())
                .Returns(false);

            IProviderVersionService providerService = CreateProviderVersionService(blobClient: blobClient);

            // Act
            IActionResult notFoundResult = await providerService.DoesProviderVersionExist(Guid.NewGuid().ToString());

            notFoundResult
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetAllProviders_WhenProviderVersionIdExists_ProviderListReturned()
        {
            // Arrange
            ProviderVersionViewModel providerVersionViewModel = CreateProviderVersion();

            providerVersionViewModel.Providers = providerVersionViewModel.Providers.Concat(new[] { GetProviderViewModel() });
            providerVersionViewModel.VersionType = ProviderVersionType.Custom;

            IMapper mapper = CreateMapper();

            ProviderVersion providerVersion = mapper.Map<ProviderVersion>(providerVersionViewModel);

            ICacheProvider cacheProvider = CreateCacheProvider();

            UploadProviderVersionValidator uploadProviderVersionValidator = new UploadProviderVersionValidator();

            ICloudBlob cloudBlob = CreateCloudBlob();
            cloudBlob.Exists().Returns(true);

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .GetBlockBlobReference(Arg.Any<string>())
                .Returns(cloudBlob);

            MemoryStream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(providerVersion)));

            blobClient
                .DownloadToStreamAsync(cloudBlob)
                .Returns(memoryStream);

            IProviderVersionService providerService = CreateProviderVersionService(blobClient: blobClient, providerVersionModelValidator: uploadProviderVersionValidator, cacheProvider: cacheProvider, mapper: mapper);

            // Act
            IActionResult okRequest = await providerService.GetAllProviders(providerVersionViewModel.ProviderVersionId, true);

            blobClient
                .Received(1)
                .GetBlockBlobReference(Arg.Any<string>());

            await blobClient.Received(1)
                .DownloadToStreamAsync(Arg.Any<ICloudBlob>());

            await cacheProvider
                .Received(1)
                .SetAsync(Arg.Any<string>(), Arg.Any<ProviderVersion>(), Arg.Any<TimeSpan>(), Arg.Any<bool>());

            okRequest
                .Should()
                .BeOfType<OkObjectResult>();
        }

        [TestMethod]
        public async Task GetProviderVersions_WhenAFundingStreamProvided_ProviderVersionsReturned()
        {
            // Arrange
            ProviderVersionViewModel providerVersionViewModel = CreateProviderVersion();

            string fundingStream = "funndingStream1";

            IProviderVersionsMetadataRepository providerVersionsMetadataRepository = CreateProviderVersionMetadataRepository();

            providerVersionsMetadataRepository
                .GetProviderVersions(Arg.Is<string>(fundingStream))
                .Returns<IEnumerable<ProviderVersionMetadata>>(new ProviderVersionMetadata[] { new ProviderVersionMetadata { FundingStream = fundingStream, ProviderVersionId = providerVersionViewModel.ProviderVersionId } });

            IProviderVersionService providerService = CreateProviderVersionService(providerVersionMetadataRepository: providerVersionsMetadataRepository);

            // Act
            IActionResult result = await providerService.GetProviderVersionsByFundingStream(fundingStream);

            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeAssignableTo<IEnumerable<ProviderVersionMetadata>>();
        }

        [TestMethod]
        public async Task GetProviderVersions_WhenAFundingStreamWithNoProviderVersionAssociated_NotFoundReturned()
        {
            // Arrange
            ProviderVersionViewModel providerVersionViewModel = CreateProviderVersion();

            string fundingStream = "funndingStream1";

            IProviderVersionsMetadataRepository providerVersionsMetadataRepository = CreateProviderVersionMetadataRepository();

            providerVersionsMetadataRepository
                .GetProviderVersions(Arg.Is<string>(fundingStream))
                .Returns(Task.FromResult<IEnumerable<ProviderVersionMetadata>>(null));

            IProviderVersionService providerService = CreateProviderVersionService(providerVersionMetadataRepository: providerVersionsMetadataRepository);

            // Act
            IActionResult result = await providerService.GetProviderVersionsByFundingStream(fundingStream);

            result
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [TestMethod]
        [DataRow(12, 12, 2019)]
        public async Task GetAllProviders_WhenADateIsSetAgainstProviderVersion_ProviderListReturned(int day, int month, int year)
        {
            // Arrange
            ProviderVersionViewModel providerVersionViewModel = CreateProviderVersion();

            providerVersionViewModel.Providers = providerVersionViewModel.Providers.Concat(new[] { GetProviderViewModel() });
            providerVersionViewModel.VersionType = ProviderVersionType.Custom;

            ICacheProvider cacheProvider = CreateCacheProvider();

            UploadProviderVersionValidator uploadProviderVersionValidator = new UploadProviderVersionValidator();

            ICloudBlob cloudBlob = CreateCloudBlob();
            cloudBlob.Exists().Returns(true);

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .GetBlockBlobReference(Arg.Any<string>())
                .Returns(cloudBlob);

            IProviderVersionsMetadataRepository providerVersionsMetadataRepository = CreateProviderVersionMetadataRepository();

            providerVersionsMetadataRepository
                .GetProviderVersionByDate(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>())
                .Returns<ProviderVersionByDate>(new ProviderVersionByDate { Day = day, Month = month, Year = year, ProviderVersionId = providerVersionViewModel.ProviderVersionId });


            IProviderVersionService providerService = CreateProviderVersionService(blobClient: blobClient, providerVersionModelValidator: uploadProviderVersionValidator, cacheProvider: cacheProvider, providerVersionMetadataRepository: providerVersionsMetadataRepository);

            // Act
            IActionResult okRequest = await providerService.GetAllProviders(year, month, day);

            blobClient
                .Received(1)
                .GetBlockBlobReference(Arg.Any<string>());

            await blobClient.Received(1)
                .DownloadToStreamAsync(Arg.Any<ICloudBlob>());
        }

        [TestMethod]
        public async Task GetAllMasterProviders_WhenAllMasterProvidersRequestedAndProviderVersionSet_MasterProvidersReturned()
        {
            // Arrange
            ProviderVersionViewModel providerVersionViewModel = CreateProviderVersion();

            providerVersionViewModel.Providers = providerVersionViewModel.Providers.Concat(new[] { GetProviderViewModel() });
            providerVersionViewModel.VersionType = ProviderVersionType.Custom;

            ICacheProvider cacheProvider = CreateCacheProvider();

            UploadProviderVersionValidator uploadProviderVersionValidator = new UploadProviderVersionValidator();

            ICloudBlob cloudBlob = CreateCloudBlob();
            cloudBlob.Exists().Returns(true);

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .GetBlockBlobReference(Arg.Any<string>())
                        .Returns(cloudBlob);

            IProviderVersionsMetadataRepository providerVersionsMetadataRepository = CreateProviderVersionMetadataRepository();

            providerVersionsMetadataRepository
                .GetMasterProviderVersion()
                .Returns<MasterProviderVersion>(new MasterProviderVersion { ProviderVersionId = providerVersionViewModel.ProviderVersionId });

            IProviderVersionService providerService = CreateProviderVersionService(blobClient: blobClient, providerVersionModelValidator: uploadProviderVersionValidator, cacheProvider: cacheProvider, providerVersionMetadataRepository: providerVersionsMetadataRepository);

            // Act
            IActionResult okRequest = await providerService.GetAllMasterProviders();

            blobClient
                .Received(1)
                        .GetBlockBlobReference(Arg.Any<string>());

            await blobClient.Received(1)
                .DownloadToStreamAsync(Arg.Any<ICloudBlob>());
        }

        [TestMethod]
        public async Task SetMasterProviderVersion_GivenAValidProviderVersion_MasterProviderIsSet()
        {
            // Arrange
            MasterProviderVersionViewModel masterProviderVersionViewModel = new MasterProviderVersionViewModel { ProviderVersionId = Guid.NewGuid().ToString() };

            ICacheProvider cacheProvider = CreateCacheProvider();

            cacheProvider
                .GetAsync<MasterProviderVersion>(Arg.Any<string>())
                .Returns(new MasterProviderVersion());

            UploadProviderVersionValidator uploadProviderVersionValidator = new UploadProviderVersionValidator();

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .BlobExistsAsync(Arg.Any<string>())
                        .Returns(true);

            IProviderVersionsMetadataRepository providerVersionsMetadataRepository = CreateProviderVersionMetadataRepository();

            IProviderVersionService providerService = CreateProviderVersionService(blobClient: blobClient, providerVersionModelValidator: uploadProviderVersionValidator, cacheProvider: cacheProvider, providerVersionMetadataRepository: providerVersionsMetadataRepository);

            // Act
            IActionResult okRequest = await providerService.SetMasterProviderVersion(masterProviderVersionViewModel);

            await cacheProvider
                .Received(1)
                .RemoveAsync<MasterProviderVersion>(Arg.Any<string>());

            await providerVersionsMetadataRepository
                .Received(1)
                .UpsertMaster(Arg.Any<MasterProviderVersion>());
        }

        [TestMethod]
        [DataRow(12, 12, 2019)]
        public async Task SetProviderVersionByDate_GivenAValidProviderVersion_ProviderVersionDateIsSet(int day, int month, int year)
        {
            // Arrange
            ICacheProvider cacheProvider = CreateCacheProvider();

            cacheProvider
                .GetAsync<ProviderVersionByDate>(Arg.Any<string>())
                .Returns(new ProviderVersionByDate());

            UploadProviderVersionValidator uploadProviderVersionValidator = new UploadProviderVersionValidator();

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .BlobExistsAsync(Arg.Any<string>())
                        .Returns(true);

            IProviderVersionsMetadataRepository providerVersionsMetadataRepository = CreateProviderVersionMetadataRepository();

            IProviderVersionService providerService = CreateProviderVersionService(blobClient: blobClient, providerVersionModelValidator: uploadProviderVersionValidator, cacheProvider: cacheProvider, providerVersionMetadataRepository: providerVersionsMetadataRepository);

            // Act
            IActionResult okRequest = await providerService.SetProviderVersionByDate(day, month, year, Guid.NewGuid().ToString());

            await cacheProvider
                .Received(1)
                .RemoveAsync<ProviderVersionByDate>(Arg.Any<string>());

            await providerVersionsMetadataRepository
                .Received(1)
                .UpsertProviderVersionByDate(Arg.Any<ProviderVersionByDate>());
        }

        [TestMethod]
        public async Task WhenProviderVersionMetadataRequestedAndNotInCacheAndExistsInRepository_ThenResponseReturned()
        {
            // Arrange
            string providerVersionId = "testProvVersion";

            ICacheProvider cacheProvider = CreateCacheProvider();
            IProviderVersionsMetadataRepository providerVersionsMetadataRepository = CreateProviderVersionMetadataRepository();

            string cacheKey = $"{CacheKeys.ProviderVersionMetadata}:{providerVersionId}";
            cacheProvider
                .GetAsync<ProviderVersionMetadataDto>(Arg.Is(cacheKey))
                .Returns((ProviderVersionMetadataDto)null);

            ProviderVersionMetadata existingProviderVersionMetadata = new ProviderVersionMetadata()
            {
                ProviderVersionId = providerVersionId,
                Created = new DateTimeOffset(2019, 3, 20, 5, 6, 22, TimeSpan.Zero),
                Description = "Test Description",
                FundingStream = "fundingStreamId",
                Name = "Provider Version Name",
                TargetDate = new DateTimeOffset(2019, 5, 2, 6, 7, 23, TimeSpan.Zero),
                Version = 22,
                VersionType = ProviderVersionType.Custom,
                ProviderVersionTypeString = "Custom",
            };

            providerVersionsMetadataRepository
                .GetProviderVersionMetadata(Arg.Is(providerVersionId))
                .Returns(existingProviderVersionMetadata);

            IProviderVersionService providerService = CreateProviderVersionService(cacheProvider: cacheProvider, providerVersionMetadataRepository: providerVersionsMetadataRepository);

            // Act 
            IActionResult response = await providerService.GetProviderVersionMetadata(providerVersionId);

            // Assert
            ProviderVersionMetadataDto expectedResponse = new ProviderVersionMetadataDto()
            {
                ProviderVersionId = providerVersionId,
                Created = new DateTimeOffset(2019, 3, 20, 5, 6, 22, TimeSpan.Zero),
                Description = "Test Description",
                FundingStream = "fundingStreamId",
                Name = "Provider Version Name",
                TargetDate = new DateTimeOffset(2019, 5, 2, 6, 7, 23, TimeSpan.Zero),
                Version = 22,
                VersionType = ProviderVersionType.Custom,
            };

            response
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeEquivalentTo(expectedResponse);

            await cacheProvider
                .Received(1)
                .GetAsync<ProviderVersionMetadataDto>(Arg.Is(cacheKey));

            await cacheProvider
                .Received(1)
                .SetAsync<ProviderVersionMetadataDto>(Arg.Is(cacheKey), Arg.Is<ProviderVersionMetadataDto>(r =>
                        r.Created == expectedResponse.Created
                        && r.Description == expectedResponse.Description
                        && r.FundingStream == expectedResponse.FundingStream
                        && r.Name == expectedResponse.Name
                        && r.ProviderVersionId == expectedResponse.ProviderVersionId
                        && r.TargetDate == expectedResponse.TargetDate
                        && r.Version == expectedResponse.Version
                        && r.VersionType == expectedResponse.VersionType),
                        Arg.Is<JsonSerializerSettings>((JsonSerializerSettings)null));

            await providerVersionsMetadataRepository
                 .Received(1)
                .GetProviderVersionMetadata(Arg.Is(providerVersionId));
        }

        [TestMethod]
        public async Task WhenProviderVersionMetadataRequestedAnInCache_ThenResponseReturned()
        {
            // Arrange
            string providerVersionId = "testProvVersion";

            ICacheProvider cacheProvider = CreateCacheProvider();
            IProviderVersionsMetadataRepository providerVersionsMetadataRepository = CreateProviderVersionMetadataRepository();

            string cacheKey = $"{CacheKeys.ProviderVersionMetadata}:{providerVersionId}";

            ProviderVersionMetadataDto existingCacheItem = new ProviderVersionMetadataDto()
            {
                ProviderVersionId = providerVersionId,
                Created = new DateTimeOffset(2019, 3, 20, 5, 6, 22, TimeSpan.Zero),
                Description = "Test Description",
                FundingStream = "fundingStreamId",
                Name = "Provider Version Name",
                TargetDate = new DateTimeOffset(2019, 5, 2, 6, 7, 23, TimeSpan.Zero),
                Version = 22,
                VersionType = ProviderVersionType.Custom,
            };

            cacheProvider
               .GetAsync<ProviderVersionMetadataDto>(Arg.Is(cacheKey))
               .Returns(existingCacheItem);

            IProviderVersionService providerService = CreateProviderVersionService(cacheProvider: cacheProvider, providerVersionMetadataRepository: providerVersionsMetadataRepository);

            // Act 
            IActionResult response = await providerService.GetProviderVersionMetadata(providerVersionId);

            // Assert
            ProviderVersionMetadataDto expectedResponse = new ProviderVersionMetadataDto()
            {
                ProviderVersionId = providerVersionId,
                Created = new DateTimeOffset(2019, 3, 20, 5, 6, 22, TimeSpan.Zero),
                Description = "Test Description",
                FundingStream = "fundingStreamId",
                Name = "Provider Version Name",
                TargetDate = new DateTimeOffset(2019, 5, 2, 6, 7, 23, TimeSpan.Zero),
                Version = 22,
                VersionType = ProviderVersionType.Custom,
            };

            response
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeEquivalentTo(expectedResponse);

            await cacheProvider
                .Received(1)
                .GetAsync<ProviderVersionMetadataDto>(Arg.Is(cacheKey));

            await cacheProvider
                .Received(0)
                .SetAsync<ProviderVersionMetadataDto>(Arg.Is(cacheKey), Arg.Any<ProviderVersionMetadataDto>(), Arg.Any<JsonSerializerSettings>());

            await providerVersionsMetadataRepository
               .Received(0)
              .GetProviderVersionMetadata(Arg.Is(providerVersionId));
        }

        [TestMethod]
        public async Task WhenProviderVersionMetadataRequestedAndDoesNotExist_ThenNotFoundObjectReturned()
        {
            // Arrange
            string providerVersionId = "testProvVersion";

            ICacheProvider cacheProvider = CreateCacheProvider();
            IProviderVersionsMetadataRepository providerVersionsMetadataRepository = CreateProviderVersionMetadataRepository();

            string cacheKey = $"{CacheKeys.ProviderVersionMetadata}:{providerVersionId}";
            cacheProvider
                .GetAsync<ProviderVersionMetadataDto>(Arg.Is(cacheKey))
                .Returns((ProviderVersionMetadataDto)null);

            ProviderVersionMetadata existingProviderVersionMetadata = null;

            providerVersionsMetadataRepository
                .GetProviderVersionMetadata(Arg.Is(providerVersionId))
                .Returns(existingProviderVersionMetadata);

            IProviderVersionService providerService = CreateProviderVersionService(cacheProvider: cacheProvider, providerVersionMetadataRepository: providerVersionsMetadataRepository);

            // Act 
            IActionResult response = await providerService.GetProviderVersionMetadata(providerVersionId);

            // Assert
            response
                .Should()
                .BeOfType<NotFoundResult>();

            await cacheProvider
                .Received(1)
                .GetAsync<ProviderVersionMetadataDto>(Arg.Is(cacheKey));

            await cacheProvider
                .Received(0)
                .SetAsync<ProviderVersionMetadataDto>(
                        Arg.Is(cacheKey),
                        Arg.Any<ProviderVersionMetadataDto>(),
                        Arg.Is<JsonSerializerSettings>((JsonSerializerSettings)null));

            await providerVersionsMetadataRepository
                 .Received(1)
                .GetProviderVersionMetadata(Arg.Is(providerVersionId));
        }

        private ICacheProvider CreateCacheProvider()
        {
            return Substitute.For<ICacheProvider>();
        }

        private IBlobClient CreateBlobClient()
        {
            return Substitute.For<IBlobClient>();
        }

        private ICloudBlob CreateCloudBlob()
        {
            return Substitute.For<ICloudBlob>();
        }

        private ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        private IProviderVersionsMetadataRepository CreateProviderVersionMetadataRepository()
        {
            return Substitute.For<IProviderVersionsMetadataRepository>();
        }

        private IProvidersResiliencePolicies CreateResiliencePolicies()
        {
            IProvidersResiliencePolicies providersResiliencePolicies = Substitute.For<IProvidersResiliencePolicies>();
            providersResiliencePolicies.ProviderVersionMetadataRepository = Policy.NoOpAsync();
            providersResiliencePolicies.BlobRepositoryPolicy = Policy.NoOpAsync();
            return providersResiliencePolicies;
        }

        private IValidator<ProviderVersionViewModel> CreateProviderVersionModelValidator()
        {
            IValidator<ProviderVersionViewModel> providerVersionModelValidator = Substitute.For<IValidator<ProviderVersionViewModel>>();
            return providerVersionModelValidator;
        }

        private IProviderVersionService CreateProviderVersionService(IBlobClient blobClient = null,
            IValidator<ProviderVersionViewModel> providerVersionModelValidator = null,
            ICacheProvider cacheProvider = null,
            IProviderVersionsMetadataRepository providerVersionMetadataRepository = null,
            IMapper mapper = null)
        {
            return new ProviderVersionService(
                cacheProvider ?? CreateCacheProvider(),
                blobClient ?? CreateBlobClient(),
                CreateLogger(),
                providerVersionModelValidator ?? CreateProviderVersionModelValidator(),
                providerVersionMetadataRepository ?? CreateProviderVersionMetadataRepository(),
                CreateResiliencePolicies(),
                mapper ?? CreateMapper());
        }

        private IMapper CreateMapper()
        {
            MapperConfiguration mapperConfiguration = new MapperConfiguration(c =>
            {
                c.AddProfile<ProviderVersionsMappingProfile>();
            });

            return mapperConfiguration.CreateMapper();
        }

        private ProviderVersionViewModel CreateProviderVersion()
        {
            return new ProviderVersionViewModel
            {
                ProviderVersionId = Guid.NewGuid().ToString(),
                Description = "Test provider version description",
                Name = "Test provider version",
                Version = 1,
                TargetDate = new DateTimeOffset(2001, 2, 3, 4, 5, 6, 7, TimeSpan.Zero),
                FundingStream = "Funding stream",
                Providers = new[]
                {
                    GetProviderViewModel()
                },

            };
        }

        public Provider GetProviderViewModel()
        {
            return new Provider
            {
                ProviderVersionId = Guid.NewGuid().ToString(),
                Name = "EstablishmentName",
                URN = "URN",
                UKPRN = "UKPRN",
                UPIN = "UPIN",
                EstablishmentNumber = "EstablishmentNumber",
                DfeEstablishmentNumber = "LA (code) EstablishmentNumber",
                Authority = "LA (name)",
                ProviderType = "TypeOfEstablishment (name)",
                ProviderSubType = "EstablishmentTypeGroup (name)",
                DateOpened = DateTime.Now,
                DateClosed = null,
                ProviderProfileIdType = "",
                LACode = "LA (code)",
                NavVendorNo = "",
                CrmAccountId = "",
                LegalName = "",
                Status = "EstablishmentStatus (name)",
                PhaseOfEducation = "PhaseOfEducation (code)",
                ReasonEstablishmentOpened = "",
                ReasonEstablishmentClosed = "",
                Successor = "",
                TrustName = "Trusts (name)",
                TrustCode = "",
                TrustStatusViewModelString = "Not applicable",
                LocalAuthorityName = "LocalAuthorityName",
                CompaniesHouseNumber = "CompaniesHouseNumber",
                GroupIdNumber = "GroupIdNumber",
                RscRegionName = "RscRegionName",
                RscRegionCode = "RscRegionCode",
                GovernmentOfficeRegionName = "GovernmentOfficeRegionName",
                GovernmentOfficeRegionCode = "GovernmentOfficeRegionCode",
                DistrictName = "DistrictName",
                DistrictCode = "DistrictCode",
                WardName = "WardName",
                WardCode = "WardCode",
                CensusWardName = "CensusWardName",
                CensusWardCode = "CensusWardCode",
                MiddleSuperOutputAreaName = "MiddleSuperOutputAreaName",
                MiddleSuperOutputAreaCode = "MiddleSuperOutputAreaCode",
                LowerSuperOutputAreaName = "LowerSuperOutputAreaName",
                LowerSuperOutputAreaCode = "LowerSuperOutputAreaCode",
                ParliamentaryConstituencyName = "ParliamentaryConstituencyName",
                ParliamentaryConstituencyCode = "ParliamentaryConstituencyCode",
                CountryCode = "CountryCode",
                CountryName = "CountryName",
                TrustStatus = TrustStatus.NotApplicable
            };
        }
    }
}
