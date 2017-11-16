using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Allocations.Boostrapper;
using Allocations.Models;
using Allocations.Repository;
using Newtonsoft.Json;
using Allocations.Models.Datasets;
using Allocations.Models.Results;
using Allocations.Models.Specs;
using Allocations.Services.Calculator;
using Allocations.Services.Compiler;
using Allocations.Services.DataImporter;
using Allocations.Services.TestRunner;
using Allocations.Services.TestRunner.Vocab;
using Newtonsoft.Json.Serialization;

namespace EndToEndDemo
{

    class Program
    {
        private static JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
        static void Main(string[] args)
        {
            var importer = new DataImporterService(); 
            Task.Run(async () =>
            {
                await GenerateBudgetModel();


                foreach (var file in Directory.GetFiles("SourceData"))
                {
                    using (var stream = new FileStream(file, FileMode.Open))
                    {
                        await importer.GetSourceDataAsync(Path.GetFileName(file), stream);
                    }
                }

                var budgetDefinition = SeedData.CreateGeneralAnnualGrant();

                var compilerOutput = BudgetCompiler.GenerateAssembly(budgetDefinition);

                if (compilerOutput.Success)
                {
                    var calc = new CalculationEngine(compilerOutput);
                    await calc.GenerateAllocations();
                }
                else
                {
                    foreach (var compilerMessage in compilerOutput.CompilerMessages)
                    {
                        Console.WriteLine(compilerMessage.Message);
                    }
                    Console.ReadKey();
                }

                await StoreAggregates(budgetDefinition, new AllocationFactory(compilerOutput.Assembly));



            }

            // Do any async anything you need here without worry
        ). GetAwaiter().GetResult();
    }


        private static async Task GenerateBudgetModel()
        {
            using (var repository = new Repository<Budget>("specs"))
            {
                await repository.CreateAsync(SeedData.CreateGeneralAnnualGrant());
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
                                            DatasetDefinitionId = type.Name,
                                            DatasetFieldDefinitionId = propertyInfo.Name,
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
                                            DatasetDefinitionId = type.Name,
                                            DatasetFieldDefinitionId = propertyInfo.Name,
                                            Min = decimals.Min().ToString("0.00"),
                                            Max = decimals.Max().ToString("0.00"),
                                            Popular = decimals.GroupBy(x => x)
                                                .Where(x => x.Count() > 1)
                                                .OrderByDescending(x => x.Count())
                                                .Select(x => x.Key.ToString("0.00")).Take(10).ToArray()
                                        });
                                        break;

                                    case "Long":

                                        var longs = typedDatasets.Select(x => (long) propertyInfo.GetValue(x))
                                            .ToArray();

                                        aggregateFields.Add(new AggregateField
                                        {
                                            DatasetDefinitionId = type.Name,
                                            DatasetFieldDefinitionId = propertyInfo.Name,
                                            Min = longs.Min().ToString(),
                                            Max = longs.Max().ToString(),
                                            Popular = longs.GroupBy(x => x)
                                                .Where(x => x.Count() > 1)
                                                .OrderByDescending(x => x.Count())
                                                .Select(x => x.Key.ToString()).Take(10).ToArray()
                                        });
                                        break;


                                    case "DateTime":

                                        var dateTimes = typedDatasets.Select(x => (DateTime) propertyInfo.GetValue(x))
                                            .ToArray();

                                        aggregateFields.Add(new AggregateField
                                        {
                                            DatasetDefinitionId = type.Name,
                                            DatasetFieldDefinitionId = propertyInfo.Name,
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
    }

}
