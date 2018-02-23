using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CalculateFunding.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Results;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using CalculationResult = CalculateFunding.Models.Results.CalculationResult;

namespace CalculateFunding.Services.Calculator
{
    public interface IDatasetProvider
    {
        //var datasetsAsJson = _repository.QueryAsJson($"SELECT * FROM ds WHERE ds.providerUrn='{provider.URN}' AND ds.deleted = false");
        IEnumerable<string> GetDatasetsAsJson(string providerId);
    }
    public class CalculationEngine
    {
        private readonly IDatasetProvider _datasetProvider = null;

        public IEnumerable<ProviderResult> GenerateAllocations(BuildProject buildProject, IEnumerable<ProviderSummary> providers)
        {
            var assembly = Assembly.Load(Convert.FromBase64String(buildProject.Build.AssemblyBase64));
            var allocationFactory = new AllocationFactory(assembly);
            var allocationModel = allocationFactory.CreateAllocationModel();

                foreach (var provider in providers)
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    var typedDatasets = new List<object>();


                    var datasetsAsJson = _datasetProvider?.GetDatasetsAsJson(provider.Id) ?? new string[0];
                    foreach (var datasetAsJson in datasetsAsJson)
                    {
                        
                        var type = allocationFactory.GetDatasetType(JsonConvert.DeserializeObject<ProviderSourceDataset>(datasetAsJson).DatasetName);

                        var blah = JsonConvert.DeserializeObject(datasetAsJson, type, new JsonSerializerSettings{ContractResolver = new CamelCasePropertyNamesContractResolver()});
                        typedDatasets.Add(blah);
                    }


                    var result = CalculateProviderResults(allocationModel, buildProject, provider, typedDatasets);

                stopwatch.Stop();
                Console.WriteLine($"Generated result for ${provider.Name} in {stopwatch.ElapsedMilliseconds}ms");
                    yield return result;
                   
                }
            
        }

        public async Task<List<object>> GetProviderDatasets(AllocationFactory allocationFactory, Reference provider, string budgetId)
        {
            var typedDatasets = new List<object>();
            var datasetsAsJson = _datasetProvider?.GetDatasetsAsJson(provider.Id) ?? new string[0];
            foreach (var datasetAsJson in datasetsAsJson)
                {
                    var dataset = JsonConvert.DeserializeObject<ProviderSourceDataset>(datasetAsJson);
                    var type = allocationFactory.GetDatasetType(dataset.DatasetName);

                    object blah = JsonConvert.DeserializeObject(datasetAsJson, type);
                    typedDatasets.Add(blah);
                }            
            
            return typedDatasets;
        }

        public ProviderResult CalculateProviderResults(AllocationModel model, BuildProject buildProject, ProviderSummary provider, List<object> typedDatasets)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var calculationResults = model.Execute(typedDatasets.ToArray()).ToArray();
            var providerCalResults = calculationResults.ToDictionary(x => x.Calculation?.Id);
            stopwatch.Stop();
            Console.WriteLine($"{providerCalResults.Count} calcs in {stopwatch.ElapsedMilliseconds}ms ({stopwatch.ElapsedMilliseconds / providerCalResults.Count: 0.0000}ms)");

            var result = new ProviderResult
            {
                Provider = provider,
                Specification = buildProject.Specification,
                SourceDatasets = typedDatasets.ToList()
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
