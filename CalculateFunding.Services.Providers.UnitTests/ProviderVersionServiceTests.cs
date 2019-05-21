using System.Threading.Tasks;
using CalculateFunding.Common.Caching;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using CalculateFunding.Models.Providers.ViewModels;
using CalculateFunding.Services.Providers.Validators;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using Microsoft.WindowsAzure.Storage.Blob;
using CalculateFunding.Services.Providers.Interfaces;
using System.IO;
using Serilog;
using FluentValidation;
using FluentAssertions;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Linq;

namespace CalculateFunding.Services.Providers.UnitTests
{
    [TestClass]
    public class ProviderVersionServiceTests
    {
        [TestMethod]
        public async Task UpdateProviderData_WhenAllPropertiesPopulated()
        {
            // Arrange
            ProviderVersionViewModel providerVersionViewModel = CreateProviderVersion();

            ICacheProvider cacheProvider = CreateCacheProvider();
            providerVersionViewModel.VersionType = ProviderVersionType.Custom;

            UploadProviderVersionValidator uploadProviderVersionValidator = new UploadProviderVersionValidator();

            IBlobClient blobClient = CreateBlobClient();

            ICloudBlob cloudBlob = CreateCloudBlob();

            IProviderVersionService providerService = CreateProviderVersionService(blobClient: blobClient, providerVersionModelValidator: uploadProviderVersionValidator, cacheProvider: cacheProvider);

            // Act
            await providerService.UploadProviderVersion("", "", providerVersionViewModel.Id, providerVersionViewModel);

            await blobClient
                .Received(1)
                .GetBlockBlobReference(Arg.Any<string>())
                .UploadFromStreamAsync(Arg.Any<MemoryStream>());
        }

        [TestMethod]
        public async Task UpdateProviderData_WhenVersionIdEmpty_UploadFails()
        {
            // Arrange
            ProviderVersionViewModel providerVersionViewModel = CreateProviderVersion();

            providerVersionViewModel.Id = null;
            providerVersionViewModel.VersionType = ProviderVersionType.Custom;

            ICacheProvider cacheProvider = CreateCacheProvider();

            UploadProviderVersionValidator uploadProviderVersionValidator = new UploadProviderVersionValidator();

            IBlobClient blobClient = CreateBlobClient();

            ICloudBlob cloudBlob = CreateCloudBlob();

            IProviderVersionService providerService = CreateProviderVersionService(blobClient: blobClient, providerVersionModelValidator: uploadProviderVersionValidator, cacheProvider: cacheProvider);

            // Act
            IActionResult badRequest = await providerService.UploadProviderVersion("", "", providerVersionViewModel.Id, providerVersionViewModel);

            badRequest
                .Should()
                .BeOfType<BadRequestObjectResult>();

            BadRequestObjectResult badRequestObject = badRequest as BadRequestObjectResult;
            SerializableError validationErrors = badRequestObject.Value as SerializableError;

            ((string[])validationErrors["Id"])[0]
                .Should()
                .Be("No provider version Id was provided to UploadProviderVersion");
        }

        [TestMethod]
        public async Task UpdateProviderData_WheDescriptionEmpty_UploadFails()
        {
            // Arrange
            ProviderVersionViewModel providerVersionViewModel = CreateProviderVersion();

            providerVersionViewModel.Description = null;
            providerVersionViewModel.VersionType = ProviderVersionType.Custom;

            ICacheProvider cacheProvider = CreateCacheProvider();

            UploadProviderVersionValidator uploadProviderVersionValidator = new UploadProviderVersionValidator();

            IBlobClient blobClient = CreateBlobClient();

            ICloudBlob cloudBlob = CreateCloudBlob();

            IProviderVersionService providerService = CreateProviderVersionService(blobClient: blobClient, providerVersionModelValidator: uploadProviderVersionValidator, cacheProvider: cacheProvider);

            // Act
            IActionResult badRequest = await providerService.UploadProviderVersion("", "", providerVersionViewModel.Id, providerVersionViewModel);

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
        public async Task UpdateProviderData_WhenProvidersEmpty_UploadFails()
        {
            // Arrange
            ProviderVersionViewModel providerVersionViewModel = CreateProviderVersion();

            providerVersionViewModel.Providers = new List<ProviderViewModel>();
            providerVersionViewModel.VersionType = ProviderVersionType.Custom;

            ICacheProvider cacheProvider = CreateCacheProvider();

            UploadProviderVersionValidator uploadProviderVersionValidator = new UploadProviderVersionValidator();

            IBlobClient blobClient = CreateBlobClient();

            ICloudBlob cloudBlob = CreateCloudBlob();

            IProviderVersionService providerService = CreateProviderVersionService(blobClient: blobClient, providerVersionModelValidator: uploadProviderVersionValidator, cacheProvider: cacheProvider);

            // Act
            IActionResult badRequest = await providerService.UploadProviderVersion("", "", providerVersionViewModel.Id, providerVersionViewModel);

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

            ICloudBlob cloudBlob = CreateCloudBlob();

            IProviderVersionService providerService = CreateProviderVersionService(blobClient: blobClient, providerVersionModelValidator: uploadProviderVersionValidator, cacheProvider: cacheProvider);

            // Act
            IActionResult badRequest = await providerService.UploadProviderVersion("", "", providerVersionViewModel.Id, providerVersionViewModel);

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

            ICloudBlob cloudBlob = CreateCloudBlob();

            IProviderVersionService providerService = CreateProviderVersionService(blobClient: blobClient, providerVersionModelValidator: uploadProviderVersionValidator, cacheProvider: cacheProvider);

            // Act
            IActionResult badRequest = await providerService.UploadProviderVersion("", "", providerVersionViewModel.Id, providerVersionViewModel);

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

            ICloudBlob cloudBlob = CreateCloudBlob();

            IProviderVersionService providerService = CreateProviderVersionService(blobClient: blobClient, providerVersionModelValidator: uploadProviderVersionValidator, cacheProvider: cacheProvider);

            // Act
            IActionResult badRequest = await providerService.UploadProviderVersion("", "", providerVersionViewModel.Id, providerVersionViewModel);

            badRequest
                .Should()
                .BeOfType<BadRequestObjectResult>();

            BadRequestObjectResult badRequestObject = badRequest as BadRequestObjectResult;
            SerializableError validationErrors = badRequestObject.Value as SerializableError;

            ((string[])validationErrors["Providers"])[0]
                .Should()
                .Be($"Duplicate UKPRN specified for {providerVersionViewModel.Providers.First().UKPRN} was provided to UploadProviderVersion");
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

        private IProviderVersionService CreateProviderVersionService(IBlobClient blobClient, IValidator<ProviderVersionViewModel> providerVersionModelValidator, ICacheProvider cacheProvider = null)
        {
            return new ProviderVersionService(
                cacheProvider ?? CreateCacheProvider(),
                blobClient,
                CreateLogger(),
                providerVersionModelValidator);
        }

        private ProviderVersionViewModel CreateProviderVersion()
        {
            return new ProviderVersionViewModel
            {
                Id = System.Guid.NewGuid().ToString(),
                Description = "Test provider version description",
                Name = "Test provider version",
                Providers = new[]
                {
                    GetProviderViewModel()
                }
            };
        }

        public ProviderViewModel GetProviderViewModel()
        {
            return new ProviderViewModel
            {
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
