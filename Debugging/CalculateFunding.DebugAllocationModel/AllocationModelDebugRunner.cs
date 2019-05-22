using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Calculator;
using CalculateFunding.Services.Calculator.Interfaces;
using CalculateFunding.Services.Providers.Interfaces;
using Serilog;

namespace CalculateFunding.DebugAllocationModel
{
    public class AllocationModelDebugRunner
    {
        private readonly ILogger _logger;
        private readonly IFeatureToggle _featureToggles;
        private readonly IProviderSourceDatasetsRepository _providerSourceDatasetsRepository;
        private readonly IProviderService _providerService;

        public AllocationModelDebugRunner(ILogger logger, IFeatureToggle featureToggles, IProviderSourceDatasetsRepository providerSourceDatasetsRepository, IProviderService providerService)
        {
            _logger = logger;
            _featureToggles = featureToggles;
            _providerSourceDatasetsRepository = providerSourceDatasetsRepository;
            _providerService = providerService;
        }

        public async Task<(IEnumerable<CalculationResult>, long)> Execute(string specificationId, string providerId)
        {
            AllocationFactory allocationFactory = new AllocationFactory(_logger, _featureToggles);

            IAllocationModel allocationModel = allocationFactory.CreateAllocationModel(typeof(Calculations).Assembly);

            IEnumerable<Models.Results.ProviderSourceDataset> providerSourceDatasetsResult = await _providerSourceDatasetsRepository.GetProviderSourceDatasetsByProviderIdsAndSpecificationId(new string[] { providerId }, specificationId);

            List<ProviderSourceDataset> providerSourceDatasets = new List<ProviderSourceDataset>(providerSourceDatasetsResult);

            IEnumerable<ProviderSummary> providers = await _providerService.FetchCoreProviderData();

            ProviderSummary provider = providers.FirstOrDefault(p => p.Id == providerId);

            Stopwatch sw = Stopwatch.StartNew();

            IEnumerable<CalculationResult> calculationResults = allocationModel.Execute(providerSourceDatasets, provider, null);
            sw.Stop();

            return (calculationResults, sw.ElapsedMilliseconds);
        }
    }
}
