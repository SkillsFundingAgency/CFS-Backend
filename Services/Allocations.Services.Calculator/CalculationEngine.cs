using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Allocations.Models;
using Allocations.Models.Datasets;
using Allocations.Models.Results;
using Allocations.Repository;
using Allocations.Services.Compiler;
using Allocations.Services.TestRunner;
using Allocations.Services.TestRunner.Vocab;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Allocations.Services.Calculator
{
    public class CalculationEngine
    {
        private readonly AllocationFactory _allocationFactory;
        private readonly BudgetCompilerOutput _compilerOutput;

        public CalculationEngine(BudgetCompilerOutput compilerOutput)
        {
            _compilerOutput = compilerOutput;
            _allocationFactory = new AllocationFactory(_compilerOutput.Assembly);
        }

        public async Task GenerateAllocations()
        {

            using (var repository = new Repository<ProviderSourceDataset>("datasets"))
            {

                var datasetsByUrn = repository.Query().Where(x => x.BudgetId == _compilerOutput.Budget.Id).ToArray().GroupBy(x => x.ProviderUrn);

                foreach (var urn in datasetsByUrn)
                {
                    var typedDatasets = new List<object>();

                    string providerName = urn.Key;
                    var datasets = urn.ToArray();
                    foreach (var dataset in datasets)
                    {
                       
                        var type = _allocationFactory.GetDatasetType(dataset.DatasetName);
                        var nameField = type.GetProperty("PoviderName");
                        if (nameField != null)
                        {
                            providerName = nameField.GetValue(dataset)?.ToString();
                        }

                        var datasetAsJson = repository.QueryAsJson($"SELECT * FROM ds WHERE ds.id='{dataset.Id}' AND ds.deleted = false").First();


                        object blah = JsonConvert.DeserializeObject(datasetAsJson, type, new JsonSerializerSettings{ContractResolver = new CamelCasePropertyNamesContractResolver()});
                        typedDatasets.Add(blah);
                    }

                    var provider = new Reference(urn.Key, providerName);
                    var result = CalculateProviderProducts(provider, typedDatasets);
                    var testResult = RunProviderTests(provider, typedDatasets, result);

                    using (var allocationRepository = new Repository<ProviderResult>("results"))
                    {
                        using (var testResultRepository = new Repository<ProviderTestResult>("results"))
                        {
                            await testResultRepository.CreateAsync(testResult);
                        }
                        await allocationRepository.CreateAsync(result);
                    }



                }
            }
        }

        public async Task<List<object>> GetProviderDatasets(Reference provider)
        {
            var typedDatasets = new List<object>();
            using (var repository = new Repository<ProviderSourceDataset>("datasets"))
            {
                var datasetsAsJson = repository.QueryAsJson($"SELECT * FROM ds WHERE ds.providerUrn='{provider.Id}' AND ds.deleted = false").ToList();

                foreach (var datasetAsJson in datasetsAsJson)
                {
                    var dataset = JsonConvert.DeserializeObject<ProviderSourceDataset>(datasetAsJson);
                    var type = _allocationFactory.GetDatasetType(dataset.DatasetName);

                    object blah = JsonConvert.DeserializeObject(datasetAsJson, type);
                    typedDatasets.Add(blah);
                }            
            }
            return typedDatasets;
        }

        public ProviderResult CalculateProviderProducts(Reference provider, List<object> typedDatasets)
        {
            var model = _allocationFactory.CreateAllocationModel();

            var calculationResults = model.Execute(_compilerOutput.Budget.Name, typedDatasets.ToArray());

            var providerAllocations = calculationResults.ToDictionary(x => x.ProductName);

            var result = new ProviderResult
            {
                Provider = provider,
                Budget = new Reference(_compilerOutput.Budget.Id, _compilerOutput.Budget.Name),
                SourceDatasets = typedDatasets.ToArray()
            };
            var productResults = new List<ProductResult>();

            foreach (var fundingPolicy in _compilerOutput.Budget.FundingPolicies)
            {
                foreach (var allocationLine in fundingPolicy.AllocationLines)
                {
                    foreach (var productFolder in allocationLine.ProductFolders)
                    {
                        foreach (var product in productFolder.Products)
                        {
                            var productResult = new ProductResult
                            {
                                FundingPolicy = new Reference(fundingPolicy.Id, fundingPolicy.Name),
                                AllocationLine = new Reference(allocationLine.Id, allocationLine.Name),
                                ProductFolder = new Reference(productFolder.Id, productFolder.Name),
                                Product = product
                            };
                            var productIdentifier = BudgetCompiler.GetIdentitier(product.Name, _compilerOutput.Budget.TargetLanguage);
                            if (providerAllocations.ContainsKey(productIdentifier))
                            {
                                productResult.Value = providerAllocations[productIdentifier].Value;
                            }

                            productResults.Add(productResult);
                        }
                    }
                }
            }
            result.ProductResults = productResults.ToArray();
            return result;
        }

        public ProviderTestResult RunProviderTests(Reference provider, List<object> typedDatasets, ProviderResult providerResult)
        {
            var gherkinValidator = new GherkinValidator(new ProductGherkinVocabulary());
            var gherkinExecutor = new GherkinExecutor(new ProductGherkinVocabulary());

            var testResult = new ProviderTestResult
            {
                Provider = provider,
                Budget = new Reference(_compilerOutput.Budget.Id, _compilerOutput.Budget.Name),
            };
            var scenarioResults = new List<ProductTestScenarioResult>();
            foreach (var productResult in providerResult.ProductResults)
            {

                if (productResult.Product.FeatureFile != null)
                {
                    var validationErrors =
                        gherkinValidator.Validate(_compilerOutput.Budget, productResult.Product.FeatureFile).ToArray();

                    var gherkinScenarioResults =
                        gherkinExecutor.Execute(productResult, typedDatasets, productResult.Product.FeatureFile);

                    foreach (var executeResult in gherkinScenarioResults)
                    {
                        scenarioResults.Add(new ProductTestScenarioResult
                        {
                            FundingPolicy = productResult.FundingPolicy,
                            AllocationLine = productResult.AllocationLine,
                            ProductFolder = productResult.ProductFolder,
                            Product = productResult.Product,
                            ProductValue = productResult.Value,
                            ScenarioName = executeResult.ScenarioName,
                            ScenarioDescription = executeResult.ScenarioDescription,
                            TestResult =
                                executeResult.StepsExecuted < executeResult.TotalSteps
                                    ? TestResult.Ignored
                                    : executeResult.HasErrors
                                        ? TestResult.Failed
                                        : TestResult.Passed,
                            StepExected = executeResult.StepsExecuted,
                            TotalSteps = executeResult.TotalSteps,
                            DatasetReferences = executeResult.Dependencies.Select(x => new DatasetReference
                            {
                                DatasetName = x.DatasetName,
                                FieldName = x.FieldName,
                                Value = x.Value
                            }).ToArray()
                        });
                    }
                }
            }

            testResult.ScenarioResults = scenarioResults.ToArray();
            return testResult;
        }

    }
}
