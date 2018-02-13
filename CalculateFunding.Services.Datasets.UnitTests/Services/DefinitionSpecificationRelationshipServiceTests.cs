using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.Datasets.Interfaces;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
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
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets.Services
{
    [TestClass]
    public class DefinitionSpecificationRelationshipServiceTests
    {
        [TestMethod]
        async public Task CreateRelationship_GivenNullModelProvided_ReturnesBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger);

            //Act
            IActionResult result = await service.CreateRelationship(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("Null CreateDefinitionSpecificationRelationshipModel was provided to CreateRelationship"));
        }

        [TestMethod]
        async public Task CreateRelationship_GivenModelButWasInvalid_ReturnesBadRequest()
        {
            //Arrange
            CreateDefinitionSpecificationRelationshipModel model = new CreateDefinitionSpecificationRelationshipModel();
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ValidationResult validationResult = new ValidationResult(new[]{
                    new ValidationFailure("prop1", "any error")
                });

            IValidator<CreateDefinitionSpecificationRelationshipModel> validator = CreateRelationshipModelValidator(validationResult);

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger, relationshipModelValidator: validator);

            //Act
            IActionResult result = await service.CreateRelationship(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        async public Task CreateRelationship_GivenValidModelButDefinitionCouldNotBeFound_ReturnsPreConditionFailed()
        {
            //Arrange
            string datasetDefinitionId = Guid.NewGuid().ToString();

            CreateDefinitionSpecificationRelationshipModel model = new CreateDefinitionSpecificationRelationshipModel
            {
                DatasetDefinitionId = datasetDefinitionId
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetDatasetDefinition(Arg.Is(datasetDefinitionId))
                .Returns((Models.Datasets.Schema.DatasetDefinition)null);

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger, datasetRepository: datasetRepository);

            //Act
            IActionResult result = await service.CreateRelationship(request);

            //Assert
            result
                .Should()
                .BeOfType<StatusCodeResult>();

            StatusCodeResult statusCodeResult = result as StatusCodeResult;

            statusCodeResult
                .StatusCode
                .Should()
                .Be(412);

            logger
                .Received(1)
                .Error(Arg.Is($"Datset definition was not found for id {model.DatasetDefinitionId}"));
        }

        [TestMethod]
        async public Task CreateRelationship_GivenValidModelButSpecificationCouldNotBeFound_ReturnsPreConditionFailed()
        {
            //Arrange
            string datasetDefinitionId = Guid.NewGuid().ToString();
            string specificationId = Guid.NewGuid().ToString();

            Models.Datasets.Schema.DatasetDefinition definition = new Models.Datasets.Schema.DatasetDefinition();
           
            CreateDefinitionSpecificationRelationshipModel model = new CreateDefinitionSpecificationRelationshipModel
            {
                DatasetDefinitionId = datasetDefinitionId,
                SpecificationId = specificationId
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetDatasetDefinition(Arg.Is(datasetDefinitionId))
                .Returns(definition);

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Any<string>())
                .Returns((Specification)null);

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger, 
                datasetRepository: datasetRepository, specificationsRepository: specificationsRepository);

            //Act
            IActionResult result = await service.CreateRelationship(request);

            //Assert
            result
                .Should()
                .BeOfType<StatusCodeResult>();

            StatusCodeResult statusCodeResult = result as StatusCodeResult;

            statusCodeResult
                .StatusCode
                .Should()
                .Be(412);

            logger
              .Received(1)
              .Error(Arg.Is($"Specification was not found for id {model.SpecificationId}"));
        }

        [TestMethod]
        async public Task CreateRelationship_GivenValidModelButFailedToSave_ReturnsFailedResult()
        {
            //Arrange
            string datasetDefinitionId = Guid.NewGuid().ToString();
            string specificationId = Guid.NewGuid().ToString();

            Models.Datasets.Schema.DatasetDefinition definition = new Models.Datasets.Schema.DatasetDefinition();

            Specification specification = new Specification();

            CreateDefinitionSpecificationRelationshipModel model = new CreateDefinitionSpecificationRelationshipModel
            {
                DatasetDefinitionId = datasetDefinitionId,
                SpecificationId = specificationId
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetDatasetDefinition(Arg.Is(datasetDefinitionId))
                .Returns(definition);

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Any<string>())
                .Returns(specification);

            datasetRepository
                .SaveDefinitionSpecificationRelationship(Arg.Any<DefinitionSpecificationRelationship>())
                .Returns(HttpStatusCode.BadRequest);

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger,
                datasetRepository: datasetRepository, specificationsRepository: specificationsRepository);

            //Act
            IActionResult result = await service.CreateRelationship(request);

            //Assert
            result
                .Should()
                .BeOfType<StatusCodeResult>();

            StatusCodeResult statusCodeResult = result as StatusCodeResult;

            statusCodeResult
                .StatusCode
                .Should()
                .Be(400);

            logger
              .Received(1)
              .Error(Arg.Is($"Failed to save relationship with status code: BadRequest"));
        }

        [TestMethod]
        async public Task CreateRelationship_GivenValidModelAndSavesWithoutError_ReturnsOK()
        {
            //Arrange
            string datasetDefinitionId = Guid.NewGuid().ToString();
            string specificationId = Guid.NewGuid().ToString();

            Models.Datasets.Schema.DatasetDefinition definition = new Models.Datasets.Schema.DatasetDefinition
            {
                Id = datasetDefinitionId
            };

            Specification specification = new Specification
            {
                Id = specificationId
            };

            CreateDefinitionSpecificationRelationshipModel model = new CreateDefinitionSpecificationRelationshipModel
            {
                DatasetDefinitionId = datasetDefinitionId,
                SpecificationId = specificationId,
                Name = "test-name",
                Description = "test description"
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetDatasetDefinition(Arg.Is(datasetDefinitionId))
                .Returns(definition);

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Any<string>())
                .Returns(specification);

            datasetRepository
                .SaveDefinitionSpecificationRelationship(Arg.Any<DefinitionSpecificationRelationship>())
                .Returns(HttpStatusCode.Created);

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger,
                datasetRepository: datasetRepository, specificationsRepository: specificationsRepository);

            //Act
            IActionResult result = await service.CreateRelationship(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            await
                datasetRepository
                    .Received(1)
                    .SaveDefinitionSpecificationRelationship(Arg.Is<DefinitionSpecificationRelationship>(
                        m => !string.IsNullOrWhiteSpace(m.Id) &&
                        m.Description == "test description" &&
                        m.Name == "test-name" &&
                        m.Specification.Id == specificationId &&
                        m.DatasetDefinition.Id == datasetDefinitionId));
        }

        [TestMethod]
        public async Task GetRelationshipsBySpecificationId_GivenSpecificationIdDoesNotExist_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger);

            //Act
            IActionResult result = await service.GetRelationshipsBySpecificationId(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No specification id was provided to GetRelationshipsBySpecificationId"));
        }

        [TestMethod]
        public async Task GetRelationshipsBySpecificationId_GivenNoDataReturned_ReturnsOK()
        {
            //Arrange
            string specificationId = Guid.NewGuid().ToString();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(specificationId) }
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger);

            //Act
            IActionResult result = await service.GetRelationshipsBySpecificationId(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult objectResult = result as OkObjectResult;

            IEnumerable<DefinitionSpecificationRelationship> items = objectResult.Value as IEnumerable<DefinitionSpecificationRelationship>;

            items
                .Count()
                .Should()
                .Be(0);
        }

        [TestMethod]
        public async Task GetRelationshipsBySpecificationId_GivenItemsReturned_ReturnsOK()
        {
            //Arrange
            string specificationId = Guid.NewGuid().ToString();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(specificationId) }
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            IEnumerable<DefinitionSpecificationRelationship> relationships = new[]
            {
                new DefinitionSpecificationRelationship(),
                new DefinitionSpecificationRelationship(),
                new DefinitionSpecificationRelationship()
            };

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetDefinitionSpecificationRelationshipsByQuery(Arg.Any<Expression<Func<DefinitionSpecificationRelationship, bool>>>())
                .Returns(relationships);

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger, datasetRepository: datasetRepository);

            //Act
            IActionResult result = await service.GetRelationshipsBySpecificationId(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult objectResult = result as OkObjectResult;

            IEnumerable<DefinitionSpecificationRelationship> items = objectResult.Value as IEnumerable<DefinitionSpecificationRelationship>;

            items
                .Count()
                .Should()
                .Be(3);
        }


        static DefinitionSpecificationRelationshipService CreateService(IDatasetRepository datasetRepository = null,
            ILogger logger = null, ISpecificationsRepository specificationsRepository = null, IValidator<CreateDefinitionSpecificationRelationshipModel> relationshipModelValidator = null)
        {
            return new DefinitionSpecificationRelationshipService(datasetRepository ?? CreateDatasetRepository(), logger ?? CreateLogger(),
                specificationsRepository ?? CreateSpecificationsRepository(), relationshipModelValidator ?? CreateRelationshipModelValidator());
        }

        static IValidator<CreateDefinitionSpecificationRelationshipModel> CreateRelationshipModelValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
                validationResult = new ValidationResult();

            IValidator<CreateDefinitionSpecificationRelationshipModel> validator = Substitute.For<IValidator<CreateDefinitionSpecificationRelationshipModel>>();

            validator
               .ValidateAsync(Arg.Any<CreateDefinitionSpecificationRelationshipModel>())
               .Returns(validationResult);

            return validator;
        }

        static ISpecificationsRepository CreateSpecificationsRepository()
        {
            return Substitute.For<ISpecificationsRepository>();
        }

        static IDatasetRepository CreateDatasetRepository()
        {
            return Substitute.For<IDatasetRepository>();
        }

        static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }
    }
}
