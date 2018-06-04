using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Calcs.Interfaces;
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
using CalculateFunding.Services.Calcs.Interfaces.CodeGen;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.CodeGeneration;

namespace CalculateFunding.Services.Calcs.Services
{
    public partial class CalculationServiceTests
    {
        [TestMethod]
        async public Task SaveCalculationVersion_GivenNoCalculationIdWasProvided_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            CalculationService service = CreateCalculationService(logger: logger);

            //Act
            IActionResult result = await service.SaveCalculationVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No calculation Id was provided to GetCalculationHistory"));
        }

        [TestMethod]
        async public Task SaveCalculationVersion_GivenCalculationIdButNoModelSupplied_ReturnsBadRequest()
        {
            //Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            CalculationService service = CreateCalculationService(logger: logger);

            //Act
            IActionResult result = await service.SaveCalculationVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is($"Null or empty source code was provided for calculation id {CalculationId}"));
        }

        [TestMethod]
        async public Task SaveCalculationVersion_GivenModelDoesNotContainSourceCiode_ReturnsBadRequest()
        {
            //Arrange
            SaveSourceCodeVersion model = new SaveSourceCodeVersion();
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            CalculationService service = CreateCalculationService(logger: logger);

            //Act
            IActionResult result = await service.SaveCalculationVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is($"Null or empty source code was provided for calculation id {CalculationId}"));
        }

        [TestMethod]
        async public Task SaveCalculationVersion_GivenCalculationExistsWithNoHistory_CreatesNewVersion()
        {
            //Arrange
            Calculation calculation = CreateCalculation();

            SaveSourceCodeVersion model = new SaveSourceCodeVersion
            {
                SourceCode = "source code"
            };
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();

            CalculationIndex calcIndex = new CalculationIndex();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .SearchById(Arg.Is(CalculationId))
                .Returns(calcIndex);

            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
            };

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(specificationSummary);

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
                buildProjectsRepository: buildProjectsRepository,
                searchRepository: searchRepository,
                specificationRepository: specificationRepository);

            //Act
            IActionResult result = await service.SaveCalculationVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            logger
                .Received(1)
                .Information(Arg.Is($"History for {CalculationId} was null or empty and needed recreating."));
        }

        [TestMethod]
        async public Task SaveCalculationVersion_GivenModelButCalculationDoesNotExist_ReturnsNotFound()
        {
            //Arrange
            SaveSourceCodeVersion model = new SaveSourceCodeVersion
            {
                SourceCode = "source code"
            };
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns((Calculation)null);

            CalculationService service = CreateCalculationService(logger: logger, calculationsRepository: calculationsRepository);

            //Act
            IActionResult result = await service.SaveCalculationVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Error(Arg.Is($"A calculation was not found for calculation id {CalculationId}"));
        }

        [TestMethod]
        async public Task SaveCalculationVersion_GivenCalculationExistsWithNoCurrent_CreatesNewVersion()
        {
            //Arrange
            Calculation calculation = CreateCalculation();
            calculation.Current = null;

            SaveSourceCodeVersion model = new SaveSourceCodeVersion
            {
                SourceCode = "source code"
            };
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();

            CalculationIndex calcIndex = new CalculationIndex();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .SearchById(Arg.Is(CalculationId))
                .Returns(calcIndex);

            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
            };

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(specificationSummary);

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
                buildProjectsRepository: buildProjectsRepository,
                searchRepository: searchRepository,
                specificationRepository: specificationRepository);

            //Act
            IActionResult result = await service.SaveCalculationVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"Current for {CalculationId} was null and needed recreating."));
        }

        [TestMethod]
        async public Task SaveCalculationVersion_GivenCalculationExistsWithNoBuildId_CreatesNewBuildProject()
        {
            //Arrange
            Calculation calculation = CreateCalculation();

            SaveSourceCodeVersion model = new SaveSourceCodeVersion
            {
                SourceCode = "source code"
            };
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();

            CalculationIndex calcIndex = new CalculationIndex();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .SearchById(Arg.Is(CalculationId))
                .Returns(calcIndex);

            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
            };

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(specificationSummary);

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
                buildProjectsRepository: buildProjectsRepository,
                searchRepository: searchRepository,
                specificationRepository: specificationRepository);

            //Act
            IActionResult result = await service.SaveCalculationVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"Build project for specification {calculation.SpecificationId} could not be found, creating a new one"));

            await
                buildProjectsRepository
                    .Received(1)
                    .CreateBuildProject(Arg.Any<BuildProject>());
        }

        [TestMethod]
        async public Task SaveCalculationVersion_GivenCalculationExistsWithButBuildProjectDoesNotExist_CreatesNewBuildProject()
        {
            // Arrange
            string buildProjectId = Guid.NewGuid().ToString();

            Calculation calculation = CreateCalculation();
            calculation.BuildProjectId = buildProjectId;

            SaveSourceCodeVersion model = new SaveSourceCodeVersion
            {
                SourceCode = "source code"
            };
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();

            CalculationIndex calcIndex = new CalculationIndex();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .SearchById(Arg.Is(CalculationId))
                .Returns(calcIndex);

            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
            };

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(specificationSummary);

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
                buildProjectsRepository: buildProjectsRepository,
                searchRepository: searchRepository,
                specificationRepository: specificationRepository);

            // Act
            IActionResult result = await service.SaveCalculationVersion(request);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"Build project for specification {calculation.SpecificationId} could not be found, creating a new one"));

            await
                buildProjectsRepository
                    .Received(1)
                    .CreateBuildProject(Arg.Any<BuildProject>());
        }

        [TestMethod]
        async public Task SaveCalculationVersion_GivenCalculationExistsWithBuildIdButNoCalculations_EnsuresCalculationsCreatedUpdatesBuildProject()
        {
            //Arrange
            string buildProjectId = Guid.NewGuid().ToString();

            BuildProject buildProject = new BuildProject
            {
                Id = buildProjectId
            };

            Calculation calculation = CreateCalculation();
            calculation.BuildProjectId = buildProjectId;

            SaveSourceCodeVersion model = new SaveSourceCodeVersion
            {
                SourceCode = "source code"
            };
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectById(Arg.Is(buildProjectId))
                .Returns(buildProject);

            CalculationIndex calcIndex = new CalculationIndex();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .SearchById(Arg.Is(CalculationId))
                .Returns(calcIndex);

            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
            };

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(specificationSummary);

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
                buildProjectsRepository: buildProjectsRepository,
                searchRepository: searchRepository,
                specificationRepository: specificationRepository);

            //Act
            IActionResult result = await service.SaveCalculationVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"Build project for specification {calculation.SpecificationId} could not be found, creating a new one"));
        }

        [TestMethod]
        async public Task SaveCalculationVersion_GivenCalculationExistsWithBuildIdButNoCalculations_EnsuresCalculationSpecificationDescriptionSetWithSingleCalculation()
        {
            //Arrange
            string buildProjectId = Guid.NewGuid().ToString();

            BuildProject buildProject = new BuildProject
            {
                Id = buildProjectId
            };

            string specificationId = "789";

            List<Models.Specs.Calculation> specCalculations = new List<Models.Specs.Calculation>();

            Models.Specs.Calculation specCalculation = new Models.Specs.Calculation()
            {
                Id = "1234",
                Name = "Calculation Name",
                Description = "Calculation Description"
            };

            specCalculations.Add(specCalculation);

            List<Calculation> calcCalculations = new List<Calculation>();

            Calculation calculation = CreateCalculation();
            calculation.BuildProjectId = buildProjectId;
            calculation.SpecificationId = specificationId;
            calculation.CalculationSpecification.Id = specCalculation.Id;
            calculation.CalculationSpecification.Name = specCalculation.Name;

            calcCalculations.Add(calculation);

            SaveSourceCodeVersion model = new SaveSourceCodeVersion
            {
                SourceCode = "source code"
            };
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .GetCalculationsBySpecificationId(specificationId)
                .Returns(calcCalculations.AsEnumerable());

            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectById(Arg.Is(buildProjectId))
                .Returns(buildProject);

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetCalculationSpecificationsForSpecification(specificationId)
                .Returns(specCalculations.AsEnumerable());

            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
            };

            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(specificationSummary);

            CalculationIndex calcIndex = new CalculationIndex();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .SearchById(Arg.Is(CalculationId))
                .Returns(calcIndex);

            ISourceFileGenerator sourceFileGenerator = Substitute.For<ISourceFileGenerator>();

            ISourceFileGeneratorProvider sourceFileGeneratorProvider = Substitute.For<ISourceFileGeneratorProvider>();
            sourceFileGeneratorProvider.CreateSourceFileGenerator(TargetLanguage.VisualBasic)
                .Returns(sourceFileGenerator);

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
                specificationRepository: specificationRepository,
                buildProjectsRepository: buildProjectsRepository,
                searchRepository: searchRepository,
                sourceFileGeneratorProvider: sourceFileGeneratorProvider);

            //Act
            IActionResult result = await service.SaveCalculationVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"Build project for specification {calculation.SpecificationId} could not be found, creating a new one"));

            await calculationsRepository
                .Received(1)
                .UpdateCalculation(Arg.Is<Calculation>(c => c.Description == specCalculation.Description));
        }

        [TestMethod]
        async public Task SaveCalculationVersion_GivenCalculationExistsWithBuildIdButNoCalculations_EnsuresCalculationSpecificationDescriptionSetWithMultipleCalculations()
        {
            //Arrange
            string buildProjectId = Guid.NewGuid().ToString();

            BuildProject buildProject = new BuildProject
            {
                Id = buildProjectId
            };

            string specificationId = "789";

            List<Models.Specs.Calculation> specCalculations = new List<Models.Specs.Calculation>();

            Models.Specs.Calculation specCalculation1 = new Models.Specs.Calculation()
            {
                Id = "121",
                Name = "Calculation One",
                Description = "Calculation Description One"
            };

            specCalculations.Add(specCalculation1);

            Models.Specs.Calculation specCalculation2 = new Models.Specs.Calculation()
            {
                Id = "122",
                Name = "Calculation Two",
                Description = "Calculation Description Two"
            };

            specCalculations.Add(specCalculation2);

            List<Calculation> calcCalculations = new List<Calculation>();

            Calculation calculation = CreateCalculation();
            calculation.BuildProjectId = buildProjectId;
            calculation.SpecificationId = specificationId;
            calculation.CalculationSpecification.Id = specCalculation1.Id;
            calculation.CalculationSpecification.Name = specCalculation1.Name;

            calcCalculations.Add(calculation);

            Calculation calculation2 = CreateCalculation();
            calculation2.Id = "12555";
            calculation2.BuildProjectId = buildProjectId;
            calculation2.SpecificationId = specificationId;
            calculation2.CalculationSpecification.Id = specCalculation2.Id;
            calculation2.CalculationSpecification.Name = specCalculation2.Name;

            calcCalculations.Add(calculation2);

            SaveSourceCodeVersion model = new SaveSourceCodeVersion
            {
                SourceCode = "source code"
            };
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            calculationsRepository
                .GetCalculationsBySpecificationId(specificationId)
                .Returns(calcCalculations.AsEnumerable());

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectById(Arg.Is(buildProjectId))
                .Returns(buildProject);

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetCalculationSpecificationsForSpecification(specificationId)
                .Returns(specCalculations.AsEnumerable());

            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
            };

            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(specificationSummary);

            CalculationIndex calcIndex = new CalculationIndex();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .SearchById(Arg.Is(CalculationId))
                .Returns(calcIndex);

            ISourceFileGenerator sourceFileGenerator = Substitute.For<ISourceFileGenerator>();

            ISourceFileGeneratorProvider sourceFileGeneratorProvider = Substitute.For<ISourceFileGeneratorProvider>();
            sourceFileGeneratorProvider.CreateSourceFileGenerator(TargetLanguage.VisualBasic)
                .Returns(sourceFileGenerator);

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
                specificationRepository: specificationRepository,
                buildProjectsRepository: buildProjectsRepository,
                searchRepository: searchRepository,
                sourceFileGeneratorProvider: sourceFileGeneratorProvider);

            //Act
            IActionResult result = await service.SaveCalculationVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"Build project for specification {calculation.SpecificationId} could not be found, creating a new one"));

            sourceFileGenerator
                .Received()
                .GenerateCode(Arg.Any<BuildProject>(), Arg.Is<IEnumerable<Calculation>>(b =>
                b.First().Description == specCalculation1.Description &&
                b.First().CalculationSpecification.Id == specCalculation1.Id &&
                b.Skip(1).First().Description == specCalculation2.Description &&
                b.Skip(1).First().CalculationSpecification.Id == specCalculation2.Id));

            await calculationsRepository
                .Received(1)
                .UpdateCalculation(Arg.Is<Calculation>(c => c.Description == specCalculation1.Description));
        }

        [TestMethod]
        async public Task SaveCalculationVersion_GivenCalculationExistsWithBuildIdButCalculationCouldNotBeFound_AddsCalculationUpdatesBuildProject()
        {
            //Arrange
            string buildProjectId = Guid.NewGuid().ToString();

            BuildProject buildProject = new BuildProject
            {
                Id = buildProjectId,
            };

            Calculation calculation = CreateCalculation();
            calculation.BuildProjectId = buildProjectId;

            SaveSourceCodeVersion model = new SaveSourceCodeVersion
            {
                SourceCode = "source code"
            };
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectById(Arg.Is(buildProjectId))
                .Returns(buildProject);

            CalculationIndex calcIndex = new CalculationIndex();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .SearchById(Arg.Is(CalculationId))
                .Returns(calcIndex);

            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
            };

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(specificationSummary);

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
               buildProjectsRepository: buildProjectsRepository,
               searchRepository: searchRepository,
               specificationRepository: specificationRepository);

            //Act
            IActionResult result = await service.SaveCalculationVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"Build project for specification {calculation.SpecificationId} could not be found, creating a new one"));
        }

        [TestMethod]
        async public Task SaveCalculationVersion_GivenCalculationExistsWithBuildIdButButNotInSearch_CreatesSearchDocument()
        {
            //Arrange
            string buildProjectId = Guid.NewGuid().ToString();

            BuildProject buildProject = new BuildProject
            {
                Id = buildProjectId,
            };

            Calculation calculation = CreateCalculation();

            SaveSourceCodeVersion model = new SaveSourceCodeVersion
            {
                SourceCode = "source code"
            };
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectById(Arg.Is(buildProjectId))
                .Returns(buildProject);

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .SearchById(Arg.Is(CalculationId))
                .Returns((CalculationIndex)null);

            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
            };

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(specificationSummary);

            CalculationService service = CreateCalculationService(logger: logger,
                calculationsRepository: calculationsRepository,
               buildProjectsRepository: buildProjectsRepository,
               searchRepository: searchRepository,
               specificationRepository: specificationRepository);

            //Act
            IActionResult result = await service.SaveCalculationVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            await
                searchRepository
                    .Received(1)
                    .Index(Arg.Any<IList<CalculationIndex>>());
        }

        [TestMethod]
        async public Task SaveCalculationVersion_GivenCalculationIsCurrentlyPublished_SetsPublishStateToUpdated()
        {
            //Arrange
            string buildProjectId = Guid.NewGuid().ToString();

            BuildProject buildProject = new BuildProject
            {
                Id = buildProjectId,
            };

            Calculation calculation = CreateCalculation();
            calculation.Current.PublishStatus = PublishStatus.Approved;

            SaveSourceCodeVersion model = new SaveSourceCodeVersion
            {
                SourceCode = "source code"
            };
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectById(Arg.Is(buildProjectId))
                .Returns(buildProject);

            CalculationIndex calcIndex = new CalculationIndex();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .SearchById(Arg.Is(CalculationId))
                .Returns(calcIndex);

            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
            };

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(specificationSummary);

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
               buildProjectsRepository: buildProjectsRepository,
               searchRepository: searchRepository,
               specificationRepository: specificationRepository);

            //Act
            IActionResult result = await service.SaveCalculationVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            calculation
                .Current
                .PublishStatus
                .Should()
                .Be(PublishStatus.Updated);

            await
                searchRepository
                .Received(1)
                .Index(Arg.Is<IList<CalculationIndex>>(m => m.First().Status == "Updated"));
        }

        [TestMethod]
        async public Task SaveCalculationVersion_GivenCalculationIsCurrentlyUpdated_SetsPublishStateToUpdated()
        {
            //Arrange
            string buildProjectId = Guid.NewGuid().ToString();

            string specificationId = Guid.NewGuid().ToString();

            BuildProject buildProject = new BuildProject
            {
                Id = buildProjectId,
            };

            Calculation calculation = CreateCalculation();
            calculation.BuildProjectId = buildProjectId;
            calculation.SpecificationId = specificationId;

            calculation.Current.PublishStatus = PublishStatus.Updated;

            SaveSourceCodeVersion model = new SaveSourceCodeVersion
            {
                SourceCode = "source code"
            };
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectById(Arg.Is(buildProjectId))
                .Returns(buildProject);

            CalculationIndex calcIndex = new CalculationIndex();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .SearchById(Arg.Is(CalculationId))
                .Returns(calcIndex);

            IMessengerService messengerService = CreateMessengerService();

            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
            };

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(specificationSummary);

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
               buildProjectsRepository: buildProjectsRepository,
               searchRepository: searchRepository,
               messengerService: messengerService,
               specificationRepository: specificationRepository);

            //Act
            IActionResult result = await service.SaveCalculationVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            calculation
                .Current
                .PublishStatus
                .Should()
                .Be(PublishStatus.Updated);

            await
                searchRepository
                    .Received(1)
                    .Index(Arg.Is<IList<CalculationIndex>>(m => m.First().Status == "Updated"));

            await
                messengerService
                    .Received(1)
                    .SendToQueue(Arg.Is("calc-events-instruct-generate-allocations"),
                        Arg.Any<BuildProject>(),
                        Arg.Any<IDictionary<string, string>>());
        }

        [TestMethod]
        async public Task SaveCalculationVersion_GivenCalculationUpdateFails_ThenExceptionIsThrown()
        {
            //Arrange
            string buildProjectId = Guid.NewGuid().ToString();

            string specificationId = Guid.NewGuid().ToString();

            BuildProject buildProject = new BuildProject
            {
                Id = buildProjectId,
            };

            Calculation calculation = CreateCalculation();
            calculation.BuildProjectId = buildProjectId;
            calculation.SpecificationId = specificationId;

            calculation.Current.PublishStatus = PublishStatus.Updated;

            SaveSourceCodeVersion model = new SaveSourceCodeVersion
            {
                SourceCode = "source code"
            };
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.InternalServerError);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectById(Arg.Is(buildProjectId))
                .Returns(buildProject);

            CalculationIndex calcIndex = new CalculationIndex();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .SearchById(Arg.Is(CalculationId))
                .Returns(calcIndex);

            IMessengerService messengerService = CreateMessengerService();

            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
            };

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(specificationSummary);

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
               buildProjectsRepository: buildProjectsRepository,
               searchRepository: searchRepository,
               messengerService: messengerService,
               specificationRepository: specificationRepository);

            //Act
            Func<Task<IActionResult>> resultFunc = async () => await service.SaveCalculationVersion(request);

            //Assert
            resultFunc
                .ShouldThrow<InvalidOperationException>()
                .Which
                .Message
                .Should()
                .Be("Update calculation returned status code 'InternalServerError' instead of OK");
        }
    }
}
