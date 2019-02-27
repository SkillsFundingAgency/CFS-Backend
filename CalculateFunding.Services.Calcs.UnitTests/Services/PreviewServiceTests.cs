using Microsoft.VisualStudio.TestTools.UnitTesting;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Calcs.Interfaces.CodeGen;
using CalculateFunding.Services.Compiler.Interfaces;
using FluentValidation;
using Serilog;
using NSubstitute;
using FluentValidation.Results;
using CalculateFunding.Services.CodeGeneration;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using FluentAssertions;
using Newtonsoft.Json;
using System.Text;
using System.IO;
using System.Collections.Generic;
using CalculateFunding.Services.Compiler;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Common.FeatureToggles;
using System.Linq;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Common.Caching;
using CalculateFunding.Services.Core.Caching;

namespace CalculateFunding.Services.Calcs.Services
{
    [TestClass]
    public class PreviewServiceTests
    {
        const string SpecificationId = "b13cd3ba-bdf8-40a8-9ec4-af48eb8a4386";
        const string CalculationId = "2d30bb44-0862-4524-a2f6-381c5534027a";
        const string BuildProjectId = "4d30bb44-0862-4524-a2f6-381c553402a7";
        const string SourceCode = "Dim i as int = 1";

        [TestMethod]
        public async Task Compile_GivenNullPreviewRequest_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            PreviewService service = CreateService(logger: logger);

            //Act
            IActionResult result = await service.Compile(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("A null preview request was supplied"));
        }

