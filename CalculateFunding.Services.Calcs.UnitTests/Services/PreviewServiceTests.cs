using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.DataSets;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Calcs.MappingProfiles;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.FeatureToggles;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using Severity = CalculateFunding.Models.Calcs.Severity;

namespace CalculateFunding.Services.Calcs.Services
{
    [TestClass]
    public class PreviewServiceTests
    {
        const string SpecificationId = "b13cd3ba-bdf8-40a8-9ec4-af48eb8a4386";
        const string CalculationId = "2d30bb44-0862-4524-a2f6-381c5534027a";
        const string BuildProjectId = "4d30bb44-0862-4524-a2f6-381c553402a7";
        const string SourceCode = "Dim i as int = 1 Return i";

        [TestMethod]
        public async Task Compile_GivenNullPreviewRequest_ReturnsBadRequest()
        {
            //Arrange
            PreviewRequest previewRequest = null;

            ILogger logger = CreateLogger();

            PreviewService service = CreateService(logger: logger);

            //Act
            IActionResult result = await service.Compile(previewRequest);

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

            ValidationResult validationResult = new ValidationResult(new[]{
                    new ValidationFailure("prop1", "oh no an error!!!")
                });

            IValidator<PreviewRequest> validator = CreatePreviewRequestValidator(validationResult);

            ILogger logger = CreateLogger();

            PreviewService service = CreateService(logger: logger, previewRequestValidator: validator);

            //Act
            IActionResult result = await service.Compile(model);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Warning(Arg.Is("The preview request failed to validate with errors: oh no an error!!!"));
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

            IValidator<PreviewRequest> validator = CreatePreviewRequestValidator();

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            BuildProject buildProject = null;

            IBuildProjectsService buildProjectsService = CreateBuildProjectsService();
            buildProjectsService
                .GetBuildProjectForSpecificationId(Arg.Is(calculation.SpecificationId))
                .Returns(buildProject);

            PreviewService service = CreateService(logger: logger, previewRequestValidator: validator, calculationsRepository: calculationsRepository, buildProjectsService: buildProjectsService);

            //Act
            IActionResult result = await service.Compile(model);

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
            IActionResult result = await service.Compile(model);

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
                Current = new CalculationVersion(),
                SpecificationId = SpecificationId,
            };

            IValidator<PreviewRequest> validator = CreatePreviewRequestValidator();

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            IBuildProjectsService buildProjectsService = CreateBuildProjectsService();
            buildProjectsService
                .GetBuildProjectForSpecificationId(Arg.Is(calculation.SpecificationId))
                .Returns((BuildProject)null);

            PreviewService service = CreateService(logger: logger, previewRequestValidator: validator, calculationsRepository: calculationsRepository, buildProjectsService: buildProjectsService);

            //Act
            IActionResult result = await service.Compile(model);

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

                Current = new CalculationVersion
                {
                    SourceCodeName = "Horace"
                },
                SpecificationId = SpecificationId,
            };

            IEnumerable<Calculation> calculations = new List<Calculation>() { calculation };

            BuildProject buildProject = new BuildProject
            {
                SpecificationId = SpecificationId
            };

            IValidator<PreviewRequest> validator = CreatePreviewRequestValidator();

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                           .GetCalculationsBySpecificationId(Arg.Is(SpecificationId))
                           .Returns(calculations);

