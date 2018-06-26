using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Options;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using CalculateFunding.Services.Calcs.Interfaces.CodeGen;
using CalculateFunding.Services.Compiler.Interfaces;
using CalculateFunding.Services.CodeGeneration;
using CalculateFunding.Services.Compiler;
using CalculateFunding.Services.Core.Interfaces.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.ServiceBus;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Caching;

namespace CalculateFunding.Services.Calcs
{
    public class BuildProjectsService : IBuildProjectsService
    {
        const int MaxPartitionSize = 1000;

        private readonly IBuildProjectsRepository _buildProjectsRepository;
        private readonly IMessengerService _messengerService;
        private readonly ServiceBusSettings _eventHubSettings;
        private readonly ILogger _logger;
        private readonly ITelemetry _telemetry;
        private readonly IProviderResultsRepository _providerResultsRepository;
        private readonly ISpecificationRepository _specificationsRepository;
        private readonly ICompilerFactory _compilerFactory;
        private readonly ISourceFileGeneratorProvider _sourceFileGeneratorProvider;
        private readonly ISourceFileGenerator _sourceFileGenerator;
        private readonly ICacheProvider _cacheProvider;
        private readonly ICalculationService _calculationService;
        private readonly ICalculationsRepository _calculationsRepository;

        public BuildProjectsService(
            IBuildProjectsRepository buildProjectsRepository,
            IMessengerService messengerService,
            ServiceBusSettings eventHubSettings,
            ILogger logger,
            ITelemetry telemetry,
            IProviderResultsRepository providerResultsRepository,
            ISpecificationRepository specificationsRepository,
            ISourceFileGeneratorProvider sourceFileGeneratorProvider,
            ICompilerFactory compilerFactory,
            ICacheProvider cacheProvider,
            ICalculationService calculationService,
            ICalculationsRepository calculationsRepository)
        {
            Guard.ArgumentNotNull(buildProjectsRepository, nameof(buildProjectsRepository));
            Guard.ArgumentNotNull(messengerService, nameof(messengerService));
            Guard.ArgumentNotNull(eventHubSettings, nameof(eventHubSettings));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(telemetry, nameof(telemetry));
            Guard.ArgumentNotNull(providerResultsRepository, nameof(providerResultsRepository));
            Guard.ArgumentNotNull(specificationsRepository, nameof(specificationsRepository));
            Guard.ArgumentNotNull(sourceFileGeneratorProvider, nameof(sourceFileGeneratorProvider));
            Guard.ArgumentNotNull(compilerFactory, nameof(compilerFactory));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(calculationService, nameof(calculationService));
            Guard.ArgumentNotNull(calculationsRepository, nameof(calculationsRepository));

            _buildProjectsRepository = buildProjectsRepository;
            _messengerService = messengerService;
            _eventHubSettings = eventHubSettings;
            _logger = logger;
            _telemetry = telemetry;
            _providerResultsRepository = providerResultsRepository;
            _specificationsRepository = specificationsRepository;
            _compilerFactory = compilerFactory;
            _sourceFileGeneratorProvider = sourceFileGeneratorProvider;
            _sourceFileGenerator = sourceFileGeneratorProvider.CreateSourceFileGenerator(TargetLanguage.VisualBasic);
            _cacheProvider = cacheProvider;
            _calculationService = calculationService;
            _calculationsRepository = calculationsRepository;
        }

        public async Task UpdateAllocations(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            IDictionary<string, string> properties = message.BuildMessageProperties();

            string specificationId = message.UserProperties["specification-id"].ToString();

            BuildProject buildProject = await _buildProjectsRepository.GetBuildProjectBySpecificationId(specificationId);

            if (buildProject == null)
            {
                _logger.Error("A null build project was provided to UpdateAllocations");

                throw new ArgumentNullException(nameof(buildProject));
            }

            if (message.UserProperties.ContainsKey("ignore-save-provider-results"))
            {
                properties.Add("ignore-save-provider-results", "true");
            }

            string cacheKey = "";

            if (message.UserProperties.ContainsKey("provider-cache-key"))
            {
                cacheKey = message.UserProperties["provider-cache-key"].ToString();
            }
            else
            {
                cacheKey = $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}";
            }

            bool summariesExist = await _cacheProvider.KeyExists<ProviderSummary>(cacheKey);

            if (!summariesExist)
            {
                await _providerResultsRepository.PopulateProviderSummariesForSpecification(specificationId);
            }

            int totalCount = (int)(await _cacheProvider.ListLengthAsync<ProviderSummary>(cacheKey));

            const string providerSummariesPartitionIndex = "provider-summaries-partition-index";

            const string providerSummariesPartitionSize = "provider-summaries-partition-size";

            properties.Add(providerSummariesPartitionSize, MaxPartitionSize.ToString());

            properties.Add("provider-cache-key", cacheKey);

            properties.Add("specification-id", specificationId);

            for (int partitionIndex = 0; partitionIndex < totalCount; partitionIndex += MaxPartitionSize)
            {

                if (properties.ContainsKey(providerSummariesPartitionIndex))
                {
                    properties[providerSummariesPartitionIndex] = partitionIndex.ToString();
                }
                else
                {
                    properties.Add(providerSummariesPartitionIndex, partitionIndex.ToString());
                }

                await _messengerService.SendToQueue<string>(ServiceBusConstants.QueueNames.CalcEngineGenerateAllocationResults, null, properties);
            }
        }