        [TestMethod]
        public async Task Compile_GivenPreviewRequestDoesNotValidate_ReturnsBadRequest()
        {
            //Arrange
            PreviewRequest model = new PreviewRequest();
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ValidationResult validationResult = new ValidationResult(new[]{
                    new ValidationFailure("prop1", "oh no an error!!!")
                });

            IValidator<PreviewRequest> validator = CreatePreviewRequestValidator(validationResult);

            ILogger logger = CreateLogger();

            PreviewService service = CreateService(logger: logger, previewRequestValidator: validator);

            //Act
            IActionResult result = await service.Compile(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Warning(Arg.Is("The preview request failed to validate with errors: oh no an error!!!"));
        }

        [TestMethod]
        public async Task Compile_GivenPreviewRequestButCalculationDoesNotExists_ReturnsPreConditionFailed()
        {
            //Arrange
            PreviewRequest model = new PreviewRequest
            {
                CalculationId = CalculationId,
                SpecificationId = SpecificationId,
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            IValidator<PreviewRequest> validator = CreatePreviewRequestValidator();

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns((Calculation)null);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();

            BuildProject buildProject = new BuildProject();

            buildProjectsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(SpecificationId))
                .Returns(buildProject);

            PreviewService service = CreateService(logger: logger, previewRequestValidator: validator, calculationsRepository: calculationsRepository, buildProjectsRepository: buildProjectsRepository);

            //Act
            IActionResult result = await service.Compile(request);

            //Assert
            result
                .Should()
                .BeOfType<PreconditionFailedResult>()
                .Which
                .Value
                .Should()
                .Be($"Calculation ('{CalculationId}') could not be found for specification Id '{SpecificationId}'");

            logger
                .Received(1)
                .Warning(Arg.Is($"Calculation ('{CalculationId}') could not be found for specification Id '{SpecificationId}'"));
        }

        [TestMethod]
        public async Task Compile_GivenPreviewRequestButSpecificationDoesNotIncludeABuildProjectId_ReturnsPreConditionFailed()
        {
            //Arrange
            Calculation calculation = new Calculation
            {
                Id = CalculationId,
                Current = new CalculationVersion(),
                SpecificationId = "123",
            };

            PreviewRequest model = new PreviewRequest
            {
                CalculationId = CalculationId,
                SpecificationId = SpecificationId,
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            IValidator<PreviewRequest> validator = CreatePreviewRequestValidator();

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            BuildProject buildProject = null;

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(calculation.SpecificationId))
                .Returns(buildProject);

            PreviewService service = CreateService(logger: logger, previewRequestValidator: validator, calculationsRepository: calculationsRepository, buildProjectsRepository: buildProjectsRepository);

            //Act
            IActionResult result = await service.Compile(request);

            //Assert
            result
                .Should()
                .BeOfType<PreconditionFailedResult>()
                .Which
                .Value
                .Should()
                .Be($"Build project for specification '{SpecificationId}' could not be found");


            logger
                .Received(1)
                .Warning(Arg.Is($"Build project for specification '{SpecificationId}' could not be found"));
        }



        [TestMethod]
        public async Task Compile_GivenPreviewRequestButCalculationHasAnEmptyStringOrNullForSpecificationId_ReturnsPreConditionFailed()
        {
            //Arrange
            Calculation calculation = new Calculation
            {
                Id = CalculationId,
                Current = new CalculationVersion(),
                SpecificationId = null,
            };

            PreviewRequest model = new PreviewRequest
            {
                CalculationId = CalculationId
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ValidationResult validationResult = new ValidationResult();
            validationResult.Errors.Add(new ValidationFailure("specificationId", "Specification ID is null"));

            IValidator<PreviewRequest> validator = CreatePreviewRequestValidator(validationResult);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            PreviewService service = CreateService(logger: logger, previewRequestValidator: validator, calculationsRepository: calculationsRepository);

            //Act
            IActionResult result = await service.Compile(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("The preview request failed to validate");

            logger
                .Received(1)
                .Warning(Arg.Is($"The preview request failed to validate with errors: Specification ID is null"));
        }

        [TestMethod]
        public async Task Compile_GivenPreviewRequestButBuildProjectDoesNotExists_ReturnsPreConditionFailed()
        {
            //Arrange
            PreviewRequest model = new PreviewRequest
            {
                CalculationId = CalculationId,
                SpecificationId = SpecificationId,
            };

            Calculation calculation = new Calculation
            {
                Id = CalculationId,
                BuildProjectId = BuildProjectId,
                Current = new CalculationVersion(),
                SpecificationId = SpecificationId,
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            IValidator<PreviewRequest> validator = CreatePreviewRequestValidator();

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(calculation.SpecificationId))
                .Returns((BuildProject)null);

            PreviewService service = CreateService(logger: logger, previewRequestValidator: validator, calculationsRepository: calculationsRepository, buildProjectsRepository: buildProjectsRepository);

            //Act
            IActionResult result = await service.Compile(request);

            //Assert
            result
                .Should()
                .BeOfType<PreconditionFailedResult>()
                .Which
                .Value
                .Should()
                .Be($"Build project for specification '{SpecificationId}' could not be found");

            logger
                .Received(1)
                .Warning(Arg.Is($"Build project for specification '{SpecificationId}' could not be found"));
        }

        [TestMethod]
        public async Task Compile_GivenBuildProjectWasFoundButCalculationsNotFound_ReturnsPreConditionFailed()
        {
            //Arrange
            PreviewRequest model = new PreviewRequest
            {
                CalculationId = CalculationId,
                SpecificationId = SpecificationId,
            };

            Calculation calculation = new Calculation
            {
                Id = CalculationId,
                BuildProjectId = BuildProjectId,
                Current = new CalculationVersion(),
                SpecificationId = SpecificationId,
            };
            BuildProject buildProject = new BuildProject();

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            IValidator<PreviewRequest> validator = CreatePreviewRequestValidator();

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(calculation.SpecificationId))
                .Returns(buildProject);

            PreviewService service = CreateService(logger: logger, previewRequestValidator: validator, calculationsRepository: calculationsRepository, buildProjectsRepository: buildProjectsRepository);

            // Act
            IActionResult result = await service.Compile(request);

            // Assert
            result
                .Should()
                .BeOfType<PreconditionFailedResult>()
                .Which
                .Value
                .Should()
                .Be("Calculation ('2d30bb44-0862-4524-a2f6-381c5534027a') could not be found for specification Id 'b13cd3ba-bdf8-40a8-9ec4-af48eb8a4386'");

            logger
                .Received(1)
                .Warning(Arg.Is($"Calculation ('2d30bb44-0862-4524-a2f6-381c5534027a') could not be found for specification Id 'b13cd3ba-bdf8-40a8-9ec4-af48eb8a4386'"));
        }

        [TestMethod]
        public async Task Compile_GivenSourceFileGeneratorCreatedFilesButCodeDidNotSucceed_ReturnsOK()
        {
            //Arrange
            PreviewRequest model = new PreviewRequest
            {
                CalculationId = CalculationId,
                SourceCode = SourceCode,
                SpecificationId = SpecificationId,
            };

            Calculation calculation = new Calculation
            {
                Id = CalculationId,
                BuildProjectId = BuildProjectId,
                Current = new CalculationVersion(),
                SpecificationId = SpecificationId,
            };

            IEnumerable<Calculation> calculations = new List<Calculation>() { calculation };

            BuildProject buildProject = new BuildProject
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

            IValidator<PreviewRequest> validator = CreatePreviewRequestValidator();

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                           .GetCalculationsBySpecificationId(Arg.Is(SpecificationId))
                           .Returns(calculations);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(SpecificationId))
                .Returns(buildProject);

            List<SourceFile> sourceFiles = new List<SourceFile>
            {
                new SourceFile { FileName = "test.vb", SourceCode = "any content"}
            };

            Build build = new Build
            {
                Success = false,
                SourceFiles = sourceFiles
            };

            Dictionary<string, string> sourceCodes = new Dictionary<string, string>()
            {
                { "TestFunction", model.SourceCode },
                { "Calc1", "return 1" }
            };

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Calculation>>())
                .Returns(build);

            sourceCodeService
                .GetCalulationFunctions(Arg.Any<IEnumerable<SourceFile>>())
                .Returns(sourceCodes);

            PreviewService service = CreateService(logger: logger, previewRequestValidator: validator, calculationsRepository: calculationsRepository,
                buildProjectsRepository: buildProjectsRepository, sourceCodeService: sourceCodeService);

            //Act
            IActionResult result = await service.Compile(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okResult = (OkObjectResult)result;
            okResult
                .Value
                .Should()
                .BeOfType<PreviewResponse>();

            PreviewResponse previewResponse = (PreviewResponse)okResult.Value;
            previewResponse
                .Calculation
                .Current
                .SourceCode
                .Should()
                .Be(SourceCode);

            previewResponse
                .CompilerOutput
                .Success
                .Should()
                .BeFalse();

            logger
                .Received(1)
                .Information(Arg.Is($"Build did not compile succesfully for calculation id {calculation.Id}"));
        }

        [TestMethod]
        public async Task Compile_GivenSourceFileGeneratorCreatedFilesAndCodeDidSucceed_ReturnsOK()
        {
            //Arrange
            PreviewRequest model = new PreviewRequest
            {
                CalculationId = CalculationId,
                SourceCode = SourceCode,
                SpecificationId = SpecificationId
            };

            Calculation calculation = new Calculation
            {
                Id = CalculationId,
                BuildProjectId = BuildProjectId,
                Current = new CalculationVersion(),
                SpecificationId = SpecificationId
            };

            IEnumerable<Calculation> calculations = new List<Calculation>() { calculation };

            BuildProject buildProject = new BuildProject
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

            IValidator<PreviewRequest> validator = CreatePreviewRequestValidator();

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(SpecificationId))
                .Returns(calculations);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(calculation.SpecificationId))
                .Returns(buildProject);

            List<SourceFile> sourceFiles = new List<SourceFile>
            {
                new SourceFile { FileName = "test.vb", SourceCode = "any content"}
            };

            Build build = new Build
            {
                Success = true,
                SourceFiles = sourceFiles
            };

