using CalculateFunding.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Calcs.Messages;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Calculator.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Core.Options;
using Microsoft.Azure.ServiceBus;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs
{
    public class BuildProjectsService : IBuildProjectsService
    {
        const int MaxPartitionSize = 100;
        const int MaxResultsCount = 1000;

        const string AllocationResultsSubscription = "calcs-events-generate-allocation-results";
        const string UpdateCosmosResultsCollection = "dataset-events-results";

        private readonly IBuildProjectsRepository _buildProjectsRepository;
        private readonly IMessengerService _messengerService;
        private readonly ServiceBusSettings _serviceBusSettings;
        private readonly ILogger _logger;
        private readonly ICalculationEngine _calculationEngine;
        private readonly IProviderResultsRepository _providerResultsRepository;
        private readonly ISpecificationRepository _specificationsRepository;

        public BuildProjectsService(IBuildProjectsRepository buildProjectsRepository, IMessengerService messengerService,
            ServiceBusSettings serviceBusSettings, ILogger logger, ICalculationEngine calculationEngine, 
            IProviderResultsRepository providerResultsRepository, ISpecificationRepository specificationsRepository)
        {
            Guard.ArgumentNotNull(buildProjectsRepository, nameof(buildProjectsRepository));
            Guard.ArgumentNotNull(messengerService, nameof(messengerService));
            Guard.ArgumentNotNull(serviceBusSettings, nameof(serviceBusSettings));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(calculationEngine, nameof(calculationEngine));
            Guard.ArgumentNotNull(providerResultsRepository, nameof(providerResultsRepository));
            Guard.ArgumentNotNull(specificationsRepository, nameof(specificationsRepository));

            _buildProjectsRepository = buildProjectsRepository;
            _messengerService = messengerService;
            _serviceBusSettings = serviceBusSettings;
            _logger = logger;
            _calculationEngine = calculationEngine;
            _providerResultsRepository = providerResultsRepository;
            _specificationsRepository = specificationsRepository;
        }

        public async Task UpdateAllocations(Message message)
        {
            GenerateAllocationsResultsMessage generateAllocationsResultsMessage = message.GetPayloadAsInstanceOf<GenerateAllocationsResultsMessage>();

            if (generateAllocationsResultsMessage == null)
            {
                _logger.Error("A null generate allocations message was provided to UpdateAllocations");

                throw new ArgumentNullException(nameof(generateAllocationsResultsMessage));
            }
            
            if(generateAllocationsResultsMessage.BuildProject == null)
            {
                return;
            }

            if (generateAllocationsResultsMessage.ProviderSummaries.IsNullOrEmpty())
            {
                return;
            }

            IEnumerable<ProviderResult> results = _calculationEngine.GenerateAllocations(generateAllocationsResultsMessage.BuildProject, 
                generateAllocationsResultsMessage.ProviderSummaries).ToList();

            //IEnumerable<UpdateProviderResultsModel> updateModels = results.Select(m => new UpdateProviderResultsModel { AllocationLineResults = m.AllocationLineResults, CalculationResults = m.CalculationResults, Id = m.Id });

            IDictionary<string, string> properties = message.BuildMessageProperties();

            await _messengerService.SendAsync(_serviceBusSettings.DatasetsServiceBusTopicName, UpdateCosmosResultsCollection, results, properties);
        }

        public async Task GenerateAllocationsInstruction(Message message)
        {
            InstructGenerateAllocationsMessage generateAllocationsMessage = message.GetPayloadAsInstanceOf<InstructGenerateAllocationsMessage>();

            if (generateAllocationsMessage == null)
            {
                _logger.Error("A null generate allocations message was provided to GenerateAllocations");

                throw new ArgumentNullException(nameof(generateAllocationsMessage));
            }

            if (string.IsNullOrWhiteSpace(generateAllocationsMessage.SpecificationId))
            {
                _logger.Error("A null or empty specification id was provided");

                throw new ArgumentNullException(nameof(generateAllocationsMessage.SpecificationId));
            }

            string specificationId = generateAllocationsMessage.SpecificationId;

            BuildProject buildProject = await GetBuildProjectForSpecificationId(specificationId);

            if (buildProject == null)
            {
                return;
            }

            Specification specification = await _specificationsRepository.GetSpecificationById(specificationId);

            if(specification == null)
            {
                _logger.Error($"Failed to find specification for specification id: {specificationId}");

                return;
            }

            HttpStatusCode statusCode = await UpdateBuildProject(buildProject, specification);

            if (!statusCode.IsSuccess())
            {
                _logger.Error($"Failed to find update build project with build project id: {buildProject.Id} with status code: {statusCode.ToString()}");

                return;
            }

            await SendGenerateAllocationMessages(buildProject, message);
        }

        async Task<BuildProject> GetBuildProjectForSpecificationId(string specificationId)
        {
            BuildProject buildProject = await _buildProjectsRepository.GetBuildProjectBySpecificationId(specificationId);

            if (buildProject == null)
            {
                _logger.Error($"Failed to find build project for specification id: {specificationId}");

                return null;
            }

            if (buildProject.Build == null || string.IsNullOrWhiteSpace(buildProject.Build.AssemblyBase64))
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

        async Task<IEnumerable<ProviderSummary>> GetProviderSummaries(int pageNumber, int top = 50)
        {
            ProviderSearchResults providers = await _providerResultsRepository.SearchProviders(new SearchModel
            {
                PageNumber = pageNumber,
                Top = top,
                IncludeFacets = false
            });

            IEnumerable<ProviderSearchResult> searchResults = providers.Results;

            return searchResults.Select(x => new ProviderSummary
                {
                    Name = x.Name,
                    Id = x.UKPRN,
                    UKPRN = x.UKPRN,
                    URN = x.URN,
                    Authority = x.Authority,
                    UPIN = x.UPIN,
                    ProviderSubType = x.ProviderSubType,
                    EstablishmentNumber = x.EstablishmentNumber,
                    ProviderType = x.ProviderType
                });
        }

        async Task<int> GetTotalCount()
        {
            ProviderSearchResults providers = await _providerResultsRepository.SearchProviders(new SearchModel
            {
                PageNumber = 1,
                Top = 1,
                IncludeFacets = false
            });

            return providers.TotalCount;
        }

        int GetPageCount(int totalCount)
        {
            int pageCount = totalCount / MaxResultsCount;

            if (pageCount % MaxResultsCount != 0)
                pageCount += 1;

            return pageCount;
        }

        async Task SendGenerateAllocationMessages(BuildProject buildProject, Message message)
        {
            int totalCount = await GetTotalCount();

            int pageCount = GetPageCount(totalCount);

            IDictionary<string, string> properties = message.BuildMessageProperties();

            List<ProviderSummary> allProvidersFromSearch = new List<ProviderSummary>(totalCount);

            IList<Task> messageTasks = new List<Task>();

            for (int pageNumber = 1; pageNumber <= pageCount; pageNumber++)
            {
                IEnumerable<ProviderSummary> providersFromSearch = (await GetProviderSummaries(pageNumber, 1000)).ToList();

                int itemCount = providersFromSearch.Count();

                for (int partitionIndex = 0; partitionIndex < itemCount; partitionIndex += MaxPartitionSize)
                {
                    IEnumerable<ProviderSummary> partitionedSummaries = providersFromSearch.Skip(partitionIndex).Take(MaxPartitionSize).ToList();

                    await _messengerService.SendAsync(_serviceBusSettings.CalcsServiceBusTopicName, AllocationResultsSubscription,
                            new GenerateAllocationsResultsMessage { ProviderSummaries = partitionedSummaries, BuildProject = buildProject },
                            properties);
                }
            }
        }
    }
}
