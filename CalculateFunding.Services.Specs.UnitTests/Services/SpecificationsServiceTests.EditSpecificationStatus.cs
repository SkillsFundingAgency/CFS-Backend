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
using CalculateFunding.Common.Caching;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Services.Core.Interfaces;

namespace CalculateFunding.Services.Specs.Services
{
    public partial class SpecificationsServiceTests
    {
        [TestMethod]
        public async Task EditSpecificationStatus_GivenNoSpecificationIdWasProvided_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.EditSpecificationStatus(request);

            //Arrange
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No specification Id was provided to EditSpecification"));
        }

        [TestMethod]
        public async Task EditSpecificationStatus_GivenNullEditModeldWasProvided_ReturnsBadRequest()
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
            IActionResult result = await service.EditSpecificationStatus(request);

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
        public async Task EditSpecificationStatus_GivenAnInvalidStatus_ReturnsBadRequest()
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

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.EditSpecificationStatus(request);

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
                .Error(Arg.Any<JsonSerializationException>(), Arg.Is($"An invalid status was provided for specification: {SpecificationId}"));
        }

        [TestMethod]
        public async Task EditSpecificationStatus_GivenSpecificationWasNotFound_ReturnsNotFoundResult()
        {
            //Arrange
            EditStatusModel specificationEditStatusModel = new EditStatusModel
            {
                PublishStatus = PublishStatus.Approved
            };

            string json = JsonConvert.SerializeObject(specificationEditStatusModel);
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
            IActionResult result = await service.EditSpecificationStatus(request);

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
        public async Task EditSpecificationStatus_GivenStatusHasntChanges_DoesNotUpdateReturnsOkResult()
        {
            //Arrange
            EditStatusModel specificationEditStatusModel = new EditStatusModel
            {
                PublishStatus = PublishStatus.Approved
            };

            string json = JsonConvert.SerializeObject(specificationEditStatusModel);
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
            specification.Current.PublishStatus = PublishStatus.Approved;

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(specification);

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository);

            //Act
            IActionResult result = await service.EditSpecificationStatus(request);

            //Arrange
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .Be(specification);

            await
                specificationsRepository
                .DidNotReceive()
                .UpdateSpecification(Arg.Any<Specification>());
        }

        [TestMethod]
        public async Task EditSpecificationStatus_GivenNewStatusButUpdatingDbReturnsBadRequest_ReturnsStatusCode400()
        {
            //Arrange
            EditStatusModel specificationEditStatusModel = new EditStatusModel
            {
                PublishStatus = PublishStatus.Approved
            };

            string json = JsonConvert.SerializeObject(specificationEditStatusModel);
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
                .UpdateSpecification(Arg.Any<Specification>())
                .Returns(HttpStatusCode.BadRequest);

            SpecificationsService service = CreateService(
                logs: logger, specificationsRepository: specificationsRepository);

            //Act
            IActionResult result = await service.EditSpecificationStatus(request);

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
        public async Task EditSpecificationStatus_GivenNewStatus_UpdatesSearchReturnsOK()
        {
            //Arrange
            EditStatusModel specificationEditStatusModel = new EditStatusModel
            {
                PublishStatus = PublishStatus.Approved
            };

            string json = JsonConvert.SerializeObject(specificationEditStatusModel);
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
                .UpdateSpecification(Arg.Any<Specification>())
                .Returns(HttpStatusCode.OK);

            ISearchRepository<SpecificationIndex> searchRepository = CreateSearchRepository();

            SpecificationVersion newSpecVersion = specification.Current.Clone() as SpecificationVersion;
            newSpecVersion.PublishStatus = PublishStatus.Approved;

            IVersionRepository<SpecificationVersion> versionRepository = CreateVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<SpecificationVersion>(), Arg.Any<SpecificationVersion>())
                .Returns(newSpecVersion);

            SpecificationsService service = CreateService(
                logs: logger, specificationsRepository: specificationsRepository, searchRepository: searchRepository, specificationVersionRepository: versionRepository);

            //Act
            IActionResult result = await service.EditSpecificationStatus(request);

            //Arrange
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeOfType<PublishStatusResultModel>()
                .Which
                .PublishStatus
                .Should()
                .Be(PublishStatus.Approved);

            specification
                .Current
                .PublishStatus
                .Should()
                .Be(PublishStatus.Approved);

            await
                searchRepository
                .Received(1)
                .Index(Arg.Is<IEnumerable<SpecificationIndex>>(m => m.First().Status == "Approved"));

            await
                versionRepository
                 .Received(1)
                 .SaveVersion(Arg.Is(newSpecVersion));
        }

        [TestMethod]
        public async Task EditSpecificationStatus_GivenSpecificationISApprovedButNewStatusIsDraft_ThenBadRequestReturned()
        {
            //Arrange
            EditStatusModel specificationEditStatusModel = new EditStatusModel
            {
                PublishStatus = PublishStatus.Draft
            };

            string json = JsonConvert.SerializeObject(specificationEditStatusModel);
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
            specification.Current.PublishStatus = PublishStatus.Approved;

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();

            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(specification);

            specificationsRepository
                .UpdateSpecification(Arg.Any<Specification>())
                .Returns(HttpStatusCode.OK);

            ISearchRepository<SpecificationIndex> searchRepository = CreateSearchRepository();

            SpecificationsService service = CreateService(
                logs: logger, specificationsRepository: specificationsRepository, searchRepository: searchRepository);

            // Act
            IActionResult result = await service.EditSpecificationStatus(request);

            // Arrange
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Publish status can't be changed to Draft from Updated or Approved");

            specification
                .Current
                .PublishStatus
                .Should()
                .Be(PublishStatus.Approved);

            await
                searchRepository
                .Received(0)
                .Index(Arg.Any<IEnumerable<SpecificationIndex>>());
        }

        [TestMethod]
        public async Task EditSpecificationStatus_GivenNewStatusOfUpdated_UpdatesSearchReturnsOK()
        {
            //Arrange
            EditStatusModel specificationEditStatusModel = new EditStatusModel
            {
                PublishStatus = PublishStatus.Updated
            };

            string json = JsonConvert.SerializeObject(specificationEditStatusModel);
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

            SpecificationVersion specificationVersion = specification.Current;

            specificationVersion.PublishStatus = PublishStatus.Approved;

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();

            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(specification);

            specificationsRepository
                .UpdateSpecification(Arg.Any<Specification>())
                .Returns(HttpStatusCode.OK);

            ISearchRepository<SpecificationIndex> searchRepository = CreateSearchRepository();

            SpecificationVersion newSpecVersion = specificationVersion.Clone() as SpecificationVersion;
            newSpecVersion.PublishStatus = PublishStatus.Updated;

            IVersionRepository<SpecificationVersion> versionRepository = CreateVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<SpecificationVersion>(), Arg.Any<SpecificationVersion>())
                .Returns(newSpecVersion);

            SpecificationsService service = CreateService(
                logs: logger, specificationsRepository: specificationsRepository, searchRepository: searchRepository, specificationVersionRepository: versionRepository);

            //Act
            IActionResult result = await service.EditSpecificationStatus(request);

            //Arrange
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeOfType<PublishStatusResultModel>()
                .Which
                .PublishStatus
                .Should()
                .Be(PublishStatus.Updated);

            specification
                .Current
                .PublishStatus
                .Should()
                .Be(PublishStatus.Updated);

            await
                searchRepository
                .Received(1)
                .Index(Arg.Is<IEnumerable<SpecificationIndex>>(m => m.First().Status == "Updated"));

            await
                versionRepository
                 .Received(1)
                 .SaveVersion(Arg.Is(newSpecVersion));
        }

		[TestMethod]
		public void EditSpecificationStatus_GivenSomethingGoesWrongDuringIndexing_ShouldThrowException()
		{
			//Arrange
			const string errorMessage = "Encountered 802 error code";

			EditStatusModel specificationEditStatusModel = new EditStatusModel
			{
				PublishStatus = PublishStatus.Approved
			};

			string json = JsonConvert.SerializeObject(specificationEditStatusModel);
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
				.UpdateSpecification(Arg.Any<Specification>())
				.Returns(HttpStatusCode.OK);

			ISearchRepository<SpecificationIndex> searchRepository = CreateSearchRepository();
			searchRepository
				.Index(Arg.Any<IEnumerable<SpecificationIndex>>())
				.Returns(new[] { new IndexError(){ErrorMessage = errorMessage } });

			SpecificationVersion newSpecVersion = specification.Current.Clone() as SpecificationVersion;
			newSpecVersion.PublishStatus = PublishStatus.Approved;

			IVersionRepository<SpecificationVersion> versionRepository = CreateVersionRepository();
			versionRepository
				.CreateVersion(Arg.Any<SpecificationVersion>(), Arg.Any<SpecificationVersion>())
				.Returns(newSpecVersion);

			SpecificationsService service = CreateService(
				logs: logger, specificationsRepository: specificationsRepository, searchRepository: searchRepository, specificationVersionRepository: versionRepository);

			// Act
			Func<Task<IActionResult>> editSpecificationStatus = async () => await service.EditSpecificationStatus(request);

			// Assert
			editSpecificationStatus
				.Should()
				.Throw<ApplicationException>()
				.Which
				.Message
				.Should()
				.Be($"Could not index specification {specification.Current.Id} because: {errorMessage}");
		}
	}
}

