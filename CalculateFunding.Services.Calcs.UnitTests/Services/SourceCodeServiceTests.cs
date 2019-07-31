using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Calcs.Interfaces.CodeGen;
using CalculateFunding.Services.CodeGeneration;
using CalculateFunding.Services.CodeGeneration.VisualBasic;
using CalculateFunding.Services.CodeMetadataGenerator.Interfaces;
using CalculateFunding.Services.Compiler;
using CalculateFunding.Services.Compiler.Interfaces;
using CalculateFunding.Services.Compiler.Languages;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Serilog;

namespace CalculateFunding.Services.Calcs.Services
{
    [TestClass]
    public class SourceCodeServiceTests
    {
        const string specificationId = "spec-id-1";
        const string calculationId = "calc-id-1";
        const string buildProjectId = "bp-id-1";

        [TestMethod]
        public void Compile_GivenStringCompareInCodeAndAggregatesIsEnabledAndCalculationAggregateFunctionsFound_CompilesCodeAndReturnsOk()
        {
            //Arrange
            string stringCompareCode = "Public Class TestClass\nPublic Property E1 As ExampleClass\nPublic Function TestFunction As String\nIf E1.ProviderType = \"goodbye\" Then\nReturn Sum(Calc1)\nElse Return \"no\"\nEnd If\nEnd Function\nEnd Class";

            Calculation calculation = new Calculation
            {
                Id = calculationId,
                Current = new CalculationVersion
                {
                    Name = "TestFunction"
                },
                SpecificationId = specificationId,
            };

            IEnumerable<Calculation> calculations = new List<Calculation>() { calculation };

            BuildProject buildProject = new BuildProject
            {
                SpecificationId = specificationId
            };

            ILogger logger = CreateLogger();

            List<SourceFile> sourceFiles = new List<SourceFile>
            {
                new SourceFile { FileName = "project.vbproj", SourceCode = "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>netcoreapp2.0</TargetFramework></PropertyGroup></Project>" },
                new SourceFile { FileName = "ExampleClass.vb", SourceCode = "Public Class ExampleClass\nPublic Property ProviderType() As String\nEnd Class" },
                new SourceFile { FileName = "Calculation.vb", SourceCode = stringCompareCode }
            };

            Build build = new Build
            {
                Success = true,
                SourceFiles = sourceFiles
            };

            Dictionary<string, string> sourceCodes = new Dictionary<string, string>()
            {
                { "TestFunction", stringCompareCode },
                { "Calc1", "return 1" }
            };

            CompilerOptions compilerOptions = new CompilerOptions();

            ISourceFileGenerator sourceFileGenerator = Substitute.For<ISourceFileGenerator>();
            sourceFileGenerator
                .GenerateCode(Arg.Is(buildProject), Arg.Is(calculations), compilerOptions)
                .Returns(sourceFiles);

            ISourceFileGeneratorProvider sourceFileGeneratorProvider = CreateSourceFileGeneratorProvider();
            sourceFileGeneratorProvider
                .CreateSourceFileGenerator(Arg.Any<TargetLanguage>())
                .Returns(sourceFileGenerator);

            ICompiler compiler = CreateCompiler();

            ICompilerFactory compilerFactory = CreateCompilerFactory(compiler, sourceFiles);

            SourceCodeService sourceCodeService = CreateSourceCodeService(sourceFileGeneratorProvider: sourceFileGeneratorProvider, compilerFactory: compilerFactory);

            //Act
            Build buildResult = sourceCodeService.Compile(buildProject, calculations, compilerOptions);

            //Assert
            compiler
                 .Received(1)
                 .GenerateCode(Arg.Is<List<SourceFile>>(m => m.Count == 3));
        }

