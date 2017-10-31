using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Allocations.Models;
using Allocations.Models.Datasets;
using Allocations.Models.Framework;
using Allocations.Models.Results;
using Allocations.Models.Specs;
using Allocations.Repository;
using Allocations.Services.TestRunner;
using Allocations.Services.TestRunner.Vocab;
using AY1718.CSharp.Allocations;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;

namespace Allocations.Functions.Engine
{
    public static class OnTimerFired
    {
        [FunctionName("OnTimerFired")]
        public static async Task Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");
            await GenerateAllocations();
        }

        private static async Task GenerateAllocations()
        {
            using (var repository = new Repository<ProviderSourceDataset>("datasets"))
            {
                var modelName = "SBS1718";


                var datasetsByUrn = repository.Query().ToArray().GroupBy(x => x.ProviderUrn);
                var allocationFactory = new AllocationFactory(typeof(SBSPrimary).Assembly);
                foreach (var urn in datasetsByUrn)
                {
                    var typedDatasets = new List<object>();

                    foreach (var dataset in urn)
                    {
                        var type = allocationFactory.GetDatasetType(dataset.DatasetName);
                        var datasetAsJson = repository.QueryAsJson($"SELECT * FROM ds WHERE ds.id='{dataset.Id}' AND ds.deleted = false").First();


                        object blah = JsonConvert.DeserializeObject(datasetAsJson, type);
                        typedDatasets.Add(blah);
                    }

                    var model =
                        allocationFactory.CreateAllocationModel(modelName);

                    var budgetDefinition = await GetBudget();

                    var gherkinValidator = new GherkinValidator(new ProductGherkinVocabulary());
                    var gherkinExecutor = new GherkinExecutor(new ProductGherkinVocabulary());


                    var calculationResults = model.Execute(modelName, urn.Key, typedDatasets.ToArray());

                    var providerAllocations = calculationResults.ToDictionary(x => x.ProductName);
                    using (var allocationRepository = new Repository<ProviderResult>("results"))
                    {
                        var result = new ProviderResult
                        {
                            Provider = new Reference(urn.Key, urn.Key),
                            Budget = new Reference(budgetDefinition.Id, budgetDefinition.Name),
                            SourceDatasets = typedDatasets.ToArray()
                        };
                        var productResults = new List<ProductResult>();
                        var testResult = new ProviderTestResult
                        {
                            Provider = new Reference(urn.Key, urn.Key),
                            Budget = new Reference(budgetDefinition.Id, budgetDefinition.Name)

                        };
                        var scenarioResults = new List<ProductTestScenarioResult>();
                        foreach (var fundingPolicy in budgetDefinition.FundingPolicies)
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
                                        if (providerAllocations.ContainsKey(product.Name))
                                        {
                                            productResult.Value = providerAllocations[product.Name].Value;
                                        }

                                        if (product.FeatureFile != null)
                                        {
                                            var validationErrors = gherkinValidator.Validate(budgetDefinition, product.FeatureFile).ToArray();

                                            var executeResults =
                                                gherkinExecutor.Execute(productResult, product.FeatureFile);

                                            foreach (var executeResult in executeResults)
                                            {
                                                scenarioResults.Add(new ProductTestScenarioResult
                                                {
                                                    FundingPolicy = new Reference(fundingPolicy.Id, fundingPolicy.Name),
                                                    AllocationLine = new Reference(allocationLine.Id, allocationLine.Name),
                                                    ProductFolder = new Reference(productFolder.Id, productFolder.Name),
                                                    Product = product,
                                                    ScenarioName = executeResult.ScenarioName,
                                                    ScenarioDescription = executeResult.ScenarioDescription,
                                                    TestResult = executeResult.HasErrors ? TestResult.Failed : TestResult.Passed
                                                });
                                            }
                                        }
                                        productResults.Add(productResult);
                                    }
                                }
                            }
                        }
                        result.ProductResults = productResults.ToArray();
                        testResult.ScenarioResults = scenarioResults.ToArray();
                        using (var testResultRepository = new Repository<ProviderTestResult>("results"))
                        {
                            await testResultRepository.CreateAsync(testResult);
                        }
                        await allocationRepository.CreateAsync(result);
                    }



                }
            }
        }

        private static async Task<Budget> GetBudget()
        {
            using (var repository = new Repository<Budget>("specs"))
            {
                return await repository.ReadAsync("budget-gag1718");
            }
        }
    }
}
