using CalculateFunding.Api.Providers.Controllers;
using CalculateFunding.Api.Providers.ViewModels;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Models;
using CalculateFunding.Models.Providers.ViewModels;
using CalculateFunding.Services.Providers.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Api.Results.UnitTests.Controllers
{
    [TestClass]
    public class MasterProviderControllerTests
    {
        [TestMethod]
        public async Task GetAllMasterProviders_CallsCorrectly()
        {
            IProviderVersionService providerVersionService = Substitute.For<IProviderVersionService>();
            IProviderVersionSearchService providerVersionSearchService = Substitute.For<IProviderVersionSearchService>();

            MasterProviderController controller = new MasterProviderController(
                providerVersionService,
                providerVersionSearchService);

            await controller.GetAllMasterProviders();

            await providerVersionService
                .Received(1)
                .GetAllProviders(null);
        }

        [TestMethod]
        public async Task SearchMasterProviders_CallsCorrectly()
        {
            IProviderVersionService providerVersionService = Substitute.For<IProviderVersionService>();
            IProviderVersionSearchService providerVersionSearchService = Substitute.For<IProviderVersionSearchService>();

            MasterProviderController controller = new MasterProviderController(
                providerVersionService,
                providerVersionSearchService);

            SearchModel searchModel = new SearchModel();

            await controller.SearchMasterProviders(searchModel);

            await providerVersionSearchService
                .Received(1)
                .SearchProviders(null, searchModel);
        }

        [TestMethod]
        [DataRow("providerId")]
        public async Task GetProviderByIdFromMaster_CallsCorrectly(string providerId)
        {
            IProviderVersionService providerVersionService = Substitute.For<IProviderVersionService>();
            IProviderVersionSearchService providerVersionSearchService = Substitute.For<IProviderVersionSearchService>();

            MasterProviderController controller = new MasterProviderController(
                providerVersionService,
                providerVersionSearchService);

            SearchModel searchModel = new SearchModel();

            await controller.GetProviderByIdFromMaster(providerId);

            await providerVersionSearchService
                .Received(1)
                .GetProviderById(null, providerId);
        }

        [TestMethod]
        public async Task SetMasterProviderVersion_CallsCorrectly()
        {
            IProviderVersionService providerVersionService = Substitute.For<IProviderVersionService>();
            IProviderVersionSearchService providerVersionSearchService = Substitute.For<IProviderVersionSearchService>();

            MasterProviderController controller = Substitute.For<MasterProviderController>(
                providerVersionService,
                providerVersionSearchService);

            SetMasterProviderViewModel setMasterProviderViewModel = new SetMasterProviderViewModel();

            await controller.SetMasterProviderVersion(setMasterProviderViewModel);

            controller
                .Received(1)
                .NoContent();
        }
    }
}
