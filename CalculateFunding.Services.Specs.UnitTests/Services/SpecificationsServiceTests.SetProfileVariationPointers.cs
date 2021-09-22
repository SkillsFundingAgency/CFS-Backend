using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Specs.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Specs.UnitTests.Services
{
    public partial class SpecificationsServiceTests
    {
        [TestMethod]
        public void SetProfileVariationPointers_SpecificationIsNull_ReturnsBadRequestObjectResult()
        {
            //Arrange
            ILogger logger = CreateLogger();
            string specificationId = null;

            SpecificationsService service = CreateService(logs: logger);

            // Act
            Func<Task<IActionResult>> func = () => service.SetProfileVariationPointers(specificationId, null);

            //Assert
            func
                .Should()
                .ThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public async Task SetProfileVariationPointers_SpecDoesNotExistsInSystem_Returns412WithErrorMessage()
        {
            //Arrange
            ILogger logger = CreateLogger();
            string specificationId = "test";
            DateTimeOffset externalPublishDate = DateTimeOffset.Now.Date;
            DateTimeOffset earliestPaymentAvailableDate = DateTimeOffset.Now.Date;

            IEnumerable<SpecificationProfileVariationPointerModel> specificationProfileVariationPointerModels = new SpecificationProfileVariationPointerModel[]
            {
                new SpecificationProfileVariationPointerModel
                {
                    FundingLineId = "FundingLineId",
                    FundingStreamId = "FundingStreamId",
                    Occurrence = 1,
                    PeriodType = "PeriodType",
                    TypeValue = "TypeValue",
                    Year = 2019
                }
            };

            string expectedErrorMessage = $"No specification ID {specificationId} were returned from the repository, result came back null";

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(specificationId)
                .Returns<Specification>(x => null);

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository);

            // Act
            IActionResult result = await service.SetProfileVariationPointers(specificationId, specificationProfileVariationPointerModels);

            //Assert
            result
                .Should()
                .BeOfType<PreconditionFailedResult>()
                .Which
                .Value
                .Should()
                .Be(expectedErrorMessage);

            logger
                .Received(1)
                .Error(Arg.Is(expectedErrorMessage));
        }

        [TestMethod]
        public async Task SetProfileVariationPointers_UpdateSpecFails_Returns412WithErrorMessage()
        {
            //Arrange
            ILogger logger = CreateLogger();
            string specificationId = "test";
            DateTimeOffset externalPublishDate = DateTimeOffset.Now.Date;
            DateTimeOffset earliestPaymentAvailableDate = DateTimeOffset.Now.Date;

            Specification specification = new Specification
            {
                Current = new SpecificationVersion
                {
                    ExternalPublicationDate = DateTimeOffset.Now.Date,
                    EarliestPaymentAvailableDate = DateTimeOffset.Now.Date
                }
            };

            IEnumerable<SpecificationProfileVariationPointerModel> specificationProfileVariationPointerModels = new SpecificationProfileVariationPointerModel[]
            {
                new SpecificationProfileVariationPointerModel
                {
                    FundingLineId = "FundingLineId",
                    FundingStreamId = "FundingStreamId",
                    Occurrence = 1,
                    PeriodType = "PeriodType",
                    TypeValue = "TypeValue",
                    Year = 2019
                }
            };

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            IPoliciesApiClient policiesApiClient = CreatePoliciesApiClient();
            IVersionRepository<SpecificationVersion> specificationVersionRepository = CreateSpecificationVersionRepository();

            specificationsRepository
                .GetSpecificationById(specificationId)
                .Returns(specification);

            SpecificationVersion clonedSpecificationVersion = null;

            specificationVersionRepository.CreateVersion(Arg.Any<SpecificationVersion>(), Arg.Any<SpecificationVersion>())
                .Returns(_ => (SpecificationVersion)_[0])
                .AndDoes(_ => clonedSpecificationVersion = _.ArgAt<SpecificationVersion>(0));

            specificationsRepository
                .UpdateSpecification(Arg.Any<Specification>())
                .Returns(HttpStatusCode.BadRequest);

            SpecificationsService service = CreateService(
                logs: logger,
                specificationsRepository: specificationsRepository,
                specificationVersionRepository: specificationVersionRepository,
                policiesApiClient: policiesApiClient);

            // Act
            IActionResult result = await service.SetProfileVariationPointers(specificationId, specificationProfileVariationPointerModels);

            //Assert
            result
               .Should()
               .BeOfType<InternalServerErrorResult>()
               .Which
               .Value
               .Should()
               .Be($"Failed to update specification for id: {specificationId} with ProfileVariationPointers {specificationProfileVariationPointerModels?.AsJson()}");
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task SetProfileVariationPointers_ValidParametersPassed_ReturnsOKAndSetsSetProfileVariationPointersOnSpec(bool merge)
        {
            //Arrange
            ILogger logger = CreateLogger();
            string specificationId = "test";
            DateTimeOffset externalPublishDate = DateTimeOffset.Now.Date;
            DateTimeOffset earliestPaymentAvailableDate = DateTimeOffset.Now.Date;

            Specification specification = new Specification
            {
                Current = new SpecificationVersion
                {
                    ExternalPublicationDate = DateTimeOffset.Now.Date,
                    EarliestPaymentAvailableDate = DateTimeOffset.Now.Date,
                    ProfileVariationPointers = new ProfileVariationPointer[]
                    {
                        new ProfileVariationPointer
                        {
                            FundingLineId = "FundingLineId",
                            FundingStreamId = "FundingStreamId",
                            Occurrence = 1,
                            PeriodType = "PeriodType",
                            TypeValue = "TypeValue",
                            Year = 2019
                        }
                    }
                }
            };

            IEnumerable<SpecificationProfileVariationPointerModel> specificationProfileVariationPointerModels = new SpecificationProfileVariationPointerModel[]
            {
                new SpecificationProfileVariationPointerModel
                {
                    FundingLineId = "FundingLineId",
                    FundingStreamId = "FundingStreamId",
                    Occurrence = 2,
                    PeriodType = "PeriodType",
                    TypeValue = "TypeValue",
                    Year = 2019
                }
            };

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            IPoliciesApiClient policiesApiClient = CreatePoliciesApiClient();
            IVersionRepository<SpecificationVersion> specificationVersionRepository = CreateSpecificationVersionRepository();

            specificationsRepository
                .GetSpecificationById(specificationId)
                .Returns(specification);

            SpecificationVersion clonedSpecificationVersion = null;

            specificationVersionRepository.CreateVersion(Arg.Any<SpecificationVersion>(), Arg.Any<SpecificationVersion>())
                .Returns(_ => (SpecificationVersion)_[0])
                .AndDoes(_ => clonedSpecificationVersion = _.ArgAt<SpecificationVersion>(0));

            specificationsRepository
                .UpdateSpecification(Arg.Any<Specification>())
                .Returns(HttpStatusCode.OK);

            SpecificationsService service = CreateService(
                logs: logger,
                specificationsRepository: specificationsRepository,
                specificationVersionRepository: specificationVersionRepository,
                policiesApiClient: policiesApiClient);

            // Act
            IActionResult result = await service.SetProfileVariationPointers(specificationId, specificationProfileVariationPointerModels, merge);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .Be(HttpStatusCode.OK);

            specificationsRepository
                .Received(1);

            var profileVariationPointers = clonedSpecificationVersion?
                .ProfileVariationPointers;

            List<ProfileVariationPointer> expectedProfileVariationPointers = new List<ProfileVariationPointer>{ new ProfileVariationPointer
                {
                    FundingLineId = "FundingLineId",
                    FundingStreamId = "FundingStreamId",
                    Occurrence = 2,
                    PeriodType = "PeriodType",
                    TypeValue = "TypeValue",
                    Year = 2019
                } };

            if (merge)
            {
                expectedProfileVariationPointers.ForEach(_ => { 
                    _.Occurrence = 2;
                });
            }

            profileVariationPointers
                .Should()
                .NotBeNull();

            profileVariationPointers
                .Should()
                .BeEquivalentTo(expectedProfileVariationPointers);
        }

        [TestMethod]
        public async Task SetProfileVariationPointer_ValidParametersPassed_ReturnsOKAndSetsSetProfileVariationPointersOnSpec()
        {
            //Arrange
            ILogger logger = CreateLogger();
            string specificationId = "test";

            Specification specification = new Specification
            {
                Current = new SpecificationVersion
                {
                    ProfileVariationPointers = new ProfileVariationPointer[] { new ProfileVariationPointer
                        {
                            FundingLineId = "FundingLineId",
                            FundingStreamId = "FundingStreamId",
                            Occurrence = 1,
                            PeriodType = "PeriodType",
                            TypeValue = "TypeValue",
                            Year = 2019
                        },
                        new ProfileVariationPointer {
                            FundingLineId = "FundingLineId2",
                            FundingStreamId = "FundingStreamId",
                            Occurrence = 1,
                            PeriodType = "PeriodType",
                            TypeValue = "TypeValue",
                            Year = 2019
                        }
                    }
                }
            };

            SpecificationProfileVariationPointerModel specificationProfileVariationPointerModel = new SpecificationProfileVariationPointerModel
            {
                FundingLineId = "FundingLineId",
                FundingStreamId = "FundingStreamId",
                Occurrence = 2,
                PeriodType = "PeriodType",
                TypeValue = "TypeValue",
                Year = 2019
            };

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            IPoliciesApiClient policiesApiClient = CreatePoliciesApiClient();
            IVersionRepository<SpecificationVersion> specificationVersionRepository = CreateSpecificationVersionRepository();

            specificationsRepository
                .GetSpecificationById(specificationId)
                .Returns(specification);

            SpecificationVersion clonedSpecificationVersion = null;

            specificationVersionRepository.CreateVersion(Arg.Any<SpecificationVersion>(), Arg.Any<SpecificationVersion>())
                .Returns(_ => (SpecificationVersion)_[0])
                .AndDoes(_ => clonedSpecificationVersion = _.ArgAt<SpecificationVersion>(0));

            specificationsRepository
                .UpdateSpecification(Arg.Any<Specification>())
                .Returns(HttpStatusCode.OK);

            SpecificationsService service = CreateService(
                logs: logger,
                specificationsRepository: specificationsRepository,
                specificationVersionRepository: specificationVersionRepository,
                policiesApiClient: policiesApiClient);

            // Act
            IActionResult result = await service.SetProfileVariationPointer(specificationId, specificationProfileVariationPointerModel);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .Be(HttpStatusCode.OK);

            specificationsRepository
                .Received(1);

            var profileVariationPointers = clonedSpecificationVersion?
                .ProfileVariationPointers;

            profileVariationPointers
                .Should()
                .NotBeNull()
                .And.HaveCount(2)
                .And.BeEquivalentTo(new ProfileVariationPointer[]{ new ProfileVariationPointer
                {
                    FundingLineId = "FundingLineId",
                    FundingStreamId = "FundingStreamId",
                    Occurrence = 2,
                    PeriodType = "PeriodType",
                    TypeValue = "TypeValue",
                    Year = 2019
                },
                new ProfileVariationPointer {
                    FundingLineId = "FundingLineId2",
                    FundingStreamId = "FundingStreamId",
                    Occurrence = 1,
                    PeriodType = "PeriodType",
                    TypeValue = "TypeValue",
                    Year = 2019
                }});
        }
    }
}
