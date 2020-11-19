using AutoMapper;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Common.ApiClient.Providers.Models.Search;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Aggregations;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.ProviderLegacy;
using CalculateFunding.Services.CalcEngine;
using CalculateFunding.Services.CalcEngine.Interfaces;
using CalculateFunding.Services.CalcEngine.MappingProfiles;
using CalculateFunding.Services.CalcEngine.UnitTests;
using CalculateFunding.Services.Calcs.MappingProfiles;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ApiProviderSummary = CalculateFunding.Common.ApiClient.Providers.Models.ProviderSummary;

namespace CalculateFunding.Services.Calculator
{
    [TestClass]
    public class CalculationEnginePreviewServiceTests
    {
        const string specificationId = "spec1";
        const string providerId = "provider1";
        const string providerVersionId = "providerVersion1";

        private ICalculationEngine _calculationEngine;
        private IMapper _mapper;
        private IProvidersApiClient _providersApiClient;
        private ISpecificationsApiClient _specificationsApiClient;
        private IProviderSourceDatasetsRepository _providerSourceDatasetsRepository;
        private ICalculationAggregationService _calculationAggregationService;
        private ICalculationsRepository _calculationsRepository;
        private ILogger _logger;
        private ICalculatorResiliencePolicies _calculatorResiliencePolicies;

        private CalculationEnginePreviewService _calculationEnginePreviewService;


        [TestInitialize]
        public void Initialize()
        {
            _calculationEngine = Substitute.For<ICalculationEngine>();
            _providersApiClient = Substitute.For<IProvidersApiClient>();
            _specificationsApiClient = Substitute.For<ISpecificationsApiClient>();
            _providerSourceDatasetsRepository = Substitute.For<IProviderSourceDatasetsRepository>();
            _calculationAggregationService = Substitute.For<ICalculationAggregationService>();
            _calculationsRepository = Substitute.For<ICalculationsRepository>();
            _logger = Substitute.For<ILogger>();
            _mapper = CreateMapper();

            _calculatorResiliencePolicies = new CalculatorResiliencePolicies
            {
                SpecificationsApiClient = Policy.NoOpAsync(),
                ProvidersApiClient = Policy.NoOpAsync(),
                CalculationsApiClient = Policy.NoOpAsync()
            };

            _calculationEnginePreviewService = new CalculationEnginePreviewService(
                _calculationEngine,
                _providersApiClient,
                _mapper,
                _calculatorResiliencePolicies,
                _specificationsApiClient,
                _providerSourceDatasetsRepository,
                _calculationAggregationService,
                _calculationsRepository,
                _logger);
        }

        [TestMethod]
        public async Task PreviewCalculationResult_GivenCachedAggregateValuesExist_CalculateProviderResultsCallReceived()
        {
            IAllocationModel allocationModel = Substitute.For<IAllocationModel>();

            _calculationEngine
                .GenerateAllocationModel(Arg.Any<Assembly>())
                .Returns(allocationModel);

            ProviderVersionSearchResult providerVersionSearchResult = new ProviderVersionSearchResult
            {
                UKPRN = providerId
            };

            IEnumerable<string> dataDefinitionRelationshipIds = new List<string>();

            SpecificationSummary specificationSummary = new SpecificationSummary
            {
                DataDefinitionRelationshipIds = dataDefinitionRelationshipIds,
                ProviderVersionId = providerVersionId
            };

            _specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(new ApiResponse<SpecificationSummary>(HttpStatusCode.OK, specificationSummary));

            _providersApiClient
                .GetProviderByIdFromProviderVersion(Arg.Is(providerVersionId), Arg.Is(providerId))
                .Returns(new ApiResponse<ProviderVersionSearchResult>(HttpStatusCode.OK, providerVersionSearchResult));

            IEnumerable<CalculationSummaryModel> calculationSummaryModels = new List<CalculationSummaryModel>
            {
                new CalculationSummaryModel(),
                new CalculationSummaryModel()
            };

            _calculationsRepository
                .GetCalculationSummariesForSpecification(Arg.Is(specificationId))
                .Returns(calculationSummaryModels);

            Dictionary<string, ProviderSourceDataset> sourceDatasets = new Dictionary<string, ProviderSourceDataset>();

            Dictionary<string, Dictionary<string, ProviderSourceDataset>> providerSourceDatasets = new Dictionary<string, Dictionary<string, ProviderSourceDataset>>
            {
                { providerId, sourceDatasets }
            };

            _providerSourceDatasetsRepository
                .GetProviderSourceDatasetsByProviderIdsAndRelationshipIds(
                    Arg.Is(specificationId),
                    Arg.Is<IEnumerable<string>>(_ => _ != null && _.Count() == 1 && _.FirstOrDefault() == providerId),
                    Arg.Is<IEnumerable<string>>(_ => _ != null && _.SequenceEqual(dataDefinitionRelationshipIds)))
                .Returns(providerSourceDatasets);

            IEnumerable<CalculationAggregation> calculationAggregations = new List<CalculationAggregation>();

            _calculationAggregationService
                .BuildAggregations(Arg.Is<BuildAggregationRequest>(_ => _ != null && _.SpecificationId == specificationId))
                .Returns(calculationAggregations);

            IActionResult actionResult = 
                await _calculationEnginePreviewService.PreviewCalculationResult(specificationId, providerId, MockData.GetMockAssembly());

            _calculationEngine
                .Received(1)
                .CalculateProviderResults(
                    Arg.Is(allocationModel),
                    specificationId,
                    Arg.Is<IEnumerable<CalculationSummaryModel>>(_ => _.SequenceEqual(calculationSummaryModels)),
                    Arg.Is<ProviderSummary>(_ => _.UKPRN == providerId),
                    Arg.Is<Dictionary<string, ProviderSourceDataset>>(_ => _.SequenceEqual(sourceDatasets)),
                    Arg.Is<IEnumerable<CalculationAggregation>>(_ => _.SequenceEqual(calculationAggregations)));
        }

        [TestMethod]
        public async Task PreviewCalculationResult_GivenProviderSummaryNotFound_ReturnsNotFound()
        {
            IAllocationModel allocationModel = Substitute.For<IAllocationModel>();

            _calculationEngine
                .GenerateAllocationModel(Arg.Any<Assembly>())
                .Returns(allocationModel);

            IEnumerable<string> dataDefinitionRelationshipIds = new List<string>();

            SpecificationSummary specificationSummary = new SpecificationSummary
            {
                DataDefinitionRelationshipIds = dataDefinitionRelationshipIds,
                ProviderVersionId = providerVersionId
            };

            _specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(new ApiResponse<SpecificationSummary>(HttpStatusCode.OK, specificationSummary));

            _providersApiClient
                .GetProviderByIdFromProviderVersion(Arg.Is(providerVersionId), Arg.Is(providerId))
                .Returns(new ApiResponse<ProviderVersionSearchResult>(HttpStatusCode.OK, (ProviderVersionSearchResult)null));

            IActionResult actionResult =
                await _calculationEnginePreviewService.PreviewCalculationResult(specificationId, providerId, MockData.GetMockAssembly());

            actionResult
                .Should()
                .BeOfType<NotFoundResult>();
        }

        private static IMapper CreateMapper()
        {
            MapperConfiguration mapperConfig = new MapperConfiguration(c =>
            {
                c.AddProfile<CalculationsMappingProfile>();
                c.AddProfile<CalcEngineMappingProfile>();
            });

            return mapperConfig.CreateMapper();
        }
    }
}
