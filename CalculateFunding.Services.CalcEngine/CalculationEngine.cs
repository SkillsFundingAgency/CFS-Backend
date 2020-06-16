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
using Serilog;
using FundingLine = CalculateFunding.Generators.Funding.Models.FundingLine;


namespace CalculateFunding.Services.CalcEngine
{
    public class CalculationEngine : ICalculationEngine
    {
        private readonly IAllocationFactory _allocationFactory;
        private readonly ILogger _logger;

        public CalculationEngine(IAllocationFactory allocationFactory, ILogger logger)
        {
            Guard.ArgumentNotNull(allocationFactory, nameof(allocationFactory));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _allocationFactory = allocationFactory;
            _logger = logger;
        }

        public IAllocationModel GenerateAllocationModel(Assembly assembly)
        {
            return _allocationFactory.CreateAllocationModel(assembly);
        }

        public ProviderResult CalculateProviderResults(IAllocationModel model, string specificationId, IEnumerable<CalculationSummaryModel> calculations,
            ProviderSummary provider, IEnumerable<ProviderSourceDataset> providerSourceDatasets, IDictionary<string, Funding> fundingStreamLines, IEnumerable<CalculationAggregation> aggregations = null)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            IEnumerable<CalculationResult> calculationResults = model.Execute(providerSourceDatasets != null ? providerSourceDatasets.ToList() : new List<ProviderSourceDataset>(), provider, fundingStreamLines, aggregations).ToArray();

            var providerCalcResults = calculationResults.ToDictionary(x => x.Calculation?.Id);
            stopwatch.Stop();

            if (providerCalcResults.Count > 0)
            {
                _logger.Debug($"{providerCalcResults.Count} calcs in {stopwatch.ElapsedMilliseconds}ms ({stopwatch.ElapsedMilliseconds / providerCalcResults.Count: 0.0000}ms)");
            }
            else
            {
                _logger.Information("There are no calculations to executed for specification ID {specificationId}", specificationId);
            }

            ProviderResult providerResult = new ProviderResult
            {
                Provider = provider,
                SpecificationId = specificationId
            };

            byte[] plainTextBytes = System.Text.Encoding.UTF8.GetBytes($"{providerResult.Provider.Id}-{providerResult.SpecificationId}");
            providerResult.Id = Convert.ToBase64String(plainTextBytes);

            List<CalculationResult> results = new List<CalculationResult>();

            if (calculations != null)
            {
                foreach (CalculationSummaryModel calculation in calculations)
                {
                    CalculationResult result = new CalculationResult
                    {
                        Calculation = calculation.GetReference(),
                        CalculationType = calculation.CalculationType,
                    };

                    if (providerCalcResults.TryGetValue(calculation.Id, out CalculationResult calculationResult))
                    {
                        result.Calculation.Id = calculationResult.Calculation?.Id;

                        // The default for the calculation is to return Decimal.MinValue - if this is the case, then subsitute a 0 value as the result, instead of the negative number.
                        if (calculationResult.Value != decimal.MinValue)
                        {
                            result.Value = calculationResult.Value;
                        }
                        else
                        {
                            result.Value = 0;
                        }

                        result.ExceptionType = calculationResult.ExceptionType;
                        result.ExceptionMessage = calculationResult.ExceptionMessage;
                        result.ExceptionStackTrace = calculationResult.ExceptionStackTrace;
                    }

                    results.Add(result);
                }
            }

            //we need a stable sort of results to enable the cache checks by overall SHA hash on the results json
            providerResult.CalculationResults = results.OrderBy(_ => _.Calculation.Id).ToList();

            return providerResult;
        }
    }
}