            IBuildProjectsService buildProjectsService = CreateBuildProjectsService();
            buildProjectsService
                .GetBuildProjectForSpecificationId(Arg.Is(SpecificationId))
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
                { "Calculations.TestFunction", model.SourceCode },
                { "Calculations.Calc1", "return 1" }
            };

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Calculation>>(), Arg.Any<CompilerOptions>())
                .Returns(build);

            sourceCodeService
                .GetCalculationFunctions(Arg.Any<IEnumerable<SourceFile>>())
                .Returns(sourceCodes);

            PreviewService service = CreateService(logger: logger, previewRequestValidator: validator, calculationsRepository: calculationsRepository,
                buildProjectsService: buildProjectsService, sourceCodeService: sourceCodeService);

            //Act
            IActionResult result = await service.Compile(model);

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
                .Information(Arg.Is($"Build did not compile successfully for calculation id {calculation.Id}"));

            sourceCodeService
                .DidNotReceive()
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Calculation>>(), Arg.Is<CompilerOptions>(
                        m => m.OptionStrictEnabled == false
                    ));
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
                Current = new CalculationVersion
                {
                    Name = "Alice",
                    SourceCodeName = "Christopher Robin"
                },
                SpecificationId = SpecificationId
            };

            CompilerOptions compilerOptions = new CompilerOptions
            {
                UseLegacyCode = true
            };

            IEnumerable<Calculation> calculations = new List<Calculation> { calculation };

            BuildProject buildProject = new BuildProject
            {
                SpecificationId = SpecificationId
            };

            IValidator<PreviewRequest> validator = CreatePreviewRequestValidator();

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(SpecificationId))
                .Returns(calculations);

            calculationsRepository
                .GetCompilerOptions(Arg.Is(SpecificationId))
                .Returns(compilerOptions);

            IBuildProjectsService buildProjectsService = CreateBuildProjectsService();
            buildProjectsService
                .GetBuildProjectForSpecificationId(Arg.Is(calculation.SpecificationId))
                .Returns(buildProject);

            List<SourceFile> sourceFiles = new List<SourceFile>
            {
                new SourceFile { FileName = "test.vb", SourceCode = "any content"}
            };

            Build build = new Build
            {
                Success = true,
                SourceFiles = sourceFiles,
                CompilerMessages = new List<CompilerMessage>
                {
                    new CompilerMessage { Location = new SourceLocation { StartLine = 1 }, Message = "They're changing guards at Buckingham Palace", Severity = Severity.Info },
                    new CompilerMessage { Location = new SourceLocation { StartLine = 2 }, Message = "Christoper Robin went down with Alice", Severity = Severity.Warning },
                    new CompilerMessage { Location = new SourceLocation { StartLine = 3 }, Message = "Alice is marrying one of the guards", Severity = Severity.Hidden },
                    new CompilerMessage { Location = new SourceLocation { StartLine = 4 }, Message = "'A soldier's life is terribly hard'", Severity = Severity.Warning },
                    new CompilerMessage { Location = new SourceLocation { StartLine = 5 }, Message = "Says Alice", Severity = Severity.Info },
                }
            };

            Dictionary<string, string> sourceCodes = new Dictionary<string, string>
            {
                { "Calculations.TestFunction", model.SourceCode },
                { "Calculations.Calc1", "return 1" },
                { "Calculations.Alice", "return 1" }
            };

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Calculation>>(), Arg.Any<CompilerOptions>())
                .Returns(build);

            sourceCodeService
                .GetCalculationFunctions(Arg.Any<IEnumerable<SourceFile>>())
                .Returns(sourceCodes);

            PreviewService service = CreateService(logger: logger,
                previewRequestValidator: validator,
                calculationsRepository: calculationsRepository,
                buildProjectsService: buildProjectsService,
                sourceCodeService: sourceCodeService);

            //Act
            IActionResult result = await service.Compile(model);

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
                .Information(Arg.Is($"Build compiled successfully for calculation id {calculation.Id}"));

            sourceCodeService
                 .Received(2)
                 .Compile(
                    Arg.Is(buildProject),
                    Arg.Is<IEnumerable<Calculation>>(m => m.Count() == 1),
                    Arg.Is<CompilerOptions>(
                         m => m.OptionStrictEnabled == false &&
                         m.UseLegacyCode == true
                     ));

            logger
                .Received(build.CompilerMessages.Count(x => x.Severity == Severity.Info))
                .Verbose(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());

            logger
                .Received(build.CompilerMessages.Count(x => x.Severity == Severity.Warning))
                .Verbose(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());

            foreach (CompilerMessage message in build.CompilerMessages)
            {
                switch (message.Severity)
                {
                    case Severity.Info:
                        logger
                            .Received(1)
                            .Verbose(Arg.Is<string>(x => x.Contains(message.Message) && x.Contains("Line: " + (message.Location.StartLine + 1).ToString())),
                                SpecificationId, calculation.Id, calculation.Name);
                        break;

                    case Severity.Warning:
                        logger
                            .Received(1)
                            .Warning(Arg.Is<string>(x => x.Contains(message.Message) && x.Contains("Line: " + (message.Location.StartLine + 1).ToString())),
                                SpecificationId, calculation.Id, calculation.Name);
                        break;

                    case Severity.Hidden:
                        logger
                            .Received(0)
                            .Verbose(Arg.Is<string>(x => x.Contains(message.Message)),
                                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
                        logger
                            .Received(0)
                            .Error(Arg.Is<string>(x => x.Contains(message.Message)),
                                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
                        logger
                            .Received(0)
                            .Warning(Arg.Is<string>(x => x.Contains(message.Message)),
                                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
                        break;
                }
            }
        }

        [TestMethod]
        public async Task Compile_GivenStringCompareInCode_CompilesCodeAndReturnsOk()
        {
            //Arrange
            const string stringCompareCode = @"Public Class AdditionalCalculations
Public Property E1 As ExampleClass
Public Function TestFunction As String
If E1.ProviderType = ""goodbye"" Then
Return ""worked""
Else Return ""no""
End If
End Function
End Class";

            PreviewRequest model = new PreviewRequest
            {
                CalculationId = CalculationId,
                SourceCode = stringCompareCode,
                SpecificationId = SpecificationId
            };

            Calculation calculation = new Calculation
            {
                Id = CalculationId,
                Current = new CalculationVersion
                {
                    SourceCodeName = "Horace",
                    Name = "Test Function"
                },
                SpecificationId = SpecificationId,
            };

            IEnumerable<Calculation> calculations = new List<Calculation>() { calculation };

            BuildProject buildProject = new BuildProject
            {
                SpecificationId = SpecificationId
            };

            IValidator<PreviewRequest> validator = CreatePreviewRequestValidator();

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(SpecificationId))
                .Returns(calculations);

            IBuildProjectsService buildProjectsService = CreateBuildProjectsService();
            buildProjectsService
                .GetBuildProjectForSpecificationId(Arg.Is(calculation.SpecificationId))
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
                { "Calculations.TestFunction", model.SourceCode },
                { "Calculations.Calc1", "return 1" }
            };

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Calculation>>(), Arg.Any<CompilerOptions>())
                .Returns(build);

            sourceCodeService
                .GetCalculationFunctions(Arg.Any<IEnumerable<SourceFile>>())
                .Returns(sourceCodes);

            Build nonPreviewBuild = new Build
            {
                Success = true,
                SourceFiles = sourceFiles
            };

            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Calculation>>(), Arg.Is<CompilerOptions>(
                        m => m.OptionStrictEnabled == false
                    )).Returns(nonPreviewBuild);

            PreviewService service = CreateService(logger: logger, previewRequestValidator: validator, calculationsRepository: calculationsRepository,
                buildProjectsService: buildProjectsService, sourceCodeService: sourceCodeService);

            //Act
            IActionResult result = await service.Compile(model);

            //Assert
            OkObjectResult okResult = result.Should().BeOfType<OkObjectResult>().Subject;

            PreviewResponse previewResponse = okResult.Value.Should().BeOfType<PreviewResponse>().Subject;

            previewResponse
                .Calculation
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
                .Information(Arg.Is($"Build compiled successfully for calculation id {calculation.Id}"));

            await
                sourceCodeService
                    .Received(1)
                    .SaveSourceFiles(Arg.Is(sourceFiles), Arg.Is(SpecificationId), Arg.Is(SourceCodeType.Release));
        }


        [TestMethod]
        public async Task Compile_GivenStringCompareInCodeNoReturnSet_ReturnsCompileErrorWithOneMessage()
        {
            //Arrange
            const string stringCompareCode = @"Public Class AdditionalCalculations
Public Property E1 As ExampleClass
Public Function TestFunction As String
REM return ""blah""
End Function
End Class";

            PreviewRequest model = new PreviewRequest
            {
                CalculationId = CalculationId,
                SourceCode = stringCompareCode,
                SpecificationId = SpecificationId
            };

            Calculation calculation = new Calculation
            {
                Id = CalculationId,
                Current = new CalculationVersion
                {
                    SourceCodeName = "Horace",
                    Name = "Test Function"
                },
                SpecificationId = SpecificationId,
            };

            IEnumerable<Calculation> calculations = new List<Calculation>() { calculation };

            BuildProject buildProject = new BuildProject
            {
                SpecificationId = SpecificationId
            };

            IValidator<PreviewRequest> validator = CreatePreviewRequestValidator();

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(SpecificationId))
                .Returns(calculations);

            IBuildProjectsService buildProjectsService = CreateBuildProjectsService();
            buildProjectsService
                .GetBuildProjectForSpecificationId(Arg.Is(calculation.SpecificationId))
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
                { "Calculations.TestFunction", model.SourceCode },
                { "Calculations.Calc1", "return 1" }
            };

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Calculation>>(), Arg.Any<CompilerOptions>())
                .Returns(build);

            sourceCodeService
                .GetCalculationFunctions(Arg.Any<IEnumerable<SourceFile>>())
                .Returns(sourceCodes);

            Build nonPreviewBuild = new Build
            {
                Success = true,
                SourceFiles = sourceFiles
            };

            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Calculation>>(), Arg.Is<CompilerOptions>(
                        m => m.OptionStrictEnabled == false
                    )).Returns(nonPreviewBuild);

            PreviewService service = CreateService(logger: logger, previewRequestValidator: validator, calculationsRepository: calculationsRepository,
                buildProjectsService: buildProjectsService, sourceCodeService: sourceCodeService);

            //Act
            IActionResult result = await service.Compile(model);

            //Assert
            OkObjectResult okResult = result.Should().BeOfType<OkObjectResult>().Subject;

            PreviewResponse previewResponse = okResult.Value.Should().BeOfType<PreviewResponse>().Subject;

            previewResponse
                .Calculation
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
              .Be("Calculations.TestFunction must have a return statement so that a calculation result will be returned");
        }

        [TestMethod]
        public async Task Compile_GivenStringCompareInCodeAndAggregatesIsEnabledAndNoAggregateFunctionsUsed_CompilesCodeAndReturnsOk()
        {
            //Arrange
            const string stringCompareCode = @"Public Class AdditionalCalculations
Public Property E1 As ExampleClass
Public Function TestFunction As String
If E1.ProviderType = ""goodbye"" Then
Return ""worked""
Else Return ""no""
End If
End Function
End Class";

            PreviewRequest model = new PreviewRequest
            {
                CalculationId = CalculationId,
                SourceCode = stringCompareCode,
                SpecificationId = SpecificationId
            };

            Calculation calculation = new Calculation
            {
                Id = CalculationId,
                Current = new CalculationVersion
                {
                    Name = "TestFunction",
                    SourceCodeName = "Horace"
                },
                SpecificationId = SpecificationId
            };

            IEnumerable<Calculation> calculations = new List<Calculation>() { calculation };

            BuildProject buildProject = new BuildProject
            {
                SpecificationId = SpecificationId
            };

            IValidator<PreviewRequest> validator = CreatePreviewRequestValidator();

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(SpecificationId))
                .Returns(calculations);

            IBuildProjectsService buildProjectsService = CreateBuildProjectsService();
            buildProjectsService
                .GetBuildProjectForSpecificationId(Arg.Is(calculation.SpecificationId))
                .Returns(buildProject);

            List<SourceFile> sourceFiles = new List<SourceFile>
            {
                new SourceFile { FileName = "project.vbproj", SourceCode = "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>netcoreapp2.0</TargetFramework></PropertyGroup></Project>" },
                new SourceFile { FileName = "ExampleClass.vb", SourceCode = "Public Class ExampleClass\nPublic Property ProviderType() As String\nEnd Class" },
                new SourceFile { FileName = "Calculation.vb", SourceCode = model.SourceCode }
            };

            Dictionary<string, string> sourceCodes = new Dictionary<string, string>()
            {
                { "Calculations.TestFunction", model.SourceCode }
            };

            Build compilerOutput = new Build
            {
                Success = true,
                SourceFiles = sourceFiles
            };

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Calculation>>(), Arg.Any<CompilerOptions>())
                .Returns(compilerOutput);

            sourceCodeService
                .GetCalculationFunctions(Arg.Any<IEnumerable<SourceFile>>())
                .Returns(sourceCodes);

            IDatasetsApiClient datasetsApiClient = CreateDatasetsApiClient();

            PreviewService service = CreateService(logger: logger, previewRequestValidator: validator, calculationsRepository: calculationsRepository,
                buildProjectsService: buildProjectsService,
                datasetsApiClient: datasetsApiClient, sourceCodeService: sourceCodeService);

            //Act
            IActionResult result = await service.Compile(model);

            //Assert
            OkObjectResult okResult = result.Should().BeOfType<OkObjectResult>().Subject;

            PreviewResponse previewResponse = okResult.Value.Should().BeOfType<PreviewResponse>().Subject;

            previewResponse
                .Calculation
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
                .Information(Arg.Is($"Build compiled successfully for calculation id {calculation.Id}"));

            await
                sourceCodeService
                    .Received(1)
                    .SaveSourceFiles(Arg.Is(sourceFiles), Arg.Is(SpecificationId), Arg.Is(SourceCodeType.Preview));
        }

        [TestMethod]
        public async Task Compile_GivenStringCompareInCodeAndAggregatesIsEnabledAndNoAggregateFunctionsUsedButNoReturnSet_ReturnsCompileErrorWithOneMessage()
        {
            //Arrange
            const string stringCompareCode = @"Public Class AdditionalCalculations
Public Property E1 As ExampleClass
Public Function TestFunction As String
' return ""blah""
End Function
End Class";

            PreviewRequest model = new PreviewRequest
            {
                CalculationId = CalculationId,
                SourceCode = stringCompareCode,
                SpecificationId = SpecificationId
            };

            Calculation calculation = new Calculation
            {
                Id = CalculationId,
                Current = new CalculationVersion
                {
                    Name = "TestFunction",
                    SourceCodeName = "Horace"
                },
                SpecificationId = SpecificationId
            };

            IEnumerable<Calculation> calculations = new List<Calculation>() { calculation };

            BuildProject buildProject = new BuildProject
            {
                SpecificationId = SpecificationId
            };

            IValidator<PreviewRequest> validator = CreatePreviewRequestValidator();

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(SpecificationId))
                .Returns(calculations);

            IBuildProjectsService buildProjectsService = CreateBuildProjectsService();
            buildProjectsService
                .GetBuildProjectForSpecificationId(Arg.Is(calculation.SpecificationId))
                .Returns(buildProject);

            List<SourceFile> sourceFiles = new List<SourceFile>
            {
                new SourceFile { FileName = "project.vbproj", SourceCode = "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>netcoreapp2.0</TargetFramework></PropertyGroup></Project>" },
                new SourceFile { FileName = "ExampleClass.vb", SourceCode = "Public Class ExampleClass\nPublic Property ProviderType() As String\nEnd Class" },
                new SourceFile { FileName = "Calculation.vb", SourceCode = model.SourceCode }
            };

            Dictionary<string, string> sourceCodes = new Dictionary<string, string>()
            {
                { "Calculations.TestFunction", model.SourceCode }
            };

            Build compilerOutput = new Build
            {
                Success = true,
                CompilerMessages = new List<CompilerMessage>(),
                SourceFiles = sourceFiles
            };

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Calculation>>(), Arg.Any<CompilerOptions>())
                .Returns(compilerOutput);

            sourceCodeService
                .GetCalculationFunctions(Arg.Any<IEnumerable<SourceFile>>())
                .Returns(sourceCodes);

            IDatasetsApiClient datasetsApiClient = CreateDatasetsApiClient();

            PreviewService service = CreateService(logger: logger, previewRequestValidator: validator, calculationsRepository: calculationsRepository,
                buildProjectsService: buildProjectsService,
                datasetsApiClient: datasetsApiClient, sourceCodeService: sourceCodeService);

            //Act
            IActionResult result = await service.Compile(model);

            //Assert
            OkObjectResult okResult = result.Should().BeOfType<OkObjectResult>().Subject;

            PreviewResponse previewResponse = okResult.Value.Should().BeOfType<PreviewResponse>().Subject;

            previewResponse
                .Calculation
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
              .Be("Calculations.TestFunction must have a return statement so that a calculation result will be returned");
        }

        [TestMethod]
        public async Task Compile_GivenStringCompareInCodeAndAggregatesIsEnabledAndAggregateFunctionsUsedButNoAggreatesFound_CompilesCodeAndReturnsOk()
        {
            //Arrange
            const string stringCompareCode = @"Public Class AdditionalCalculations
Public Property E1 As ExampleClass
Public Function TestFunction As String
Dim a = Sum(Calculations.whatever) as Decimal
If E1.ProviderType = ""goodbye"" Then
Return ""worked""
Else Return ""no""
End If
End Function
End Class";

            PreviewRequest model = new PreviewRequest
            {
                CalculationId = CalculationId,
                SourceCode = stringCompareCode,
                SpecificationId = SpecificationId
            };

            Calculation calculation = new Calculation
            {
                Id = CalculationId,
                Current = new CalculationVersion
                {
                    SourceCodeName = "Horace",
                    Name = "TestFunction"
                },
                SpecificationId = SpecificationId,

            };

            IEnumerable<Calculation> calculations = new List<Calculation>() { calculation };

            BuildProject buildProject = new BuildProject
            {
                SpecificationId = SpecificationId
            };

            IValidator<PreviewRequest> validator = CreatePreviewRequestValidator();

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(SpecificationId))
                .Returns(calculations);

            IBuildProjectsService buildProjectsService = CreateBuildProjectsService();
            buildProjectsService
                .GetBuildProjectForSpecificationId(Arg.Is(calculation.SpecificationId))
                .Returns(buildProject);

            List<SourceFile> sourceFiles = new List<SourceFile>
            {
                new SourceFile { FileName = "project.vbproj", SourceCode = "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>netcoreapp2.0</TargetFramework></PropertyGroup></Project>" },
                new SourceFile { FileName = "ExampleClass.vb", SourceCode = "Public Class ExampleClass\nPublic Property ProviderType() As String\nEnd Class" },
                new SourceFile { FileName = "Calculation.vb", SourceCode = model.SourceCode }
            };

            Dictionary<string, string> sourceCodes = new Dictionary<string, string>
            {
                { "Calculations.TestFunction", model.SourceCode },
                { "Calculations.Calc1", "return 1" },
                { "Calculations.whatever", "return 1" }
            };

            Build compilerOutput = new Build
            {
                Success = true,
                SourceFiles = sourceFiles,
                CompilerMessages = new List<CompilerMessage>()
            };

            IDatasetsApiClient datasetsApiClient = CreateDatasetsApiClient();

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Calculation>>(), Arg.Any<CompilerOptions>())
                .Returns(compilerOutput);

            sourceCodeService
                .GetCalculationFunctions(Arg.Any<IEnumerable<SourceFile>>())
                .Returns(sourceCodes);

            PreviewService service = CreateService(logger: logger, previewRequestValidator: validator, calculationsRepository: calculationsRepository,
                buildProjectsService: buildProjectsService,
                datasetsApiClient: datasetsApiClient, sourceCodeService: sourceCodeService);

            //Act
            IActionResult result = await service.Compile(model);

            //Assert
            OkObjectResult okResult = result.Should().BeOfType<OkObjectResult>().Subject;

            PreviewResponse previewResponse = okResult.Value.Should().BeOfType<PreviewResponse>().Subject;

            previewResponse
                .Calculation
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
                Current = new CalculationVersion(),
                SpecificationId = SpecificationId
            };

            IEnumerable<Calculation> calculations = new List<Calculation>() { calculation };

            BuildProject buildProject = new BuildProject();

            IValidator<PreviewRequest> validator = CreatePreviewRequestValidator();

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(SpecificationId))
                .Returns(calculations);

            IBuildProjectsService buildProjectsService = CreateBuildProjectsService();
            buildProjectsService
                .GetBuildProjectForSpecificationId(Arg.Is(calculation.SpecificationId))
                .Returns(buildProject);

            IEnumerable<Common.ApiClient.DataSets.Models.DatasetSchemaRelationshipModel> relationshipModels = new[]
            {
                new Common.ApiClient.DataSets.Models.DatasetSchemaRelationshipModel
                {
                    Fields = new[]
                    {
                        new Common.ApiClient.DataSets.Models.DatasetSchemaRelationshipField{ Name = "field Def 1", SourceName = "FieldDef1", SourceRelationshipName = "Rel1" }
                    }
                }
            };

            IMapper mapper = CreateMapper();
            IDatasetsApiClient datasetsApiClient = CreateDatasetsApiClient();
            datasetsApiClient
                .GetDatasetSchemaRelationshipModelsForSpecificationId(Arg.Is(SpecificationId))
                .Returns(new ApiResponse<IEnumerable<Common.ApiClient.DataSets.Models.DatasetSchemaRelationshipModel>>(HttpStatusCode.OK, relationshipModels));

            PreviewService service = CreateService(logger: logger, previewRequestValidator: validator, calculationsRepository: calculationsRepository,
                buildProjectsService: buildProjectsService,
                datasetsApiClient: datasetsApiClient,
                mapper: mapper);

            //Act
            IActionResult result = await service.Compile(model);

            //Assert
            OkObjectResult okResult = result.Should().BeOfType<OkObjectResult>().Subject;

            PreviewResponse previewResponse = okResult.Value.Should().BeOfType<PreviewResponse>().Subject;

            previewResponse
                .Calculation
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
              .Be("Datasets.whatever is not an aggregable field");
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
                Current = new CalculationVersion(),
                SpecificationId = SpecificationId
            };

            IEnumerable<Calculation> calculations = new List<Calculation>() { calculation };

            BuildProject buildProject = new BuildProject();

            IValidator<PreviewRequest> validator = CreatePreviewRequestValidator();

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(SpecificationId))
                .Returns(calculations);

            IBuildProjectsService buildProjectsService = CreateBuildProjectsService();
            buildProjectsService
                .GetBuildProjectForSpecificationId(Arg.Is(calculation.SpecificationId))
                .Returns(buildProject);   

            IEnumerable<Common.ApiClient.DataSets.Models.DatasetSchemaRelationshipModel> relationshipModels = new[]
            {
                new Common.ApiClient.DataSets.Models.DatasetSchemaRelationshipModel
                {
                    Fields = new[]
                    {
                        new Common.ApiClient.DataSets.Models.DatasetSchemaRelationshipField{ Name = "field Def 1", SourceName = "FieldDef1", SourceRelationshipName = "Rel1" }
                    }
                }
            };

            IDatasetsApiClient datasetsApiClient = CreateDatasetsApiClient();
            datasetsApiClient
                .GetDatasetSchemaRelationshipModelsForSpecificationId(Arg.Is(SpecificationId))
                .Returns(new ApiResponse<IEnumerable<Common.ApiClient.DataSets.Models.DatasetSchemaRelationshipModel>>(HttpStatusCode.OK, relationshipModels));

            PreviewService service = CreateService(logger: logger, previewRequestValidator: validator, calculationsRepository: calculationsRepository,
                buildProjectsService: buildProjectsService,
                datasetsApiClient: datasetsApiClient);

            //Act
            IActionResult result = await service.Compile(model);

            //Assert
            OkObjectResult okResult = result.Should().BeOfType<OkObjectResult>().Subject;

            PreviewResponse previewResponse = okResult.Value.Should().BeOfType<PreviewResponse>().Subject;

            previewResponse
                .Calculation
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
              .Be("Datasets.whatever is not an aggregable field");
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
                Current = new CalculationVersion(),
                SpecificationId = SpecificationId
            };

            IEnumerable<Calculation> calculations = new List<Calculation>() { calculation };

            BuildProject buildProject = new BuildProject();

            IValidator<PreviewRequest> validator = CreatePreviewRequestValidator();

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(SpecificationId))
                .Returns(calculations);

            IBuildProjectsService buildProjectsService = CreateBuildProjectsService();
            buildProjectsService
                .GetBuildProjectForSpecificationId(Arg.Is(calculation.SpecificationId))
                .Returns(buildProject);

            IEnumerable<Common.ApiClient.DataSets.Models.DatasetSchemaRelationshipModel> relationshipModels = new[]
            {
                new Common.ApiClient.DataSets.Models.DatasetSchemaRelationshipModel
                {
                    Fields = new[]
                    {
                        new Common.ApiClient.DataSets.Models.DatasetSchemaRelationshipField{ Name = "field Def 1", SourceName = "FieldDef1", SourceRelationshipName = "Rel1" }
                    }
                }
            };

            IDatasetsApiClient datasetsApiClient = CreateDatasetsApiClient();
            datasetsApiClient
                .GetDatasetSchemaRelationshipModelsForSpecificationId(Arg.Is(SpecificationId))               
                .Returns(new ApiResponse<IEnumerable<Common.ApiClient.DataSets.Models.DatasetSchemaRelationshipModel>>(HttpStatusCode.OK, relationshipModels));


            ICacheProvider cacheProvider = CreateCacheProvider();

            PreviewService service = CreateService(logger: logger, previewRequestValidator: validator, calculationsRepository: calculationsRepository,
                buildProjectsService: buildProjectsService,
                datasetsApiClient: datasetsApiClient, cacheProvider: cacheProvider);

            //Act
            IActionResult result = await service.Compile(model);

            //Assert
            OkObjectResult okResult = result.Should().BeOfType<OkObjectResult>().Subject;

            PreviewResponse previewResponse = okResult.Value.Should().BeOfType<PreviewResponse>().Subject;

            previewResponse
                .Calculation
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
               .Be(Severity.Error);

            previewResponse
              .CompilerOutput
              .CompilerMessages
              .First()
              .Message
              .Should()
              .Be("Datasets.whatever1 is not an aggregable field");

            previewResponse
              .CompilerOutput
              .CompilerMessages
              .ElementAt(1)
              .Message
              .Should()
              .Be("Datasets.whatever2 is not an aggregable field");

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
                Current = new CalculationVersion(),
                SpecificationId = SpecificationId
            };

            IEnumerable<Calculation> calculations = new List<Calculation> { calculation };

            BuildProject buildProject = new BuildProject();

            IValidator<PreviewRequest> validator = CreatePreviewRequestValidator();

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(SpecificationId))
                .Returns(calculations);

            IBuildProjectsService buildProjectsService = CreateBuildProjectsService();
            buildProjectsService
                .GetBuildProjectForSpecificationId(Arg.Is(calculation.SpecificationId))
                .Returns(buildProject);

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

            IDatasetsApiClient datasetsApiClient = CreateDatasetsApiClient();

            PreviewService service = CreateService(logger: logger, previewRequestValidator: validator, calculationsRepository: calculationsRepository,
                buildProjectsService: buildProjectsService,
                datasetsApiClient: datasetsApiClient, cacheProvider: cacheProvider);

            //Act
            IActionResult result = await service.Compile(model);

            //Assert
            OkObjectResult okResult = result.Should().BeOfType<OkObjectResult>().Subject;

            PreviewResponse previewResponse = okResult.Value.Should().BeOfType<PreviewResponse>().Subject;

            previewResponse
                .Calculation
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
               .Be(Severity.Error);

            previewResponse
              .CompilerOutput
              .CompilerMessages
              .First()
              .Message
              .Should()
              .Be("Datasets.whatever is not an aggregable field");

            await
                datasetsApiClient
                .DidNotReceive()
                .GetDatasetSchemaRelationshipModelsForSpecificationId(Arg.Any<string>());
        }

        [DataTestMethod]
        [DataRow(CalculationDataType.Boolean)]
        [DataRow(CalculationDataType.String)]
        public async Task Compile_GivenStringCompareInCodeAndAggregatesIsEnabledAndAggregateFunctionsUsedAndNotDecimalDataType_ReturnsCompileErrorDoesNotHaveDecimalDataType
            (CalculationDataType calculationDataType)
        {
            string calculationName = "Calc1";
            string calculationIdentifier = $"Calculations.{calculationName}";

            //Arrange
            string stringCompareCode = $@"Public Class TestClass
                Public Function TestFunction As Decimal
                Return Sum({calculationIdentifier})
                End Function
                End Class";

            PreviewRequest model = new PreviewRequest
            {
                CalculationId = CalculationId,
                SourceCode = stringCompareCode,
                SpecificationId = SpecificationId
            };

            Calculation calculation = new Calculation
            {
                Id = CalculationId,
                Current = new CalculationVersion
                {
                    Name = calculationName,
                    SourceCodeName = "Horace",
                    DataType = calculationDataType
                },
                SpecificationId = SpecificationId
            };

            IEnumerable<Calculation> calculations = new List<Calculation>() { calculation };

            BuildProject buildProject = new BuildProject
            {
                SpecificationId = SpecificationId
            };

            IValidator<PreviewRequest> validator = CreatePreviewRequestValidator();

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(SpecificationId))
                .Returns(calculations);

            IBuildProjectsService buildProjectsService = CreateBuildProjectsService();
            buildProjectsService
                .GetBuildProjectForSpecificationId(Arg.Is(calculation.SpecificationId))
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

            Dictionary<string, string> sourceCodes = new Dictionary<string, string>
            {
                { calculationIdentifier, "return False" }
            };

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Calculation>>(), Arg.Any<CompilerOptions>())
                .Returns(build);

            sourceCodeService
                .GetCalculationFunctions(Arg.Any<IEnumerable<SourceFile>>())
                .Returns(sourceCodes);

            IDatasetsApiClient datasetsApiClient = CreateDatasetsApiClient();

            PreviewService service = CreateService(logger: logger, previewRequestValidator: validator, calculationsRepository: calculationsRepository,
                buildProjectsService: buildProjectsService,
                datasetsApiClient: datasetsApiClient, sourceCodeService: sourceCodeService);

            //Act
            IActionResult result = await service.Compile(model);

            //Assert
            OkObjectResult okResult = result.Should().BeOfType<OkObjectResult>().Subject;

            PreviewResponse previewResponse = okResult.Value.Should().BeOfType<PreviewResponse>().Subject;

            previewResponse
                .Calculation
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
               .Be($"Only decimal fields can be used on aggregation. {calculationIdentifier} has data type of {calculationDataType}");

            await
               sourceCodeService
                   .Received(1)
                   .SaveSourceFiles(Arg.Is(sourceFiles), Arg.Is(SpecificationId), Arg.Is(SourceCodeType.Preview));
        }

        [TestMethod]
        public async Task Compile_GivenStringCompareInCodeAndAggregatesIsEnabledAndCalculationAggregateFunctionsFoundInAnyCase_CompilesCodeAndReturnsOk()
        {
            //Arrange
            const string stringCompareCode = @"Public Class TestClass
Public Property E1 As ExampleClass
Public Function TestFunction As String
If E1.ProviderType = ""goodbye"" Then
Return Sum({calcReference})
Else Return ""no""
End If
End Function
End Class";

            PreviewRequest model = new PreviewRequest
            {
                CalculationId = CalculationId,
                SourceCode = stringCompareCode,
                SpecificationId = SpecificationId
            };

            Calculation calculation = new Calculation
            {
                Id = CalculationId,
                Current = new CalculationVersion
                {
                    Name = "TestFunction",
                    SourceCodeName = "Horace"
                },
                SpecificationId = SpecificationId
            };

            IEnumerable<Calculation> calculations = new List<Calculation> { calculation };

            BuildProject buildProject = new BuildProject
            {
                SpecificationId = SpecificationId
            };

            IValidator<PreviewRequest> validator = CreatePreviewRequestValidator();

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(SpecificationId))
                .Returns(calculations);

            IBuildProjectsService buildProjectsService = CreateBuildProjectsService();
            buildProjectsService
                .GetBuildProjectForSpecificationId(Arg.Is(calculation.SpecificationId))
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

            Dictionary<string, string> sourceCodes = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "Calculations.TestFunction", model.SourceCode },
                { "Calculations.Calc1", "return 1" }
            };

            IDatasetsApiClient datasetsApiClient = CreateDatasetsApiClient();

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Calculation>>(), Arg.Any<CompilerOptions>())
                .Returns(build);

            sourceCodeService
                .GetCalculationFunctions(Arg.Any<IEnumerable<SourceFile>>())
                .Returns(sourceCodes);

            PreviewService service = CreateService(logger: logger, previewRequestValidator: validator, calculationsRepository: calculationsRepository,
                buildProjectsService: buildProjectsService, datasetsApiClient: datasetsApiClient, sourceCodeService: sourceCodeService);

            //Act
            IActionResult result = await service.Compile(model);

            //Assert
            OkObjectResult okResult = result.Should().BeOfType<OkObjectResult>().Subject;

            PreviewResponse previewResponse = okResult.Value.Should().BeOfType<PreviewResponse>().Subject;

            previewResponse
                .Calculation
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
            const string stringCompareCode = @"Public Class AdditionalCalculations
