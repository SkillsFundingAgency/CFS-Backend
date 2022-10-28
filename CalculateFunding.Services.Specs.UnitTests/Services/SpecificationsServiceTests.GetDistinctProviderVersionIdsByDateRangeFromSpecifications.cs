using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Services.Specs.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Specs.UnitTests.Services
{
    public partial class SpecificationsServiceTests
    {
        private readonly IEnumerable<string> _providerVersionIds = new List<string> { "psg-2022-02-01-70", "psg-2022-02-01-80" };
        private readonly IEnumerable<string> specificationsIds = new List<string> { "289eb3f8-b331-4a29-ad20-7209fe3560fb","3b0d6cad-9173-4a89-ba07-013ae3a27d17" };

        [TestMethod]
        public async Task ReturnsOkObjectResult_DistinctProviderVersionIdsFromSpecifications()
        {
            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository.GetDistinctProviderVersionIdsFromSpecifications(specificationsIds)
                .Returns(_providerVersionIds);
            SpecificationsService service = CreateService(specificationsRepository: specificationsRepository);

            OkObjectResult providerVersionIdsResult =
                await service.GetDistinctProviderVersionIdsFromSpecifications(specificationsIds) as OkObjectResult;

            IEnumerable<string> providerVersionIds = providerVersionIdsResult.Value as IEnumerable<string>;
            providerVersionIds.Count().Should().Be(2);
            providerVersionIds.First().Should().Be("psg-2022-02-01-70");
            providerVersionIds.Last().Should().Be("psg-2022-02-01-80");
        }

        [TestMethod]
        public async Task ReturnsOkObjectResultWithNo_ProviderVersionIds_DistinctProviderVersionIdsFromSpecifications()
        {
            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository.GetDistinctProviderVersionIdsFromSpecifications(specificationsIds)
                .Returns(new List<string>() { });
            SpecificationsService service = CreateService(specificationsRepository: specificationsRepository);

            OkObjectResult providerVersionIdsResult =
                await service.GetDistinctProviderVersionIdsFromSpecifications(specificationsIds) as OkObjectResult;

            IEnumerable<string> providerVersionIds = providerVersionIdsResult.Value as IEnumerable<string>;
            providerVersionIdsResult.Should().BeOfType<OkObjectResult>();
            providerVersionIds.Count().Should().Be(0);
        }

    }
}
