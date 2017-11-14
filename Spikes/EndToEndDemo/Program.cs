using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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
            var b = GetBudget();



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

                var budgetDefinition = await GetBudget();

                var compilerOutput = BudgetCompiler.GenerateAssembly(budgetDefinition);

                await StoreAggregates(budgetDefinition, new AllocationFactory(compilerOutput.Assembly));

                var calc = new CalculationEngine(compilerOutput);
                await calc.GenerateAllocations();

            }

            // Do any async anything you need here without worry
        ). GetAwaiter().GetResult();
    }


        private static async Task GenerateBudgetModel()
        {
            using (var repository = new Repository<Budget>("specs"))
            {
                await repository.CreateAsync(await GetBudget());
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


        private static async Task<Budget> GetBudget()
        {
            return new Budget
            {
                Name = "GAG1718",
                TargetLanguage = TargetLanguage.VisualBasic,
                AcademicYear = "2017-2018",
                FundingStream = "General Annual Grant",

                FundingPolicies = new[]
                {
                    new FundingPolicy
                    {

                        Name = "School Block Share",
                        AllocationLines = new []
                        {
                            new AllocationLine
                            {
                                Name =  "Pupil Led Factors",
                                ProductFolders = new[]
                                {
                                    new ProductFolder
                                    {
                                        Name = "Primary",
                                        Products = new[]
                                        {
                                            new Product
                                            {
                                                Name = "P004_PriRate",
                                                Description = "This is obtained from the \'1617\' APT Proforma Dataset - Basic Entitlement Primary Amount Per Pupil",
                                                FeatureFile = "Feature: P004_PriRate\r\n" +
                                                "@mytag\r\n" +
                                                "Scenario: Only Primary providers should have Primary Rate\r\n" +
                                                "Given 'Phase' in 'APT Provider Information' is equal to 'Primary'\r\n" +
                                                "And 'NORPrimary' in 'Census Number Counts' is greater than '0'\r\n" +
                                                "Then the result should be greater than 0\r\n\r\n" +
                                                "Scenario: Only Primary providers should have Primary Rate\r\n" +
                                                "Given 'Phase' in 'APT Provider Information' is not 'Primary'\r\n" +
                                                "Then the result should be greater than '0'\r\n\r\n" +
                                                "Scenario: Primary Rate should be greater than 2000\r\n" +
                                                "Given 'Phase' in 'APT Provider Information' is 'Primary'\r\n" +
                                                "And 'NORPrimary' in 'Census Number Counts' is greater than '0'\r\n" +
                                                "Then the result should be greater than or equal to 2000",
                                                TestProviders = new []
                                                {
                                                    new Reference("140002", "The Blyth Academy"),
                                                    new Reference("138257", "Cramlington Village Primary School")
                                                },

                                                Calculation = new ProductCalculation
                                                {
                                                    CalculationType = CalculationType.CSharp,
                                                    SourceCode = @"
	Return Me.APTBasicEntitlement.PrimaryAmountPerPupil
"
                                                }
                                            },
                                            new Product
                                            {
                                                Name = "P005_PriBESubtotal",
                                                FeatureFile = "Feature: P004_PriRate\r\n" +
                                                              "@mytag\r\n" +
                                                              "Scenario: Only Primary providers should have Primary Rate\r\n" +
                                                              "Given 'Phase' in 'APT Provider Information' is equal to 'Primary'\r\n" +
                                                              "And 'NORPrimary' in 'Census Number Counts' is greater than '0'\r\n" +
                                                              "Then the result should be greater than 0\r\n\r\n" +
                                                              "Scenario: Only Primary providers should have Primary Rate\r\n" +
                                                              "Given 'Phase' in 'APT Provider Information' is not 'Primary'\r\n" +
                                                              "Then the result should be greater than '0'\r\n\r\n" +
                                                              "Scenario: Primary Rate should be greater than 2000\r\n" +
                                                              "Given 'Phase' in 'APT Provider Information' is 'Primary'\r\n" +
                                                              "And 'NORPrimary' in 'Census Number Counts' is greater than '0'\r\n" +
                                                              "Then the result should be greater than or equal to 2000",
                                                TestProviders = new []
                                                {
                                                    new Reference("140002", "The Blyth Academy"),
                                                    new Reference("138257", "Cramlington Village Primary School")
                                                },




                                                Calculation = new ProductCalculation
                                                {
                                                    CalculationType = CalculationType.CSharp,
                                                    SourceCode = @"
	Dim t As DateTime = New DateTime(2018, 4, 1)
	Dim flag As Boolean = Me.APTProviderInformation.DateOpened > t
	Dim result As Decimal
	If flag Then
		result = Me.APTBasicEntitlement.PrimaryAmount
	Else
		result = Me.P004_PriRate() * Me.CensusNumberCounts.NORPrimary
	End If
	Return result
                                                    "
                                                }
                                            },
                                            new Product
                                            {
                                                Name = "P006a_NSEN_PriBE_Percent",
                                                Calculation = new ProductCalculation
                                                {
                                                    CalculationType = CalculationType.CSharp,
                                                    SourceCode = @"
	Return Me.APTBasicEntitlement.PrimaryNotionalSEN
                                                    "
                                                }
                                            },
                                            new Product
                                            {
                                                Name = "P006_NSEN_PriBE",
                                                Calculation = new ProductCalculation
                                                {
                                                    CalculationType = CalculationType.CSharp,
                                                    SourceCode = @"
	Return Me.P006a_NSEN_PriBE_Percent() * Me.P005_PriBESubtotal()
                                                    "
                                                }
                                            },
                                        }
                                    },
                                }
                            }
                        }

                    }
                },
                DatasetDefinitions = new[]
                {
                    new DatasetDefinition
                    {
                        Name = "APT Provider Information",
                        FieldDefinitions = new []
                        {
                            new DatasetFieldDefinition
                            {
                                Name = "UPIN",
                                Type = FieldType.String
                            },
                            new DatasetFieldDefinition
                            {
                                Name = "DateOpened",
                                LongName = "Date Opened",
                                Type = FieldType.DateTime
                            },
                            new DatasetFieldDefinition
                            {
                                Name = "LocalAuthority",
                                LongName = "Local Authority",
                                Type = FieldType.String
                            },
                            new DatasetFieldDefinition
                            {
                                Name = "Phase",
                                LongName = "Phase",
                                Type = FieldType.String
                            }
                        }
                    },

                    new DatasetDefinition
                    {
                        Name = "APT Basic Entitlement",
                        FieldDefinitions = new []
                        {
                            new DatasetFieldDefinition
                            {
                                Name = "PrimaryAmountPerPupil",
                                LongName = "Primary Amount Per Pupil",
                                Type = FieldType.Decimal
                            },
                            new DatasetFieldDefinition
                            {
                                Name = "PrimaryAmount",
                                LongName = "Primary Amount",
                                Type = FieldType.Decimal
                            },
                            new DatasetFieldDefinition
                            {
                                Name = "PrimaryNotionalSEN",
                                LongName = "Primary Notional SEN",
                                Type = FieldType.Decimal
                            },
                        }
                    },

                    new DatasetDefinition
                    {
                        Name = "Census Number Counts",
                        FieldDefinitions = new []
                        {
                            new DatasetFieldDefinition
                            {
                                Name = "NOR Primary",
                                LongName = "NOR Primary",
                                Type = FieldType.Integer
                            }
                        }
                    },

                }
            };
        }
    }

}
