using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Calcs.Messages;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Calculator.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Options;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Services.Core.Interfaces.EventHub;
using Microsoft.Azure.EventHubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using CalculateFunding.Services.Calcs.Interfaces.CodeGen;
using CalculateFunding.Services.Compiler.Interfaces;
using CalculateFunding.Services.CodeGeneration;
using CalculateFunding.Services.Compiler;

namespace CalculateFunding.Services.Calcs
{
    public class BuildProjectsService : IBuildProjectsService
    {
        const int MaxPartitionSize = 100;
        const string UpdateCosmosResultsCollection = "dataset-events-results";

        private readonly IBuildProjectsRepository _buildProjectsRepository;
        private readonly IMessengerService _messengerService;
        private readonly EventHubSettings _eventHubSettings;
        private readonly ILogger _logger;
        private readonly ICalculationEngine _calculationEngine;
        private readonly IProviderResultsRepository _providerResultsRepository;
        private readonly ISpecificationRepository _specificationsRepository;
        private readonly ICompilerFactory _compilerFactory;
        private readonly ISourceFileGeneratorProvider _sourceFileGeneratorProvider;
        private readonly ISourceFileGenerator _sourceFileGenerator;

        public BuildProjectsService(IBuildProjectsRepository buildProjectsRepository, IMessengerService messengerService,
            EventHubSettings eventHubSettings, ILogger logger, ICalculationEngine calculationEngine, 
            IProviderResultsRepository providerResultsRepository, ISpecificationRepository specificationsRepository,
            ISourceFileGeneratorProvider sourceFileGeneratorProvider,
            ICompilerFactory compilerFactory)
        {
            Guard.ArgumentNotNull(buildProjectsRepository, nameof(buildProjectsRepository));
            Guard.ArgumentNotNull(messengerService, nameof(messengerService));
            Guard.ArgumentNotNull(eventHubSettings, nameof(eventHubSettings));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(calculationEngine, nameof(calculationEngine));
            Guard.ArgumentNotNull(providerResultsRepository, nameof(providerResultsRepository));
            Guard.ArgumentNotNull(specificationsRepository, nameof(specificationsRepository));

            _buildProjectsRepository = buildProjectsRepository;
            _messengerService = messengerService;
            _eventHubSettings = eventHubSettings;
            _logger = logger;
            _calculationEngine = calculationEngine;
            _providerResultsRepository = providerResultsRepository;
            _specificationsRepository = specificationsRepository;
            _compilerFactory = compilerFactory;
            _sourceFileGeneratorProvider = sourceFileGeneratorProvider;
            _sourceFileGenerator = sourceFileGeneratorProvider.CreateSourceFileGenerator(TargetLanguage.VisualBasic);
        }

        public async Task UpdateAllocations(EventData message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            BuildProject buildProject = message.GetPayloadAsInstanceOf<BuildProject>();

            if (buildProject == null)
            {
                _logger.Error("A null build project was provided to UpdateAllocations");

                throw new ArgumentNullException(nameof(buildProject));
            }

            if (buildProject.Specification == null)
            {
                if (!message.Properties.ContainsKey("specification-id"))
                {
                    _logger.Error("Specification id key not found in message properties");

                    throw new KeyNotFoundException("Specification id key not found in message properties");
                }

                string specificationId = message.Properties["specification-id"].ToString();

                if (string.IsNullOrWhiteSpace(specificationId))
                {
                    _logger.Error($"Message does not contain a specification id");

                    throw new ArgumentNullException(nameof(specificationId));
                }

                Specification specification = await _specificationsRepository.GetSpecificationById(specificationId);

                if (specification == null)
                {
                    _logger.Error($"Failed to find specification for specification id: {specificationId}");

                    throw new ArgumentException(nameof(specification));
                }

                HttpStatusCode statusCode = await UpdateBuildProject(buildProject, specification);

                if (!statusCode.IsSuccess())
                {
                    _logger.Error($"Failed to update build project with build project id: {buildProject.Id} with status code: {statusCode.ToString()}");

                    throw new Exception($"Failed to update build project with build project id: {buildProject.Id} with status code: {statusCode.ToString()}");
                }
            }

            IEnumerable<ProviderSummary> providerSummaries = await _providerResultsRepository.GetAllProviderSummaries();

            if(providerSummaries.IsNullOrEmpty())
            {
                _logger.Error("No provider summaries found");

                throw new Exception("No provider summaries found");
            }

            Func<string, string, Task<IEnumerable<ProviderSourceDataset>>> getProviderSourceDatasetsFunc = (providerId, specificationId) =>
            {
                return _providerResultsRepository.GetProviderSourceDatasetsByProviderIdAndSpecificationId(providerId, specificationId);
            };

            IDictionary<string, string> properties = message.BuildMessageProperties();

            int itemCount = providerSummaries.Count();

            for (int partitionIndex = 0; partitionIndex < itemCount; partitionIndex += MaxPartitionSize)
            {
                IEnumerable<ProviderResult> results = (await _calculationEngine.GenerateAllocations(buildProject,
                         providerSummaries.Skip(partitionIndex).Take(MaxPartitionSize), getProviderSourceDatasetsFunc)).ToList();

                await _messengerService.SendAsync(UpdateCosmosResultsCollection, results, properties);
            }
        }