        [TestMethod]
        public void Compile_ErrorThrown_ReturnsErrorAsCompilerMessage()
        {
            //Arrange
            BuildProject buildProject = new BuildProject { SpecificationId = "3456" };
            IEnumerable<Calculation> calculations = new List<Calculation>();

            string errorMessage = "The sky is red, I don't understand";

            ISourceFileGenerator sourceFileGenerator = Substitute.For<ISourceFileGenerator>();
            sourceFileGenerator
                .GenerateCode(buildProject, calculations, Arg.Any<CompilerOptions>())
                .Throws(new Exception(errorMessage));

            ISourceFileGeneratorProvider sourceFileGeneratorProvider = CreateSourceFileGeneratorProvider();
            sourceFileGeneratorProvider
                .CreateSourceFileGenerator(TargetLanguage.VisualBasic)
                .Returns(sourceFileGenerator);

            SourceCodeService sourceCodeService = CreateSourceCodeService(sourceFileGeneratorProvider: sourceFileGeneratorProvider);

            //Act
            Build result = sourceCodeService.Compile(buildProject, calculations);

            //Assert
            result.CompilerMessages.Count
                .Should()
                .Be(1);
            result.CompilerMessages.Count(x => x.Message == errorMessage && x.Severity == Severity.Error)
                .Should()
                .Be(1);

            sourceFileGenerator
                .Received(1)
                .GenerateCode(buildProject,
                    calculations,
                    Arg.Is<CompilerOptions>(x => x.SpecificationId == buildProject.SpecificationId && !x.OptionStrictEnabled));
        }

        [TestMethod]
        public void SaveAssembly_GivenBuildProjectDoesntContainABuildObject_ThrowsArgumentException()
        {
            //Arrange
            BuildProject buildProject = new BuildProject
            {
                SpecificationId = specificationId,
            };

            ISourceFileRepository sourceFileRepository = CreateSourceFileRepository();

            ILogger logger = CreateLogger();

            SourceCodeService sourceFileService = CreateSourceCodeService(sourceFileRepository, logger);

            //Act
            Func<Task> test = async () => await sourceFileService.SaveAssembly(buildProject);

            //Assert
            test
                .Should()
                .ThrowExactly<ArgumentException>()
                .Which
                .Message
                .Should()
                .Be($"Assembly not present on build project for specification id: '{buildProject.SpecificationId}'");
        }

        [TestMethod]
        public void SaveAssembly_GivenBuildProjectDoesntNotContainAssembly_ThrowsArgumentException()
        {
            //Arrange
            BuildProject buildProject = new BuildProject
            {
                SpecificationId = specificationId,
                Build = new Build()
            };

            ISourceFileRepository sourceFileRepository = CreateSourceFileRepository();

            ILogger logger = CreateLogger();

            SourceCodeService sourceFileService = CreateSourceCodeService(sourceFileRepository, logger);

            //Act
            Func<Task> test = async () => await sourceFileService.SaveAssembly(buildProject);

            //Assert
            test
                .Should()
                .ThrowExactly<ArgumentException>()
                .Which
                .Message
                .Should()
                .Be($"Assembly not present on build project for specification id: '{buildProject.SpecificationId}'");
        }

        [TestMethod]
        public void SaveAssembly_GivenAssemblyButFailsToSave_ThrowsException()
        {
            //Arrange
            BuildProject buildProject = new BuildProject
            {
                SpecificationId = specificationId,
                Build = new Build
                {
                    Assembly = new byte[100]
                }
            };

            ISourceFileRepository sourceFileRepository = CreateSourceFileRepository();
            sourceFileRepository.When(x => x.SaveAssembly(Arg.Is(buildProject.Build.Assembly), Arg.Is(buildProject.SpecificationId)))
                                .Do(x => { throw new Exception(); });

            ILogger logger = CreateLogger();

            SourceCodeService sourceFileService = CreateSourceCodeService(sourceFileRepository, logger);

            //Act
            Func<Task> test = async () => await sourceFileService.SaveAssembly(buildProject);

            //Assert
            test
                .Should()
                .ThrowExactly<Exception>();

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Is($"Failed to save assembly for specification id '{buildProject.SpecificationId}'"));
        }

