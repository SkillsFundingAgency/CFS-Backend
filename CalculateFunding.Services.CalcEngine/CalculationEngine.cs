using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Aggregations;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.ProviderLegacy;
using CalculateFunding.Services.CalcEngine.Interfaces;
using CalculateFunding.Services.Core.Interfaces.Logging;
using Serilog;

namespace CalculateFunding.Services.CalcEngine
{
    public class CalculationEngine : ICalculationEngine
    {
        private readonly IAllocationFactory _allocationFactory;
        private readonly ILogger _logger;
        private readonly ITelemetry _telemetry;

        public CalculationEngine(IAllocationFactory allocationFactory, ILogger logger, ITelemetry telemetry)
        {
            Guard.ArgumentNotNull(allocationFactory, nameof(allocationFactory));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(telemetry, nameof(telemetry));

            _allocationFactory = allocationFactory;
            _logger = logger;
            _telemetry = telemetry;
        }

        public IAllocationModel GenerateAllocationModel(Assembly assembly)
        {
            return _allocationFactory.CreateAllocationModel(assembly);
        }

        public ProviderResult CalculateProviderResults(
            IAllocationModel model,
            string specificationId,
            IEnumerable<CalculationSummaryModel> calculations,
            ProviderSummary provider,
            IDictionary<string, ProviderSourceDataset> providerSourceDatasets,
            IEnumerable<CalculationAggregation> aggregations = null)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            CalculationResultContainer calculationResultContainer = model.Execute(providerSourceDatasets, provider, aggregations);

            IEnumerable<CalculationResult> calculationResultItems = calculationResultContainer.CalculationResults;
            stopwatch.Stop();

            IDictionary<string, double> metrics = new Dictionary<string, double>()
                {
                    { "calculation-provider-calcsMs", stopwatch.ElapsedMilliseconds },
                    { "calculation-provider-calcsTotal", calculations.AnyWithNullCheck() ? calculations.Count() : 0 },
                    { "calculation-provider-exceptions", calculationResultItems.AnyWithNullCheck() ? calculationResultItems.Count(c=> !string.IsNullOrWhiteSpace(c.ExceptionMessage)) : 0 },
                };

            _telemetry.TrackEvent("CalculationRunProvider",
                new Dictionary<string, string>()
                {
                    { "specificationId" , specificationId },
                },
                metrics
            );

            if (calculationResultItems.AnyWithNullCheck() && calculationResultItems.Count() > 0)
            {
                _logger.Information($"Processed results for {calculationResultItems.Count()} calcs in {stopwatch.ElapsedMilliseconds}ms ({stopwatch.ElapsedMilliseconds / calculationResultItems.Count(): 0.0000}ms)");
            }
            else
            {
                _logger.Information("There are no calculations to executed for specification ID {specificationId}", specificationId);
            }

            byte[] plainTextBytes = System.Text.Encoding.UTF8.GetBytes($"{provider.Id}-{specificationId}");

            ProviderResult providerResult = new ProviderResult
            {
                Id = Convert.ToBase64String(plainTextBytes),
                Provider = provider,
                SpecificationId = specificationId
            };

            if (calculationResultItems.AnyWithNullCheck())
            {
                foreach (CalculationResult calcResult in calculationResultItems)
                {
                    CalculationSummaryModel calculationSummaryModel = calculations.First(c => c.Id == calcResult.Calculation.Id);

                    calcResult.CalculationDataType = calculationSummaryModel.CalculationValueType.ToCalculationDataType();

                    if (calcResult.CalculationDataType == CalculationDataType.Decimal && Decimal.Equals(decimal.MinValue, calcResult.Value))
                    {
                        // The default for the calculation is to return Decimal.MinValue - if this is the case, then subsitute a 0 value as the result, instead of the negative number.
                        calcResult.Value = 0;
                    }
                }
            }

            //we need a stable sort of results to enable the cache checks by overall SHA hash on the results json
            providerResult.CalculationResults = calculationResultContainer.CalculationResults?.OrderBy(_ => _.Calculation.Id).ToList();
            providerResult.FundingLineResults = calculationResultContainer.FundingLineResults.OrderBy(_ => _.FundingLine.Id).ToList();

            return providerResult;
        }
    }
}