        public async Task UpdateBuildProjectRelationships(EventData message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            DatasetRelationshipSummary relationship = message.GetPayloadAsInstanceOf<DatasetRelationshipSummary>();

            if (relationship == null)
            {
                _logger.Error("A null relationship message was provided to UpdateBuildProjectRelationships");

                throw new ArgumentNullException(nameof(relationship));
            }

            if (!message.Properties.ContainsKey("specification-id"))
            {
                _logger.Error("Message properties does not contain a specification id");

                throw new KeyNotFoundException("specification-id");
            }

            string specificationId = message.Properties["specification-id"].ToString();

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error($"Message does not contain a specification id");

                throw new ArgumentNullException(nameof(specificationId));
            }

            BuildProject buildProject = await GetBuildProjectForSpecificationId(specificationId);

            if (buildProject == null)
            {
                throw new Exception($"Unable to find build project for specification id: {specificationId}");
            }

            if (buildProject.DatasetRelationships == null)
                buildProject.DatasetRelationships = new List<DatasetRelationshipSummary>();

            if(!buildProject.DatasetRelationships.Any(m => m.Name == relationship.Name))
            {
                buildProject.DatasetRelationships.Add(relationship);

                await CompileBuildProject(buildProject);
            } 
        }

        public async Task<IActionResult> GetBuildProjectBySpecificationId(HttpRequest request)
        {
            request.Query.TryGetValue("specificationId", out var specId);

            var specificationId = specId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification Id was provided to GetBuildProjectBySpecificationId");

                return new BadRequestObjectResult("Null or empty specificationId Id provided");
            }

            BuildProject buildProject = await GetBuildProjectForSpecificationId(specificationId);

            if (buildProject == null)
                return new NotFoundResult();

            return new OkObjectResult(buildProject);
        }

        async Task<BuildProject> GetBuildProjectForSpecificationId(string specificationId)
        {
            BuildProject buildProject = await _buildProjectsRepository.GetBuildProjectBySpecificationId(specificationId);

            if (buildProject == null)
            {
                _logger.Error($"Failed to find build project for specification id: {specificationId}");

                return null;
            }

            if (buildProject.Build == null)
            {
                _logger.Error($"Failed to find build project assembly for build project id: {buildProject.Id}");

                return null;
            }

            return buildProject;
        }

        Task<HttpStatusCode> UpdateBuildProject(BuildProject buildProject, Specification specification)
        {
            buildProject.Specification = new SpecificationSummary
            {
                Id = specification.Id,
                Name = specification.Name,
                FundingStream = specification.FundingStream,
                Period = specification.AcademicYear
            };

            return _buildProjectsRepository.UpdateBuildProject(buildProject);
        }

       
        async public Task CompileBuildProject(BuildProject buildProject)
        {
            buildProject.Build = Compile(buildProject);

            HttpStatusCode statusCode = await _buildProjectsRepository.UpdateBuildProject(buildProject);

            if (!statusCode.IsSuccess())
            {
                throw new Exception($"Failed to update build project for id: {buildProject.Id} with status code {statusCode.ToString()}");
            }
        }

        Build Compile(BuildProject buildProject)
        {
            IEnumerable<SourceFile> sourceFiles = _sourceFileGenerator.GenerateCode(buildProject);

            ICompiler compiler = _compilerFactory.GetCompiler(sourceFiles);

            return compiler.GenerateCode(sourceFiles?.ToList());
        }
    }
}