        [TestMethod]
        public async Task SaveAssembly_GivenAssemblyAndSabeSuccessful_LogsSuccess()
        {
            //Arrange
            BuildProject buildProject = new BuildProject
            {
                SpecificationId = specificationId,
                Build = new Build
                {
                    Assembly = new byte[100]
                }
            };

            ISourceFileRepository sourceFileRepository = CreateSourceFileRepository();

            ILogger logger = CreateLogger();

            SourceCodeService sourceFileService = CreateSourceCodeService(sourceFileRepository, logger);

            //Act
            await sourceFileService.SaveAssembly(buildProject);

            //Assert
            logger
                .Received(1)
                .Information($"Saved assembly for specification id: '{buildProject.SpecificationId}'");
        }

        [TestMethod]
        public async Task GetAssembly_GivenAssemblyDoesNotExist_CompilesNewAssembly()
        {
            //Arrange
            BuildProject buildProject = new BuildProject
            {
                SpecificationId = specificationId,
            };

            Calculation calculation = new Calculation
            {
                Id = calculationId,
                Current = new CalculationVersion
                {
                    Name = "TestFunction"
                },
                SpecificationId = specificationId
            };

            IEnumerable<Calculation> calculations = new List<Calculation>() { calculation };

            ISourceFileRepository sourceFileRepository = CreateSourceFileRepository();
            sourceFileRepository
                .DoesAssemblyExist(Arg.Is(specificationId))
                .Returns(false);

            List<SourceFile> sourceFiles = new List<SourceFile>
            {
                new SourceFile { FileName = "project.vbproj", SourceCode = "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>netcoreapp2.0</TargetFramework></PropertyGroup></Project>" },
                new SourceFile { FileName = "ExampleClass.vb", SourceCode = "Public Class ExampleClass\nPublic Property ProviderType() As String\nEnd Class" },
                new SourceFile { FileName = "Calculation.vb", SourceCode = "code" }
            };

            buildProject.Build = new Build
            {
                SourceFiles = sourceFiles,
            };

            Build newBuild = new Build
            {
                SourceFiles = sourceFiles,
                Assembly = new byte[100]
            };

            ISourceFileGenerator sourceFileGenerator = Substitute.For<ISourceFileGenerator>();
            sourceFileGenerator
                .GenerateCode(Arg.Is(buildProject), Arg.Any<IEnumerable<Calculation>>(), Arg.Any<CompilerOptions>())
                .Returns(sourceFiles);

            ISourceFileGeneratorProvider sourceFileGeneratorProvider = CreateSourceFileGeneratorProvider();
            sourceFileGeneratorProvider
                .CreateSourceFileGenerator(Arg.Any<TargetLanguage>())
                .Returns(sourceFileGenerator);

            ICompiler compiler = CreateCompiler();
            compiler
                .GenerateCode(Arg.Any<List<SourceFile>>())
                .Returns(newBuild);

            ICompilerFactory compilerFactory = CreateCompilerFactory(compiler, sourceFiles);

            SourceCodeService sourceCodeService = CreateSourceCodeService(sourceFileGeneratorProvider: sourceFileGeneratorProvider, compilerFactory: compilerFactory);

            //Act
            byte[] assembly = await sourceCodeService.GetAssembly(buildProject);

            //Assert
            assembly
                .Should()
                .NotBeNull();

            assembly
                .Length
                .Should()
                .Be(100);
        }

        [TestMethod]
        public void GetAssembly_GivenNullStreamReturned_ThrowsException()
        {
            //Arrange
            BuildProject buildProject = new BuildProject
            {
                SpecificationId = specificationId,
            };

            ISourceFileRepository sourceFileRepository = CreateSourceFileRepository();
            sourceFileRepository
                .DoesAssemblyExist(Arg.Is(specificationId))
                .Returns(true);

            sourceFileRepository
                .GetAssembly(Arg.Is(specificationId))
                .Returns((Stream)null);

            ILogger logger = CreateLogger();

            SourceCodeService sourceCodeService = CreateSourceCodeService(sourceFileRepository, logger);

            //Act
            Func<Task> test = async () => await sourceCodeService.GetAssembly(buildProject);

            //Assert
            test
                .Should()
                .ThrowExactly<Exception>()
                .Which
                .Message
                .Should()
                .Be($"Failed to get assembly for specification id: '{specificationId}'");
        }

