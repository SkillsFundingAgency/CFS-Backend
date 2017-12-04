using System.Collections.Generic;
using CalculateFunding.Models;
using CalculateFunding.Models.Specs;

namespace CalculateFunding.Bootstrapper
{
    public static class SeedData
    {
        public static Budget Budget(string acronym, string name, string academicYear, string fundingStream)
        {
            return new Budget
            {
                Acronym = acronym,
                Name = name,
                AcademicYear = academicYear,
                FundingStream = fundingStream,
                TargetLanguage = TargetLanguage.VisualBasic
            };
        }

        public static Budget WithFundingPolicies(this Budget budget, params FundingPolicy[] fundingPolicies)
        {
            budget.FundingPolicies = new List<FundingPolicy>(fundingPolicies);
            return budget;
        }

        public static FundingPolicy FundingPolicy(string name)
        {
            return new FundingPolicy {Name = name};


        }
        public static FundingPolicy WithAllocationLines(this FundingPolicy fundingPolicy, params AllocationLine[] allocationLines)
        {
            fundingPolicy.AllocationLines = new List<AllocationLine>(allocationLines);
            return fundingPolicy;
        }


        public static AllocationLine AllocationLine(string name)
        {
            return new AllocationLine { Name = name };
        }
        public static AllocationLine WithProductFolders(this AllocationLine allocationLine, params ProductFolder[] productFolders)
        {
            allocationLine.ProductFolders = new List<ProductFolder>(productFolders);
            return allocationLine;
        }

        public static ProductFolder ProductFolder(string name)
        {
            return new ProductFolder { Name = name };
        }
        public static ProductFolder WithProducts(this ProductFolder productFolder, params Product[] products)
        {
            productFolder.Products = new List<Product>(products);
            return productFolder;
        }

        public static Product Product(string name)
        {
            return new Product
            {
                Name = name,
                Calculation = new ProductCalculation { SourceCode= $"Throw new NotImplementedException(\"{name} is not implemented\")" },
                TestScenarios = new List<ProductTestScenario>()
            }.WithTestScenarios(
                TestScenario("Product should not have any errors")
                    .WithThenSteps(new ThenStep(TestStepType.ThenExceptionNotThrown))
                );
        }
   
        public static Product WithDescription(this Product product, string description)
        {
            product.Description = description;
            return product;
        }

        public static Product WithSBSTestProviders(this Product product)
        {
            product.TestProviders = new List<Reference>
            {
                new Reference("140002", "The Blyth Academy"),
                new Reference("138257", "Cramlington Village Primary School")
            };
            return product;
        }

        public static Product WithTestScenarios(this Product product, params ProductTestScenario[] testScenarios)
        {
            product.TestScenarios.AddRange(new List<ProductTestScenario>(testScenarios));
            return product;
        }

        public static ProductTestScenario TestScenario(string name)
        {
            return new ProductTestScenario { Name = name };
        }

        public static ProductTestScenario WithGivenSteps(this ProductTestScenario testScenario, params GivenStep[] givenSteps)
        {
            testScenario.GivenSteps = new List<GivenStep>(givenSteps);
            return testScenario;
        }

        public static ProductTestScenario WithThenSteps(this ProductTestScenario testScenario, params ThenStep[] thenSteps)
        {
            testScenario.ThenSteps = new List<ThenStep>(thenSteps);
            return testScenario;
        }

        public static Product WithCalculation(this Product product,string calculation)
        {
            product.Calculation.SourceCode = calculation?.Trim();
            return product;
        }

        public static Budget WithDatasets(this Budget budget, params DatasetDefinition[] datasetDefinitions)
        {
            budget.DatasetDefinitions = new List<DatasetDefinition>(datasetDefinitions);
            return budget;
        }

        public static DatasetDefinition Dataset(string name, string description = null)
        {
            return new DatasetDefinition { Name = name, Description = description};
        }

        public static DatasetDefinition WithFields(this DatasetDefinition datasetDefinition, params DatasetFieldDefinition[] fieldDefinitions)
        {
            datasetDefinition.FieldDefinitions = new List<DatasetFieldDefinition>(fieldDefinitions);
            return datasetDefinition;
        }

        public static DatasetFieldDefinition Field(string name, FieldType fieldType = FieldType.String, string longName = null)
        {
            return new DatasetFieldDefinition { Name = name, Type  = fieldType, LongName = longName};
        }


        public static Budget CreateGeneralAnnualGrant()
        {
            return Budget("gag1718", "General Annual Grant 17-18", "2017-2018", "General Annual Grant")
                .WithFundingPolicies(
                    FundingPolicy("School Block Share")
                        .WithAllocationLines(
                            AllocationLine("Pupil Led Factors")
                                .WithProductFolders(
                                    Primary()
                                ),
                            AllocationLine("Other Factors"),
                            AllocationLine("Exceptional Factors")                  
                            ),
                    FundingPolicy("Minimum Funding Guarantee"),
                    FundingPolicy("Post Opening Grant"),
                    FundingPolicy("Post 16 High Needs Funding")
                        )
                        .WithDatasets(
                            Dataset("APT Provider Information")
                                .WithFields(
                                    Field("UPIN"),
                                    Field("DateOpened", FieldType.DateTime, "DateOpened"),
                                    Field("LocalAuthority", longName:"Local Authority"),
                                    Field("Phase")
                                ),
                            Dataset("APT Basic Entitlement")
                                .WithFields(
                                    Field("PrimaryAmountPerPupil", FieldType.Decimal, "Primary Amount Per Pupil"),
                                    Field("PrimaryAmount", FieldType.Decimal, "Primary Amount")
                                   
                                ),
                            Dataset("Census Number Counts")
                                .WithFields(
                                    Field("NORPrimary", FieldType.Integer, "NOR Primary")
                                ),
                            Dataset("APT Local Authority")
                                .WithFields(
                                    Field("PrimaryNotionalSEN", FieldType.Decimal, "Primary Notional SEN")
                                )

                        );
        }

