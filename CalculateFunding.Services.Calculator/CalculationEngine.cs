using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Calculator.Interfaces;
using Serilog;
using CalculationResult = CalculateFunding.Models.Results.CalculationResult;

namespace CalculateFunding.Services.Calculator
{
    public class CalculationEngine : ICalculationEngine
    {
        private readonly IAllocationFactory _allocationFactory;
        private readonly ILogger _logger;

        public CalculationEngine(IAllocationFactory allocationFactory, ILogger logger)
        {
            _allocationFactory = allocationFactory;
            _logger = logger;
        }

        public IAllocationModel GenerateAllocationModel(BuildProject buildProject)
        {
            Assembly assembly = Assembly.Load(Convert.FromBase64String(buildProject.Build.AssemblyBase64));

            return _allocationFactory.CreateAllocationModel(assembly);
        }

        async public Task<IEnumerable<ProviderResult>> GenerateAllocations(BuildProject buildProject, IEnumerable<ProviderSummary> providers, Func<string, string, Task<IEnumerable<ProviderSourceDataset>>> getProviderSourceDatasets)
        {
            var assembly = Assembly.Load(Convert.FromBase64String(buildProject.Build.AssemblyBase64));

            var allocationModel = _allocationFactory.CreateAllocationModel(assembly);

            IList<ProviderResult> providerResults = new List<ProviderResult>();

            Parallel.ForEach(providers, new ParallelOptions { MaxDegreeOfParallelism = 5 }, provider =>
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                IEnumerable<ProviderSourceDataset> providerSourceDatasets = getProviderSourceDatasets(provider.Id, buildProject.Specification.Id).Result;

                if (providerSourceDatasets == null)
                {
                    providerSourceDatasets = Enumerable.Empty<ProviderSourceDataset>();
                }

                var result = CalculateProviderResults(allocationModel, buildProject, provider, providerSourceDatasets.ToList());

                providerResults.Add(result);

                stopwatch.Stop();
                _logger.Debug($"Generated result for {provider.Name} in {stopwatch.ElapsedMilliseconds}ms");
            });
            //foreach (var provider in providers)
            //{
            //    var stopwatch = new Stopwatch();
            //    stopwatch.Start();

            //    IEnumerable<ProviderSourceDataset> providerSourceDatasets = await getProviderSourceDatasets(provider.Id, buildProject.Specification.Id);

            //    var result = CalculateProviderResults(allocationModel, buildProject, provider, providerSourceDatasets.ToList());

            //    providerResults.Add(result);

            //    stopwatch.Stop();
            //    Console.WriteLine($"Generated result for ${provider.Name} in {stopwatch.ElapsedMilliseconds}ms");
            //}

            return providerResults;
        }

        public ProviderResult CalculateProviderResults(IAllocationModel model, BuildProject buildProject, ProviderSummary provider, IEnumerable<ProviderSourceDataset> providerSourceDatasets)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            IEnumerable<CalculationResult> calculationResults;
            try
            {
                calculationResults = model.Execute(providerSourceDatasets.ToList()).ToArray();
            }
            catch(Exception ex)
            {
                throw new Exception(ex.ToString());
            }

            var providerCalResults = calculationResults.ToDictionary(x => x.Calculation?.Id);
            stopwatch.Stop();
            _logger.Debug($"{providerCalResults.Count} calcs in {stopwatch.ElapsedMilliseconds}ms ({stopwatch.ElapsedMilliseconds / providerCalResults.Count: 0.0000}ms)");

            var result = new ProviderResult
            {
                Provider = provider,
                Specification = buildProject.Specification
            };
	        var plainTextBytes = System.Text.Encoding.UTF8.GetBytes($"{result.Provider.Id}-{result.Specification.Id}");
			result.Id = System.Convert.ToBase64String(plainTextBytes);
			
            var productResults = new List<CalculationResult>();

            foreach (var calculation in buildProject.Calculations)
            {
                var productResult = new CalculationResult
                {
                    Calculation = calculation.GetReference()
                };
                if (providerCalResults.TryGetValue(calculation.Id, out var calculationResult))
                {
                    productResult.CalculationSpecification = calculationResult.CalculationSpecification;
                    productResult.AllocationLine = calculationResult.AllocationLine;
                    productResult.PolicySpecifications = calculationResult.PolicySpecifications;
	                if (calculationResult.Value != decimal.MinValue)
	                {
		                productResult.Value = calculationResult.Value;
					}		
                    productResult.Exception = calculationResult.Exception;
                }

                productResults.Add(productResult);
                        
            }
            result.CalculationResults = productResults.ToList();
	        result.AllocationLineResults = productResults.Where(x => x.AllocationLine != null)
		        .GroupBy(x => x.AllocationLine).Select(x => new AllocationLineResult
		        {
			        AllocationLine = x.Key,
			        Value = x.Sum(v => v.Value ?? decimal.Zero)
		        }).ToList();
            return result;
        }



    }
}
