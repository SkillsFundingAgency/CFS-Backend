using System.Threading.Tasks;
using CalculateFunding.Common.Caching;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using CalculateFunding.Models.Providers.ViewModels;
using CalculateFunding.Services.Providers.Validators;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Providers.Interfaces;
using System.IO;
using Serilog;
using FluentValidation;
using FluentAssertions;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Linq;
using Polly;
using AutoMapper;
using CalculateFunding.Models.Providers;
using System;
using AutoMapper.Configuration;
using CalculateFunding.Models.MappingProfiles;
using Newtonsoft.Json;
using System.Text;
using Microsoft.Azure.Storage.Blob;

namespace CalculateFunding.Services.Providers.UnitTests
{
    [TestClass]
    public class ProviderVersionServiceTests
    {
        [TestMethod]
        public async Task UploadProviderVersion_WhenAllPropertiesPopulated()
        {
            // Arrange
            ProviderVersionViewModel providerVersionViewModel = CreateProviderVersion();

            ICacheProvider cacheProvider = CreateCacheProvider();
            providerVersionViewModel.VersionType = ProviderVersionType.Custom;

            UploadProviderVersionValidator uploadProviderVersionValidator = new UploadProviderVersionValidator();

            ICloudBlob cloudBlob = CreateCloudBlob();

            IBlobClient blobClient = CreateBlobClient();
            blobClient.GetBlockBlobReference(Arg.Any<string>()).Returns(cloudBlob);

            IProviderVersionService providerService = CreateProviderVersionService(blobClient: blobClient, providerVersionModelValidator: uploadProviderVersionValidator, cacheProvider: cacheProvider);

            // Act
            await providerService.UploadProviderVersion("Action", "Controller", providerVersionViewModel.ProviderVersionId, providerVersionViewModel);

            blobClient
                .Received(1)
                .GetBlockBlobReference(Arg.Any<string>());

            await cloudBlob
                .Received(1)
                .UploadFromStreamAsync(Arg.Any<MemoryStream>());
        }

        [TestMethod]
        public async Task UploadProviderVersion_WhenVersionIdEmpty_UploadFails()
        {
            // Arrange
            ProviderVersionViewModel providerVersionViewModel = CreateProviderVersion();

            providerVersionViewModel.ProviderVersionId = null;
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

            ((string[])validationErrors["ProviderVersionId"])[0]
                .Should()
                .Be("No provider version Id was provided to UploadProviderVersion");
        }

        [TestMethod]
        public async Task UploadProviderVersion_WheDescriptionEmpty_UploadFails()
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

            ((string[])validationErrors["VersionType"])[0]
                .Should()
                .Be("No provider version type provided to UploadProviderVersion");
        }

        [TestMethod]
        public async Task UpdateProviderData_WhenAllReuiredProviderPropertiesEmpty_UploadFails()
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
        public async Task Exists_WhenProviderVersionDoesntExist_FalseReturned()
        {
            // Arrange
            IBlobClient blobClient = CreateBlobClient();

            blobClient
                .BlobExistsAsync(Arg.Any<string>())
                .Returns(false);

            IProviderVersionService providerService = CreateProviderVersionService(blobClient: blobClient);

            // Act
            bool exists = await providerService.Exists(Guid.NewGuid().ToString());

            exists
                .Should()
                .BeFalse();
        }

        [TestMethod]
        public async Task Exists_WhenProviderVersionDoesntExist_TrueReturned()
        {
            // Arrange
            IBlobClient blobClient = CreateBlobClient();

            blobClient
                .BlobExistsAsync(Arg.Any<string>())
                .Returns(true);

            IProviderVersionService providerService = CreateProviderVersionService(blobClient: blobClient);

            // Act
            bool exists = await providerService.Exists(Guid.NewGuid().ToString());

            exists
                .Should()
                .BeTrue();
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
        [DataRow(12,12,2019)]
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
        [DataRow(12,12,2019)]
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
            return providersResiliencePolicies;
        }

        private IValidator<ProviderVersionViewModel> CreateProviderVersionModelValidator()
        {
            IValidator<ProviderVersionViewModel> providerVersionModelValidator = Substitute.For<IValidator<ProviderVersionViewModel>>();
            return providerVersionModelValidator;
        }

        private IProviderVersionService CreateProviderVersionService(IBlobClient blobClient, IValidator<ProviderVersionViewModel> providerVersionModelValidator = null, ICacheProvider cacheProvider = null, IProviderVersionsMetadataRepository providerVersionMetadataRepository = null, IMapper mapper = null)
        {
            return new ProviderVersionService(
                cacheProvider ?? CreateCacheProvider(),
                blobClient,
                CreateLogger(),
                providerVersionModelValidator ?? CreateProviderVersionModelValidator(),
                providerVersionMetadataRepository ?? CreateProviderVersionMetadataRepository(),
                CreateResiliencePolicies(),
                mapper ?? CreateMapper());
        }

        private IMapper CreateMapper()
        {
            Mapper.Reset();
            MapperConfigurationExpression mappings = new MapperConfigurationExpression();
            mappings.AddProfile<ProviderVersionsMappingProfile>();
            Mapper.Initialize(mappings);
            
            return Mapper.Instance;
        }

        private ProviderVersionViewModel CreateProviderVersion()
        {
            return new ProviderVersionViewModel
            {
                ProviderVersionId = System.Guid.NewGuid().ToString(),
                Description = "Test provider version description",
                Name = "Test provider version",
                Providers = new[]
                {
                    GetProviderViewModel()
                }
            };
        }

        public Provider GetProviderViewModel()
        {
            return new Provider
            {
                ProviderVersionId = System.Guid.NewGuid().ToString(),
                Name = "EstablishmentName",
                URN = "URN",
                UKPRN = "UKPRN",
                UPIN = "UPIN",
                EstablishmentNumber = "EstablishmentNumber",
                DfeEstablishmentNumber = "LA (code) EstablishmentNumber",
                Authority = "LA (name)",
                ProviderType = "TypeOfEstablishment (name)",
                ProviderSubType = "EstablishmentTypeGroup (name)",
                DateOpened = System.DateTime.Now,
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
                TrustCode = ""
            };
        }
    }
}
