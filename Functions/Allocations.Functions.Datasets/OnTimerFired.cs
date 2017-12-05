using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Allocations.Models.Datasets;
using Allocations.Models.Specs;
using Allocations.Repository;
using Allocations.Services.Calculator;
using Allocations.Services.Compiler;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Allocations.Functions.Datasets
{
    public static class OnTimerFired
    {
        private static JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        [FunctionName("on-timer-fired")]
        public static async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");

            var budgets = await GetBudgets();

            foreach (var budget in budgets)
            {

                var compilerOutput = BudgetCompiler.GenerateAssembly(budget);

                var allocationFactory = new AllocationFactory(compilerOutput.Assembly);

                await StoreAggregates(budget, allocationFactory);
            }

        }

        private static async Task StoreAggregates(Budget budget, AllocationFactory allocationFactory)
        {
            using (var aggregateRepository = new Repository<AggregateDataset>("datasets"))
            {
                using (var repository = new Repository<ProviderSourceDataset>("datasets"))
                {
                    {

                        var typeNames = allocationFactory.GetDatasetTypeNames();
                        foreach (var typeName in typeNames)
                        {
                            var type = allocationFactory.GetDatasetType(typeName);
                            var datasetsAsJson = repository
                                .QueryAsJson(
                                    $"SELECT * FROM ds WHERE ds.documentType = 'ProviderSourceDataset' AND ds.budgetId = '{budget.Id}' AND ds.datasetName='{typeName}' AND ds.deleted = false")
                                .ToArray();

                            var typedDatasets = new List<object>();

                            foreach (var json in datasetsAsJson)
                            {
                                object sourceRecord =
                                    JsonConvert.DeserializeObject(json, type, _jsonSerializerSettings);

                                typedDatasets.Add(sourceRecord);
                            }

                            var StandardFields = new List<string>
                            {
                                "Id",
                                "BudgetId",
                                "ProviderUrn",
                                "ProviderName",
                                "DatasetName"
                            };
                            var aggregateFields = new List<AggregateField>();
                            foreach (var propertyInfo in type.GetProperties()
                                .Where(x => !StandardFields.Contains(x.Name)))
                            {
                                var propertyTypeName = propertyInfo.PropertyType.Name;
                                switch (propertyTypeName)
                                {
                                    case "String":

                                        var strings = typedDatasets.Select(x => propertyInfo.GetValue(x) as string)
                                            .ToArray();

                                        aggregateFields.Add(new AggregateField
                                        {
                                            DatasetDefinitionId = type.Name.Replace(" ", "-").ToLowerInvariant(),
                                            DatasetFieldDefinitionId = propertyInfo.Name.ToLowerInvariant(),
                                            Min = strings.Min(),
                                            Max = strings.Max(),
                                            Popular = strings.GroupBy(x => x)
                                                .Where(x => x.Count() > 1)
                                                .OrderByDescending(x => x.Count())
                                                .Select(x => x.Key).Take(10).ToArray()
                                        });
                                        break;

                                    case "Decimal":

                                        var decimals = typedDatasets.Select(x => (decimal)propertyInfo.GetValue(x))
                                            .ToArray();

                                        aggregateFields.Add(new AggregateField
                                        {
                                            DatasetDefinitionId = type.Name.Replace(" ", "-").ToLowerInvariant(),
                                            DatasetFieldDefinitionId = propertyInfo.Name.ToLowerInvariant(),
                                            Min = decimals.Min().ToString("0.00"),
                                            Max = decimals.Max().ToString("0.00"),
                                            Average = decimals.Average().ToString("0.00"),
                                            Popular = decimals.GroupBy(x => x)
                                                .Where(x => x.Count() > 1)
                                                .OrderByDescending(x => x.Count())
                                                .Select(x => x.Key.ToString("0.00")).Take(10).ToArray()
                                        });
                                        break;

                                    case "Long":

                                        var longs = typedDatasets.Select(x => (long)propertyInfo.GetValue(x))
                                            .ToArray();

                                        aggregateFields.Add(new AggregateField
                                        {
                                            DatasetDefinitionId = type.Name.Replace(" ", "-").ToLowerInvariant(),
                                            DatasetFieldDefinitionId = propertyInfo.Name.ToLowerInvariant(),
                                            Min = longs.Min().ToString(),
                                            Max = longs.Max().ToString(),
                                            Average = longs.Average().ToString("0.00"),
                                            Popular = longs.GroupBy(x => x)
                                                .Where(x => x.Count() > 1)
                                                .OrderByDescending(x => x.Count())
                                                .Select(x => x.Key.ToString()).Take(10).ToArray()
                                        });
                                        break;


                                    case "DateTime":

                                        var dateTimes = typedDatasets.Select(x => (DateTime)propertyInfo.GetValue(x))
                                            .ToArray();

                                        aggregateFields.Add(new AggregateField
                                        {
                                            DatasetDefinitionId = type.Name.Replace(" ", "-").ToLowerInvariant(),
                                            DatasetFieldDefinitionId = propertyInfo.Name.ToLowerInvariant(),
                                            Min = dateTimes.Min().ToString("dd/MM/yyyy"),
                                            Max = dateTimes.Max().ToString("dd/MM/yyyy"),
                                            Popular = dateTimes.GroupBy(x => x)
                                                .Where(x => x.Count() > 1)
                                                .OrderByDescending(x => x.Count())
                                                .Select(x => x.Key.ToString("dd/MM/yyyy")).Take(10).ToArray()
                                        });
                                        break;
                                }
                            }


                            var aggregateRecord = new AggregateDataset
                            {
                                BudgetId = budget.Id,
                                DatasetName = typeName,
                                AggregateFields = aggregateFields.ToArray()
                            };
                            await aggregateRepository.UpsertAsync(aggregateRecord);
                        }
                    }
                }
            }
        }

        private static async Task<List<Budget>> GetBudgets()
        {
            using (var repository = new Repository<Budget>("specs"))
            {
                return repository.Query().ToList();
            }
        }
    }
            
}