        [TestMethod]
        public async Task GetAssembly_GivenStreamReturned_ReturnsAssembly()
        {
            //Arrange
            BuildProject buildProject = new BuildProject
            {
                SpecificationId = specificationId,
            };

            ISourceFileRepository sourceFileRepository = CreateSourceFileRepository();
            sourceFileRepository
                .DoesAssemblyExist(Arg.Is(specificationId))
                .Returns(true);

            Stream stream = new MemoryStream(new byte[100]);

            sourceFileRepository
                .GetAssembly(Arg.Is(specificationId))
                .Returns(stream);

            ILogger logger = CreateLogger();

            SourceCodeService sourceCodeService = CreateSourceCodeService(sourceFileRepository, logger);

            //Act
            byte[] assembly = await sourceCodeService.GetAssembly(buildProject);

            //Assert
            assembly
                .Should()
                .NotBeNull();

            assembly
                .Length
                .Should()
                .Be(100);
        }

        [TestMethod]
        public async Task SaveSourceFiles_GivenSourceFiles_CompressesAndSaves()
        {
            //Arrange
            IEnumerable<SourceFile> sourceFiles = new[]
            {
                new SourceFile { FileName = "project.vbproj", SourceCode = "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>netcoreapp2.0</TargetFramework></PropertyGroup></Project>" },
                new SourceFile { FileName = "ExampleClass.vb", SourceCode = "Public Class ExampleClass\nPublic Property ProviderType() As String\nEnd Class" },
                new SourceFile { FileName = "Calculation.vb", SourceCode = "code" }
            };

            ISourceFileRepository sourceFileRepository = CreateSourceFileRepository();

            SourceCodeService sourceCodeService = CreateSourceCodeService(sourceFileRepository);

            //Act
            await sourceCodeService.SaveSourceFiles(sourceFiles, specificationId, SourceCodeType.Release);

            //Assert
            await
                sourceFileRepository
                    .Received(1)
                    .SaveSourceFiles(Arg.Any<byte[]>(), Arg.Is(specificationId), Arg.Is("release"));
        }

        [TestMethod]
        public async Task SaveSourceFiles_GivenEmptySourceFiles_LogsAndDoesNotSave()
        {
            //Arrange
            IEnumerable<SourceFile> sourceFiles = Enumerable.Empty<SourceFile>();

            ISourceFileRepository sourceFileRepository = CreateSourceFileRepository();

            ILogger logger = CreateLogger();

            SourceCodeService sourceCodeService = CreateSourceCodeService(sourceFileRepository, logger);

            //Act
            await sourceCodeService.SaveSourceFiles(sourceFiles, specificationId, SourceCodeType.Preview);

            //Assert
            await
                sourceFileRepository
                    .DidNotReceive()
                    .SaveSourceFiles(Arg.Any<byte[]>(), Arg.Is(specificationId), Arg.Is("preview"));

            logger
                .Received(1)
                .Error($"Failed to compress source files for specification id: '{specificationId}'");
        }

