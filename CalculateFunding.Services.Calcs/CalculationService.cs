
using CalculateFunding.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Exceptions;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.CodeGeneration;
using CalculateFunding.Services.Core.Extensions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Calcs.Interfaces.CodeGen;
using CalculateFunding.Services.Compiler;
using CalculateFunding.Services.Compiler.Interfaces;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Models.Code;
using CalculateFunding.Services.CodeMetadataGenerator.Interfaces;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Logging;
using Microsoft.Azure.ServiceBus;
using CalculateFunding.Services.Core.Constants;

namespace CalculateFunding.Services.Calcs
{
    public class CalculationService : ICalculationService
    {
        private readonly ICalculationsRepository _calculationsRepository;
        private readonly ILogger _logger;
        private readonly ISearchRepository<CalculationIndex> _searchRepository;
        private readonly IValidator<Calculation> _calculationValidator;
        private readonly IBuildProjectsRepository _buildProjectsRepository;
        private readonly ICompilerFactory _compilerFactory;
        private readonly ISourceFileGenerator _sourceFileGenerator;
        private readonly IMessengerService _messengerService;
        private readonly ServiceBusSettings _eventHubSettings;
        private readonly ICodeMetadataGeneratorService _codeMetadataGenerator;
        private readonly ISpecificationRepository _specsRepository;
        private readonly ITelemetry _telemetry;

        public CalculationService(
            ICalculationsRepository calculationsRepository,
            ILogger logger,
            ITelemetry telemetry,
            ISearchRepository<CalculationIndex> searchRepository,
            IValidator<Calculation> calculationValidator,
            IBuildProjectsRepository buildProjectsRepository,
            ISourceFileGeneratorProvider sourceFileGeneratorProvider,
            ICompilerFactory compilerFactory,
            IMessengerService messengerService,
            ServiceBusSettings eventHubSettings,
            ICodeMetadataGeneratorService codeMetadataGenerator,
            ISpecificationRepository specificationRepository)
        {
            Guard.ArgumentNotNull(codeMetadataGenerator, nameof(codeMetadataGenerator));
            Guard.ArgumentNotNull(specificationRepository, nameof(specificationRepository));

            _calculationsRepository = calculationsRepository;
            _logger = logger;
            _telemetry = telemetry;
            _searchRepository = searchRepository;
            _calculationValidator = calculationValidator;
            _buildProjectsRepository = buildProjectsRepository;
            _compilerFactory = compilerFactory;
            _sourceFileGenerator = sourceFileGeneratorProvider.CreateSourceFileGenerator(TargetLanguage.VisualBasic);
            _messengerService = messengerService;
            _eventHubSettings = eventHubSettings;
            _codeMetadataGenerator = codeMetadataGenerator;
            _specsRepository = specificationRepository;
        }

        Build Compile(BuildProject buildProject)
        {
            IEnumerable<SourceFile> sourceFiles = _sourceFileGenerator.GenerateCode(buildProject);

            ICompiler compiler = _compilerFactory.GetCompiler(sourceFiles);

            return compiler.GenerateCode(sourceFiles?.ToList());
        }

        async public Task<IActionResult> GetCalculationHistory(HttpRequest request)
        {
            request.Query.TryGetValue("calculationId", out var calcId);

            var calculationId = calcId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(calculationId))
            {
                _logger.Error("No calculation Id was provided to GetCalculationHistory");

                return new BadRequestObjectResult("Null or empty calculation Id provided");
            }

            IEnumerable<CalculationVersion> history = await _calculationsRepository.GetVersionHistory(calculationId);

            if (history == null)
            {
                _logger.Information($"A calculation was not found for calculation id {calculationId}");

                return new NotFoundResult();
            }

            return new OkObjectResult(history);
        }

        async public Task<IActionResult> GetCalculationVersions(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            CalculationVersionsCompareModel compareModel = JsonConvert.DeserializeObject<CalculationVersionsCompareModel>(json);

            //Need custom validator here

            if (compareModel == null || string.IsNullOrEmpty(compareModel.CalculationId) || compareModel.Versions == null || compareModel.Versions.Count() < 2)
            {
                _logger.Warning("A null or invalid compare model was provided for comparing models");

                return new BadRequestObjectResult("A null or invalid compare model was provided for comparing models");
            }

            IEnumerable<CalculationVersion> versions = await _calculationsRepository.GetCalculationVersions(compareModel);

            if (versions == null)
            {
                _logger.Information($"A calculation was not found for calculation id {compareModel.CalculationId}");

                return new NotFoundResult();
            }

            return new OkObjectResult(versions);
        }

