using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Serilog;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using FluentAssertions;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Primitives;
using System.Linq.Expressions;
using System.Linq;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Constants;

namespace CalculateFunding.Services.Specs.Services
{
    public partial class SpecificationsServiceTests
    {

        [TestMethod]
        public async Task EditSpecification_GivenNoSpecificationIdWasProvided_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.EditSpecification(request);

            //Arrange
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No specification Id was provided to EditSpecification"));
        }

        [TestMethod]
        public async Task EditSpecification_GivenNullEditModeldWasProvided_ReturnsBadRequest()
        {
            //Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) },
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.EditSpecification(request);

            //Arrange
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No edit modeld was provided to EditSpecification"));
        }

        [TestMethod]
        public async Task EditSpecification_WhenInvalidModelProvided_ThenValidationErrorReturned()
        {
            // Arrange
            ValidationResult validationResult = new ValidationResult();
            validationResult.Errors.Add(new ValidationFailure("error", "error"));

            IValidator<SpecificationEditModel> validator = CreateEditSpecificationValidator(validationResult);

            SpecificationsService specificationsService = CreateService(specificationEditModelValidator: validator);

            SpecificationEditModel specificationEditModel = new SpecificationEditModel();

            string json = JsonConvert.SerializeObject(specificationEditModel);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpContext context = Substitute.For<HttpContext>();

            HttpRequest request = Substitute.For<HttpRequest>();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) },
            });

            request
                .Query
                .Returns(queryStringValues);
            request
                .Body
                .Returns(stream);

            request
                .HttpContext
                .Returns(context);

            // Act
            IActionResult result = await specificationsService.EditSpecification(request);

            // Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .BeOfType<SerializableError>()
                .Which
                .Should()
                .HaveCount(1);

            await validator
                .Received(1)
                .ValidateAsync(Arg.Any<SpecificationEditModel>());
        }

        [TestMethod]
        public async Task EditSpecification_GivenSpecificationWasNotFound_ReturnsNotFoundResult()
        {
            //Arrange
            SpecificationEditModel specificationEditModel = new SpecificationEditModel();

            string json = JsonConvert.SerializeObject(specificationEditModel);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpContext context = Substitute.For<HttpContext>();

            HttpRequest request = Substitute.For<HttpRequest>();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) },
            });

            request
                .Query
                .Returns(queryStringValues);
            request
                .Body
                .Returns(stream);

            request
                .HttpContext
                .Returns(context);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns((Specification)null);

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository);

            //Act
            IActionResult result = await service.EditSpecification(request);

            //Arrange
            result
                .Should()
                .BeOfType<NotFoundObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Specification not found");

            logger
                .Received(1)
                .Warning(Arg.Is($"Failed to find specification for id: {SpecificationId}"));
        }

        [TestMethod]
        public async Task EditSpecification_GivenSpecificationWasfoundAndFundingPeriodChangedButFailedToGetFundingPeriodsFromCosmos_ReturnsPreConditionFailedresult()
        {
            //Arrange
            SpecificationEditModel specificationEditModel = new SpecificationEditModel
            {
                FundingPeriodId = "fp10"
            };

            string json = JsonConvert.SerializeObject(specificationEditModel);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpContext context = Substitute.For<HttpContext>();

            HttpRequest request = Substitute.For<HttpRequest>();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) },
            });

            request
                .Query
                .Returns(queryStringValues);
            request
                .Body
                .Returns(stream);

            request
                .HttpContext
                .Returns(context);

            ILogger logger = CreateLogger();

            Specification specification = CreateSpecification();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(specification);

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository);

            //Act
            IActionResult result = await service.EditSpecification(request);

            //Arrange
            result
                .Should()
                .BeOfType<PreconditionFailedResult>()
                .Which
                .Value
                .Should()
                .Be($"Unable to find funding period with ID '{specificationEditModel.FundingPeriodId}'.");
        }

        [TestMethod]
        public async Task EditSpecification_GivenSpecificationWasFoundAndFundingPeriodChangedAndfundinfgStreamsChangedButFailsToFindFundingStreams_ReturnsInternalServerErrorResult()
        {
            //Arrange
            SpecificationEditModel specificationEditModel = new SpecificationEditModel
            {
                FundingPeriodId = "fp10"
            };

            FundingPeriod fundingPeriod = new FundingPeriod
            {
                Id = "fp10",
                Name = "fp 10"
            };

            string json = JsonConvert.SerializeObject(specificationEditModel);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpContext context = Substitute.For<HttpContext>();

            HttpRequest request = Substitute.For<HttpRequest>();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) },
            });

            request
                .Query
                .Returns(queryStringValues);
            request
                .Body
                .Returns(stream);

            request
                .HttpContext
                .Returns(context);

            ILogger logger = CreateLogger();

            Specification specification = CreateSpecification();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();

            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(specification);

            specificationsRepository
                .GetFundingPeriodById(Arg.Is(fundingPeriod.Id))
                .Returns(fundingPeriod);

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository);

            //Act
            IActionResult result = await service.EditSpecification(request);

            //Arrange
            result
                .Should()
                .BeOfType<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be("No funding streams were retrieved to add to the Specification");
        }

        [TestMethod]
        public async Task EditSpecification_GivenFailsToUpdateCosomosWithBadRequest_ReturnsBadRequest()
        {
            //Arrange
            SpecificationEditModel specificationEditModel = new SpecificationEditModel
            {
                FundingPeriodId = "fp10"
            };

            FundingPeriod fundingPeriod = new FundingPeriod
            {
                Id = "fp10",
                Name = "fp 10"
            };

            IEnumerable<FundingStream> fundingStreams = new[]
            {
                new FundingStream()
            };

            string json = JsonConvert.SerializeObject(specificationEditModel);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpContext context = Substitute.For<HttpContext>();

            HttpRequest request = Substitute.For<HttpRequest>();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) },
            });

            request
                .Query
                .Returns(queryStringValues);
            request
                .Body
                .Returns(stream);

            request
                .HttpContext
                .Returns(context);

            ILogger logger = CreateLogger();

            Specification specification = CreateSpecification();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();

            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(specification);

            specificationsRepository
                .GetFundingPeriodById(Arg.Is(fundingPeriod.Id))
                .Returns(fundingPeriod);

            specificationsRepository
                .GetFundingStreams(Arg.Any<Expression<Func<FundingStream, bool>>>())
                .Returns(fundingStreams);

            specificationsRepository
                .UpdateSpecification(Arg.Any<Specification>())
                .Returns(HttpStatusCode.BadRequest);

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository);

            //Act
            IActionResult result = await service.EditSpecification(request);

            //Arrange
            result
                .Should()
                .BeOfType<StatusCodeResult>()
                .Which
                .StatusCode
                .Should()
                .Be(400);
        }

        [TestMethod]
        public async Task EditSpecification_GivenChanges_UpdatesSearchAndSendsMessage()
        {
            //Arrange
            SpecificationEditModel specificationEditModel = new SpecificationEditModel
            {
                FundingPeriodId = "fp10",
                Name = "new spec name",
                FundingStreamIds = new[] { "fs11" }
            };

            FundingPeriod fundingPeriod = new FundingPeriod
            {
                Id = "fp10",
                Name = "fp 10"
            };

            IEnumerable<FundingStream> fundingStreams = new[]
            {
                new FundingStream{
                    AllocationLines = new List<AllocationLine>
                    {
                        new AllocationLine { Id = "al1", Name = "al2"}
                    }
                }
            };

            string json = JsonConvert.SerializeObject(specificationEditModel);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpContext context = Substitute.For<HttpContext>();

            HttpRequest request = Substitute.For<HttpRequest>();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) },
            });

            request
                .Query
                .Returns(queryStringValues);
            request
                .Body
                .Returns(stream);

            request
                .HttpContext
                .Returns(context);

            ILogger logger = CreateLogger();

            Specification specification = CreateSpecification();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();

            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(specification);

            specificationsRepository
                .GetFundingPeriodById(Arg.Is(fundingPeriod.Id))
                .Returns(fundingPeriod);

            specificationsRepository
                .GetFundingStreams(Arg.Any<Expression<Func<FundingStream, bool>>>())
                .Returns(fundingStreams);

            specificationsRepository
                .UpdateSpecification(Arg.Any<Specification>())
                .Returns(HttpStatusCode.OK);

            ISearchRepository<SpecificationIndex> searchRepository = CreateSearchRepository();

            ICacheProvider cacheProvider = CreateCacheProvider();

            IMessengerService messengerService = CreateMessengerService();

            SpecificationsService service = CreateService(
                logs: logger, specificationsRepository: specificationsRepository, searchRepository: searchRepository, cacheProvider: cacheProvider, messengerService: messengerService);

            //Act
            IActionResult result = await service.EditSpecification(request);

            //Arrange
            await
                searchRepository
                    .Received(1)
                    .Index(Arg.Is<IEnumerable<SpecificationIndex>>(
                            m => m.First().Id == SpecificationId &&
                            m.First().Name == "new spec name" &&
                            m.First().FundingPeriodId == "fp10" &&
                            m.First().FundingStreamIds.Count() == 1
                        ));

            await
                cacheProvider
                    .Received(1)
                    .RemoveAsync<SpecificationSummary>(Arg.Is($"{CacheKeys.SpecificationSummaryById}{specification.Id}"));

            await
                messengerService
                    .Received(1)
                    .SendToTopic(Arg.Is(ServiceBusConstants.TopicNames.EditSpecification),
                                Arg.Is<SpecificationVersionComparisonModel>(
                                    m => m.Id == SpecificationId &&
                                    m.Current.Name == "new spec name" &&
                                    m.Previous.Name == "Spec name"
                                    ), Arg.Any<IDictionary<string, string>>());
        }

        [TestMethod]
        public async Task EditSpecification_GivenChangesAndSpecContainsPoliciesAndCalculations_UpdatesSearchAndSendsMessage()
        {
            //Arrange
            SpecificationEditModel specificationEditModel = new SpecificationEditModel
            {
                FundingPeriodId = "fp10",
                Name = "new spec name",
                FundingStreamIds = new[] { "fs11" }
            };

            FundingPeriod fundingPeriod = new FundingPeriod
            {
                Id = "fp10",
                Name = "fp 10"
            };

            IEnumerable<FundingStream> fundingStreams = new[]
            {
                new FundingStream{
                    AllocationLines = new List<AllocationLine>
                    {
                        new AllocationLine { Id = "al1", Name = "al2"}
                    }
                }
            };

            string json = JsonConvert.SerializeObject(specificationEditModel);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpContext context = Substitute.For<HttpContext>();

            HttpRequest request = Substitute.For<HttpRequest>();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) },
            });

            request
                .Query
                .Returns(queryStringValues);
            request
                .Body
                .Returns(stream);

            request
                .HttpContext
                .Returns(context);

            ILogger logger = CreateLogger();

            Specification specification = CreateSpecification();
            specification
                .Current
                .Policies = new[]
                {
                    new Policy
                    {
                        Calculations = new[]
                        {
                            new Calculation { AllocationLine = new AllocationLine { Id = "oldallocationlineid"} }
                        }
                    }
                };

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();

            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(specification);

            specificationsRepository
                .GetFundingPeriodById(Arg.Is(fundingPeriod.Id))
                .Returns(fundingPeriod);

            specificationsRepository
                .GetFundingStreams(Arg.Any<Expression<Func<FundingStream, bool>>>())
                .Returns(fundingStreams);

            specificationsRepository
                .UpdateSpecification(Arg.Any<Specification>())
                .Returns(HttpStatusCode.OK);

            ISearchRepository<SpecificationIndex> searchRepository = CreateSearchRepository();

            ICacheProvider cacheProvider = CreateCacheProvider();

            IMessengerService messengerService = CreateMessengerService();

            SpecificationsService service = CreateService(
                logs: logger, specificationsRepository: specificationsRepository, searchRepository: searchRepository, cacheProvider: cacheProvider, messengerService: messengerService);

            //Act
            IActionResult result = await service.EditSpecification(request);

            //Arrange
            await
                searchRepository
                    .Received(1)
                    .Index(Arg.Is<IEnumerable<SpecificationIndex>>(
                            m => m.First().Id == SpecificationId &&
                            m.First().Name == "new spec name" &&
                            m.First().FundingPeriodId == "fp10" &&
                            m.First().FundingStreamIds.Count() == 1
                        ));

            await
                cacheProvider
                    .Received(1)
                    .RemoveAsync<SpecificationSummary>(Arg.Is($"{CacheKeys.SpecificationSummaryById}{specification.Id}"));

            await
                messengerService
                    .Received(1)
                    .SendToTopic(Arg.Is(ServiceBusConstants.TopicNames.EditSpecification),
                                Arg.Is<SpecificationVersionComparisonModel>(
                                    m => m.Id == SpecificationId &&
                                    m.Current.Name == "new spec name" &&
                                    m.Previous.Name == "Spec name"
                                    ), Arg.Any<IDictionary<string, string>>());
        }

        [TestMethod]
        public async Task EditSpecification_GivenChangesIncludingFundingPeriod_EnsuresCacheCorrectlyInvalidates()
        {
            //Arrange
            SpecificationEditModel specificationEditModel = new SpecificationEditModel
            {
                FundingPeriodId = "fp10",
                Name = "new spec name",
                FundingStreamIds = new[] { "fs11" }
            };

            FundingPeriod fundingPeriod = new FundingPeriod
            {
                Id = "fp10",
                Name = "fp 10"
            };

            IEnumerable<FundingStream> fundingStreams = new[]
            {
                new FundingStream()
            };

            string json = JsonConvert.SerializeObject(specificationEditModel);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpContext context = Substitute.For<HttpContext>();

            HttpRequest request = Substitute.For<HttpRequest>();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) },
            });

            request
                .Query
                .Returns(queryStringValues);
            request
                .Body
                .Returns(stream);

            request
                .HttpContext
                .Returns(context);

            ILogger logger = CreateLogger();

            Specification specification = CreateSpecification();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();

            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(specification);

            specificationsRepository
                .GetFundingPeriodById(Arg.Is(fundingPeriod.Id))
                .Returns(fundingPeriod);

            specificationsRepository
                .GetFundingStreams(Arg.Any<Expression<Func<FundingStream, bool>>>())
                .Returns(fundingStreams);

            specificationsRepository
                .UpdateSpecification(Arg.Any<Specification>())
                .Returns(HttpStatusCode.OK);

            ICacheProvider cacheProvider = CreateCacheProvider();

            SpecificationsService service = CreateService(
                logs: logger, specificationsRepository: specificationsRepository, cacheProvider: cacheProvider);

            //Act
            IActionResult result = await service.EditSpecification(request);

            //Arrange
            await
                cacheProvider
                    .Received(1)
                    .RemoveAsync<SpecificationSummary>(Arg.Is($"{CacheKeys.SpecificationSummaryById}{specification.Id}"));

            await
                cacheProvider
                    .Received(1)
                    .RemoveAsync<List<SpecificationSummary>>(Arg.Is($"{CacheKeys.SpecificationSummariesByFundingPeriodId}fp10"));
        }

        [TestMethod]
        public async Task EditSpecification_GivenChangesButFundingPeriodUnchanged_EnsuresCacheCorrectlyInvalidates()
        {
            //Arrange
            SpecificationEditModel specificationEditModel = new SpecificationEditModel
            {
                FundingPeriodId = "FP1",
                Name = "new spec name",
                FundingStreamIds = new[] { "fs11" }
            };

            FundingPeriod fundingPeriod = new FundingPeriod
            {
                Id = "FP1",
                Name = "fp 1"
            };

            IEnumerable<FundingStream> fundingStreams = new[]
            {
                new FundingStream()
            };

            string json = JsonConvert.SerializeObject(specificationEditModel);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpContext context = Substitute.For<HttpContext>();

            HttpRequest request = Substitute.For<HttpRequest>();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) },
            });

            request
                .Query
                .Returns(queryStringValues);
            request
                .Body
                .Returns(stream);

            request
                .HttpContext
                .Returns(context);

            ILogger logger = CreateLogger();

            Specification specification = CreateSpecification();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();

            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(specification);

            specificationsRepository
                .GetFundingPeriodById(Arg.Is(fundingPeriod.Id))
                .Returns(fundingPeriod);

            specificationsRepository
                .GetFundingStreams(Arg.Any<Expression<Func<FundingStream, bool>>>())
                .Returns(fundingStreams);

            specificationsRepository
                .UpdateSpecification(Arg.Any<Specification>())
                .Returns(HttpStatusCode.OK);

            ICacheProvider cacheProvider = CreateCacheProvider();

            SpecificationsService service = CreateService(
                logs: logger, specificationsRepository: specificationsRepository, cacheProvider: cacheProvider);

            //Act
            IActionResult result = await service.EditSpecification(request);

            //Arrange
            await
                cacheProvider
                    .Received(1)
                    .RemoveAsync<SpecificationSummary>(Arg.Is($"{CacheKeys.SpecificationSummaryById}{specification.Id}"));

            await
                cacheProvider
                    .DidNotReceive()
                    .RemoveAsync<List<SpecificationSummary>>(Arg.Is($"{CacheKeys.SpecificationSummariesByFundingPeriodId}fp1"));
        }

    }
}