Public Property E1 As ExampleClass
Public Function TestFunction As String
If E1.ProviderType = ""goodbye"" Then
Return Sum(Calculations.Calc1)\nElse Return ""no""
End If
End Function
Public Function Calc1 As String
If E1.ProviderType = ""goodbye"" Then
Return Sum(Calculations.Calc2)
Else Return ""no""
End If
End Function
End Class";

            PreviewRequest model = new PreviewRequest
            {
                CalculationId = CalculationId,
                SourceCode = stringCompareCode,
                SpecificationId = SpecificationId
            };

            Calculation calculation = new Calculation
            {
                Id = CalculationId,
                Current = new CalculationVersion
                {
                    Name = "TestFunction",
                    SourceCodeName = "Horace",
                    Namespace = CalculationNamespace.Additional
                },
                SpecificationId = SpecificationId,
            };

            IEnumerable<Calculation> calculations = new List<Calculation>() { calculation };

            BuildProject buildProject = new BuildProject
            {
                SpecificationId = SpecificationId
            };

            IValidator<PreviewRequest> validator = CreatePreviewRequestValidator();

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(SpecificationId))
                .Returns(calculations);

            IBuildProjectsService buildProjectsService = CreateBuildProjectsService();
            buildProjectsService
                .GetBuildProjectForSpecificationId(Arg.Is(calculation.SpecificationId))
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

            Dictionary<string, string> sourceCodes = new Dictionary<string, string>
            {
                { "Calculations.TestFunction", model.SourceCode },
                { "Calculations.Calc1", "return 1" },
                { "Calculations.Calc2", "Avg(Calculations.TestFunction)" }
            };

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Calculation>>(), Arg.Any<CompilerOptions>())
                .Returns(build);

            sourceCodeService
                .GetCalculationFunctions(Arg.Any<IEnumerable<SourceFile>>())
                .Returns(sourceCodes);

            IDatasetsApiClient datasetsApiClient = CreateDatasetsApiClient();

            PreviewService service = CreateService(logger: logger, previewRequestValidator: validator, calculationsRepository: calculationsRepository,
                buildProjectsService: buildProjectsService,
                datasetsApiClient: datasetsApiClient, sourceCodeService: sourceCodeService);

            //Act
            IActionResult result = await service.Compile(model);

            //Assert
            OkObjectResult okResult = result.Should().BeOfType<OkObjectResult>().Subject;

            PreviewResponse previewResponse = okResult.Value.Should().BeOfType<PreviewResponse>().Subject;

            previewResponse
                .Calculation
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
               .Be("Calculations.TestFunction is already referenced in an aggregation that would cause nesting");
        }

        [TestMethod]
        public async Task Compile_GivenStringCompareInCodeAndAggregatesIsEnabledAndCalculationAggregateFunctionsFoundButFoundNestedAggregate_ReturnsCompilerError()
        {
            //Arrange
            const string stringCompareCode = @"Public Class TestClass
Public Property E1 As AdditionalCalculations
Public Function TestFunction As String
If E1.ProviderType = ""goodbye"" Then
Return Sum(Calculations.Calc1)
Else Return ""no""
End If
End Function
End Class";

            PreviewRequest model = new PreviewRequest
            {
                CalculationId = CalculationId,
                SourceCode = stringCompareCode,
                SpecificationId = SpecificationId
            };

            Calculation calculation = new Calculation
            {
                Id = CalculationId,
                Current = new CalculationVersion
                {
                    Name = "Calc2",
                    SourceCodeName = "Horace"
                },
                SpecificationId = SpecificationId
            };

            IEnumerable<Calculation> calculations = new List<Calculation>() { calculation };

            BuildProject buildProject = new BuildProject
            {
                SpecificationId = SpecificationId
            };

            IValidator<PreviewRequest> validator = CreatePreviewRequestValidator();

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(SpecificationId))
                .Returns(calculations);

            IBuildProjectsService buildProjectsService = CreateBuildProjectsService();
            buildProjectsService
                .GetBuildProjectForSpecificationId(Arg.Is(calculation.SpecificationId))
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

            Dictionary<string, string> sourceCodes = new Dictionary<string, string>
            {
                { "Calculations.TestFunction", "return 1" },
                { "Calculations.Calc1", "return Avg(Calculations.TestFunction)" },
                { "Calculations.Calc2", "return Avg(Calculations.Calc1)" }
            };

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Calculation>>(), Arg.Any<CompilerOptions>())
                .Returns(build);

            sourceCodeService
                .GetCalculationFunctions(Arg.Any<IEnumerable<SourceFile>>())
                .Returns(sourceCodes);

            IDatasetsApiClient datasetsApiClient = CreateDatasetsApiClient();

            PreviewService service = CreateService(logger: logger, previewRequestValidator: validator, calculationsRepository: calculationsRepository,
                buildProjectsService: buildProjectsService,
                datasetsApiClient: datasetsApiClient, sourceCodeService: sourceCodeService);

            //Act
            IActionResult result = await service.Compile(model);

            //Assert
            OkObjectResult okResult = result.Should().BeOfType<OkObjectResult>().Subject;

            PreviewResponse previewResponse = okResult.Value.Should().BeOfType<PreviewResponse>().Subject;

            previewResponse
                .Calculation
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
               .Be("Calculations.Calc2 cannot reference another calc that is being aggregated");

            await
               sourceCodeService
                   .Received(1)
                   .SaveSourceFiles(Arg.Is(sourceFiles), Arg.Is(SpecificationId), Arg.Is(SourceCodeType.Preview));
        }

        [TestMethod]
        public async Task Compile_GivenSourceFileGeneratorCreatedFilesAndCodeFailedButWithFilterableErrorMessages_ReturnsOKWithoutErrors()
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
                Current = new CalculationVersion
                {
                    SourceCodeName = "Horace",
                    Name = "Test Function"
                },
                SpecificationId = SpecificationId
            };

            IEnumerable<Calculation> calculations = new List<Calculation>() { calculation };

            BuildProject buildProject = new BuildProject
            {
                SpecificationId = SpecificationId
            };

            IValidator<PreviewRequest> validator = CreatePreviewRequestValidator();

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(SpecificationId))
                .Returns(calculations);

            IBuildProjectsService buildProjectsService = CreateBuildProjectsService();
            buildProjectsService
                .GetBuildProjectForSpecificationId(Arg.Is(calculation.SpecificationId))
                .Returns(buildProject);

            List<SourceFile> sourceFiles = new List<SourceFile>
            {
                new SourceFile { FileName = "test.vb", SourceCode = "any content"}
            };

            Build build = new Build
            {
                Success = true,
                SourceFiles = sourceFiles,
                CompilerMessages = new List<CompilerMessage>
                {
                    new CompilerMessage{ Severity = Severity.Error, Message = PreviewService.DoubleToNullableDecimalErrorMessage },
                    new CompilerMessage{ Severity = Severity.Error, Message = PreviewService.NullableDoubleToDecimalErrorMessage }
                }
            };

            Dictionary<string, string> sourceCodes = new Dictionary<string, string>
            {
                { "Calculations.TestFunction", model.SourceCode },
                { "Calculations.Calc1", "return 1" }
            };

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Calculation>>(), Arg.Any<CompilerOptions>())
                .Returns(build);

            sourceCodeService
                .GetCalculationFunctions(Arg.Any<IEnumerable<SourceFile>>())
                .Returns(sourceCodes);

            PreviewService service = CreateService(logger: logger, previewRequestValidator: validator, calculationsRepository: calculationsRepository,
                buildProjectsService: buildProjectsService, sourceCodeService: sourceCodeService);

            //Act
            IActionResult result = await service.Compile(model);

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
                .Information(Arg.Is($"Build compiled successfully for calculation id {calculation.Id}"));
        }

        [TestMethod]
        public async Task Compile_GivenSourceFileGeneratorCreatedFilesAndCodeFailed_LogsErrors()
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
                Current = new CalculationVersion
                {
                    SourceCodeName = "Horace"
                },
                SpecificationId = SpecificationId
            };

            IEnumerable<Calculation> calculations = new List<Calculation>() { calculation };

            BuildProject buildProject = new BuildProject
            {
                SpecificationId = SpecificationId
            };

            IValidator<PreviewRequest> validator = CreatePreviewRequestValidator();

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(SpecificationId))
                .Returns(calculations);

            IBuildProjectsService buildProjectsService = CreateBuildProjectsService();
            buildProjectsService
                .GetBuildProjectForSpecificationId(Arg.Is(calculation.SpecificationId))
                .Returns(buildProject);

            List<SourceFile> sourceFiles = new List<SourceFile>
            {
                new SourceFile { FileName = "test.vb", SourceCode = "any content"}
            };

            Build build = new Build
            {
                Success = true,
                SourceFiles = sourceFiles,
                CompilerMessages = new List<CompilerMessage>
                {
                    new CompilerMessage{ Severity = Severity.Error, Message = "The chief defect of Henry King", Location = new SourceLocation { StartLine = 1 } },
                    new CompilerMessage{ Severity = Severity.Error, Message = "Was chewing little bits of string", Location = new SourceLocation { StartLine = 2 } }
                }
            };

            Dictionary<string, string> sourceCodes = new Dictionary<string, string>
            {
                { "Calculations.TestFunction", model.SourceCode },
                { "Calculations.Calc1", "return 1" }
            };

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Calculation>>(), Arg.Any<CompilerOptions>())
                .Returns(build);

            sourceCodeService
                .GetCalculationFunctions(Arg.Any<IEnumerable<SourceFile>>())
                .Returns(sourceCodes); ;

            PreviewService service = CreateService(logger: logger, previewRequestValidator: validator, calculationsRepository: calculationsRepository,
                buildProjectsService: buildProjectsService, sourceCodeService: sourceCodeService);

            //Act
            IActionResult result = await service.Compile(model);

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
                .Information(Arg.Is($"Build did not compile successfully for calculation id {calculation.Id}"));

            foreach (CompilerMessage message in build.CompilerMessages)
            {
                logger
                    .Received(1)
                    .Error(Arg.Is<string>(x => x.Contains(message.Message) && x.Contains((message.Location.StartLine + 1).ToString())),
                        Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
            }
        }

        [TestMethod]
        public async Task Compile_GivenSourceFileGeneratorCreatedFilesAndCodeFailedButWithNonAndFilterableErrorMessage_ReturnsOKWithErrors()
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
                Current = new CalculationVersion
                {
                    SourceCodeName = "Horace"
                },
                SpecificationId = SpecificationId
            };

            IEnumerable<Calculation> calculations = new List<Calculation> { calculation };

            BuildProject buildProject = new BuildProject
            {
                SpecificationId = SpecificationId
            };

            IValidator<PreviewRequest> validator = CreatePreviewRequestValidator();

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(SpecificationId))
                .Returns(calculations);

            IBuildProjectsService buildProjectsService = CreateBuildProjectsService();
            buildProjectsService
                .GetBuildProjectForSpecificationId(Arg.Is(calculation.SpecificationId))
                .Returns(buildProject);

            List<SourceFile> sourceFiles = new List<SourceFile>
            {
                new SourceFile { FileName = "test.vb", SourceCode = "any content"}
            };

            Build build = new Build
            {
                Success = true,
                SourceFiles = sourceFiles,
                CompilerMessages = new List<CompilerMessage>
                {
                    new CompilerMessage{ Severity = Severity.Error, Message = PreviewService.DoubleToNullableDecimalErrorMessage, Location = new SourceLocation { StartLine = 1 }},
                    new CompilerMessage{ Severity = Severity.Error, Message = PreviewService.NullableDoubleToDecimalErrorMessage, Location = new SourceLocation { StartLine = 1 } },
                    new CompilerMessage{ Severity = Severity.Error, Message = "Failed", Location = new SourceLocation { StartLine = 1 } }
                }
            };

            Dictionary<string, string> sourceCodes = new Dictionary<string, string>
            {
                { "TestFunction", model.SourceCode },
                { "Calc1", "return 1" }
            };

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Calculation>>(), Arg.Any<CompilerOptions>())
                .Returns(build);

            sourceCodeService
                .GetCalculationFunctions(Arg.Any<IEnumerable<SourceFile>>())
                .Returns(sourceCodes); ;

            PreviewService service = CreateService(logger: logger, previewRequestValidator: validator, calculationsRepository: calculationsRepository,
                buildProjectsService: buildProjectsService, sourceCodeService: sourceCodeService);

            //Act
            IActionResult result = await service.Compile(model);

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
                .SourceCode
                .Should()
                .Be(SourceCode);

            previewResponse
                .CompilerOutput
                .Success
                .Should()
                .BeFalse();

            previewResponse
               .CompilerOutput
               .CompilerMessages
               .Should()
               .HaveCount(1);
        }

