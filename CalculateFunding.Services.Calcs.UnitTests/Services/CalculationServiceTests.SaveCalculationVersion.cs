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
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Models.Jobs;
using System.Security.Claims;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;

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

            CalculationVersion calculationVersion = calculation.Current as CalculationVersion;
            
            IVersionRepository<CalculationVersion> versionRepository = CreateCalculationVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<CalculationVersion>(), Arg.Any<CalculationVersion>())
                .Returns(calculationVersion);

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
                buildProjectsRepository: buildProjectsRepository,
                searchRepository: searchRepository,
                specificationRepository: specificationRepository,
                calculationVersionRepository: versionRepository);

            //Act
            IActionResult result = await service.SaveCalculationVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            await
              versionRepository
               .Received(1)
               .SaveVersion(Arg.Is(calculationVersion));
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

            CalculationVersion calculationVersion = new CalculationVersion
            {
                Date = DateTimeOffset.Now.ToLocalTime(),
                PublishStatus = PublishStatus.Draft,
                Version = 1
            };

            IVersionRepository<CalculationVersion> versionRepository = CreateCalculationVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<CalculationVersion>(), Arg.Any<CalculationVersion>())
                .Returns(calculationVersion);

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
                buildProjectsRepository: buildProjectsRepository,
                searchRepository: searchRepository,
                specificationRepository: specificationRepository,
                calculationVersionRepository: versionRepository);

            //Act
            IActionResult result = await service.SaveCalculationVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"Current for {CalculationId} was null and needed recreating."));

            await
              versionRepository
               .Received(1)
               .SaveVersion(Arg.Is(calculationVersion));
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

            CalculationVersion calculationVersion = calculation.Current.Clone() as CalculationVersion;

            IVersionRepository<CalculationVersion> versionRepository = CreateCalculationVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<CalculationVersion>(), Arg.Any<CalculationVersion>())
                .Returns(calculationVersion);

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
                buildProjectsRepository: buildProjectsRepository,
                searchRepository: searchRepository,
                specificationRepository: specificationRepository,
                calculationVersionRepository: versionRepository);

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

            await
              versionRepository
               .Received(1)
               .SaveVersion(Arg.Is(calculationVersion));
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

            CalculationVersion calculationVersion = calculation.Current.Clone() as CalculationVersion;

            IVersionRepository<CalculationVersion> versionRepository = CreateCalculationVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<CalculationVersion>(), Arg.Any<CalculationVersion>())
                .Returns(calculationVersion);

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
                buildProjectsRepository: buildProjectsRepository,
                searchRepository: searchRepository,
                specificationRepository: specificationRepository,
                calculationVersionRepository: versionRepository);

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

            await
              versionRepository
               .Received(1)
               .SaveVersion(Arg.Is(calculationVersion));
        }

        [TestMethod]
        async public Task SaveCalculationVersion_GivenCalculationExistsWithBuildIdButNoCalculations_EnsuresCalculationsCreatedUpdatesBuildProject()
        {
            //Arrange
            string buildProjectId = Guid.NewGuid().ToString();

            BuildProject buildProject = null;

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
                .GetBuildProjectBySpecificationId(Arg.Is(calculation.SpecificationId))
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

            CalculationVersion calculationVersion = calculation.Current.Clone() as CalculationVersion;

            IVersionRepository<CalculationVersion> versionRepository = CreateCalculationVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<CalculationVersion>(), Arg.Any<CalculationVersion>())
                .Returns(calculationVersion);

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
                buildProjectsRepository: buildProjectsRepository,
                searchRepository: searchRepository,
                specificationRepository: specificationRepository,
                calculationVersionRepository: versionRepository);

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
              versionRepository
               .Received(1)
               .SaveVersion(Arg.Is(calculationVersion));
        }

        [TestMethod]
        async public Task SaveCalculationVersion_GivenCalculationExistsWithBuildIdButNoCalculations_EnsuresCalculationSpecificationDescriptionSetWithSingleCalculation()
        {
            //Arrange
            string buildProjectId = Guid.NewGuid().ToString();

            BuildProject buildProject = null;

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
                .GetBuildProjectBySpecificationId(Arg.Is(specificationId))
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

            CalculationVersion calculationVersion = calculation.Current.Clone() as CalculationVersion;

            IVersionRepository<CalculationVersion> versionRepository = CreateCalculationVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<CalculationVersion>(), Arg.Any<CalculationVersion>())
                .Returns(calculationVersion);

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
                specificationRepository: specificationRepository,
                buildProjectsRepository: buildProjectsRepository,
                searchRepository: searchRepository,
                sourceFileGeneratorProvider: sourceFileGeneratorProvider,
                calculationVersionRepository: versionRepository);

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

            await
              versionRepository
               .Received(1)
               .SaveVersion(Arg.Is(calculationVersion));
        }

        [TestMethod]
        async public Task SaveCalculationVersion_GivenCalculationExistsWithBuildIdButNoCalculations_EnsuresCalculationSpecificationDescriptionSetWithMultipleCalculations()
        {
            //Arrange
            string buildProjectId = Guid.NewGuid().ToString();

            BuildProject buildProject = null;

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
                .GetBuildProjectBySpecificationId(Arg.Is(specificationId))
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

            CalculationVersion calculationVersion = calculation.Current.Clone() as CalculationVersion;

            IVersionRepository<CalculationVersion> versionRepository = CreateCalculationVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<CalculationVersion>(), Arg.Any<CalculationVersion>())
                .Returns(calculationVersion);

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
                specificationRepository: specificationRepository,
                buildProjectsRepository: buildProjectsRepository,
                searchRepository: searchRepository,
                sourceFileGeneratorProvider: sourceFileGeneratorProvider,
                calculationVersionRepository: versionRepository);

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

            await
              versionRepository
               .Received(1)
               .SaveVersion(Arg.Is(calculationVersion));
        }

        [TestMethod]
        async public Task SaveCalculationVersion_GivenCalculationExistsWithBuildIdButCalculationCouldNotBeFound_AddsCalculationUpdatesBuildProject()
        {
            //Arrange
            string buildProjectId = Guid.NewGuid().ToString();

            BuildProject buildProject = null;

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
                .GetBuildProjectBySpecificationId(Arg.Is(calculation.SpecificationId))
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

            CalculationVersion calculationVersion = calculation.Current.Clone() as CalculationVersion;

            IVersionRepository<CalculationVersion> versionRepository = CreateCalculationVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<CalculationVersion>(), Arg.Any<CalculationVersion>())
                .Returns(calculationVersion);

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
               buildProjectsRepository: buildProjectsRepository,
               searchRepository: searchRepository,
               specificationRepository: specificationRepository,
               calculationVersionRepository: versionRepository);

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
              versionRepository
               .Received(1)
               .SaveVersion(Arg.Is(calculationVersion));
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

            CalculationVersion calculationVersion = calculation.Current.Clone() as CalculationVersion;

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
                .GetBuildProjectBySpecificationId(Arg.Is(calculation.SpecificationId))
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

            IVersionRepository<CalculationVersion> versionRepository = CreateCalculationVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<CalculationVersion>(), Arg.Any<CalculationVersion>())
                .Returns(calculationVersion);

            CalculationService service = CreateCalculationService(logger: logger,
                calculationsRepository: calculationsRepository,
               buildProjectsRepository: buildProjectsRepository,
               searchRepository: searchRepository,
               specificationRepository: specificationRepository,
               calculationVersionRepository: versionRepository);

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

            await
              versionRepository
               .Received(1)
               .SaveVersion(Arg.Is(calculationVersion));
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
                .GetBuildProjectBySpecificationId(Arg.Is(calculation.SpecificationId))
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

            CalculationVersion calculationVersion = calculation.Current as CalculationVersion;
            calculationVersion.PublishStatus = PublishStatus.Updated;

            IVersionRepository<CalculationVersion> versionRepository = CreateCalculationVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<CalculationVersion>(), Arg.Any<CalculationVersion>())
                .Returns(calculationVersion);

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
               buildProjectsRepository: buildProjectsRepository,
               searchRepository: searchRepository,
               specificationRepository: specificationRepository,
               calculationVersionRepository: versionRepository);

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
              versionRepository
               .Received(1)
               .SaveVersion(Arg.Is(calculationVersion));
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
                .GetBuildProjectBySpecificationId(Arg.Is(specificationId))
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

            CalculationVersion calculationVersion = calculation.Current as CalculationVersion;
            calculationVersion.PublishStatus = PublishStatus.Updated;

            IVersionRepository<CalculationVersion> versionRepository = CreateCalculationVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<CalculationVersion>(), Arg.Any<CalculationVersion>())
                .Returns(calculationVersion);

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
               buildProjectsRepository: buildProjectsRepository,
               searchRepository: searchRepository,
               messengerService: messengerService,
               specificationRepository: specificationRepository,
               calculationVersionRepository: versionRepository);

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
                        Arg.Is((string)null),
                        Arg.Any<IDictionary<string, string>>());

            await
              versionRepository
               .Received(1)
               .SaveVersion(Arg.Is(calculationVersion));
        }

        [TestMethod]
        async public Task SaveCalculationVersion_GivenFeatureToggleIsOn_SetsPublishStateToUpdatedAddNewJob()
        {
            //Arrange
            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
                .IsJobServiceEnabled()
                .Returns(true);

            string buildProjectId = Guid.NewGuid().ToString();

            string specificationId = Guid.NewGuid().ToString();

            BuildProject buildProject = new BuildProject
            {
                Id = buildProjectId,
                SpecificationId = specificationId
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

            ClaimsPrincipal principle = new ClaimsPrincipal(new[]
            {
                new ClaimsIdentity(new []{ new Claim(ClaimTypes.Sid, UserId), new Claim(ClaimTypes.Name, Username) })
            });

            HttpContext context = Substitute.For<HttpContext>();
            context
                .User
                .Returns(principle);

            request
                .HttpContext
                .Returns(context);

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
                .GetBuildProjectBySpecificationId(Arg.Is(specificationId))
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

            CalculationVersion calculationVersion = calculation.Current as CalculationVersion;
            calculationVersion.PublishStatus = PublishStatus.Updated;

            IVersionRepository<CalculationVersion> versionRepository = CreateCalculationVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<CalculationVersion>(), Arg.Any<CalculationVersion>())
                .Returns(calculationVersion);

            IJobsRepository jobsRepository = CreateJobsRepository();
            jobsRepository
                .CreateJob(Arg.Any<JobCreateModel>())
                .Returns(new Job { Id = "job-id-1" });

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
               buildProjectsRepository: buildProjectsRepository,
               searchRepository: searchRepository,
               messengerService: messengerService,
               specificationRepository: specificationRepository,
               calculationVersionRepository: versionRepository,
               featureToggle: featureToggle,
               jobsRepository: jobsRepository);

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
                    .DidNotReceive()
                    .SendToQueue(Arg.Is("calc-events-instruct-generate-allocations"),
                        Arg.Any<string>(),
                        Arg.Any<IDictionary<string, string>>());
            await
                jobsRepository
                    .Received(1)
                    .CreateJob(Arg.Is<JobCreateModel>(
                        m => 
                            m.InvokerUserDisplayName == Username &&
                            m.InvokerUserId == UserId &&
                            m.JobDefinitionId == JobConstants.DefinitionNames.CreateInstructAllocationJob &&
                            m.Properties["specification-id"] == specificationId &&
                            m.Trigger.EntityId == CalculationId &&
                            m.Trigger.EntityType == nameof(Calculation) &&
                            m.Trigger.Message == $"Saving calculation: '{CalculationId}' for specification: '{calculation.SpecificationId}'"
                        ));

            logger
               .Received(1)
               .Information(Arg.Is($"New job of type '{JobConstants.DefinitionNames.CreateInstructAllocationJob}' created with id: 'job-id-1'"));
        }

        [TestMethod]
        async public Task SaveCalculationVersion_GivenFeatureToggleIsOnAndUserIsNull_SetsPublishStateToUpdatedAddNewJobWithEmptyUser()
        {
            //Arrange
            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
                .IsJobServiceEnabled()
                .Returns(true);

            string buildProjectId = Guid.NewGuid().ToString();

            string specificationId = Guid.NewGuid().ToString();

            BuildProject buildProject = new BuildProject
            {
                Id = buildProjectId,
                SpecificationId = specificationId
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

            ClaimsPrincipal principle = new ClaimsPrincipal(new[]
            {
                new ClaimsIdentity(new []{ new Claim(ClaimTypes.Sid, UserId), new Claim(ClaimTypes.Name, Username) })
            });

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
                .GetBuildProjectBySpecificationId(Arg.Is(specificationId))
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

            CalculationVersion calculationVersion = calculation.Current as CalculationVersion;
            calculationVersion.PublishStatus = PublishStatus.Updated;

            IVersionRepository<CalculationVersion> versionRepository = CreateCalculationVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<CalculationVersion>(), Arg.Any<CalculationVersion>())
                .Returns(calculationVersion);

            IJobsRepository jobsRepository = CreateJobsRepository();
            jobsRepository
                .CreateJob(Arg.Any<JobCreateModel>())
                .Returns(new Job { Id = "job-id-1" });

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
               buildProjectsRepository: buildProjectsRepository,
               searchRepository: searchRepository,
               messengerService: messengerService,
               specificationRepository: specificationRepository,
               calculationVersionRepository: versionRepository,
               featureToggle: featureToggle,
               jobsRepository: jobsRepository);

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
                    .DidNotReceive()
                    .SendToQueue(Arg.Is("calc-events-instruct-generate-allocations"),
                        Arg.Any<string>(),
                        Arg.Any<IDictionary<string, string>>());
            await
                jobsRepository
                    .Received(1)
                    .CreateJob(Arg.Is<JobCreateModel>(
                        m =>
                            m.InvokerUserDisplayName == "" &&
                            m.InvokerUserId == "" &&
                            m.JobDefinitionId == JobConstants.DefinitionNames.CreateInstructAllocationJob &&
                            m.Properties["specification-id"] == specificationId &&
                            m.Trigger.EntityId == CalculationId &&
                            m.Trigger.EntityType == nameof(Calculation) &&
                            m.Trigger.Message == $"Saving calculation: '{CalculationId}' for specification: '{calculation.SpecificationId}'"
                        ));

            logger
               .Received(1)
               .Information(Arg.Is($"New job of type '{JobConstants.DefinitionNames.CreateInstructAllocationJob}' created with id: 'job-id-1'"));
        }

        [TestMethod]
        async public Task SaveCalculationVersion_GivenFeatureToggleIsOnButCreatingJobReturnsNull_LogsErrorReturnsInternalServerError()
        {
            //Arrange
            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
                .IsJobServiceEnabled()
                .Returns(true);

            string buildProjectId = Guid.NewGuid().ToString();

            string specificationId = Guid.NewGuid().ToString();

            BuildProject buildProject = new BuildProject
            {
                Id = buildProjectId,
                SpecificationId = specificationId
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

            ClaimsPrincipal principle = new ClaimsPrincipal(new[]
            {
                new ClaimsIdentity(new []{ new Claim(ClaimTypes.Sid, UserId), new Claim(ClaimTypes.Name, Username) })
            });

            HttpContext context = Substitute.For<HttpContext>();
            context
                .User
                .Returns(principle);

            request
                .HttpContext
                .Returns(context);

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
                .GetBuildProjectBySpecificationId(Arg.Is(specificationId))
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

            CalculationVersion calculationVersion = calculation.Current as CalculationVersion;
            calculationVersion.PublishStatus = PublishStatus.Updated;

            IVersionRepository<CalculationVersion> versionRepository = CreateCalculationVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<CalculationVersion>(), Arg.Any<CalculationVersion>())
                .Returns(calculationVersion);

            IJobsRepository jobsRepository = CreateJobsRepository();
            jobsRepository
                .CreateJob(Arg.Any<JobCreateModel>())
                .Returns((Job)null);

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
               buildProjectsRepository: buildProjectsRepository,
               searchRepository: searchRepository,
               messengerService: messengerService,
               specificationRepository: specificationRepository,
               calculationVersionRepository: versionRepository,
               featureToggle: featureToggle,
               jobsRepository: jobsRepository);

            //Act
            IActionResult result = await service.SaveCalculationVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be($"Failed to create job of type '{JobConstants.DefinitionNames.CreateInstructAllocationJob}' on specification '{specificationId}'");

            await
                messengerService
                    .DidNotReceive()
                    .SendToQueue(Arg.Is("calc-events-instruct-generate-allocations"),
                        Arg.Any<string>(),
                        Arg.Any<IDictionary<string, string>>());
            await
                jobsRepository
                    .Received(1)
                    .CreateJob(Arg.Is<JobCreateModel>(
                        m =>
                            m.InvokerUserDisplayName == Username &&
                            m.InvokerUserId == UserId &&
                            m.JobDefinitionId == JobConstants.DefinitionNames.CreateInstructAllocationJob &&
                            m.Properties["specification-id"] == specificationId &&
                            m.Trigger.EntityId == CalculationId &&
                            m.Trigger.EntityType == nameof(Calculation) &&
                            m.Trigger.Message == $"Saving calculation: '{CalculationId}' for specification: '{calculation.SpecificationId}'"
                        ));

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Is($"Failed to create job of type '{JobConstants.DefinitionNames.CreateInstructAllocationJob}' on specification '{specificationId}'"));
        }

        [TestMethod]
        public void SaveCalculationVersion_GivenCalculationUpdateFails_ThenExceptionIsThrown()
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
                .GetBuildProjectBySpecificationId(Arg.Is(specificationId))
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
                .Should().Throw<InvalidOperationException>()
                .Which
                .Message
                .Should()
                .Be("Update calculation returned status code 'InternalServerError' instead of OK");
        }

        [TestMethod]
        async public Task SaveCalculationVersion_GivenFeatureToggleIsOnAndCalcsContainCalculationAggregates_AddsNewJobToAggregateCalculations()
        {
            //Arrange
            IEnumerable<Calculation> calculations = new[]
            {
                new Calculation
                {
                    Current = new CalculationVersion
                    {
                        SourceCode = "return Sum(Calc1)"
                    }
                }
            };

            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
                .IsJobServiceEnabled()
                .Returns(true);

            string buildProjectId = Guid.NewGuid().ToString();

            string specificationId = Guid.NewGuid().ToString();

            BuildProject buildProject = new BuildProject
            {
                Id = buildProjectId,
                SpecificationId = specificationId
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

            ClaimsPrincipal principle = new ClaimsPrincipal(new[]
            {
                new ClaimsIdentity(new []{ new Claim(ClaimTypes.Sid, UserId), new Claim(ClaimTypes.Name, Username) })
            });

            HttpContext context = Substitute.For<HttpContext>();
            context
                .User
                .Returns(principle);

            request
                .HttpContext
                .Returns(context);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns(calculations);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(specificationId))
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

            CalculationVersion calculationVersion = calculation.Current as CalculationVersion;
            calculationVersion.PublishStatus = PublishStatus.Updated;

            IVersionRepository<CalculationVersion> versionRepository = CreateCalculationVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<CalculationVersion>(), Arg.Any<CalculationVersion>())
                .Returns(calculationVersion);

            IJobsRepository jobsRepository = CreateJobsRepository();
            jobsRepository
                .CreateJob(Arg.Any<JobCreateModel>())
                .Returns(new Job { Id = "job-id-1" });

           
            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
               buildProjectsRepository: buildProjectsRepository,
               searchRepository: searchRepository,
               messengerService: messengerService,
               specificationRepository: specificationRepository,
               calculationVersionRepository: versionRepository,
               featureToggle: featureToggle,
               jobsRepository: jobsRepository);

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
                    .DidNotReceive()
                    .SendToQueue(Arg.Is("calc-events-instruct-generate-allocations"),
                        Arg.Any<string>(),
                        Arg.Any<IDictionary<string, string>>());
            await
                jobsRepository
                    .Received(1)
                    .CreateJob(Arg.Is<JobCreateModel>(
                        m =>
                            m.InvokerUserDisplayName == Username &&
                            m.InvokerUserId == UserId &&
                            m.JobDefinitionId == JobConstants.DefinitionNames.CreateInstructGenerateAggregationsAllocationJob &&
                            m.Properties["specification-id"] == specificationId &&
                            m.Trigger.EntityId == CalculationId &&
                            m.Trigger.EntityType == nameof(Calculation) &&
                            m.Trigger.Message == $"Saving calculation: '{CalculationId}' for specification: '{calculation.SpecificationId}'"
                        ));

            logger
               .Received(1)
               .Information(Arg.Is($"New job of type '{JobConstants.DefinitionNames.CreateInstructAllocationJob}' created with id: 'job-id-1'"));
        }
    }
}
