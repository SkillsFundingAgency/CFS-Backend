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
using Allocations.Services.DataImporter;
using Allocations.Services.TestRunner;
using Allocations.Services.TestRunner.Vocab;

namespace EndToEndDemo
{

    class Program
    {
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


                await GenerateAllocations();
            }

            // Do any async anything you need here without worry
        ). GetAwaiter().GetResult();
    }

    private static async Task GenerateAllocations()
        {
            using (var repository = new Repository<ProviderSourceDataset>("datasets"))
            {
                
                var budgetDefinition = await GetBudget();
                var datasetsByUrn = repository.Query().ToArray().GroupBy(x => x.ProviderUrn);
                var allocationFactory = new AllocationFactory(budgetDefinition);
                foreach (var urn in datasetsByUrn)
                {
                     var typedDatasets = new List<object>();

                    string providerName = urn.Key;
                    foreach (var dataset in urn)
                    {
                        var type = allocationFactory.GetDatasetType(dataset.DatasetName);
                        var nameField = type.GetProperty("PoviderName");
                        if (nameField != null)
                        {
                            providerName = nameField.GetValue(dataset)?.ToString();
                        }

                        var datasetAsJson = repository.QueryAsJson($"SELECT * FROM ds WHERE ds.id='{dataset.Id}' AND ds.deleted = false").First();


                        object blah = JsonConvert.DeserializeObject(datasetAsJson, type);
                        typedDatasets.Add(blah);
                    }

                    var model =
                        allocationFactory.CreateAllocationModel(budgetDefinition.Name);

   
                    var calculationResults = model.Execute(budgetDefinition.Name, urn.Key, typedDatasets.ToArray());
                    
                    var providerAllocations = calculationResults.ToDictionary(x => x.ProductName);

                    var gherkinValidator = new GherkinValidator(new ProductGherkinVocabulary());
                    var gherkinExecutor = new GherkinExecutor(new ProductGherkinVocabulary());


                    using (var allocationRepository = new Repository<ProviderResult>("results"))
                    {
                        var result = new ProviderResult
                        {
                            Provider = new Reference(urn.Key, providerName),
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
                                                gherkinExecutor.Execute(productResult, typedDatasets, product.FeatureFile);

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
                                                    TestResult = 
                                                        executeResult.StepsExecuted < executeResult.TotalSteps 
                                                        ? TestResult.Ignored
                                                        : executeResult.HasErrors 
                                                            ? TestResult.Failed 
                                                            : TestResult.Passed ,
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

        private static async Task GenerateBudgetModel()
        {
            using (var repository = new Repository<Budget>("specs"))
            {
                await repository.CreateAsync(await GetBudget());
            }


        }

        private static async Task<Budget> GetBudget()
        {
            return new Budget
            {
                Name = "GAG1718",
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
                                                Description = "",
                                                FeatureFile = "Feature: P004_PriRate/n/n" +
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
                                                Calculation = new ProductCalculation
                                                {
                                                    CalculationType = CalculationType.CSharp,
                                                    SourceCode = @"
     public partial class PupilLedFactors
    {
        public CalculationResult P004_PriRate()
        {
            return new CalculationResult(""P004_PriRate"", APTBasicEntitlement.PrimaryAmountPerPupil);

        }
    }

"
                                                }
                                            },
                                            new Product
                                            {
                                                Name = "P005_PriBESubtotal",
                                                Calculation = new ProductCalculation
                                                {
                                                    CalculationType = CalculationType.CSharp,
                                                    SourceCode = @"
     public partial class PupilLedFactors
    {
        private static readonly DateTime April2018CutOff = new DateTime(2018, 4, 1);

        public CalculationResult P005_PriBESubtotal()
        {
            if (APTProviderInformation.DateOpened > April2018CutOff)
            {
                return new CalculationResult(""P005_PriBESubtotal"", APTBasicEntitlement.PrimaryAmount);
            }

            return new CalculationResult(""P005_PriBESubtotal"", P004_PriRate().Value * CensusNumberCounts.NORPrimary);
        }
    }
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
     public partial class PupilLedFactors
    {

        public CalculationResult P006a_NSEN_PriBE_Percent()
        {
            return new CalculationResult(""P006a_NSEN_PriBE_Percent"", APTBasicEntitlement.PrimaryNotionalSEN);
        }
    }
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
     public partial class PupilLedFactors
    {
        public CalculationResult P006_NSEN_PriBE()
        {
            return new CalculationResult(""P006_NSEN_PriBE"", P006a_NSEN_PriBE_Percent().Value * P005_PriBESubtotal().Value);
        }

    }
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
                                Type = TypeCode.String
                            },
                            new DatasetFieldDefinition
                            {
                                Name = "ProviderName",
                                LongName = "Provider Name",
                                Type = TypeCode.String
                            },
                            new DatasetFieldDefinition
                            {
                                Name = "DateOpened",
                                LongName = "Date Opened",
                                Type = TypeCode.DateTime
                            },
                            new DatasetFieldDefinition
                            {
                                Name = "LocalAuthority",
                                LongName = "Local Authority",
                                Type = TypeCode.String
                            },
                            new DatasetFieldDefinition
                            {
                                Name = "Phase",
                                LongName = "Phase",
                                Type = TypeCode.String
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
                                Type = TypeCode.Decimal
                            },
                            new DatasetFieldDefinition
                            {
                                Name = "PrimaryAmount",
                                LongName = "Primary Amount",
                                Type = TypeCode.Decimal
                            },
                            new DatasetFieldDefinition
                            {
                                Name = "PrimaryNotionalSEN",
                                LongName = "Primary Notional SEN",
                                Type = TypeCode.Decimal
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
                                Type = TypeCode.Int32
                            }
                        }
                    },

                }
            };
        }
    }


}
