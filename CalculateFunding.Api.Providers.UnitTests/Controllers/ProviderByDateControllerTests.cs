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
    public class ProviderByDateControllerTests
    {
        [TestMethod]
        [DataRow(2019,05,25)]
        public async Task SetProviderDateProviderVersion_CallsCorrectly(int year, int month, int day)
        {
            IProviderVersionService providerVersionService = Substitute.For<IProviderVersionService>();
            IProviderVersionSearchService providerVersionSearchService = Substitute.For<IProviderVersionSearchService>();

            ProviderByDateController controller = Substitute.For<ProviderByDateController>(providerVersionService, providerVersionSearchService);


            SetProviderVersionDateViewModel setProviderVersionDateViewModel = new SetProviderVersionDateViewModel();

            await controller.SetProviderDateProviderVersion(year, month, day, setProviderVersionDateViewModel);

            controller
                .Received(1)
                .NoContent();
        }

        [TestMethod]
        [DataRow(2019, 05, 25)]
        public async Task GetProvidersByVersion_CallsCorrectly(int year, int month, int day)
        {
            IProviderVersionService providerVersionService = Substitute.For<IProviderVersionService>();
            IProviderVersionSearchService providerVersionSearchService = Substitute.For<IProviderVersionSearchService>();

            ProviderByDateController controller = new ProviderByDateController(
                providerVersionService,
                providerVersionSearchService);

            SearchModel searchModel = new SearchModel();

            await controller.GetProvidersByVersion(year, month, day);

            await providerVersionService
                .Received(1)
                .GetAllProviders(year, month, day);
        }

        [TestMethod]
        [DataRow(2019, 05, 25)]
        public async Task SearchProvidersInProviderVersionAssociatedWithDate_CallsCorrectly(int year, int month, int day)
        {
            IProviderVersionService providerVersionService = Substitute.For<IProviderVersionService>();
            IProviderVersionSearchService providerVersionSearchService = Substitute.For<IProviderVersionSearchService>();

            ProviderByDateController controller = new ProviderByDateController(
                providerVersionService,
                providerVersionSearchService);

            SearchModel searchModel = new SearchModel();

            await controller.SearchProvidersInProviderVersionAssociatedWithDate(year, month, day, searchModel);

            await providerVersionSearchService
                .Received(1)
                .SearchProviders(year, month, day, searchModel);
        }

        [TestMethod]
        [DataRow(2019, 05, 25,  "providerId")]
        public async Task GetProviderByIdFromProviderVersion_CallsCorrectly(int year, int month, int day, string providerId)
        {
            IProviderVersionService providerVersionService = Substitute.For<IProviderVersionService>();
            IProviderVersionSearchService providerVersionSearchService = Substitute.For<IProviderVersionSearchService>();

            ProviderByDateController controller = new ProviderByDateController(
                providerVersionService,
                providerVersionSearchService);

            await controller.GetProviderByIdFromProviderVersion(year, month, day, providerId);

            await providerVersionSearchService
                .Received(1)
                .GetProviderById(year, month, day, providerId);
        }

        [TestMethod]
        [DataRow(2019, 05)]
        public async Task GetAvailableProvidersByMonth_CallsCorrectly(int year, int month)
        {
            IProviderVersionService providerVersionService = Substitute.For<IProviderVersionService>();
            IProviderVersionSearchService providerVersionSearchService = Substitute.For<IProviderVersionSearchService>();

            ProviderByDateController controller = Substitute.For<ProviderByDateController>(
                providerVersionService,
                providerVersionSearchService);

            await controller.GetAvailableProvidersByMonth(year, month);

            controller
                .Received(1)
                .Ok(Arg.Any<IEnumerable>());
        }
    }
}
