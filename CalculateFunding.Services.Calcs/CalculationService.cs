
using CalculateFunding.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Exceptions;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.CodeGeneration;
using CalculateFunding.Services.Core.Extensions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Search.Models;
using Microsoft.Azure.ServiceBus;
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

	    public CalculationService(ICalculationsRepository calculationsRepository, ILogger logger,
            ISearchRepository<CalculationIndex> searchRepository, IValidator<Calculation> calculationValidator,
            IBuildProjectsRepository buildProjectsRepository, ISourceFileGeneratorProvider sourceFileGeneratorProvider, ICompilerFactory compilerFactory)
        {
            _calculationsRepository = calculationsRepository;
            _logger = logger;
            _searchRepository = searchRepository;
            _calculationValidator = calculationValidator;
            _buildProjectsRepository = buildProjectsRepository;
	        _compilerFactory = compilerFactory;
	        _sourceFileGenerator = sourceFileGeneratorProvider.CreateSourceFileGenerator(TargetLanguage.VisualBasic);
		}

	    Build Compile(BuildProject buildProject)
	    {
		    IEnumerable<SourceFile> sourceFiles = _sourceFileGenerator.GenerateCode(buildProject);

		    ICompiler compiler = _compilerFactory.GetCompiler(sourceFiles);

		    return compiler.GenerateCode(sourceFiles.ToList());

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

            if(calculation != null)
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

                if (result == HttpStatusCode.Created)
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

            if(sourceCodeVersion == null || string.IsNullOrWhiteSpace(sourceCodeVersion.SourceCode))
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

            if(calculation.Current == null)
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

            await UpdateBuildProject(calculation.Specification);

            await UpdateSearch(calculation);

            CalculationCurrentVersion currentVersion = GetCurrentVersionFromCalculation(calculation);

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

        async Task UpdateSearch(Calculation calculation)
        {
            IList<IndexError> indexingResults = await _searchRepository.Index(new List<CalculationIndex>
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
                    AllocationLineId = calculation.AllocationLine.Id,
                    AllocationLineName = calculation.AllocationLine.Name,
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

        async Task CreateBuildProject(SpecificationSummary specification, List<Calculation> calculations)
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
        }

        async Task UpdateBuildProject(SpecificationSummary specification)
        {
	        var calculations = await _calculationsRepository.GetCalculationsBySpecificationId(specification.Id);
	        var buildProject = await _buildProjectsRepository.GetBuildProjectBySpecificationId(specification.Id);

            if (buildProject == null)
            {
                _logger.Warning($"Build project for specification {specification.Id} could not be found, creating a new one");

                await CreateBuildProject(specification, calculations.ToList());
            }
            else
            {
	            buildProject.Calculations = calculations.ToList();
	            buildProject.Build = Compile(buildProject);
				await _buildProjectsRepository.UpdateBuildProject(buildProject);
            }
            
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
    }
}
