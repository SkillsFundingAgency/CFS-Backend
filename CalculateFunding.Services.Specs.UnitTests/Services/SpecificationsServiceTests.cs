using AutoMapper;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Logging;
using CalculateFunding.Services.Specs.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Serilog;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog.Debugging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using FluentAssertions;
using System.Collections.Specialized;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Primitives;
using System.Linq.Expressions;
using System.Linq;
using Newtonsoft.Json;
using System.IO;

namespace CalculateFunding.Services.Specs.Services
{
    [TestClass]
    public class SpecificationsServiceTests
    {
        const string SpecificationId = "ffa8ccb3-eb8e-4658-8b3f-f1e4c3a8f313";
        const string AcademicYearId = "18/19";
        const string SpecificationName = "Test Spec 001";
        const string PolicyName = "Test Policy 001";
        const string CalculationName = "Test Calc 001";

        [TestMethod]
        public async Task GetSpecificationById_GivenSpecificationIdDoesNotExist_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.GetSpecificationById(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Any<string>());
        }

        [TestMethod]
        public async Task GetSpecificationById_GivenSpecificationWasNotFound_ReturnsNotFoundt()
        {
            //Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns((Specification)null);

            SpecificationsService service = CreateService(specifcationsRepository: specificationsRepository, logs: logger);

            //Act
            IActionResult result = await service.GetSpecificationById(request);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"A specification for id {SpecificationId} could not found"));
        }

        [TestMethod]
        public async Task GetSpecificationById_GivenSpecificationWasFound_ReturnsSuccesst()
        {
            //Arrange
            Specification specification = new Specification();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(specification);

            SpecificationsService service = CreateService(specifcationsRepository: specificationsRepository, logs: logger);

            //Act
            IActionResult result = await service.GetSpecificationById(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();
        }

        [TestMethod]
        public async Task GetSpecificationByAcademicYearId_GivenNoSpecificationsReturned_ReturnsSuccess()
        {
            //Arrange
            IEnumerable<Specification> specs = Enumerable.Empty<Specification>();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "academicYearId", new StringValues(AcademicYearId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationsByQuery(Arg.Any<Expression<Func<Specification, bool>>>())
                .Returns(specs);

            SpecificationsService service = CreateService(specifcationsRepository: specificationsRepository, logs: logger);

            //Act
            IActionResult result = await service.GetSpecificationByAcademicYearId(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            IEnumerable<Specification> objContent = (IEnumerable<Specification>)((OkObjectResult) result).Value;

            objContent
                .Count()
                .Should()
                .Be(0);

            logger
                .Received(1)
                .Information(Arg.Is($"No specifications found for academic year with id {AcademicYearId}"));
        }

        [TestMethod]
        public async Task GetSpecificationByAcademicYearId_GivenSpecificationsReturned_ReturnsSuccess()
        {
            //Arrange
            IEnumerable<Specification> specs = new[]
            {
                new Specification(),
                new Specification()
            };

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "academicYearId", new StringValues(AcademicYearId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                 .GetSpecificationsByQuery(Arg.Any<Expression<Func<Specification, bool>>>())
                 .Returns(specs);

            SpecificationsService service = CreateService(specifcationsRepository: specificationsRepository, logs: logger);

            //Act
            IActionResult result = await service.GetSpecificationByAcademicYearId(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            IEnumerable<Specification> objContent = (IEnumerable<Specification>)((OkObjectResult)result).Value;

            objContent
                .Count()
                .Should()
                .Be(2);

            logger
                .Received(1)
                .Information(Arg.Is($"Found {specs.Count()} specifications for academic year with id {AcademicYearId}"));
        }

        [TestMethod]
        public async Task GetSpecificationByAcademicYearId_GivenAcademicYearIdDoesNotExist_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.GetSpecificationByAcademicYearId(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Any<string>());
        }

        [TestMethod]
        public async Task GetSpecificationByName_GivenSpecificationNameDoesNotExist_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.GetSpecificationByName(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Any<string>());
        }

        [TestMethod]
        public async Task GetSpecificationByName_GivenSpecificationWasNotFound_ReturnsNotFoundt()
        {
            //Arrange
            IEnumerable<Specification> specs = Enumerable.Empty<Specification>();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationName", new StringValues(SpecificationName) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                 .GetSpecificationsByQuery(Arg.Any<Expression<Func<Specification, bool>>>())
                 .Returns(specs);

            SpecificationsService service = CreateService(specifcationsRepository: specificationsRepository, logs: logger);

            //Act
            IActionResult result = await service.GetSpecificationByName(request);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Information(Arg.Is($"Specification was not found for name: {SpecificationName}"));
        }

        [TestMethod]
        public async Task GetSpecificationByName_GivenSpecificationReturned_ReturnsSuccess()
        {
            //Arrange
            IEnumerable<Specification> specs = new[]
            {
                new Specification()
            };

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationName", new StringValues(SpecificationName) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                 .GetSpecificationsByQuery(Arg.Any<Expression<Func<Specification, bool>>>())
                 .Returns(specs);

            SpecificationsService service = CreateService(specifcationsRepository: specificationsRepository, logs: logger);

            //Act
            IActionResult result = await service.GetSpecificationByName(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            Specification objContent = (Specification)((OkObjectResult)result).Value;

            objContent
                .Should()
                .NotBeNull();

            logger
                .Received(1)
                .Information(Arg.Is($"Specification found for name: {SpecificationName}"));
        }

        [TestMethod]
        public async Task GetPolicyByName_GivenModelDoesNotContainASpecificationId_ReturnsBadRequest()
        {
            //Arrange
            PolicyGetModel model = new PolicyGetModel();
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.GetPolicyByName(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No specification id was provided to GetPolicyByName"));
        }

        [TestMethod]
        public async Task GetPolicyByName_GivenModelDoesNotContainAPolicyName_ReturnsBadRequest()
        {
            //Arrange
            PolicyGetModel model = new PolicyGetModel
            {
                SpecificationId = SpecificationId
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.GetPolicyByName(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No policy name was provided to GetPolicyByName"));
        }

        [TestMethod]
        public async Task GetPolicyByName_GivenSpecificationDoesNotExist_ReturnsPreConditionFailed()
        {
            //Arrange
            PolicyGetModel model = new PolicyGetModel
            {
                SpecificationId = SpecificationId,
                Name = PolicyName
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns((Specification)null);

            SpecificationsService service = CreateService(specifcationsRepository: specificationsRepository, logs: logger);

            //Act
            IActionResult result = await service.GetPolicyByName(request);

            //Assert
            result
                .Should()
                .BeOfType<StatusCodeResult>();

            StatusCodeResult statusCodeResult = (StatusCodeResult)result;

            statusCodeResult
                .StatusCode
                .Should()
                .Be(412);

            logger
                .Received(1)
                .Error(Arg.Is($"No specification was found for specification id {SpecificationId}"));
        }

        [TestMethod]
        public async Task GetPolicyByName_GivenSpecificationExistsAndPolicyExists_ReturnsSuccess()
        {
            //Arrange
            Specification spec = new Specification
            {
                Policies = new[]
                {
                    new Policy{ Name = PolicyName}
                }
            };

            PolicyGetModel model = new PolicyGetModel
            {
                SpecificationId = SpecificationId,
                Name = PolicyName
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(spec);

            SpecificationsService service = CreateService(specifcationsRepository: specificationsRepository, logs: logger);

            //Act
            IActionResult result = await service.GetPolicyByName(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            logger
                .Received(1)
                .Information(Arg.Is($"A policy was found for specification id {SpecificationId} and name {PolicyName}"));
        }

        [TestMethod]
        public async Task GetPolicyByName_GivenSpecificationExistsAndPolicyDoesNotExist_ReturnsNotFound()
        {
            //Arrange
            Specification spec = new Specification();

            PolicyGetModel model = new PolicyGetModel
            {
                SpecificationId = SpecificationId,
                Name = PolicyName
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(spec);

            SpecificationsService service = CreateService(specifcationsRepository: specificationsRepository, logs: logger);

            //Act
            IActionResult result = await service.GetPolicyByName(request);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Information(Arg.Is($"A policy was not found for specification id {SpecificationId} and name {PolicyName}"));
        }

        [TestMethod]
        public async Task GetCalculationByName_GivenModelDoesNotContainASpecificationId_ReturnsBadRequest()
        {
            //Arrange
            CalculationGetModel model = new CalculationGetModel();
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.GetCalculationByName(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No specification id was provided to GetCalculationByName"));
        }

        [TestMethod]
        public async Task GetCalculationByName_GivenModelDoesNotContainAcalculationName_ReturnsBadRequest()
        {
            //Arrange
            CalculationGetModel model = new CalculationGetModel
            {
                SpecificationId = SpecificationId
            };
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.GetCalculationByName(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No calculation name was provided to GetCalculationByName"));
        }

        [TestMethod]
        public async Task GetCalculationByName_GivenSpecificationExistsAndCalculationExists_ReturnsSuccess()
        {
            //Arrange
            Calculation calc = new Calculation { Name = CalculationName };

            CalculationGetModel model = new CalculationGetModel
            {
                SpecificationId = SpecificationId,
                Name = CalculationName
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCalculationBySpecificationIdAndCalculationName(Arg.Is(SpecificationId), Arg.Is(CalculationName))
                .Returns(calc);

            SpecificationsService service = CreateService(specifcationsRepository: specificationsRepository, logs: logger);

            //Act
            IActionResult result = await service.GetCalculationByName(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            logger
                .Received(1)
                .Information(Arg.Is($"A calculation was found for specification id {SpecificationId} and name {CalculationName}"));
        }

        [TestMethod]
        public async Task GetCalculationByName_GivenSpecificationExistsAndCalculationDoesNotExist_ReturnsNotFound()
        {
            //Arrange
            CalculationGetModel model = new CalculationGetModel
            {
                SpecificationId = SpecificationId,
                Name = CalculationName
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCalculationBySpecificationIdAndCalculationName(Arg.Is(SpecificationId), Arg.Is(CalculationName))
                .Returns((Calculation)null);

            SpecificationsService service = CreateService(specifcationsRepository: specificationsRepository, logs: logger);

            //Act
            IActionResult result = await service.GetCalculationByName(request);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Information(Arg.Is($"A calculation was not found for specification id {SpecificationId} and name {CalculationName}"));
        }

        static SpecificationsService CreateService(IMapper mapper = null, ISpecificationsRepository specifcationsRepository = null, 
            ILogger logs = null, IValidator<PolicyCreateModel> policyCreateModelValidator = null,
            IValidator<SpecificationCreateModel> specificationCreateModelvalidator = null, IValidator<CalculationCreateModel> calculationCreateModelValidator = null)
        {
            return new SpecificationsService(mapper ?? CreateMapper(), specifcationsRepository ?? CreateSpecificationsRepository(), logs ?? CreateLogger(), policyCreateModelValidator ?? CreatePolicyValidator(),
                specificationCreateModelvalidator ?? CreateSpecificationValidator(), calculationCreateModelValidator ?? CreateCalculationValidator());
        }

        static IMapper CreateMapper()
        {
            return Substitute.For<IMapper>();
        }

        static ISpecificationsRepository CreateSpecificationsRepository()
        {
            return Substitute.For<ISpecificationsRepository>();
        }

        static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

       
        static IValidator<PolicyCreateModel> CreatePolicyValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
                validationResult = new ValidationResult();

            IValidator<PolicyCreateModel> validator = Substitute.For<IValidator<PolicyCreateModel>>();

            validator
               .ValidateAsync(Arg.Any<PolicyCreateModel>())
               .Returns(validationResult);

            return validator;
        }

        static IValidator<SpecificationCreateModel> CreateSpecificationValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
                validationResult = new ValidationResult();

            IValidator<SpecificationCreateModel> validator = Substitute.For<IValidator<SpecificationCreateModel>>();

            validator
               .ValidateAsync(Arg.Any<SpecificationCreateModel>())
               .Returns(validationResult);

            return validator;
        }

        static IValidator<CalculationCreateModel> CreateCalculationValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
                validationResult = new ValidationResult();

            IValidator<CalculationCreateModel> validator = Substitute.For<IValidator<CalculationCreateModel>>();

            validator
               .ValidateAsync(Arg.Any<CalculationCreateModel>())
               .Returns(validationResult);

            return validator;
        }
    }

   
}
