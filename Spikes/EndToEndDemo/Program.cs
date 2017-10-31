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
using Allocations.Models.Framework;
using Allocations.Repository;
using AY1718.CSharp.Datasets;
using Newtonsoft.Json;
using Allocations.Models.Datasets;
using Allocations.Models.Results;
using Allocations.Models.Specs;
using Allocations.Services.Calculator;
using Allocations.Services.DataImporter;
using Allocations.Services.TestRunner;
using Allocations.Services.TestRunner.Vocab;
using AY1718.CSharp.Allocations;

namespace EndToEndDemo
{

    class Program
    {
        static void Main(string[] args)
        {
            var b = GetBudget();

            var roslyn = new RoslynDatasetAssemblyFactory();
            var assembly = roslyn.Test(b);

            foreach (var t in assembly.GetTypes())
            {
                
            }

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

                    var budgetDefinition = GetBudget();

                    var gherkinValidator = new GherkinValidator(new ProductGherkinVocabulary());


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
                                            var errors = gherkinValidator.Validate(GetBudget(), product.FeatureFile).ToArray();
                                        }
                                        productResults.Add(productResult);
                                    }
                                }
                            }
                        }
                        result.ProductResults = productResults.ToArray();
                        await allocationRepository.CreateAsync(result);
                    }



                }
            }
        }

        private static async Task GenerateBudgetModel()
        {
            var databaseName = ConfigurationManager.AppSettings["DocumentDB.DatabaseName"];

            var endpoint = new Uri(ConfigurationManager.AppSettings["DocumentDB.Endpoint"]);
            var key = ConfigurationManager.AppSettings["DocumentDB.Key"];
            using (var repository = new Repository<Budget>("specs"))
            {
                await repository.CreateAsync(GetBudget());
            }


        }

        private static Budget GetBudget()
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
                                                "And 'PrimaryNOR' in 'APT Provider Information' is greater than 0\r\n" +
                                                "Then 'P004_PriRate' should be greater than 0\r\n\r\n" +
                                                "Scenario: Only Primary providers should have Primary Rate\r\n" +
                                                "Given 'Phase' in 'APT Provider Information' is not 'Primary'\r\n" +
                                                "Then 'P004_PriRate' should be 0\r\n\r\n" +
                                                "Scenario: Primary Rate should be greater than 2000\r\n" +
                                                "Given 'Phase' in 'APT Provider Information' is 'Primary'\r\n" +
                                                "And 'PrimaryNOR' in 'APT Provider Information' is greater than 0\r\n" +
                                                "Then 'P004_PriRate' should be greater than or equal to 2000"
                                            },
                                            new Product {Name = "P005_PriBESubtotal"},
                                            new Product {Name = "P006a_NSEN_PriBE_Percent"},
                                            new Product {Name = "P006_NSEN_PriBE"},
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
                                Type = TypeCode.DateTime
                            },
                            new DatasetFieldDefinition
                            {
                                Name = "Phase",
                                LongName = "Phase",
                                Type = TypeCode.DateTime
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
