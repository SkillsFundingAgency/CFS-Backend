using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
        private readonly Specification _specFundingStreams1 = new Specification
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

        private readonly Specification _specFundingStreams2 = new Specification
        {
            Id = "spec2",
            Current = new SpecificationVersion
            {
                FundingStreams = new List<Reference>
                {
                    new Reference("PSG", "name1")

                }
            },
            IsSelectedForFunding = true
        };

       

        private IEnumerable<string> _fundingStreamIds = new List<string>() { "PSG", "DSG", "PSG1" };


        [TestMethod]
        public async Task ReturnsOkObjectResult_DistinctFundingStreamIdsForSpecifications()
        {
            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository.GetDistinctFundingStreamsForSpecifications()
                .Returns(_fundingStreamIds);
            SpecificationsService service = CreateService(specificationsRepository: specificationsRepository);

            OkObjectResult fundingStreamIdResult =
                await service.GetDistinctFundingStreamsForSpecifications() as OkObjectResult;

            IEnumerable<string> fundingStreamIds = fundingStreamIdResult.Value as IEnumerable<string>;
            fundingStreamIds.Count().Should().Be(3);
            fundingStreamIds.First().Should().Be("PSG");
            fundingStreamIds.Skip(1).First().Should().Be("DSG");
        }

        [TestMethod]
        public async Task ReturnsOkObjectResultWithNoFundingStreamIds_DistinctFundingStreamIdsForSpecifications()
        {
            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository.GetDistinctFundingStreamsForSpecifications()
                .Returns(new List<string>() { });
            SpecificationsService service = CreateService(specificationsRepository: specificationsRepository);

            OkObjectResult fundingStreamIdResult =
                await service.GetDistinctFundingStreamsForSpecifications() as OkObjectResult;

            IEnumerable<string> fundingStreamIds = fundingStreamIdResult.Value as IEnumerable<string>;
            fundingStreamIdResult.Should().BeOfType<OkObjectResult>();
            fundingStreamIds.Count().Should().Be(0);
        }
    }
}
