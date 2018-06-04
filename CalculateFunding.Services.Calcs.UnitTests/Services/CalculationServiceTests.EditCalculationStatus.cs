using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs.Services
{
    public partial class CalculationServiceTests
    {
        [TestMethod]
        public async Task EditCalculationStatus_GivenNoCalculationIdWasProvided_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            CalculationService service = CreateCalculationService(logger: logger);

            //Act
            IActionResult result = await service.EditCalculationStatus(request);

            //Arrange
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No calculation Id was provided to EditCalculationStatus"));
        }

        [TestMethod]
        public async Task EditCalculationStatus_GivenNullEditModeldWasProvided_ReturnsBadRequest()
        {
            //Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) },
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            CalculationService service = CreateCalculationService(logger: logger);

            //Act
            IActionResult result = await service.EditCalculationStatus(request);

            //Arrange
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Null status model provided");

            logger
                .Received(1)
                .Error(Arg.Is("A null status model was provided"));
        }

        [TestMethod]
        public async Task EditCalculationStatus_GivenAnInvalidStatus_ReturnsBadRequest()
        {
            //Arrange

            string json = @"{
	                            ""publishStatus"" : ""whatever""
                            }";

            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpContext context = Substitute.For<HttpContext>();

            HttpRequest request = Substitute.For<HttpRequest>();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) },
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

            CalculationService service = CreateCalculationService(logger: logger);

            //Act
            IActionResult result = await service.EditCalculationStatus(request);

            //Arrange
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("An invalid status was provided");

            logger
                .Received(1)
                .Error(Arg.Any<JsonSerializationException>(), Arg.Is($"An invalid status was provided for calculation: {CalculationId}"));
        }

        [TestMethod]
        public async Task EditCalculationStatus_GivenCalculationWasNotFound_ReturnsNotFoundResult()
        {
            //Arrange
            EditStatusModel CalculationEditStatusModel = new EditStatusModel
            {
                PublishStatus = PublishStatus.Approved
            };

            string json = JsonConvert.SerializeObject(CalculationEditStatusModel);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpContext context = Substitute.For<HttpContext>();

            HttpRequest request = Substitute.For<HttpRequest>();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) },
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

            ICalculationsRepository CalculationsRepository = CreateCalculationsRepository();
            CalculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns((Calculation)null);

            CalculationService service = CreateCalculationService(logger: logger, calculationsRepository: CalculationsRepository);

            //Act
            IActionResult result = await service.EditCalculationStatus(request);

            //Arrange
            result
                .Should()
                .BeOfType<NotFoundObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Calculation not found");

            logger
                .Received(1)
                .Warning(Arg.Is($"A calculation was not found for calculation id {CalculationId}"));
        }


        [TestMethod]
        public async Task EditCalculationStatus_GivenCurrentCalculationWasNotFound_ReturnsNotFoundResult()
        {
            //Arrange
            EditStatusModel CalculationEditStatusModel = new EditStatusModel
            {
                PublishStatus = PublishStatus.Approved
            };

            string json = JsonConvert.SerializeObject(CalculationEditStatusModel);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpContext context = Substitute.For<HttpContext>();

            HttpRequest request = Substitute.For<HttpRequest>();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) },
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

            Calculation calculation = new Calculation();

            ICalculationsRepository CalculationsRepository = CreateCalculationsRepository();
            CalculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            CalculationService service = CreateCalculationService(logger: logger, calculationsRepository: CalculationsRepository);

            //Act
            IActionResult result = await service.EditCalculationStatus(request);

            //Arrange
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"A current calculation was not found for calculation id {CalculationId}"));
        }

        [TestMethod]
        public async Task EditCalculationStatus_GivenStatusHasntChanges_DoesNotUpdateReturnsOkResult()
        {
            //Arrange
            EditStatusModel CalculationEditStatusModel = new EditStatusModel
            {
                PublishStatus = PublishStatus.Approved
            };

            string json = JsonConvert.SerializeObject(CalculationEditStatusModel);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpContext context = Substitute.For<HttpContext>();

            HttpRequest request = Substitute.For<HttpRequest>();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) },
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

            Calculation calculation = CreateCalculation();
            calculation.Current.PublishStatus = PublishStatus.Approved;

            ICalculationsRepository CalculationsRepository = CreateCalculationsRepository();
            CalculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            CalculationService service = CreateCalculationService(logger: logger, calculationsRepository: CalculationsRepository);

            //Act
            IActionResult result = await service.EditCalculationStatus(request);

            //Arrange
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .Be(calculation.Current);

            await
                CalculationsRepository
                .DidNotReceive()
                .UpdateCalculation(Arg.Any<Calculation>());
        }

        [TestMethod]
        public async Task EditCalculationStatus_GivenNewStatusButNoSpecSummaryFound_ReturnsPreConditionFailed()
        {
            //Arrange
            EditStatusModel CalculationEditStatusModel = new EditStatusModel
            {
                PublishStatus = PublishStatus.Approved
            };

            string json = JsonConvert.SerializeObject(CalculationEditStatusModel);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpContext context = Substitute.For<HttpContext>();

            HttpRequest request = Substitute.For<HttpRequest>();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) },
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

            Calculation calculation = CreateCalculation();

            ICalculationsRepository CalculationsRepository = CreateCalculationsRepository();

            CalculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns((Models.Specs.SpecificationSummary)null);

            CalculationService service = CreateCalculationService(
                logger: logger, calculationsRepository: CalculationsRepository, specificationRepository: specificationRepository);

            //Act
            IActionResult result = await service.EditCalculationStatus(request);

            //Arrange
            result
                .Should()
                .BeAssignableTo<PreconditionFailedResult>()
                .Which
                .Value
                .Should()
                .Be("Specification not found");
        }

        [TestMethod]
        public async Task EditCalculationStatus_GivenNewStatusButUpdatingDbReturnsBadRequest_ReturnsStatusCode400()
        {
            //Arrange
            EditStatusModel CalculationEditStatusModel = new EditStatusModel
            {
                PublishStatus = PublishStatus.Approved
            };

            string json = JsonConvert.SerializeObject(CalculationEditStatusModel);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpContext context = Substitute.For<HttpContext>();

            HttpRequest request = Substitute.For<HttpRequest>();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) },
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

            Calculation calculation = CreateCalculation();

            ICalculationsRepository CalculationsRepository = CreateCalculationsRepository();

            CalculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            CalculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.BadRequest);

            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary();

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(specificationSummary);

            CalculationService service = CreateCalculationService(
                logger: logger, calculationsRepository: CalculationsRepository, specificationRepository: specificationRepository);

            //Act
            IActionResult result = await service.EditCalculationStatus(request);

            //Arrange
            result
                .Should()
                .BeAssignableTo<StatusCodeResult>()
                .Which
                .StatusCode
                .Should()
                .Be(400);
        }

        [TestMethod]
        public async Task EditCalculationStatus_GivenNewStatus_UpdatesSearchReturnsOK()
        {
            //Arrange
            EditStatusModel CalculationEditStatusModel = new EditStatusModel
            {
                PublishStatus = PublishStatus.Approved
            };

            string json = JsonConvert.SerializeObject(CalculationEditStatusModel);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpContext context = Substitute.For<HttpContext>();

            HttpRequest request = Substitute.For<HttpRequest>();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) },
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

            Calculation calculation = CreateCalculation();

            ICalculationsRepository CalculationsRepository = CreateCalculationsRepository();

            CalculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            CalculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);


            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary();

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(specificationSummary);

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();

            CalculationService service = CreateCalculationService(
                logger: logger, calculationsRepository: CalculationsRepository, searchRepository: searchRepository, specificationRepository: specificationRepository);

            //Act
            IActionResult result = await service.EditCalculationStatus(request);

            //Arrange
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .Be(calculation.Current);

            calculation
                .Current
                .PublishStatus
                .Should()
                .Be(PublishStatus.Approved);

            await
                searchRepository
                .Received(1)
                .Index(Arg.Is<IEnumerable<CalculationIndex>>(m => m.First().Status == "Approved"));
        }

        [TestMethod]
        public async Task EditCalculationStatus_GivenCalculationIsApprovedButNewStatusIsDraft_UpdatesSearchReturnsOK()
        {
            //Arrange
            EditStatusModel CalculationEditStatusModel = new EditStatusModel
            {
                PublishStatus = PublishStatus.Draft
            };

            string json = JsonConvert.SerializeObject(CalculationEditStatusModel);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpContext context = Substitute.For<HttpContext>();

            HttpRequest request = Substitute.For<HttpRequest>();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) },
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

            Calculation calculation = CreateCalculation();
            calculation.Current.PublishStatus = PublishStatus.Approved;

            ICalculationsRepository CalculationsRepository = CreateCalculationsRepository();

            CalculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            CalculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);


            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary();

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(specificationSummary);

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();

            CalculationService service = CreateCalculationService(
                logger: logger, calculationsRepository: CalculationsRepository, searchRepository: searchRepository, specificationRepository: specificationRepository);

            //Act
            IActionResult result = await service.EditCalculationStatus(request);

            //Arrange
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .Be(calculation.Current);

            calculation
                .Current
                .PublishStatus
                .Should()
                .Be(PublishStatus.Draft);

            await
                searchRepository
                .Received(1)
                .Index(Arg.Is<IEnumerable<CalculationIndex>>(m => m.First().Status == "Draft"));
        }

        [TestMethod]
        public async Task EditCalculationStatus_GivenNewStatusOfUpdated_UpdatesSearchReturnsOK()
        {
            //Arrange
            EditStatusModel CalculationEditStatusModel = new EditStatusModel
            {
                PublishStatus = PublishStatus.Updated
            };

            string json = JsonConvert.SerializeObject(CalculationEditStatusModel);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpContext context = Substitute.For<HttpContext>();

            HttpRequest request = Substitute.For<HttpRequest>();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) },
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

            Calculation calculation = CreateCalculation();
            calculation.Current.PublishStatus = PublishStatus.Approved;

            ICalculationsRepository CalculationsRepository = CreateCalculationsRepository();

            CalculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            CalculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();

            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary();

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(specificationSummary);

            CalculationService service = CreateCalculationService(
                logger: logger, calculationsRepository: CalculationsRepository, searchRepository: searchRepository, specificationRepository: specificationRepository);

            //Act
            IActionResult result = await service.EditCalculationStatus(request);

            //Arrange
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .Be(calculation.Current);

            calculation
                .Current
                .PublishStatus
                .Should()
                .Be(PublishStatus.Updated);

            await
                searchRepository
                .Received(1)
                .Index(Arg.Is<IEnumerable<CalculationIndex>>(m => m.First().Status == "Updated"));
        }
    }
}