        private static ProductFolder Primary()
        {
            return ProductFolder("Primary")
                .WithProducts(
                    Product("P004_PriRate")
                        .WithDescription("This is obtained from the \'1718\' APT Proforma Dataset - Basic Entitlement Primary Amount Per Pupil")
                        .WithCalculation(@"Return Me.APTBasicEntitlement.PrimaryAmountPerPupil")
                        .WithSBSTestProviders()
                        .WithTestScenarios(
                            TestScenario("Only Primary providers should have Primary Rate")
                                .WithGivenSteps(
                                    new GivenStep("APT Provider Information", "Phase", ComparisonOperator.EqualTo, "Primary"),
                                    new GivenStep("Census Number Counts", "NORPrimary", ComparisonOperator.GreaterThan, "0"),
                                    new GivenStep("APT Provider Information", "Phase", ComparisonOperator.EqualTo, "Primary")
                                )
                                .WithThenSteps(new ThenStep(ComparisonOperator.GreaterThan, "0.00")
                                )
                                                        

                        ),


                    Product("P005_PriBESubtotal")
                        .WithDescription("Full year amount of basic entitlemet for academies that have primary pupils calculated by multiplying primary pupils by the LA-determinded rate, except for in-year openers funded on census, where the LA -calculated allocation is picked up from the New ISB sheet of the APT Aggregation.\r\n")
                        .WithCalculation(@"
	Dim t As DateTime = New DateTime(2018, 4, 1)
	Dim flag As Boolean = Me.APTProviderInformation.DateOpened > t
	Dim result As Decimal
	If flag Then
		result = Me.APTBasicEntitlement.PrimaryAmount
	Else
		result = Me.P004_PriRate() * Me.CensusNumberCounts.NORPrimary
	End If
	Return result")
                        .WithSBSTestProviders()
                        .WithTestScenarios(
                            TestScenario("Only Primary providers should have Primary Sub Total")
                                .WithGivenSteps(
                                    new GivenStep("APT Provider Information", "Phase", ComparisonOperator.EqualTo, "Primary"),
                                    new GivenStep("Census Number Counts", "NORPrimary", ComparisonOperator.GreaterThan, "0"),
                                    new GivenStep("APT Provider Information", "Phase", ComparisonOperator.EqualTo, "Primary")
                                )
                                .WithThenSteps(new ThenStep(ComparisonOperator.GreaterThan, "0.00")
                                ),

                            TestScenario("Non-Primary providers should not have Primary Sub Total")
                                .WithGivenSteps(
                                    new GivenStep("APT Provider Information", "Phase", ComparisonOperator.NotEqualTo, "Primary")
                                )
                                .WithThenSteps(new ThenStep(ComparisonOperator.EqualTo, "0.00")
                                )


                        ),

                    Product("P006a_NSEN_PriBE_Percent")
                        .WithDescription("The Primary Basic entitlement NSEN percentage is multiplied by the Primary Basic Entitlement funding to produce the full year NSEN amount attributable to primary basic entitlement.\r\n")
                        .WithCalculation(@"Return Me.APTBasicEntitlement.PrimaryNotionalSEN")
                        .WithSBSTestProviders()
                        .WithTestScenarios(
                            TestScenario("Only Primary providers should have Primary Notional SEN")
                                .WithGivenSteps(
                                    new GivenStep("APT Provider Information", "Phase", ComparisonOperator.EqualTo, "Primary")
                                )
                                .WithThenSteps(new ThenStep(ComparisonOperator.GreaterThan, "0.00")
                                ),

                            TestScenario("Non-Primary providers should not have Primary Notional SEN")
                                .WithGivenSteps(
                                    new GivenStep("APT Provider Information", "Phase", ComparisonOperator.NotEqualTo, "Primary")
                                )
                                .WithThenSteps(new ThenStep(ComparisonOperator.EqualTo, "0.00")
                                )

                        ),

                    Product("P006_NSEN_PriBE")
                        .WithDescription("This figure is obtained from the '1718 APT Proforma dataset - Basic Entitlement Primary Notional SEN'")
                        .WithCalculation(@"Return Me.P006a_NSEN_PriBE_Percent() * Me.P005_PriBESubtotal()")
                        .WithSBSTestProviders()
                        .WithTestScenarios(
                            TestScenario("Only Primary providers should have Primary Notional Sub Total")
                                .WithGivenSteps(
                                    new GivenStep("APT Provider Information", "Phase", ComparisonOperator.EqualTo, "Primary")
                                )
                                .WithThenSteps(new ThenStep(ComparisonOperator.GreaterThan, "0.00")
                                ),

                            TestScenario("Non-Primary providers should not have Primary Notional Sub Total")
                                .WithGivenSteps(
                                    new GivenStep("APT Provider Information", "Phase", ComparisonOperator.NotEqualTo, "Primary")
                                )
                                .WithThenSteps(new ThenStep(ComparisonOperator.EqualTo, "0.00")
                                )

                        ),

                    Product("P007_InYearPriBE_Subtotal")
                        .WithDescription("This calculation determines the actual Primary Basic Entitlement allocation due for the Academic Year 18/19, pro-rating the allocation if an academy opened in-year.")
                        .WithCalculation(@"Return Decimal.Zero")
                        .WithSBSTestProviders()


                        


                );
        }
    }
}