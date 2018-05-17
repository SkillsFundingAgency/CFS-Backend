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
        public async Task Compile_GivenSourceFileGeneratorWasNotFound_ReturnsInternalServerError()
        {
            // Arrange
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

            List<Calculation> calculations = new List<Calculation>() { calculation };

            BuildProject buildProject = new BuildProject
            {
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

            ISourceFileGeneratorProvider sourceFileGeneratorProvider = CreateSourceFileGeneratorProvider();
            sourceFileGeneratorProvider
                .CreateSourceFileGenerator(Arg.Any<TargetLanguage>())
                .Returns((ISourceFileGenerator)null);

            PreviewService service = CreateService(logger: logger, previewRequestValidator: validator, calculationsRepository: calculationsRepository, buildProjectsRepository: buildProjectsRepository);

            // Act
            IActionResult result = await service.Compile(request);

            // Assert
            result
                .Should()
                .BeOfType<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be("Source file generator was not created");

            logger
                .Received(1)
                .Warning(Arg.Is($"Source file generator was not created"));
        }

        [TestMethod]
        public async Task Compile_GivenSourceFileGeneratorWasCreatedButDidNotGenerateAnyFiles_ReturnsInternalServerError()
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

            IEnumerable<Calculation> calculations = new List<Calculation>() { calculation };

            BuildProject buildProject = new BuildProject
            {
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

            ISourceFileGenerator sourceFileGenerator = Substitute.For<ISourceFileGenerator>();

            IEnumerable<SourceFile> sourceFiles = new List<SourceFile>();

            sourceFileGenerator
                .GenerateCode(Arg.Is(buildProject), Arg.Is(calculations))
                .Returns(sourceFiles);

            ISourceFileGeneratorProvider sourceFileGeneratorProvider = CreateSourceFileGeneratorProvider(sourceFileGenerator);

            PreviewService service = CreateService(logger: logger, previewRequestValidator: validator, calculationsRepository: calculationsRepository,
                buildProjectsRepository: buildProjectsRepository, sourceFileGeneratorProvider: sourceFileGeneratorProvider);

            //Act
            IActionResult result = await service.Compile(request);

            //Assert
            result
                .Should()
                .BeOfType<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be("Source file generator did not generate any source file");

            logger
                .Received(1)
                .Warning(Arg.Is($"Source file generator did not generate any source file"));
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

            ISourceFileGenerator sourceFileGenerator = Substitute.For<ISourceFileGenerator>();

            List<SourceFile> sourceFiles = new List<SourceFile>
            {
                new SourceFile()
            };

            sourceFileGenerator
                .GenerateCode(Arg.Is(buildProject), Arg.Is(calculations))
                .Returns(sourceFiles);

            ISourceFileGeneratorProvider sourceFileGeneratorProvider = CreateSourceFileGeneratorProvider(sourceFileGenerator);

            Build build = new Build
            {
                Success = false
            };

            ICompiler compiler = Substitute.For<ICompiler>();
            compiler
                .GenerateCode(Arg.Any<List<SourceFile>>())
                .Returns(build);

            ICompilerFactory compilerFactory = CreateCompilerFactory();
            compilerFactory
                .GetCompiler(Arg.Is(sourceFiles))
                .Returns(compiler);



            PreviewService service = CreateService(logger: logger, previewRequestValidator: validator, calculationsRepository: calculationsRepository,
                buildProjectsRepository: buildProjectsRepository, sourceFileGeneratorProvider: sourceFileGeneratorProvider, compilerFactory: compilerFactory);

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

            ISourceFileGenerator sourceFileGenerator = Substitute.For<ISourceFileGenerator>();

            List<SourceFile> sourceFiles = new List<SourceFile>
            {
                new SourceFile()
            };

            sourceFileGenerator
                .GenerateCode(Arg.Is(buildProject), Arg.Is(calculations))
                .Returns(sourceFiles);

            ISourceFileGeneratorProvider sourceFileGeneratorProvider = CreateSourceFileGeneratorProvider(sourceFileGenerator);

            Build build = new Build
            {
                Success = true
            };

            ICompiler compiler = Substitute.For<ICompiler>();
            compiler
                .GenerateCode(Arg.Any<List<SourceFile>>())
                .Returns(build);

            ICompilerFactory compilerFactory = CreateCompilerFactory();
            compilerFactory
                .GetCompiler(Arg.Is(sourceFiles))
                .Returns(compiler);

            PreviewService service = CreateService(logger: logger, previewRequestValidator: validator, calculationsRepository: calculationsRepository,
                buildProjectsRepository: buildProjectsRepository, sourceFileGeneratorProvider: sourceFileGeneratorProvider, compilerFactory: compilerFactory);

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

        static PreviewService CreateService(ISourceFileGeneratorProvider sourceFileGeneratorProvider = null,
            ILogger logger = null, IBuildProjectsRepository buildProjectsRepository = null, ICompilerFactory compilerFactory = null,
            IValidator<PreviewRequest> previewRequestValidator = null, ICalculationsRepository calculationsRepository = null)
        {
            return new PreviewService(sourceFileGeneratorProvider ?? CreateSourceFileGeneratorProvider(), logger ?? CreateLogger(),
                buildProjectsRepository ?? CreateBuildProjectsRepository(), compilerFactory ?? CreateCompilerFactory(), previewRequestValidator ?? CreatePreviewRequestValidator(),
                calculationsRepository ?? CreateCalculationsRepository());
        }

        static ISourceFileGeneratorProvider CreateSourceFileGeneratorProvider(ISourceFileGenerator sourceFileGenerator = null)
        {

            ISourceFileGeneratorProvider provider = Substitute.For<ISourceFileGeneratorProvider>();
            provider
                .CreateSourceFileGenerator(Arg.Is(TargetLanguage.VisualBasic))
                .Returns(sourceFileGenerator);

            return provider;
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
