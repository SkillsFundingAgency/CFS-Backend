using CalculateFunding.Api.Providers.Controllers;
using CalculateFunding.Models;
using CalculateFunding.Models.Providers.ViewModels;
using CalculateFunding.Services.Providers.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Api.Results.UnitTests.Controllers
{
    [TestClass]
    public class ProviderByVersionControllerTests
    {
        [TestMethod]
        public async Task SearchProviderVersions_CallsCorrectly()
        {
            IProviderVersionService providerVersionService = Substitute.For<IProviderVersionService>();
            IProviderVersionSearchService providerVersionSearchService = Substitute.For<IProviderVersionSearchService>();

            ProviderByVersionController controller = new ProviderByVersionController(
                providerVersionService,
                providerVersionSearchService);

            SearchModel searchModel = new SearchModel();

            await controller.SearchProviderVersions(searchModel);

            await providerVersionSearchService
                .Received(1)
                .SearchProviderVersions(searchModel);
        }

        [TestMethod]
        [DataRow("providerVersionId")]
        public async Task SearchProvidersInProviderVersion_CallsCorrectly(
            string providerVersionId)
        {
            IProviderVersionService providerVersionService = Substitute.For<IProviderVersionService>();
            IProviderVersionSearchService providerVersionSearchService = Substitute.For<IProviderVersionSearchService>();

            ProviderByVersionController controller = new ProviderByVersionController(
                providerVersionService,
                providerVersionSearchService);

            SearchModel searchModel = new SearchModel();

            await controller.SearchProvidersInProviderVersion(providerVersionId, searchModel);

            await providerVersionSearchService
                .Received(1)
                .SearchProviders(providerVersionId, searchModel);
        }

        [TestMethod]
        [DataRow("providerVersionId")]
        public async Task GetProvidersByVersion_CallsCorrectly(
            string providerVersionId)
        {
            IProviderVersionService providerVersionService = Substitute.For<IProviderVersionService>();
            IProviderVersionSearchService providerVersionSearchService = Substitute.For<IProviderVersionSearchService>();

            ProviderByVersionController controller = new ProviderByVersionController(
                providerVersionService,
                providerVersionSearchService);

            await controller.GetProvidersByVersion(providerVersionId);

            await providerVersionService
                .Received(1)
                .GetAllProviders(providerVersionId);
        }

        [TestMethod]
        [DataRow("providerVersionId", "providerId")]
        public async Task GetProviderByIdFromProviderVersion_CallsCorrectly(
               string providerVersionId, 
               string providerId)
        {
            IProviderVersionService providerVersionService = Substitute.For<IProviderVersionService>();
            IProviderVersionSearchService providerVersionSearchService = Substitute.For<IProviderVersionSearchService>();

            ProviderByVersionController controller = new ProviderByVersionController(
                providerVersionService,
                providerVersionSearchService);

            await controller.GetProviderByIdFromProviderVersion(providerVersionId, providerId);

            await providerVersionSearchService
                .Received(1)
                .GetProviderById(providerVersionId, providerId);
        }

        [TestMethod]
        [DataRow("providerVersionId")]
        public async Task UploadProviderVersion_CallsCorrectly(
             string providerVersionId)
        {
            IProviderVersionService providerVersionService = Substitute.For<IProviderVersionService>();
            IProviderVersionSearchService providerVersionSearchService = Substitute.For<IProviderVersionSearchService>();

            ProviderVersionViewModel providers = new ProviderVersionViewModel();

            ProviderByVersionController controller = new ProviderByVersionController(
                providerVersionService,
                providerVersionSearchService);

            controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext() {
                RouteData = new Microsoft.AspNetCore.Routing.RouteData
                (
                    new Microsoft.AspNetCore.Routing.RouteValueDictionary
                    (
                        new Dictionary<string, string>()
                        {
                            { "controller", "ProviderByVersion" }
                        }
                    )
                )
            };

            await controller.UploadProviderVersion(providerVersionId, providers);

            await providerVersionService
                .Received(1)
                .UploadProviderVersion("GetProvidersByVersion", "ProviderByVersion", providerVersionId, providers);
        }
    }
}