#if NCRUNCH
        [Ignore]
#endif
        [TestMethod]
        [DynamicData(nameof(CodeContainsItsOwnNameTestCases), DynamicDataSourceType.Method)]
        public async Task Compile_GivenCodeContainsItsOwnName_ReturnsBasedOnWhetherNameIsToken(
            CalculationNamespace calcNamespace,
            string calculationName,
            string sourceCode,
            bool codeContainsName,
            int? isToken)
        {
            static string BuildCalcName(Calculation calc, string name)
            {
                return $"{calc.Namespace}.{name}";
            }

            //Arrange
            string calcName = "Alice";

            PreviewRequest model = new PreviewRequest
            {
                CalculationId = CalculationId,
                SourceCode = sourceCode,
                SpecificationId = SpecificationId
            };

            Calculation calculation = new Calculation
            {
                Id = CalculationId,
                Current = new CalculationVersion
                {
                    SourceCodeName = calculationName,
                    Namespace = calcNamespace,
                    Name = calcName
                },
                FundingStreamId = "xyz",
                SpecificationId = SpecificationId
            };

            IEnumerable<Calculation> calculations = new List<Calculation>() { calculation };

            BuildProject buildProject = new BuildProject
            {
                SpecificationId = SpecificationId
            };

            IValidator<PreviewRequest> validator = CreatePreviewRequestValidator();

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(SpecificationId))
                .Returns(calculations);

            IBuildProjectsService buildProjectsService = CreateBuildProjectsService();
            buildProjectsService
                .GetBuildProjectForSpecificationId(Arg.Is(calculation.SpecificationId))
                .Returns(buildProject);

            List<SourceFile> sourceFiles = new List<SourceFile>
            {
                new SourceFile { FileName = "Calculation.vb", SourceCode = model.SourceCode }
            };

            Build build = new Build
            {
                Success = true,
                SourceFiles = sourceFiles,
                CompilerMessages = new List<CompilerMessage>()
            };

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Calculation>>(), Arg.Any<CompilerOptions>())
                .Returns(build);

            Dictionary<string, string> sourceCodes = new Dictionary<string, string>
            {
                { BuildCalcName(calculation, "TestFunction"), model.SourceCode },
                { BuildCalcName(calculation, "Calc1"), "return 1" },
                { BuildCalcName(calculation, calcName), "return 1" }
            };

            sourceCodeService
                .GetCalculationFunctions(Arg.Any<IEnumerable<SourceFile>>())
                .Returns(sourceCodes);

            Build nonPreviewBuild = new Build
            {
                Success = true,
                SourceFiles = sourceFiles,
                CompilerMessages = new List<CompilerMessage>()
            };

            sourceCodeService
                .Compile(Arg.Any<BuildProject>(),
                    Arg.Any<IEnumerable<Calculation>>(),
                    Arg.Any<CompilerOptions>())
                .Returns(nonPreviewBuild);

            ITokenChecker tokenChecker = CreateTokenChecker();
            tokenChecker
                .CheckIsToken(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>())
                .Returns(isToken);

            PreviewService service = CreateService(logger: logger,
                previewRequestValidator: validator,
                calculationsRepository: calculationsRepository,
                buildProjectsService: buildProjectsService,
                sourceCodeService: sourceCodeService,
                tokenChecker: tokenChecker);

            //Act
            IActionResult result = await service.Compile(model);

            //Assert
            OkObjectResult okResult = result.Should().BeOfType<OkObjectResult>().Subject;

            PreviewResponse previewResponse = okResult.Value.Should().BeOfType<PreviewResponse>().Subject;

            previewResponse
                .Calculation
                .SourceCode
                .Should()
                .Be(sourceCode);

            previewResponse
                .CompilerOutput.Success
                .Should().Be(!(isToken != null && codeContainsName));

            if (codeContainsName)
            {
                tokenChecker
                    .Received(1)
                    .CheckIsToken(sourceCode, calcNamespace.ToString(), calculationName, sourceCode.IndexOf(calculationName));
            }

            if (codeContainsName && isToken != null)
            {
                previewResponse
                    .CompilerOutput.CompilerMessages.Single().Message
                    .Should().Be($"Circular reference detected - Calculation '{calculationName}' calls itself");
            }
            else
            {
                previewResponse
                    .CompilerOutput.CompilerMessages.Count()
                    .Should().Be(0);
            }

            await sourceCodeService
                    .Received(1)
                    .SaveSourceFiles(Arg.Is(sourceFiles), Arg.Is(SpecificationId), Arg.Is(SourceCodeType.Release));
        }

        private static IEnumerable<object[]> CodeContainsItsOwnNameTestCases()
        {
            foreach (var tokenPosition in new int?[] { 1, null })
            {
                foreach (var calcNamespace in new[] { CalculationNamespace.Additional, CalculationNamespace.Template })
                {
                    foreach (var calculationName in new[] { "Horace", "Alice" })
                    {
                        yield return new object[] { calcNamespace, calculationName, $"Return {calculationName}", true, tokenPosition };
                        yield return new object[] { calcNamespace, calculationName, $"Return {calculationName.ToUpper()}", true, tokenPosition };
                        yield return new object[] { calcNamespace, calculationName, $"Return {calculationName.ToLower()}", true, tokenPosition };
                        yield return new object[] { calcNamespace, calculationName, $"Return 1", false, tokenPosition };
                    }
                }
            }
        }