        [TestMethod]
        public void CompileBuildProject_WhenBuildingCalculation_ThenCompilationUsesSourceCodeName()
        {
            // Arrange
            string specificationId = "test-spec1";
            List<Calculation> calculations = new List<Calculation>
            {
                new Calculation
                {
                    Id = "calcId1",
                    Current = new CalculationVersion
                    {
                        SourceCode = "return 10",
                        Name = "calc 1",
                        SourceCodeName = "differentCalcName",
                        Description = "test calc",
                    }
                }
            };

            SourceCodeService sourceCodeService = CreateServiceWithRealCompiler();

            BuildProject buildProject = new BuildProject
            {
                SpecificationId = specificationId,
                Id = Guid.NewGuid().ToString(),
                Name = specificationId
            };

            CompilerOptions compilerOptions = new CompilerOptions();

            // Act
            Build build = sourceCodeService.Compile(buildProject, calculations, compilerOptions);

            // Assert
            build.Success.Should().BeTrue();

            string calcSourceCode = build.SourceFiles.First(s => s.FileName == "Calculations.vb").SourceCode;
            calcSourceCode.Should().Contain($"Dim differentCalcName As Func(Of decimal?) = nothing");
            calcSourceCode.Should().NotContain($"Dim calc1 As Func(Of decimal?) = nothing");
            calcSourceCode.Should().Contain($"differentCalcName()");
            calcSourceCode.Should().NotContain($"calc1()");
        }

        private static SourceCodeService CreateSourceCodeService(
            ISourceFileRepository sourceFilesRepository = null,
            ILogger logger = null,
            ICalculationsRepository calculationsRepository = null,
            ISourceFileGeneratorProvider sourceFileGeneratorProvider = null,
            ICompilerFactory compilerFactory = null,
            ICodeMetadataGeneratorService codeMetadataGenerator = null,
            ICalcsResiliencePolicies resiliencePolicies = null)
        {
            return new SourceCodeService(
                sourceFilesRepository ?? CreateSourceFileRepository(),
                logger ?? CreateLogger(),
                calculationsRepository ?? CreateCalculationsRepository(),
                sourceFileGeneratorProvider ?? CreateSourceFileGeneratorProvider(),
                compilerFactory ?? CreateCompilerFactory(),
                codeMetadataGenerator ?? CreateCodeMetadataGeneratorService(),
                resiliencePolicies ?? CreatePolicies());
        }

        private SourceCodeService CreateServiceWithRealCompiler(IFeatureToggle featureToggle = null)
        {
            ILogger logger = CreateLogger();
            ISourceFileGeneratorProvider sourceFileGeneratorProvider = CreateSourceFileGeneratorProvider();
            sourceFileGeneratorProvider
                .CreateSourceFileGenerator(Arg.Is(TargetLanguage.VisualBasic))
                .Returns(new VisualBasicSourceFileGenerator(logger));

            VisualBasicCompiler vbCompiler = new VisualBasicCompiler(logger);
            CompilerFactory compilerFactory = new CompilerFactory(null, vbCompiler);

            return CreateSourceCodeService(sourceFileGeneratorProvider: sourceFileGeneratorProvider, calculationsRepository: Substitute.For<ICalculationsRepository>(), logger: logger, compilerFactory: compilerFactory);
        }

        private static ICalcsResiliencePolicies CreatePolicies()
        {
            return CalcsResilienceTestHelper.GenerateTestPolicies();
        }

        private static ISourceFileRepository CreateSourceFileRepository()
        {
            return Substitute.For<ISourceFileRepository>();
        }

        private static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        private static ICalculationsRepository CreateCalculationsRepository()
        {
            return Substitute.For<ICalculationsRepository>();
        }

        private static ISourceFileGeneratorProvider CreateSourceFileGeneratorProvider()
        {
            return Substitute.For<ISourceFileGeneratorProvider>();
        }

        private static ICompilerFactory CreateCompilerFactory(ICompiler compiler = null, IEnumerable<SourceFile> sourceFiles = null)
        {
            ICompilerFactory compilerFactory = Substitute.For<ICompilerFactory>();
            compilerFactory
                .GetCompiler(Arg.Is(sourceFiles))
                .Returns(compiler);

            return compilerFactory;
        }

        private static ICompiler CreateCompiler()
        {
            return Substitute.For<ICompiler>();
        }

        private static ICodeMetadataGeneratorService CreateCodeMetadataGeneratorService()
        {
            return Substitute.For<ICodeMetadataGeneratorService>();
        }
    }
}