        async public Task<IActionResult> GetCalculationCurrentVersion(HttpRequest request)
        {
            request.Query.TryGetValue("calculationId", out var calcId);

            var calculationId = calcId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(calculationId))
            {
                _logger.Error("No calculation Id was provided to GetCalculationCurrentVersion");

                return new BadRequestObjectResult("Null or empty calculation Id provided");
            }

            Calculation calculation = await _calculationsRepository.GetCalculationById(calculationId);

            if (calculation == null)
            {
                _logger.Information($"A calculation was not found for calculation id {calculationId}");

                return new NotFoundResult();
            }

            if (calculation.Current == null)
            {
                _logger.Information($"A current calculation was not found for calculation id {calculationId}");

                return new NotFoundResult();
            }

            CalculationCurrentVersion calculationCurrentVersion = GetCurrentVersionFromCalculation(calculation);

            return new OkObjectResult(calculationCurrentVersion);
        }

        async public Task<IActionResult> GetCalculationById(HttpRequest request)
        {
            request.Query.TryGetValue("calculationId", out var calcId);

            var calculationId = calcId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(calculationId))
            {
                _logger.Error("No calculation Id was provided to GetCalculationById");

                return new BadRequestObjectResult("Null or empty calculation Id provided");
            }

            Calculation calculation = await _calculationsRepository.GetCalculationById(calculationId);

            if (calculation != null)
            {
                _logger.Information($"A calculation was found for calculation id {calculationId}");

                return new OkObjectResult(calculation);
            }

            _logger.Information($"A calculation was not found for calculation id {calculationId}");

            return new NotFoundResult();
        }

        async public Task CreateCalculation(Message message)
        {
            Reference user = message.GetUserDetails();

            Calculation calculation = message.GetPayloadAsInstanceOf<Calculation>();

            if (calculation == null)
            {
                _logger.Error("A null calculation was provided to CalculateFunding.Services.Calcs.CreateCalculation");
            }
            else
            {
                var validationResult = await _calculationValidator.ValidateAsync(calculation);

                if (!validationResult.IsValid)
                {
                    throw new InvalidModelException(GetType().ToString(), validationResult.Errors.Select(m => m.ErrorMessage).ToArraySafe());
                }

                calculation.Current = new CalculationVersion
                {
                    PublishStatus = PublishStatus.Draft,
                    Author = user,
                    Date = DateTime.UtcNow,
                    Version = 1,
                    DecimalPlaces = 6,
                    SourceCode = CodeGenerationConstants.VisualBasicDefaultSourceCode
                };

                calculation.History = new List<CalculationVersion>
                {
                    new CalculationVersion
                    {
                        PublishStatus = PublishStatus.Draft,
                        Author = user,
                        Date = DateTime.UtcNow,
                        Version = 1,
                        DecimalPlaces = 6,
                        SourceCode = CodeGenerationConstants.VisualBasicDefaultSourceCode
                    }
                };


                HttpStatusCode result = await _calculationsRepository.CreateDraftCalculation(calculation);

                if (result.IsSuccess())
                {
                    _logger.Information($"Calculation with id: {calculation.Id} was succesfully saved to Cosmos Db");

                    await UpdateSearch(calculation);

                    await UpdateBuildProject(calculation.Specification);
                }
                else
                {
                    _logger.Error($"There was problem creating a new calculation with id {calculation.Id} in Cosmos Db with status code {(int)result}");
                }
            }
        }

        async public Task<IActionResult> SaveCalculationVersion(HttpRequest request)
        {
            request.Query.TryGetValue("calculationId", out var calcId);

            string calculationId = calcId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(calculationId))
            {
                _logger.Error("No calculation Id was provided to GetCalculationHistory");

                return new BadRequestObjectResult("Null or empty calculation Id provided");
            }

            string json = await request.GetRawBodyStringAsync();

            SaveSourceCodeVersion sourceCodeVersion = JsonConvert.DeserializeObject<SaveSourceCodeVersion>(json);

            if (sourceCodeVersion == null || string.IsNullOrWhiteSpace(sourceCodeVersion.SourceCode))
            {
                _logger.Error($"Null or empty source code was provided for calculation id {calculationId}");

                return new BadRequestObjectResult("Null or empty calculation Id provided");
            }

            Reference user = request.GetUser();

            Calculation calculation = await _calculationsRepository.GetCalculationById(calculationId);

            if (calculation == null)
            {
                _logger.Error($"A calculation was not found for calculation id {calculationId}");

                return new NotFoundResult();
            }

            if (calculation.History.IsNullOrEmpty())
            {
                _logger.Information($"History for {calculationId} was null or empty and needed recreating.");
                calculation.History = new List<CalculationVersion>();
            }

            int nextVersionNumber = GetNextVersionNumberFromCalculationVersions(calculation.History);

            if (calculation.Current == null)
            {
                _logger.Warning($"Current for {calculationId} was null and needed recreating.");
                calculation.Current = new CalculationVersion();
            }
            calculation.Current.SourceCode = sourceCodeVersion.SourceCode;

            CalculationVersion newVersion = new CalculationVersion
            {
                Version = nextVersionNumber,
                Author = user,
                Date = DateTime.UtcNow,
                DecimalPlaces = 6,
                PublishStatus = (calculation.Current.PublishStatus == PublishStatus.Published
                                    || calculation.Current.PublishStatus == PublishStatus.Updated)
                                    ? PublishStatus.Updated : PublishStatus.Draft,
                SourceCode = sourceCodeVersion.SourceCode
            };

            calculation.Current = newVersion;

            calculation.History.Add(newVersion);

            HttpStatusCode statusCode = await _calculationsRepository.UpdateCalculation(calculation);

            BuildProject buildProject = await UpdateBuildProject(calculation.Specification);

            await UpdateSearch(calculation);

            CalculationCurrentVersion currentVersion = GetCurrentVersionFromCalculation(calculation);

            await SendGenerateAllocationsMessage(buildProject, request);

            _telemetry.TrackEvent("InstructCalculationAllocationEventRun",
               new Dictionary<string, string>()
               {
                        { "specificationId" , buildProject.Specification.Id },
                        { "buildProjectId" , buildProject.Id },
                        { "calculationId" , calculationId }
               },
               new Dictionary<string, double>()
               {
                    { "InstructCalculationAllocationEventRunCalc" , 1 },
                    { "InstructCalculationAllocationEventRun" , 1 }
               }
           );

            return new OkObjectResult(currentVersion);
        }

        async public Task<IActionResult> PublishCalculationVersion(HttpRequest request)
        {
            request.Query.TryGetValue("calculationId", out var calcId);

            var calculationId = calcId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(calculationId))
            {
                _logger.Error("No calculation Id was provided to PublishCalculationVersion");

                return new BadRequestObjectResult("Null or empty calculation Id provided");
            }

            Calculation calculation = await _calculationsRepository.GetCalculationById(calculationId);

            if (calculation == null)
            {
                _logger.Information($"A calculation was not found for calculation id {calculationId}");

                return new NotFoundResult();
            }

            if (calculation.Current == null)
            {
                _logger.Information($"A current calculation was not found for calculation id {calculationId}");

                return new NotFoundResult();
            }

            if (calculation.Current.PublishStatus != PublishStatus.Published)
            {
                calculation.Current.PublishStatus = PublishStatus.Published;

                calculation.Published = calculation.Current;

                await _calculationsRepository.UpdateCalculation(calculation);

                await UpdateBuildProject(calculation.Specification);

                await UpdateSearch(calculation);
            }

            return new OkObjectResult(calculation.Current);
        }

        public async Task<IActionResult> GetCalculationCodeContext(HttpRequest request)
        {
            request.Query.TryGetValue("specificationId", out var specId);

            var specificationId = specId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specificationId was provided to GetCalculationCodeContext");

                return new BadRequestObjectResult("Null or empty specificationId provided");
            }

            _logger.Information("Generating code context for {specificationId}", specificationId);

            BuildProject project = await _buildProjectsRepository.GetBuildProjectBySpecificationId(specificationId);
            if (project == null)
            {
                _logger.Error($"Build Project was null for Specification {specificationId}");

                return new StatusCodeResult(500);
            }

            if (project.Build == null)
            {
                _logger.Error($"Build was null for Specification {specificationId} with Build Project ID {project.Id}");

                return new StatusCodeResult(500);
            }

            if (project.Build.AssemblyBase64 == null)
            {
                _logger.Error($"Build AssemblyBase64 was null for Specification {specificationId} with Build Project ID {project.Id}");

                return new StatusCodeResult(500);
            }

            if (project.Build.AssemblyBase64.Length == 0)
            {
                _logger.Error($"Build AssemblyBase64 was zero bytes for Specification {specificationId} with Build Project ID {project.Id}");

                return new StatusCodeResult(500);
            }

            byte[] rawAssembly = Convert.FromBase64String(project.Build.AssemblyBase64);

            IEnumerable<TypeInformation> result = _codeMetadataGenerator.GetTypeInformation(rawAssembly);

            return new OkObjectResult(result);
        }

        async Task UpdateSearch(Calculation calculation)
        {
            IEnumerable<IndexError> indexingResults = await _searchRepository.Index(new List<CalculationIndex>
            {
                new CalculationIndex
                {
                    Id = calculation.Id,
                    Name = calculation.Name,
                    CalculationSpecificationId = calculation.CalculationSpecification.Id,
                    CalculationSpecificationName = calculation.CalculationSpecification.Name,
                    SpecificationName = calculation.Specification.Name,
                    SpecificationId = calculation.Specification.Id,
                    PeriodId = calculation.Period.Id,
                    PeriodName = calculation.Period.Name,
                    AllocationLineId = calculation.AllocationLine?.Id,
                    AllocationLineName = calculation.AllocationLine?.Name,
                    PolicySpecificationIds   = calculation.Policies.Select(m => m.Id).ToArraySafe(),
                    PolicySpecificationNames = calculation.Policies.Select(m => m.Name).ToArraySafe(),
                    SourceCode = calculation.Current.SourceCode,
                    Status = calculation.Current.PublishStatus.ToString(),
                    FundingStreamId = calculation.FundingStream.Id,
                    FundingStreamName = calculation.FundingStream.Name,
                    LastUpdatedDate = DateTimeOffset.Now,
                }
            });
        }

        async Task<BuildProject> CreateBuildProject(SpecificationSummary specification, List<Calculation> calculations)
        {
            BuildProject buildproject = new BuildProject
            {
                Calculations = calculations,
                Specification = specification,
                Id = Guid.NewGuid().ToString(),
                Name = specification.Name
            };

            buildproject.Build = Compile(buildproject);

            await _buildProjectsRepository.CreateBuildProject(buildproject);

            return buildproject;
        }

        async Task<BuildProject> UpdateBuildProject(SpecificationSummary specification)
        {
            Task<IEnumerable<Calculation>> calculationsRequest = _calculationsRepository.GetCalculationsBySpecificationId(specification.Id);
            Task<BuildProject> buildProjectRequest = _buildProjectsRepository.GetBuildProjectBySpecificationId(specification.Id);
            Task<IEnumerable<Models.Specs.Calculation>> calculationSpecificationsRequest = _specsRepository.GetCalculationSpecificationsForSpecification(specification.Id);

            await TaskHelper.WhenAllAndThrow(calculationsRequest);

            List<Calculation> calculations = new List<Calculation>(calculationsRequest.Result);
            BuildProject buildProject = buildProjectRequest.Result;
            IEnumerable<Models.Specs.Calculation> calculationSpecifications = calculationSpecificationsRequest.Result;

            // Adds the Calculation Description retreived from the Calculation Specification.
            // Other descriptions are included as part of the denormalised data storage in CosmosDB
            foreach(Models.Specs.Calculation specCalculation in calculationSpecifications)
            {
                Calculation calculation = calculations.Where(c => c.CalculationSpecification.Id == specCalculation.Id).FirstOrDefault();
                if (calculation != null)
                {
                    calculation.Description = specCalculation.Description;
                }
            }

            if (buildProject == null)
            {
                _logger.Warning($"Build project for specification {specification.Id} could not be found, creating a new one");

                buildProject = await CreateBuildProject(specification, calculations);
            }
            else
            {
                buildProject.Calculations = calculations;
                buildProject.Build = Compile(buildProject);
                await _buildProjectsRepository.UpdateBuildProject(buildProject);
            }

            return buildProject;
        }

        int GetNextVersionNumberFromCalculationVersions(IEnumerable<CalculationVersion> versions)
        {
            if (!versions.Any())
                return 1;

            int maxVersion = versions.Max(m => m.Version);

            return maxVersion + 1;
        }

        CalculationCurrentVersion GetCurrentVersionFromCalculation(Calculation calculation)
        {
            CalculationCurrentVersion calculationCurrentVersion = new CalculationCurrentVersion
            {
                SpecificationId = calculation.Specification?.Id,
                Author = calculation.Current?.Author,
                Date = calculation.Current?.Date,
                CalculationSpecification = calculation.CalculationSpecification,
                PeriodName = calculation.Period.Name,
                Id = calculation.Id,
                Name = calculation.Name,
                Status = calculation.Current?.PublishStatus.ToString(),
                SourceCode = calculation.Current?.SourceCode ?? CodeGenerationConstants.VisualBasicDefaultSourceCode,
                Version = calculation.Current.Version
            };

            return calculationCurrentVersion;
        }

        Task SendGenerateAllocationsMessage(BuildProject buildProject, HttpRequest request)
        {
            IDictionary<string, string> properties = CreateMessageProperties(request);

            properties.Add("specification-id", buildProject.Specification.Id);

            return _messengerService.SendToQueue(ServiceBusConstants.QueueNames.CalculationJobInitialiser,
                buildProject,
                properties);
        }

        IDictionary<string, string> CreateMessageProperties(HttpRequest request)
        {
            Reference user = request.GetUser();

            IDictionary<string, string> properties = new Dictionary<string, string>();
            properties.Add("sfa-correlationId", request.GetCorrelationId());

            if (user != null)
            {
                properties.Add("user-id", user.Id);
                properties.Add("user-name", user.Name);
            }

            return properties;
        }


    }
}
