using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Code;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Calcs.Interfaces.CodeGen;
using CalculateFunding.Services.CodeGeneration;
using CalculateFunding.Services.CodeMetadataGenerator.Interfaces;
using CalculateFunding.Services.Compiler;
using CalculateFunding.Services.Compiler.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Calcs
{
    public class SourceCodeService : ISourceCodeService
    {
        private readonly ISourceFileRepository _sourceFilesRepository;
        private readonly ILogger _logger;
        private readonly ICalculationsRepository _calculationsRepository;
        private readonly ISourceFileGeneratorProvider _sourceFileGeneratorProvider;
        private readonly ICompilerFactory _compilerFactory;
        private readonly ISourceFileGenerator _sourceFileGenerator;
        private readonly ICodeMetadataGeneratorService _codeMetadataGenerator;
        private readonly Policy _sourceFilesRepositoryPolicy;
        private readonly Policy _calculationsRepositoryPolicy;

        public SourceCodeService(
            ISourceFileRepository sourceFilesRepository,
            ILogger logger,
            ICalculationsRepository calculationsRepository,
            ISourceFileGeneratorProvider sourceFileGeneratorProvider,
            ICompilerFactory compilerFactory,
            ICodeMetadataGeneratorService codeMetadataGenerator,
            ICalcsResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(sourceFilesRepository, nameof(sourceFilesRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(calculationsRepository, nameof(calculationsRepository));
            Guard.ArgumentNotNull(sourceFileGeneratorProvider, nameof(sourceFileGeneratorProvider));
            Guard.ArgumentNotNull(compilerFactory, nameof(compilerFactory));
            Guard.ArgumentNotNull(codeMetadataGenerator, nameof(codeMetadataGenerator));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));

            _sourceFilesRepository = sourceFilesRepository;
            _logger = logger;
            _calculationsRepository = calculationsRepository;
            _sourceFileGeneratorProvider = sourceFileGeneratorProvider;
            _compilerFactory = compilerFactory;
            _sourceFileGenerator = sourceFileGeneratorProvider.CreateSourceFileGenerator(TargetLanguage.VisualBasic);
            _codeMetadataGenerator = codeMetadataGenerator;
            _sourceFilesRepositoryPolicy = resiliencePolicies.SourceFilesRepository;
            _calculationsRepositoryPolicy = resiliencePolicies.CalculationsRepository;
        }

        public async Task SaveAssembly(BuildProject buildProject)
        {
            Guard.ArgumentNotNull(buildProject, nameof(buildProject));

            if (buildProject.Build != null && !buildProject.Build.Assembly.IsNullOrEmpty())
            {
                try
                {
                    await _sourceFilesRepositoryPolicy.ExecuteAsync(() => _sourceFilesRepository.SaveAssembly(buildProject.Build.Assembly, buildProject.SpecificationId));

                    _logger.Information($"Saved assembly for specification id: '{buildProject.SpecificationId}'");
                }
                catch (Exception ex)
                {
                    string message = $"Failed to save assembly for specification id '{buildProject.SpecificationId}'";

                    _logger.Error(ex, message);

                    throw;
                }
            }
            else
            {
                string message = $"Assembly not present on build project for specification id: '{buildProject.SpecificationId}'";
                _logger.Error(message);

                throw new ArgumentException(message);
            }
        }

        public async Task<byte[]> GetAssembly(BuildProject buildProject)
        {
            Guard.ArgumentNotNull(buildProject, nameof(buildProject));

            byte[] rawAssembly = null;

            bool assemblyExists = await _sourceFilesRepositoryPolicy.ExecuteAsync(() => _sourceFilesRepository.DoesAssemblyExist(buildProject.SpecificationId));

            if (!assemblyExists)
            {
                if (buildProject.Build.Assembly.IsNullOrEmpty())
                {
                    IEnumerable<Calculation> calculations = await _calculationsRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.GetCalculationsBySpecificationId(buildProject.SpecificationId));

                    buildProject.Build = Compile(buildProject, calculations);
                }

                rawAssembly = buildProject.Build.Assembly;
            }
            else
            {
                Stream stream = await _sourceFilesRepositoryPolicy.ExecuteAsync(() => _sourceFilesRepository.GetAssembly(buildProject.SpecificationId));

                if (stream == null)
                {
                    string message = $"Failed to get assembly for specification id: '{buildProject.SpecificationId}'";
                    _logger.Error(message);

                    throw new Exception(message);
                }

                rawAssembly = stream.ReadAllBytes();
            }

            return rawAssembly;
        }

        public Build Compile(BuildProject buildProject, IEnumerable<Calculation> calculations, CompilerOptions compilerOptions = null)
        {
            try
            {
                if (compilerOptions == null)
                {
                    compilerOptions = new CompilerOptions { SpecificationId = buildProject.SpecificationId, OptionStrictEnabled = false };
                }

                IEnumerable<SourceFile> sourceFiles = GenerateSourceFiles(buildProject, calculations, compilerOptions);

                ICompiler compiler = _compilerFactory.GetCompiler(sourceFiles);

                return compiler.GenerateCode(sourceFiles?.ToList());
            }
            catch (Exception e)
            {
                return new Build { CompilerMessages = new List<CompilerMessage> { new CompilerMessage { Message = e.Message, Severity = Severity.Error } } };
            }
        }

        public IEnumerable<SourceFile> GenerateSourceFiles(BuildProject buildProject, IEnumerable<Calculation> calculations, CompilerOptions compilerOptions)
        {
            Guard.ArgumentNotNull(buildProject, nameof(buildProject));
            Guard.ArgumentNotNull(calculations, nameof(calculations));
            Guard.ArgumentNotNull(compilerOptions, nameof(compilerOptions));

            return _sourceFileGenerator.GenerateCode(buildProject, calculations, compilerOptions);
        }

        public async Task<IEnumerable<TypeInformation>> GetTypeInformation(BuildProject buildProject)
        {
            byte[] rawAssembly = await GetAssembly(buildProject);

            return _codeMetadataGenerator.GetTypeInformation(rawAssembly);
        }

        public IDictionary<string, string> GetCalculationFunctions(IEnumerable<SourceFile> sourceFiles)
        {
            ICompiler compiler = _compilerFactory.GetCompiler(sourceFiles);

            return compiler.GetCalculationFunctions(sourceFiles);
        }

        public async Task SaveSourceFiles(IEnumerable<SourceFile> sourceFiles, string specificationId, SourceCodeType sourceCodeType)
        {
            Guard.ArgumentNotNull(sourceFiles, nameof(sourceFiles));
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            string sourceType;
            switch (sourceCodeType)
            {
                case SourceCodeType.Release:
                    sourceType = "release";
                    break;
                case SourceCodeType.Preview:
                    sourceType = "preview";
                    break;
                case SourceCodeType.Diagnostics:
                    sourceType = "diagnostics";
                    break;
                default:
                    throw new InvalidOperationException("Unknown SourceCodeType");
            }

            IEnumerable<(string filename, string content)> files = sourceFiles.Select(m => (m.FileName, m.SourceCode));

            byte[] compressedFiles = ZipUtils.ZipFiles(files);

            if (compressedFiles.IsNullOrEmpty())
            {
                _logger.Error($"Failed to compress source files for specification id: '{specificationId}'");
            }
            else
            {
                await _sourceFilesRepositoryPolicy.ExecuteAsync(() => _sourceFilesRepository.SaveSourceFiles(compressedFiles, specificationId, sourceType));
            }
        }

        public async Task DeleteAssembly(string specificationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            await _sourceFilesRepositoryPolicy.ExecuteAsync(() => _sourceFilesRepository.DeleteAssembly(specificationId));
        }
    }
}
