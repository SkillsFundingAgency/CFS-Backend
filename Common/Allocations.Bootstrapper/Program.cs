using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Allocations.Models;
using Allocations.Models.Results;
using Allocations.Models.Specs;
using Allocations.Repository;
using CommandLine;

namespace Allocations.Boostrapper
{
    class Options
    {
        [Option("cosmosDBConnectionString", Required = true, HelpText = @"Azure Document DB connection string")]
        public string CosmosDBConnectionString { get; set; }

        [Option("searchServiceName", Required = true, HelpText = "Azure search service name (just the name, not the full endpoint)")]
        public string SearchServiceName { get; set; }

        [Option("searchPrimaryKey", Required = true, HelpText = "Azure search service primary key")]
        public string SearchPrimaryKey { get; set; }
    }
    class Program
    {
        static int Main(string[] args)
        { 
            
            var result = Parser.Default.ParseArguments<Options>(args);
            Console.WriteLine($"Started with {args}");
            try
            {
                 Task.Run(async () =>
                {
                    try
                    {
                        var searchInitializer = new SearchInitializer(result.Value.SearchServiceName,
                            result.Value.SearchPrimaryKey, result.Value.CosmosDBConnectionString);

                        await searchInitializer.Initialise(typeof(ProductTestScenarioResultIndex));
                        await searchInitializer.Initialise(typeof(ProviderResultIndex));
                    }
                    catch (Exception e)
                    {
                        
                        Console.WriteLine(e);
                        throw;
                    }

                    Console.WriteLine("Seed budget");
                    using (var repo = new Repository<Budget>("specs", result.Value.CosmosDBConnectionString))
                    {
                        await repo.CreateAsync(SeedData.GetBudget());
                    }


                }).Wait();
                Console.WriteLine("Completed successfully");
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return -1;
            /*
             */

        }



    }

    public class SeedData
    {
        public static Budget GetBudget()
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
                                                TestScenarios = new List<ProductTestScenario>
                                                {
                                                    new ProductTestScenario
                                                    {
                                                        Name = "Only Primary providers should have Primary Rate",
                                                        GivenSteps = new List<GivenStep>
                                                        {
                                                            new GivenStep("APT Provider Information", "Phase", ComparisonOperator.EqualTo, "Primary"),
                                                            new GivenStep("Census Number Counts", "NORPrimary", ComparisonOperator.GreaterThan, "0"),
                                                            new GivenStep("APT Provider Information", "Phase", ComparisonOperator.EqualTo, "Primary"),

                                                        },
                                                        ThenSteps = new List<ThenStep>
                                                        {
                                                            new ThenStep(ComparisonOperator.GreaterThan, "0")
                                                        }
                                                    },

                                                    new ProductTestScenario
                                                    {
                                                        Name = "Non-Primary providers should not have Primary Rate",
                                                        GivenSteps = new List<GivenStep>
                                                        {
                                                            new GivenStep("APT Provider Information", "Phase", ComparisonOperator.NotEqualTo, "Primary")
                                                        },
                                                        ThenSteps = new List<ThenStep>
                                                        {
                                                            new ThenStep(ComparisonOperator.EqualTo, "2000")
                                                        }
                                                    },

                                                    new ProductTestScenario
                                                    {
                                                        Name = "Non-Primary providers should not have Primary Rate",
                                                        GivenSteps = new List<GivenStep>
                                                        {
                                                            new GivenStep("APT Provider Information", "Phase", ComparisonOperator.EqualTo, "Primary"),
                                                            new GivenStep("Census Number Counts", "NORPrimary", ComparisonOperator.GreaterThan, "0"),
                                                            new GivenStep("APT Provider Information", "Phase", ComparisonOperator.EqualTo, "Primary"),

                                                        },
                                                        ThenSteps = new List<ThenStep>
                                                        {
                                                            new ThenStep(ComparisonOperator.GreaterThan, "0")
                                                        }
                                                    }
                                                },

                                                TestProviders = new List<Reference>
                                                {
                                                    new Reference("140002", "The Blyth Academy"),
                                                    new Reference("138257", "Cramlington Village Primary School")
                                                },

                                                Calculation = new ProductCalculation
                                                {
                                                    SourceCode = @"
	Return Me.APTBasicEntitlement.PrimaryAmountPerPupil
"
                                                }
                                            },
                                            new Product
                                            {
                                                Name = "P005_PriBESubtotal",
                                                TestScenarios = new List<ProductTestScenario>
                                                {
                                                    new ProductTestScenario
                                                    {
                                                        Name = "Only Primary providers should have Primary Rate",
                                                        GivenSteps = new List<GivenStep>
                                                        {
                                                            new GivenStep("APT Provider Information", "Phase", ComparisonOperator.EqualTo, "Primary"),
                                                            new GivenStep("Census Number Counts", "NORPrimary", ComparisonOperator.GreaterThan, "0"),
                                                            new GivenStep("APT Provider Information", "Phase", ComparisonOperator.EqualTo, "Primary"),

                                                        },
                                                        ThenSteps = new List<ThenStep>
                                                        {
                                                            new ThenStep(ComparisonOperator.GreaterThan, "0")
                                                        }
                                                    },

                                                    new ProductTestScenario
                                                    {
                                                        Name = "Non-Primary providers should not have Primary Rate",
                                                        GivenSteps = new List<GivenStep>
                                                        {
                                                            new GivenStep("APT Provider Information", "Phase", ComparisonOperator.NotEqualTo, "Primary")
                                                        },
                                                        ThenSteps = new List<ThenStep>
                                                        {
                                                            new ThenStep(ComparisonOperator.EqualTo, "2000")
                                                        }
                                                    },

                                                    new ProductTestScenario
                                                    {
                                                        Name = "Non-Primary providers should not have Primary Rate",
                                                        GivenSteps = new List<GivenStep>
                                                        {
                                                            new GivenStep("APT Provider Information", "Phase", ComparisonOperator.EqualTo, "Primary"),
                                                            new GivenStep("Census Number Counts", "NORPrimary", ComparisonOperator.GreaterThan, "0"),
                                                            new GivenStep("APT Provider Information", "Phase", ComparisonOperator.EqualTo, "Primary"),

                                                        },
                                                        ThenSteps = new List<ThenStep>
                                                        {
                                                            new ThenStep(ComparisonOperator.GreaterThan, "0")
                                                        }
                                                    }
                                                },

                                                TestProviders = new List<Reference>
                                                {
                                                    new Reference("140002", "The Blyth Academy"),
                                                    new Reference("138257", "Cramlington Village Primary School")
                                                },




                                                Calculation = new ProductCalculation
                                                {
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
