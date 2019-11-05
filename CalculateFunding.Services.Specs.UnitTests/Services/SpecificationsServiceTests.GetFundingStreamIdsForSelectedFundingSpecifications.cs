using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Specs.UnitTests.Services
{
    public partial class SpecificationsServiceTests
    {
        private readonly Specification _specNotSelectedForFunding = new Specification
        {
            Id = "spec2",
            Current = new SpecificationVersion
            {
                FundingStreams = new List<Reference>
                    {
                        new Reference("SomeID", "name1")
                    }
            },
            IsSelectedForFunding = false
        };

        private readonly Specification _specSelectedForFunding = new Specification
            {
                Id = "spec1",
                Current = new SpecificationVersion
                {
                    FundingStreams = new List<Reference>
                    {
                        new Reference("PSG", "name1"),
                        new Reference("PSG", "name2"),
                        new Reference("PSG2", "name3")
                    }
                },
                IsSelectedForFunding = true
            };

        [TestMethod]
        public async Task ReturnsDistinctFundingStreamIds_GivenValidSelectedSpecWithFundingStreams()
        {
            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository.GetSpecificationsByQuery()
                .Returns(new[] { _specSelectedForFunding, _specNotSelectedForFunding });
            SpecificationsService service = CreateService(specificationsRepository: specificationsRepository);

            OkObjectResult fundingStreamIdResult =
                await service.GetFundingStreamIdsForSelectedFundingSpecifications() as OkObjectResult;

            IEnumerable<string> fundingStreamIds = fundingStreamIdResult.Value as IEnumerable<string>;
            fundingStreamIds.Count().Should().Be(2);
            fundingStreamIds.First().Should().Be("PSG");
            fundingStreamIds.Skip(1).First().Should().Be("PSG2");
        }

        [TestMethod]
        public async Task ReturnsOkObjectResultWithNoFundingStreamIds_GivenNoSpecificationWithSelectedForFunding()
        {
            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository.GetSpecificationsByQuery()
                .Returns(new[] { _specNotSelectedForFunding });
            SpecificationsService service = CreateService(specificationsRepository: specificationsRepository);

            OkObjectResult fundingStreamIdResult =
                await service.GetFundingStreamIdsForSelectedFundingSpecifications() as OkObjectResult;

            IEnumerable<string> fundingStreamIds = fundingStreamIdResult.Value as IEnumerable<string>;
            fundingStreamIdResult.Should().BeOfType<OkObjectResult>();
            fundingStreamIds.Count().Should().Be(0);
        }
    }
}