            Dictionary<string, string> sourceCodes = new Dictionary<string, string>()
            {
                { "TestFunction", model.SourceCode },
                { "Calc1", "return 1" }
            };

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Calculation>>())
                .Returns(build);

            sourceCodeService
                .GetCalulationFunctions(Arg.Any<IEnumerable<SourceFile>>())
                .Returns(sourceCodes); ;

            PreviewService service = CreateService(logger: logger, previewRequestValidator: validator, calculationsRepository: calculationsRepository,
                buildProjectsRepository: buildProjectsRepository, sourceCodeService: sourceCodeService);

            //Act
            IActionResult result = await service.Compile(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okResult = (OkObjectResult)result;
            okResult
                .Value
                .Should()
                .BeOfType<PreviewResponse>();

            PreviewResponse previewResponse = (PreviewResponse)okResult.Value;
            previewResponse
                .Calculation
                .Current
                .SourceCode
                .Should()
                .Be(SourceCode);

            previewResponse
                .CompilerOutput
                .Success
                .Should()
                .BeTrue();

            logger
                .Received(1)
                .Information(Arg.Is($"Build compiled succesfully for calculation id {calculation.Id}"));
        }

        [TestMethod]
        public async Task Compile_GivenStringCompareInCode_CompilesCodeAndReturnsOk()
        {
            //Arrange
            string stringCompareCode = "Public Class TestClass\nPublic Property E1 As ExampleClass\nPublic Function TestFunction As String\nIf E1.ProviderType = \"goodbye\" Then\nReturn \"worked\"\nElse Return \"no\"\nEnd If\nEnd Function\nEnd Class";

            PreviewRequest model = new PreviewRequest
            {
                CalculationId = CalculationId,
                SourceCode = stringCompareCode,
                SpecificationId = SpecificationId
            };

            Calculation calculation = new Calculation
            {
                Id = CalculationId,
                BuildProjectId = BuildProjectId,
                Current = new CalculationVersion(),
                SpecificationId = SpecificationId
            };

            IEnumerable<Calculation> calculations = new List<Calculation>() { calculation };

            BuildProject buildProject = new BuildProject
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

            IValidator<PreviewRequest> validator = CreatePreviewRequestValidator();

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(SpecificationId))
                .Returns(calculations);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(calculation.SpecificationId))
                .Returns(buildProject);

            List<SourceFile> sourceFiles = new List<SourceFile>
            {
                new SourceFile { FileName = "project.vbproj", SourceCode = "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>netcoreapp2.0</TargetFramework></PropertyGroup></Project>" },
                new SourceFile { FileName = "ExampleClass.vb", SourceCode = "Public Class ExampleClass\nPublic Property ProviderType() As String\nEnd Class" },
                new SourceFile { FileName = "Calculation.vb", SourceCode = model.SourceCode }
            };

            Build build = new Build
            {
                Success = true,
                SourceFiles = sourceFiles
            };

            Dictionary<string, string> sourceCodes = new Dictionary<string, string>()
            {
                { "TestFunction", model.SourceCode },
                { "Calc1", "return 1" }
            };

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Calculation>>())
                .Returns(build);

            sourceCodeService
                .GetCalulationFunctions(Arg.Any<IEnumerable<SourceFile>>())
                .Returns(sourceCodes);

            PreviewService service = CreateService(logger: logger, previewRequestValidator: validator, calculationsRepository: calculationsRepository,
                buildProjectsRepository: buildProjectsRepository, sourceCodeService: sourceCodeService);

            //Act
            IActionResult result = await service.Compile(request);

            //Assert
            OkObjectResult okResult = result.Should().BeOfType<OkObjectResult>().Subject;

            PreviewResponse previewResponse = okResult.Value.Should().BeOfType<PreviewResponse>().Subject;

            previewResponse
                .Calculation
                .Current
                .SourceCode
                .Should()
                .Be(stringCompareCode);

            previewResponse
                .CompilerOutput
                .Success
                .Should()
                .BeTrue();

            logger
                .Received(1)
                .Information(Arg.Is($"Build compiled succesfully for calculation id {calculation.Id}"));
        }

        [TestMethod]
        public async Task Compile_GivenStringCompareInCodeAndAggregatesIsEnabledAndNoAggregateFunctionsUsed_CompilesCodeAndReturnsOk()
        {
            //Arrange
            string stringCompareCode = "Public Class TestClass\nPublic Property E1 As ExampleClass\nPublic Function TestFunction As String\nIf E1.ProviderType = \"goodbye\" Then\nReturn \"worked\"\nElse Return \"no\"\nEnd If\nEnd Function\nEnd Class";

            PreviewRequest model = new PreviewRequest
            {
                CalculationId = CalculationId,
                SourceCode = stringCompareCode,
                SpecificationId = SpecificationId
            };

            Calculation calculation = new Calculation
            {
                Id = CalculationId,
                BuildProjectId = BuildProjectId,
                Current = new CalculationVersion(),
                SpecificationId = SpecificationId,
                Name = "TestFunction"
            };

            IEnumerable<Calculation> calculations = new List<Calculation>() { calculation };

            BuildProject buildProject = new BuildProject
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

            IValidator<PreviewRequest> validator = CreatePreviewRequestValidator();

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(SpecificationId))
                .Returns(calculations);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(calculation.SpecificationId))
                .Returns(buildProject);

            List<SourceFile> sourceFiles = new List<SourceFile>
            {
                new SourceFile { FileName = "project.vbproj", SourceCode = "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>netcoreapp2.0</TargetFramework></PropertyGroup></Project>" },
                new SourceFile { FileName = "ExampleClass.vb", SourceCode = "Public Class ExampleClass\nPublic Property ProviderType() As String\nEnd Class" },
                new SourceFile { FileName = "Calculation.vb", SourceCode = model.SourceCode }
            };

            Dictionary<string, string> sourceCodes = new Dictionary<string, string>()
            {
                { "TestFunction", model.SourceCode }
            };

            Build compilerOutput = new Build
            {
                Success = true,
                SourceFiles = sourceFiles
            };

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Calculation>>())
                .Returns(compilerOutput);

            sourceCodeService
                .GetCalulationFunctions(Arg.Any<IEnumerable<SourceFile>>())
                .Returns(sourceCodes);

            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
                .IsAggregateSupportInCalculationsEnabled()
                .Returns(true);

            featureToggle
                .IsAggregateOverCalculationsEnabled()
                .Returns(true);

            IDatasetRepository datasetRepository = CreateDatasetRepository();

            PreviewService service = CreateService(logger: logger, previewRequestValidator: validator, calculationsRepository: calculationsRepository,
                buildProjectsRepository: buildProjectsRepository, 
                datasetRepository: datasetRepository, featureToggle: featureToggle, sourceCodeService: sourceCodeService);

            //Act
            IActionResult result = await service.Compile(request);

            //Assert
            OkObjectResult okResult = result.Should().BeOfType<OkObjectResult>().Subject;

            PreviewResponse previewResponse = okResult.Value.Should().BeOfType<PreviewResponse>().Subject;

            previewResponse
                .Calculation
                .Current
                .SourceCode
                .Should()
                .Be(stringCompareCode);

            previewResponse
                .CompilerOutput
                .Success
                .Should()
                .BeTrue();

            logger
                .Received(1)
                .Information(Arg.Is($"Build compiled succesfully for calculation id {calculation.Id}"));

            await
                sourceCodeService
                    .Received(1)
                    .SaveSourceFiles(Arg.Is(sourceFiles), Arg.Is(SpecificationId));
        }

        [TestMethod]
        public async Task Compile_GivenStringCompareInCodeAndAggregatesIsEnabledAndAggregateFunctionsUsedButNoAggreatesFound_CompilesCodeAndReturnsOk()
        {
            //Arrange
            string stringCompareCode = "Public Class TestClass\nPublic Property E1 As ExampleClass\nPublic Function TestFunction As String\nDim a = Sum(whatever) as Decimal\nIf E1.ProviderType = \"goodbye\" Then\nReturn \"worked\"\nElse Return \"no\"\nEnd If\nEnd Function\nEnd Class";

            PreviewRequest model = new PreviewRequest
            {
                CalculationId = CalculationId,
                SourceCode = stringCompareCode,
                SpecificationId = SpecificationId
            };

            Calculation calculation = new Calculation
            {
                Id = CalculationId,
                BuildProjectId = BuildProjectId,
                Current = new CalculationVersion(),
                SpecificationId = SpecificationId
            };

            IEnumerable<Calculation> calculations = new List<Calculation>() { calculation };

            BuildProject buildProject = new BuildProject
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

            IValidator<PreviewRequest> validator = CreatePreviewRequestValidator();

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(SpecificationId))
                .Returns(calculations);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(calculation.SpecificationId))
                .Returns(buildProject);

            List<SourceFile> sourceFiles = new List<SourceFile>
            {
                new SourceFile { FileName = "project.vbproj", SourceCode = "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>netcoreapp2.0</TargetFramework></PropertyGroup></Project>" },
                new SourceFile { FileName = "ExampleClass.vb", SourceCode = "Public Class ExampleClass\nPublic Property ProviderType() As String\nEnd Class" },
                new SourceFile { FileName = "Calculation.vb", SourceCode = model.SourceCode }
            };

            Dictionary<string, string> sourceCodes = new Dictionary<string, string>();

            Build compilerOutput = new Build
            {
                Success = true,
                SourceFiles = sourceFiles
            };

            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
                .IsAggregateSupportInCalculationsEnabled()
                .Returns(true);

            IDatasetRepository datasetRepository = CreateDatasetRepository();

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Calculation>>())
                .Returns(compilerOutput);

            sourceCodeService
                .GetCalulationFunctions(Arg.Any<IEnumerable<SourceFile>>())
                .Returns(sourceCodes);

            PreviewService service = CreateService(logger: logger, previewRequestValidator: validator, calculationsRepository: calculationsRepository,
                buildProjectsRepository: buildProjectsRepository,
                datasetRepository: datasetRepository, featureToggle: featureToggle, sourceCodeService: sourceCodeService);

            //Act
            IActionResult result = await service.Compile(request);

            //Assert
            OkObjectResult okResult = result.Should().BeOfType<OkObjectResult>().Subject;

            PreviewResponse previewResponse = okResult.Value.Should().BeOfType<PreviewResponse>().Subject;

            previewResponse
                .Calculation
                .Current
                .SourceCode
                .Should()
                .Be(stringCompareCode);

            previewResponse
                .CompilerOutput
                .Success
                .Should()
                .BeTrue();
        }

        [TestMethod]
        public async Task Compile_GivenStringCompareInCodeAndAggregatesIsEnabledAndAggregateFunctionsUsedButNotContainedInAggregateFields_ReturnsCompileError()
        {
            //Arrange
            string stringCompareCode = "Public Class TestClass\nPublic Property E1 As ExampleClass\nPublic Function TestFunction As String\nDim summedUp = Sum(Datasets.whatever) as Decimal\nIf E1.ProviderType = \"goodbye\" Then\nReturn \"worked\"\nElse Return \"no\"\nEnd If\nEnd Function\nEnd Class";

            PreviewRequest model = new PreviewRequest
            {
                CalculationId = CalculationId,
                SourceCode = stringCompareCode,
                SpecificationId = SpecificationId
            };

            Calculation calculation = new Calculation
            {
                Id = CalculationId,
                BuildProjectId = BuildProjectId,
                Current = new CalculationVersion(),
                SpecificationId = SpecificationId
            };

            IEnumerable<Calculation> calculations = new List<Calculation>() { calculation };

            BuildProject buildProject = new BuildProject();

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            IValidator<PreviewRequest> validator = CreatePreviewRequestValidator();

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(SpecificationId))
                .Returns(calculations);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(calculation.SpecificationId))
                .Returns(buildProject);

            List<SourceFile> sourceFiles = new List<SourceFile>
            {
                new SourceFile { FileName = "project.vbproj", SourceCode = "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>netcoreapp2.0</TargetFramework></PropertyGroup></Project>" },
                new SourceFile { FileName = "ExampleClass.vb", SourceCode = "Public Class ExampleClass\nPublic Property ProviderType() As String\nEnd Class" },
                new SourceFile { FileName = "Calculation.vb", SourceCode = model.SourceCode }
            };

           
            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
                .IsAggregateSupportInCalculationsEnabled()
                .Returns(true);

            IEnumerable<DatasetSchemaRelationshipModel> relationshipModels = new[]
            {
                new DatasetSchemaRelationshipModel
                {
                    Fields = new[]
                    {
                        new DatasetSchemaRelationshipField{ Name = "field Def 1", SourceName = "FieldDef1", SourceRelationshipName = "Rel1" }
                    }
                }
            };

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetDatasetSchemaRelationshipModelsForSpecificationId(Arg.Is(SpecificationId))
                .Returns(relationshipModels);

            PreviewService service = CreateService(logger: logger, previewRequestValidator: validator, calculationsRepository: calculationsRepository,
                buildProjectsRepository: buildProjectsRepository,
                datasetRepository: datasetRepository, featureToggle: featureToggle);

            //Act
            IActionResult result = await service.Compile(request);

            //Assert
            OkObjectResult okResult = result.Should().BeOfType<OkObjectResult>().Subject;

            PreviewResponse previewResponse = okResult.Value.Should().BeOfType<PreviewResponse>().Subject;

            previewResponse
                .Calculation
                .Current
                .SourceCode
                .Should()
                .Be(stringCompareCode);

            previewResponse
                .CompilerOutput
                .Success
                .Should()
                .BeFalse();

            previewResponse
               .CompilerOutput
               .CompilerMessages
               .First()
               .Severity
               .Should()
               .Be(Models.Calcs.Severity.Error);

            previewResponse
              .CompilerOutput
              .CompilerMessages
              .First()
              .Message
              .Should()
              .Be("Datasets.whatever is not an aggretable field");
        }

        [TestMethod]
        public async Task Compile_GivenStringCompareInCodeAndAggregatesIsEnabledAndMultipleAggregateFunctionsUsedWithSameParameterButNotContinedInAggregateFields_ReturnsCompileErrorWithOneMessage()
        {
            //Arrange
            string stringCompareCode = "Public Class TestClass\nPublic Property E1 As ExampleClass\nPublic Function TestFunction As String\nDim average = Avg(Datasets.whatever) as Decimal\nDim summedUp = Sum(Datasets.whatever) as Decimal\nIf E1.ProviderType = \"goodbye\" Then\nReturn \"worked\"\nElse Return \"no\"\nEnd If\nEnd Function\nEnd Class";

            PreviewRequest model = new PreviewRequest
            {
                CalculationId = CalculationId,
                SourceCode = stringCompareCode,
                SpecificationId = SpecificationId
            };

            Calculation calculation = new Calculation
            {
                Id = CalculationId,
                BuildProjectId = BuildProjectId,
                Current = new CalculationVersion(),
                SpecificationId = SpecificationId
            };

            IEnumerable<Calculation> calculations = new List<Calculation>() { calculation };

            BuildProject buildProject = new BuildProject();

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            IValidator<PreviewRequest> validator = CreatePreviewRequestValidator();

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(SpecificationId))
                .Returns(calculations);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(calculation.SpecificationId))
                .Returns(buildProject);

            List<SourceFile> sourceFiles = new List<SourceFile>
            {
                new SourceFile { FileName = "project.vbproj", SourceCode = "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>netcoreapp2.0</TargetFramework></PropertyGroup></Project>" },
                new SourceFile { FileName = "ExampleClass.vb", SourceCode = "Public Class ExampleClass\nPublic Property ProviderType() As String\nEnd Class" },
                new SourceFile { FileName = "Calculation.vb", SourceCode = model.SourceCode }
            };

            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
                .IsAggregateSupportInCalculationsEnabled()
                .Returns(true);

            IEnumerable<DatasetSchemaRelationshipModel> relationshipModels = new[]
             {
                new DatasetSchemaRelationshipModel
                {
                    Fields = new[]
                    {
                        new DatasetSchemaRelationshipField{ Name = "field Def 1", SourceName = "FieldDef1", SourceRelationshipName = "Rel1" }
                    }
                }
            };

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetDatasetSchemaRelationshipModelsForSpecificationId(Arg.Is(SpecificationId))
                .Returns(relationshipModels);

            PreviewService service = CreateService(logger: logger, previewRequestValidator: validator, calculationsRepository: calculationsRepository,
                buildProjectsRepository: buildProjectsRepository,
                datasetRepository: datasetRepository, featureToggle: featureToggle);

            //Act
            IActionResult result = await service.Compile(request);

            //Assert
            OkObjectResult okResult = result.Should().BeOfType<OkObjectResult>().Subject;

            PreviewResponse previewResponse = okResult.Value.Should().BeOfType<PreviewResponse>().Subject;

            previewResponse
                .Calculation
                .Current
                .SourceCode
                .Should()
                .Be(stringCompareCode);

            previewResponse
                .CompilerOutput
                .Success
                .Should()
                .BeFalse();

            previewResponse
               .CompilerOutput
               .CompilerMessages
               .Count()
               .Should()
               .Be(1);

            previewResponse
               .CompilerOutput
               .CompilerMessages
               .First()
               .Severity
               .Should()
               .Be(Models.Calcs.Severity.Error);

            previewResponse
              .CompilerOutput
              .CompilerMessages
              .First()
              .Message
              .Should()
              .Be("Datasets.whatever is not an aggretable field");
        }

        [TestMethod]
        public async Task Compile_GivenStringCompareInCodeAndAggregatesIsEnabledAndMultipleAggregateFunctionsUsedWithDiffrentSameParametersButNotContinedInAggregateFields_ReturnsCompileErrorWithOneMessage()
        {
            //Arrange
            string stringCompareCode = "Public Class TestClass\nPublic Property E1 As ExampleClass\nPublic Function TestFunction As String\nDim average = Avg(Datasets.whatever1) as Decimal\nDim summedUp = Sum(Datasets.whatever2) as Decimal\nIf E1.ProviderType = \"goodbye\" Then\nReturn \"worked\"\nElse Return \"no\"\nEnd If\nEnd Function\nEnd Class";

            PreviewRequest model = new PreviewRequest
            {
                CalculationId = CalculationId,
                SourceCode = stringCompareCode,
                SpecificationId = SpecificationId
            };

            Calculation calculation = new Calculation
            {
                Id = CalculationId,
                BuildProjectId = BuildProjectId,
                Current = new CalculationVersion(),
                SpecificationId = SpecificationId
            };

            IEnumerable<Calculation> calculations = new List<Calculation>() { calculation };

            BuildProject buildProject = new BuildProject();

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            IValidator<PreviewRequest> validator = CreatePreviewRequestValidator();

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(SpecificationId))
                .Returns(calculations);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(calculation.SpecificationId))
                .Returns(buildProject);

            List<SourceFile> sourceFiles = new List<SourceFile>
            {
                new SourceFile { FileName = "project.vbproj", SourceCode = "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>netcoreapp2.0</TargetFramework></PropertyGroup></Project>" },
                new SourceFile { FileName = "ExampleClass.vb", SourceCode = "Public Class ExampleClass\nPublic Property ProviderType() As String\nEnd Class" },
                new SourceFile { FileName = "Calculation.vb", SourceCode = model.SourceCode }
            };

            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
                .IsAggregateSupportInCalculationsEnabled()
                .Returns(true);

            IEnumerable<DatasetSchemaRelationshipModel> relationshipModels = new[]
            {
                new DatasetSchemaRelationshipModel
                {
                    Fields = new[]
                    {
                        new DatasetSchemaRelationshipField{ Name = "field Def 1", SourceName = "FieldDef1", SourceRelationshipName = "Rel1" }
                    }
                }
            };

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetDatasetSchemaRelationshipModelsForSpecificationId(Arg.Is(SpecificationId))
                .Returns(relationshipModels);

            ICacheProvider cacheProvider = CreateCacheProvider();

            PreviewService service = CreateService(logger: logger, previewRequestValidator: validator, calculationsRepository: calculationsRepository,
                buildProjectsRepository: buildProjectsRepository,
                datasetRepository: datasetRepository, featureToggle: featureToggle, cacheProvider: cacheProvider);

            //Act
            IActionResult result = await service.Compile(request);

            //Assert
            OkObjectResult okResult = result.Should().BeOfType<OkObjectResult>().Subject;

            PreviewResponse previewResponse = okResult.Value.Should().BeOfType<PreviewResponse>().Subject;

            previewResponse
                .Calculation
                .Current
                .SourceCode
                .Should()
                .Be(stringCompareCode);

            previewResponse
                .CompilerOutput
                .Success
                .Should()
                .BeFalse();

            previewResponse
               .CompilerOutput
               .CompilerMessages
               .Count()
               .Should()
               .Be(2);

            previewResponse
               .CompilerOutput
               .CompilerMessages
               .First()
               .Severity
               .Should()
               .Be(Models.Calcs.Severity.Error);

            previewResponse
              .CompilerOutput
              .CompilerMessages
              .First()
              .Message
              .Should()
              .Be("Datasets.whatever1 is not an aggretable field");

            previewResponse
              .CompilerOutput
              .CompilerMessages
              .ElementAt(1)
              .Message
              .Should()
              .Be("Datasets.whatever2 is not an aggretable field");

            await
                cacheProvider
                    .Received(1)
                    .SetAsync<List<DatasetSchemaRelationshipModel>>(Arg.Is($"{CacheKeys.DatasetRelationshipFieldsForSpecification}{SpecificationId}"), Arg.Any<List<DatasetSchemaRelationshipModel>>());
        }

        [TestMethod]
        public async Task Compile_GivenStringCompareInCodeAndAggregatesIsEnabledAndAggregateFunctionsUsedAndFieldsInCacheButNotContainedInAggregateFields_ReturnsCompileErrorDoesNotFetchFieldsFromDatabase()
        {
            //Arrange
            string stringCompareCode = "Public Class TestClass\nPublic Property E1 As ExampleClass\nPublic Function TestFunction As String\nDim summedUp = Sum(Datasets.whatever) as Decimal\nIf E1.ProviderType = \"goodbye\" Then\nReturn \"worked\"\nElse Return \"no\"\nEnd If\nEnd Function\nEnd Class";

            PreviewRequest model = new PreviewRequest
            {
                CalculationId = CalculationId,
                SourceCode = stringCompareCode,
                SpecificationId = SpecificationId
            };

            Calculation calculation = new Calculation
            {
                Id = CalculationId,
                BuildProjectId = BuildProjectId,
                Current = new CalculationVersion(),
                SpecificationId = SpecificationId
            };

            IEnumerable<Calculation> calculations = new List<Calculation>() { calculation };

            BuildProject buildProject = new BuildProject();

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            IValidator<PreviewRequest> validator = CreatePreviewRequestValidator();

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(SpecificationId))
                .Returns(calculations);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(calculation.SpecificationId))
                .Returns(buildProject);

            List<SourceFile> sourceFiles = new List<SourceFile>
            {
                new SourceFile { FileName = "project.vbproj", SourceCode = "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>netcoreapp2.0</TargetFramework></PropertyGroup></Project>" },
                new SourceFile { FileName = "ExampleClass.vb", SourceCode = "Public Class ExampleClass\nPublic Property ProviderType() As String\nEnd Class" },
                new SourceFile { FileName = "Calculation.vb", SourceCode = model.SourceCode }
            };

            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
                .IsAggregateSupportInCalculationsEnabled()
                .Returns(true);

            IEnumerable<DatasetSchemaRelationshipModel> relationshipModels = new[]
            {
                new DatasetSchemaRelationshipModel
                {
                    Fields = new[]
                    {
                        new DatasetSchemaRelationshipField{ Name = "field Def 1", SourceName = "FieldDef1", SourceRelationshipName = "Rel1" }
                    }
                }
            };

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<List<DatasetSchemaRelationshipModel>>(Arg.Is($"{CacheKeys.DatasetRelationshipFieldsForSpecification}{SpecificationId}"))
                .Returns(relationshipModels.ToList());

            IDatasetRepository datasetRepository = CreateDatasetRepository();
           
            PreviewService service = CreateService(logger: logger, previewRequestValidator: validator, calculationsRepository: calculationsRepository,
                buildProjectsRepository: buildProjectsRepository,
                datasetRepository: datasetRepository, featureToggle: featureToggle, cacheProvider: cacheProvider);

            //Act
            IActionResult result = await service.Compile(request);

            //Assert
            OkObjectResult okResult = result.Should().BeOfType<OkObjectResult>().Subject;

            PreviewResponse previewResponse = okResult.Value.Should().BeOfType<PreviewResponse>().Subject;

            previewResponse
                .Calculation
                .Current
                .SourceCode
                .Should()
                .Be(stringCompareCode);

            previewResponse
                .CompilerOutput
                .Success
                .Should()
                .BeFalse();

            previewResponse
               .CompilerOutput
               .CompilerMessages
               .First()
               .Severity
               .Should()
               .Be(Models.Calcs.Severity.Error);

            previewResponse
              .CompilerOutput
              .CompilerMessages
              .First()
              .Message
              .Should()
              .Be("Datasets.whatever is not an aggretable field");

            await
                datasetRepository
                .DidNotReceive()
                .GetDatasetSchemaRelationshipModelsForSpecificationId(Arg.Any<string>());

        }

        [TestMethod]
        public async Task Compile_GivenStringCompareInCodeAndAggregatesIsEnabledAndCalculationAggregateFunctionsFound_CompilesCodeAndReturnsOk()
        {
            //Arrange
            string stringCompareCode = "Public Class TestClass\nPublic Property E1 As ExampleClass\nPublic Function TestFunction As String\nIf E1.ProviderType = \"goodbye\" Then\nReturn Sum(Calc1)\nElse Return \"no\"\nEnd If\nEnd Function\nEnd Class";

            PreviewRequest model = new PreviewRequest
            {
                CalculationId = CalculationId,
                SourceCode = stringCompareCode,
                SpecificationId = SpecificationId
            };

            Calculation calculation = new Calculation
            {
                Id = CalculationId,
                BuildProjectId = BuildProjectId,
                Current = new CalculationVersion(),
                SpecificationId = SpecificationId,
                Name = "TestFunction"
            };

            IEnumerable<Calculation> calculations = new List<Calculation>() { calculation };

            BuildProject buildProject = new BuildProject
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

            IValidator<PreviewRequest> validator = CreatePreviewRequestValidator();

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(SpecificationId))
                .Returns(calculations);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(calculation.SpecificationId))
                .Returns(buildProject);

            List<SourceFile> sourceFiles = new List<SourceFile>
            {
                new SourceFile { FileName = "project.vbproj", SourceCode = "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>netcoreapp2.0</TargetFramework></PropertyGroup></Project>" },
                new SourceFile { FileName = "ExampleClass.vb", SourceCode = "Public Class ExampleClass\nPublic Property ProviderType() As String\nEnd Class" },
                new SourceFile { FileName = "Calculation.vb", SourceCode = model.SourceCode }
            };

            Build build = new Build
            {
                Success = true,
                SourceFiles = sourceFiles,
                CompilerMessages = new List<CompilerMessage>()
            };
       
            Dictionary<string, string> sourceCodes = new Dictionary<string, string>()
            {
                { "TestFunction", model.SourceCode },
                { "Calc1", "return 1" }
            };

            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
                .IsAggregateOverCalculationsEnabled()
                .Returns(true);

            IDatasetRepository datasetRepository = CreateDatasetRepository();

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Calculation>>())
                .Returns(build);

            sourceCodeService
                .GetCalulationFunctions(Arg.Any<IEnumerable<SourceFile>>())
                .Returns(sourceCodes);

            PreviewService service = CreateService(logger: logger, previewRequestValidator: validator, calculationsRepository: calculationsRepository,
                buildProjectsRepository: buildProjectsRepository, datasetRepository: datasetRepository, featureToggle: featureToggle, sourceCodeService: sourceCodeService);

            //Act
            IActionResult result = await service.Compile(request);

            //Assert
            OkObjectResult okResult = result.Should().BeOfType<OkObjectResult>().Subject;

            PreviewResponse previewResponse = okResult.Value.Should().BeOfType<PreviewResponse>().Subject;

            previewResponse
                .Calculation
                .Current
                .SourceCode
                .Should()
                .Be(stringCompareCode);

            previewResponse
                .CompilerOutput
                .Success
                .Should()
                .BeTrue();
        }

        [TestMethod]
        public async Task Compile_GivenStringCompareInCodeAndAggregatesIsEnabledAndCalculationAggregateFunctionsFoundButAlreadyInAnAggregate_ReturnsCompilerError()
        {
            //Arrange
            string stringCompareCode = "Public Class TestClass\nPublic Property E1 As ExampleClass\nPublic Function TestFunction As String\nIf E1.ProviderType = \"goodbye\" Then\nReturn Sum(Calc1)\nElse Return \"no\"\nEnd If\nEnd Function\nPublic Function Calc1 As String\nIf E1.ProviderType = \"goodbye\" Then\nReturn Sum(Calc2)\nElse Return \"no\"\nEnd If\nEnd Function\nEnd Class";

            PreviewRequest model = new PreviewRequest
            {
                CalculationId = CalculationId,
                SourceCode = stringCompareCode,
                SpecificationId = SpecificationId
            };

            Calculation calculation = new Calculation
            {
                Id = CalculationId,
                BuildProjectId = BuildProjectId,
                Current = new CalculationVersion(),
                SpecificationId = SpecificationId,
                Name = "TestFunction"
            };

            IEnumerable<Calculation> calculations = new List<Calculation>() { calculation };

            BuildProject buildProject = new BuildProject
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

            IValidator<PreviewRequest> validator = CreatePreviewRequestValidator();

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(SpecificationId))
                .Returns(calculations);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(calculation.SpecificationId))
                .Returns(buildProject);

            List<SourceFile> sourceFiles = new List<SourceFile>
            {
                new SourceFile { FileName = "project.vbproj", SourceCode = "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>netcoreapp2.0</TargetFramework></PropertyGroup></Project>" },
                new SourceFile { FileName = "ExampleClass.vb", SourceCode = "Public Class ExampleClass\nPublic Property ProviderType() As String\nEnd Class" },
                new SourceFile { FileName = "Calculation.vb", SourceCode = model.SourceCode }
            };

            Build build = new Build
            {
                Success = true,
                SourceFiles = sourceFiles,
                CompilerMessages = new List<CompilerMessage>()
            };

            Dictionary<string, string> sourceCodes = new Dictionary<string, string>()
            {
                { "TestFunction", model.SourceCode },
                { "Calc1", "return 1" },
                { "Calc2", "Avg(TestFunction)" }
            };

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Calculation>>())
                .Returns(build);

            sourceCodeService
                .GetCalulationFunctions(Arg.Any<IEnumerable<SourceFile>>())
                .Returns(sourceCodes);

            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
                .IsAggregateOverCalculationsEnabled()
                .Returns(true);

            IDatasetRepository datasetRepository = CreateDatasetRepository();

            PreviewService service = CreateService(logger: logger, previewRequestValidator: validator, calculationsRepository: calculationsRepository,
                buildProjectsRepository: buildProjectsRepository,
                datasetRepository: datasetRepository, featureToggle: featureToggle, sourceCodeService: sourceCodeService);

            //Act
            IActionResult result = await service.Compile(request);

            //Assert
            OkObjectResult okResult = result.Should().BeOfType<OkObjectResult>().Subject;

            PreviewResponse previewResponse = okResult.Value.Should().BeOfType<PreviewResponse>().Subject;

            previewResponse
                .Calculation
                .Current
                .SourceCode
                .Should()
                .Be(stringCompareCode);

            previewResponse
                .CompilerOutput
                .CompilerMessages
                .Count()
                .Should()
                .Be(1);

            previewResponse
               .CompilerOutput
               .CompilerMessages
               .First()
               .Message
               .Should()
               .Be($"TestFunction is already referenced in an aggregation that would cause nesting");
        }

        [TestMethod]
        public async Task Compile_GivenStringCompareInCodeAndAggregatesIsEnabledAndCalculationAggregateFunctionsFoundButFoundNestedAggregate_ReturnsCompilerError()
        {
            //Arrange
            string stringCompareCode = "Public Class TestClass\nPublic Property E1 As ExampleClass\nPublic Function TestFunction As String\nIf E1.ProviderType = \"goodbye\" Then\nReturn Sum(Calc1)\nElse Return \"no\"\nEnd If\nEnd Function\nEnd Class";

            PreviewRequest model = new PreviewRequest
            {
                CalculationId = CalculationId,
                SourceCode = stringCompareCode,
                SpecificationId = SpecificationId
            };

            Calculation calculation = new Calculation
            {
                Id = CalculationId,
                BuildProjectId = BuildProjectId,
                Current = new CalculationVersion(),
                SpecificationId = SpecificationId,
                Name = "Calc2"
            };

            IEnumerable<Calculation> calculations = new List<Calculation>() { calculation };

            BuildProject buildProject = new BuildProject
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

            IValidator<PreviewRequest> validator = CreatePreviewRequestValidator();

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(SpecificationId))
                .Returns(calculations);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(calculation.SpecificationId))
                .Returns(buildProject);

            List<SourceFile> sourceFiles = new List<SourceFile>
            {
                new SourceFile { FileName = "project.vbproj", SourceCode = "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>netcoreapp2.0</TargetFramework></PropertyGroup></Project>" },
                new SourceFile { FileName = "ExampleClass.vb", SourceCode = "Public Class ExampleClass\nPublic Property ProviderType() As String\nEnd Class" },
                new SourceFile { FileName = "Calculation.vb", SourceCode = model.SourceCode }
            };

            Build build = new Build
            {
                Success = true,
                SourceFiles = sourceFiles,
                CompilerMessages = new List<CompilerMessage>()
            };
  
            Dictionary<string, string> sourceCodes = new Dictionary<string, string>()
            {
                { "TestFunction", "return 1" },
                { "Calc1", "return Avg(TestFunction)" },
                { "Calc2", "return Avg(Calc1)" }
            };

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Calculation>>())
                .Returns(build);

            sourceCodeService
                .GetCalulationFunctions(Arg.Any<IEnumerable<SourceFile>>())
                .Returns(sourceCodes);

            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
                .IsAggregateOverCalculationsEnabled()
                .Returns(true);

            IDatasetRepository datasetRepository = CreateDatasetRepository();

            PreviewService service = CreateService(logger: logger, previewRequestValidator: validator, calculationsRepository: calculationsRepository,
                buildProjectsRepository: buildProjectsRepository,
                datasetRepository: datasetRepository, featureToggle: featureToggle, sourceCodeService: sourceCodeService);

            //Act
            IActionResult result = await service.Compile(request);

            //Assert
            OkObjectResult okResult = result.Should().BeOfType<OkObjectResult>().Subject;

            PreviewResponse previewResponse = okResult.Value.Should().BeOfType<PreviewResponse>().Subject;

            previewResponse
                .Calculation
                .Current
                .SourceCode
                .Should()
                .Be(stringCompareCode);

            previewResponse
                .CompilerOutput
                .CompilerMessages
                .Count()
                .Should()
                .Be(1);

            previewResponse
               .CompilerOutput
               .CompilerMessages
               .First()
               .Message
               .Should()
               .Be($"Calc2 cannot reference another calc that is being aggregated");

            await
               sourceCodeService
                   .Received(1)
                   .SaveSourceFiles(Arg.Is(sourceFiles), Arg.Is(SpecificationId));
        }

        static PreviewService CreateService(
            ILogger logger = null, 
            IBuildProjectsRepository buildProjectsRepository = null, 
            IValidator<PreviewRequest> previewRequestValidator = null, 
            ICalculationsRepository calculationsRepository = null, 
            IDatasetRepository datasetRepository = null, 
            IFeatureToggle featureToggle = null, 
            ICacheProvider cacheProvider = null, 
            ISourceCodeService sourceCodeService = null)
        {
            return new PreviewService(
                logger ?? CreateLogger(),
                buildProjectsRepository ?? CreateBuildProjectsRepository(), 
                previewRequestValidator ?? CreatePreviewRequestValidator(),
                calculationsRepository ?? CreateCalculationsRepository(), 
                datasetRepository ?? CreateDatasetRepository(), 
                featureToggle ?? CreateFeatureToggle(), 
                cacheProvider ?? CreateCacheProvider(), 
                sourceCodeService ?? CreateSourceCodeService());
        }

        static ISourceCodeService CreateSourceCodeService()
        {
            return Substitute.For<ISourceCodeService>();
        }

        static ISourceFileGeneratorProvider CreateSourceFileGeneratorProvider(ISourceFileGenerator sourceFileGenerator = null)
        {
            ISourceFileGeneratorProvider provider = Substitute.For<ISourceFileGeneratorProvider>();
            provider
                .CreateSourceFileGenerator(Arg.Is(TargetLanguage.VisualBasic))
                .Returns(sourceFileGenerator);

            return provider;
        }

        static IFeatureToggle CreateFeatureToggle()
        {
            IFeatureToggle featureToggle = Substitute.For<IFeatureToggle>();
            featureToggle
                .IsAggregateSupportInCalculationsEnabled()
                .Returns(false);

            return featureToggle;
        }

        static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        static IBuildProjectsRepository CreateBuildProjectsRepository()
        {
            return Substitute.For<IBuildProjectsRepository>();
        }

        static ICompilerFactory CreateCompilerFactory()
        {
            return Substitute.For<ICompilerFactory>();
        }

        static ICacheProvider CreateCacheProvider()
        {
            return Substitute.For<ICacheProvider>();
        }

        static IValidator<PreviewRequest> CreatePreviewRequestValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
                validationResult = new ValidationResult();

            IValidator<PreviewRequest> validator = Substitute.For<IValidator<PreviewRequest>>();

            validator
               .ValidateAsync(Arg.Any<PreviewRequest>())
               .Returns(validationResult);

            return validator;
        }

        static ICalculationsRepository CreateCalculationsRepository()
        {
            return Substitute.For<ICalculationsRepository>();
        }

        static IDatasetRepository CreateDatasetRepository()
        {
            return Substitute.For<IDatasetRepository>();
        }

        static PreviewRequest CreatePreviewRequest()
        {
            return new PreviewRequest
            {
                SpecificationId = SpecificationId,
                CalculationId = CalculationId,
                SourceCode = SourceCode,
                DecimalPlaces = 6
            };
        }
    }
}