#if NCRUNCH
        [Ignore]
#endif
        [TestMethod]
        [DynamicData(nameof(LogMessagesTestCases), DynamicDataSourceType.Method)]
        public void LogMessages_LogsCorrectly(CompilerMessage[] compilerMessages, BuildProject buildProject, Calculation calculation, string[] logMessages)
        {
            //Arrange
            ILogger logger = Substitute.For<ILogger>();

            PreviewService previewService = CreateService(logger: logger);

            Build compilerOutput = new Build { CompilerMessages = compilerMessages.ToList() };

            //Act
            previewService.LogMessages(compilerOutput, buildProject, calculation);

            //Assert
            for (int ix = 0; ix < compilerMessages.Length; ix++)
            {
                switch (compilerMessages[ix].Severity)
                {
                    case Severity.Info:
                        logger.Received(1).Verbose(logMessages[ix], buildProject.SpecificationId, calculation.Id, calculation.Name);
                        break;

                    case Severity.Warning:
                        logger.Received(1).Warning(logMessages[ix], buildProject.SpecificationId, calculation.Id, calculation.Name);
                        break;

                    case Severity.Error:
                        logger.Received(1).Error(logMessages[ix], buildProject.SpecificationId, calculation.Id, calculation.Name);
                        break;
                }
            }

            logger
                .Received(compilerMessages.Count(x => x.Severity == Severity.Info))
                .Verbose(Arg.Any<string>(), buildProject.SpecificationId, calculation.Id, calculation.Name);

            logger
                .Received(compilerMessages.Count(x => x.Severity == Severity.Warning))
                .Warning(Arg.Any<string>(), buildProject.SpecificationId, calculation.Id, calculation.Name);

            logger
                .Received(compilerMessages.Count(x => x.Severity == Severity.Error))
                .Error(Arg.Any<string>(), buildProject.SpecificationId, calculation.Id, calculation.Name);
        }

        private static IEnumerable<object[]> LogMessagesTestCases()
        {
            BuildProject buildProject = new BuildProject { SpecificationId = "Esme" };
            Calculation calculation = new Calculation { Id = "Gytha", Current = new CalculationVersion { Name = "Magrat" } };
            yield return new object[] { new CompilerMessage[0], buildProject, calculation, new string[] { } };

            IEnumerable<dynamic[]> messages = new List<dynamic[]>
            {
                new[]
                {
                    new { Message = "No one can tell me", Owner = "Christopher", Line = 2718, Severity = Severity.Info }
                },
                new[]
                {
                    new { Message = "Nobody knows", Owner = "James", Line = 42, Severity = Severity.Info },
                    new { Message = "Where the wind comes from", Owner = "James", Line = 3, Severity = Severity.Warning },
                    new { Message = "Where the wind goes", Owner = "Morrison", Line = 141, Severity = Severity.Warning },
                    new { Message = "It's flying from somewhere", Owner = "Morrison", Line = 592, Severity = Severity.Error },
                    new { Message = "As fast as it can", Owner = "Weatherby", Line = 653, Severity = Severity.Error },
                    new { Message = "I couldn't keep up with it", Owner = "George", Line = 589, Severity = Severity.Error },
                    new { Message = "Not if I ran", Owner = "Dupree", Line=793, Severity = Severity.Error }
                }
            };

            foreach (dynamic[] message in messages)
            {
                yield return new object[]
                {
                    message.Select(x => new CompilerMessage
                    {
                        Message = x.Message,
                        Location = new SourceLocation
                        {
                            Owner = new Reference { Name=x.Owner },
                            StartLine = x.Line
                        },
                        Severity = x.Severity
                    }).ToArray(),
                    buildProject,
                    calculation,
                    message.Select(x => $@"Error while compiling code preview: {x.Message}
Line: {x.Line + 1}

Specification ID: {{specificationId}}
Calculation ID: {{calculationId}}
Calculation Name: {{calculationName}}").ToArray()
                };
            }
        }

        static PreviewService CreateService(
            ILogger logger = null,
            IBuildProjectsService buildProjectsService = null,
            IValidator<PreviewRequest> previewRequestValidator = null,
            ICalculationsRepository calculationsRepository = null,
            IDatasetsApiClient datasetsApiClient = null,
            ICacheProvider cacheProvider = null,
            ISourceCodeService sourceCodeService = null,
            ITokenChecker tokenChecker = null,
            IMapper mapper = null)
        {
            return new PreviewService(
                logger ?? CreateLogger(),
                buildProjectsService ?? CreateBuildProjectsService(),
                previewRequestValidator ?? CreatePreviewRequestValidator(),
                calculationsRepository ?? CreateCalculationsRepository(),
                datasetsApiClient ?? CreateDatasetsApiClient(),
                cacheProvider ?? CreateCacheProvider(),
                sourceCodeService ?? CreateSourceCodeService(),
                tokenChecker ?? CreateTokenChecker(),
                CalcsResilienceTestHelper.GenerateTestPolicies(),
                mapper ?? CreateMapper());
        }

        static ISourceCodeService CreateSourceCodeService()
        {
            return Substitute.For<ISourceCodeService>();
        }

        static IFeatureToggle CreateFeatureToggle()
        {
            IFeatureToggle featureToggle = Substitute.For<IFeatureToggle>();

            return featureToggle;
        }

        static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        static IBuildProjectsService CreateBuildProjectsService()
        {
            return Substitute.For<IBuildProjectsService>();
        }

        static ICacheProvider CreateCacheProvider()
        {
            return Substitute.For<ICacheProvider>();
        }

        static IValidator<PreviewRequest> CreatePreviewRequestValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
            {
                validationResult = new ValidationResult();
            }

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

        static IDatasetsApiClient CreateDatasetsApiClient()
        {
            return Substitute.For<IDatasetsApiClient>();
        }

        static ITokenChecker CreateTokenChecker()
        {
            return Substitute.For<ITokenChecker>();
        }

        private static IMapper CreateMapper()
        {
            MapperConfiguration mapperConfig = new MapperConfiguration(c =>
            {
                c.AddProfile<CalculationsMappingProfile>();
            });

            return mapperConfig.CreateMapper();
        }
    }
}
