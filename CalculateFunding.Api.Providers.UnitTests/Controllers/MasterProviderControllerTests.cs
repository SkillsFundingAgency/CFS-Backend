using CalculateFunding.Api.Providers.Controllers;
using CalculateFunding.Models;
using CalculateFunding.Models.Providers;
using CalculateFunding.Models.Providers.ViewModels;
using CalculateFunding.Services.Providers.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
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
                .GetAllMasterProviders();
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
                .SearchMasterProviders(searchModel);
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
                .GetProviderByIdFromMaster(providerId);
        }

        [TestMethod]
        public async Task SetMasterProviderVersion_CallsCorrectly()
        {
            IProviderVersionService providerVersionService = Substitute.For<IProviderVersionService>();
            IProviderVersionSearchService providerVersionSearchService = Substitute.For<IProviderVersionSearchService>();

            providerVersionService
                .GetAllProviders(Arg.Any<string>())
                .Returns(new OkObjectResult(new Provider { ProviderVersionId = "providerVersionId" }));


            MasterProviderController controller = Substitute.For<MasterProviderController>(
                providerVersionService,
                providerVersionSearchService);

            MasterProviderVersionViewModel masterProviderVersionViewModel = new MasterProviderVersionViewModel();

            await controller.SetMasterProviderVersion(masterProviderVersionViewModel);

            await providerVersionService
                .Received(1)
                .SetMasterProviderVersion(Arg.Any<MasterProviderVersionViewModel>());
        }
    }
}