        public async Task<IActionResult> UpdateBuildProjectRelationships(HttpRequest request)
        {
            request.Query.TryGetValue("specificationId", out var specId);

            var specificationId = specId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification Id was provided to UpdateBuildProjectRelationships");

                return new BadRequestObjectResult("Null or empty specification Id provided");
            }

            string json = await request.GetRawBodyStringAsync();

            DatasetRelationshipSummary relationship = JsonConvert.DeserializeObject<DatasetRelationshipSummary>(json);

            if (relationship == null)
            {
                _logger.Error("A null relationship message was provided to UpdateBuildProjectRelationships");

                return new BadRequestObjectResult("Null relationship provided");
            }

            BuildProject buildProject = await GetBuildProjectForSpecificationId(specificationId);

            if (buildProject == null)
            {
                return new StatusCodeResult(412);
            }

            if (buildProject.DatasetRelationships == null)
                buildProject.DatasetRelationships = new List<DatasetRelationshipSummary>();

            if (!buildProject.DatasetRelationships.Any(m => m.Name == relationship.Name))
            {
                buildProject.DatasetRelationships.Add(relationship);

                await CompileBuildProject(buildProject);
            }

            return new OkObjectResult(buildProject);
        }

        public async Task UpdateBuildProjectRelationships(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            DatasetRelationshipSummary relationship = message.GetPayloadAsInstanceOf<DatasetRelationshipSummary>();

            if (relationship == null)
            {
                _logger.Error("A null relationship message was provided to UpdateBuildProjectRelationships");

                throw new ArgumentNullException(nameof(relationship));
            }

            if (!message.UserProperties.ContainsKey("specification-id"))
            {
                _logger.Error("Message properties does not contain a specification id");

                throw new KeyNotFoundException("specification-id");
            }

            string specificationId = message.UserProperties["specification-id"].ToString();

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error($"Message does not contain a specification id");

                throw new ArgumentNullException(nameof(specificationId));
            }

            BuildProject buildProject = await GetBuildProjectForSpecificationId(specificationId);

            if (buildProject == null)
            {
                SpecificationSummary specification = await _specificationsRepository.GetSpecificationSummaryById(specificationId);

                if (specification == null)
                {
                    throw new Exception($"Unable to find specification for specification id: {specificationId}");
                }

                buildProject = await _calculationService.CreateBuildProject(specificationId, Enumerable.Empty<Models.Calcs.Calculation>());

                if (buildProject == null)
                {
                    throw new Exception($"Unable to find create build project for specification id: {specificationId}");
                }
            }

            if (buildProject.DatasetRelationships == null)
                buildProject.DatasetRelationships = new List<DatasetRelationshipSummary>();

            if (!buildProject.DatasetRelationships.Any(m => m.Name == relationship.Name))
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
                SpecificationSummary specificationSummary = await _specificationsRepository.GetSpecificationSummaryById(specificationId);
                if (specificationSummary == null)
                {
                    _logger.Error($"Failed to find build project for specification id: {specificationId}");

                    return null;
                }
                else
                {
                    buildProject = await _calculationService.CreateBuildProject(specificationSummary.Id, Enumerable.Empty<Models.Calcs.Calculation>());
                }
            }

            if (buildProject.Build == null)
            {
                _logger.Error($"Failed to find build project assembly for build project id: {buildProject.Id}");

                return null;
            }

            return buildProject;
        }

        public async Task CompileBuildProject(BuildProject buildProject)
        {
            IEnumerable<Models.Calcs.Calculation> calculations = await _calculationsRepository.GetCalculationsBySpecificationId(buildProject.SpecificationId);

            buildProject.Build = Compile(buildProject, calculations);

            HttpStatusCode statusCode = await _buildProjectsRepository.UpdateBuildProject(buildProject);

            if (!statusCode.IsSuccess())
            {
                throw new Exception($"Failed to update build project for id: {buildProject.Id} with status code {statusCode.ToString()}");
            }
        }

        public async Task<IActionResult> OutputBuildProjectToFilesystem(HttpRequest request)
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

            IEnumerable<Models.Calcs.Calculation> calculations = await _calculationsRepository.GetCalculationsBySpecificationId(specificationId);

            IEnumerable<SourceFile> sourceFiles = _sourceFileGenerator.GenerateCode(buildProject, calculations);

            string sourceDirectory = @"c:\dev\vbout";
            foreach (SourceFile sourceFile in sourceFiles)
            {
                string filename = sourceDirectory + "\\" + sourceFile.FileName;
                string directory = System.IO.Path.GetDirectoryName(filename);
                if (!System.IO.Directory.Exists(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }

                System.IO.File.WriteAllText(filename, sourceFile.SourceCode);
            }


            return new OkObjectResult(buildProject);

        }

        Task<HttpStatusCode> UpdateBuildProject(BuildProject buildProject)
        {
            return _buildProjectsRepository.UpdateBuildProject(buildProject);
        }

        Build Compile(BuildProject buildProject, IEnumerable<Models.Calcs.Calculation> calculations)
        {
            IEnumerable<SourceFile> sourceFiles = _sourceFileGenerator.GenerateCode(buildProject, calculations);

            ICompiler compiler = _compilerFactory.GetCompiler(sourceFiles);

            return compiler.GenerateCode(sourceFiles?.ToList());
        }
    }
}
