using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models;
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
        private const string ExpectedFundingPeriodId = "AY-1920";
        private const string ExpectedFundingPeriodName = "Schools Academic Year 2019-20";
        private const string FundingStreamIdForExpectedFundingPeriod = "PSG";
        private static readonly FundingPeriod expectedFundingPeriod =
            new FundingPeriod
            {
                Id = ExpectedFundingPeriodId,
                Name = ExpectedFundingPeriodName
            };
        private static readonly Reference fundingStreamForExpectedFundingPeriod =
            new Reference(FundingStreamIdForExpectedFundingPeriod, "PE and Sport Premium Grant");
        private readonly Specification _specWithFundingPeriodAndFundingStream = new Specification
        {
            Id = "valid-spec-1",
            Current = new SpecificationVersion
            {
                FundingPeriod = expectedFundingPeriod,
                FundingStreams = new List<Reference>
                {
                    fundingStreamForExpectedFundingPeriod,
                    new Reference("SOME-ID", "XYZ")
                }
            },
            IsSelectedForFunding = true
        };
        private readonly Specification _specWithFundingPeriodAndFundingStream2 = new Specification
        {
            Id = "valid-spec-2",
            Current = new SpecificationVersion
            {
                FundingPeriod = expectedFundingPeriod,
                FundingStreams = new List<Reference>
                {
                    fundingStreamForExpectedFundingPeriod
                }
            },
            IsSelectedForFunding = true
        };
        private readonly Specification _specWithNoFundingStream = new Specification
        {
            Id = "spec-no-funding-stream",
            Current = new SpecificationVersion
            {
                FundingPeriod = new FundingPeriod
                {
                    Id = "SOME-ID",
                    Name = "XYZ"
                }
            },
            IsSelectedForFunding = true
        };

        private IMapper mapper;
        private IPoliciesApiClient policiesApiClient;
        private ISpecificationsRepository specificationsRepository;

        [TestInitialize]
        public void Setup()
        {
            specificationsRepository = CreateSpecificationsRepository();

            policiesApiClient = CreatePoliciesApiClient();
            policiesApiClient.GetFundingPeriods()
                .Returns(new ApiResponse<IEnumerable<FundingPeriod>>(HttpStatusCode.OK, new[]
                {
                    new FundingPeriod
                    {
                        Id = ExpectedFundingPeriodId,
                        Period = ExpectedFundingPeriodName
                    },
                    new FundingPeriod
                    {
                        Id = "FIRST DIFFERENT ID",
                        Period = "A DIFFERENT PERIOD"
                    },
                    new FundingPeriod
                    {
                        Id = "A DIFFERENT ID",
                        Period = ExpectedFundingPeriodName
                    }
                }));

            mapper = CreateMapper();
            mapper.Map<SpecificationSummary>(_specWithFundingPeriodAndFundingStream)
                .Returns(MapSpecification(_specWithFundingPeriodAndFundingStream));
            mapper.Map<SpecificationSummary>(_specWithFundingPeriodAndFundingStream2)
                .Returns(MapSpecification(_specWithFundingPeriodAndFundingStream2));
            mapper.Map<SpecificationSummary>(_specWithNoFundingStream)
                .Returns(MapSpecification(_specWithNoFundingStream));
        }

        [TestMethod]
        public async Task ReturnsDistinctFundingPeriods_GivenFundingStreamIdForAnSpecWithFundingPeriodAndMatchingIdAndPeriod()
        {
            specificationsRepository.GetSpecificationsByQuery(Arg.Any<Expression<Func<DocumentEntity<Specification>, bool>>>())
                .Returns(new[]
                {
                    _specWithFundingPeriodAndFundingStream,
                });
            SpecificationsService service = CreateService(
                specificationsRepository: specificationsRepository,
                policiesApiClient: policiesApiClient,
                mapper: mapper);

            OkObjectResult fundingPeriodsResult =
                await service.GetFundingPeriodsByFundingStreamIdsForSelectedSpecifications(FundingStreamIdForExpectedFundingPeriod) as OkObjectResult;

            await specificationsRepository
               .Received(1)
               .GetSpecificationsByQuery(Arg.Any<Expression<Func<DocumentEntity<Specification>, bool>>>());

            IEnumerable<Reference> fundingPeriod = fundingPeriodsResult.Value as IEnumerable<Reference>;
            fundingPeriod.Count().Should().Be(1);
            fundingPeriod.First().Id.Should().Be(ExpectedFundingPeriodId);
        }

        [TestMethod]
        public async Task ReturnsOkObjectResultWithNoFundingPeriod_GivenFundingStreamIdHasNoAssociationToSpecificationWithFundingPeriod()
        {
            specificationsRepository.GetSpecificationsByQuery(Arg.Any<Expression<Func<DocumentEntity<Specification>, bool>>>())
                .Returns(new[]
                {
                    _specWithNoFundingStream,
                });
            SpecificationsService service = CreateService(
                specificationsRepository: specificationsRepository,
                policiesApiClient: policiesApiClient,
                mapper: mapper);

            OkObjectResult fundingPeriodsResult =
                await service.GetFundingPeriodsByFundingStreamIdsForSelectedSpecifications(FundingStreamIdForExpectedFundingPeriod) as OkObjectResult;

            IEnumerable<Reference> fundingPeriod = fundingPeriodsResult.Value as IEnumerable<Reference>;
            fundingPeriodsResult.Should().BeOfType<OkObjectResult>();
            fundingPeriod.Count().Should().Be(0);
        }

        private static SpecificationSummary MapSpecification(Specification specification) =>
            new SpecificationSummary
            {
                Id = specification.Id,
                FundingPeriod = specification.Current.FundingPeriod,
                FundingStreams = specification.Current.FundingStreams,
            };
    }
}
