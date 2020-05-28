using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.ProviderLegacy;
using CalculateFunding.Models.Specs;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.CalcEngine.UnitTests
{
    public static class MockData
    {
        public static string SerializedBuildProject()
        {
            var sb = new StringBuilder();
            sb.AppendLine(@" {");
            sb.AppendLine(@"		""specification"": {");
            sb.AppendLine(@"			""period"": {");
            sb.AppendLine(@"				""id"": ""1718"",");
            sb.AppendLine(@"				""name"": ""2017/18""");
            sb.AppendLine(@"			},");
            sb.AppendLine(@"			""fundingStream"": {");
            sb.AppendLine(@"				""id"": ""YPLRA"",");
            sb.AppendLine(@"				""name"": ""16-19 Learner Responsive""");
            sb.AppendLine(@"			},");
            sb.AppendLine(@"			""id"": ""82b15316-eb4b-4167-9dee-57bab61f8a58"",");
            sb.AppendLine(@"			""name"": ""YP 201718 16-19 Learner Responsive""");
            sb.AppendLine(@"		},");
            sb.AppendLine(@"		""targetLanguage"": ""VisualBasic"",");
            sb.AppendLine(@"		""calculations"": [");
            sb.AppendLine(@"			{");
            sb.AppendLine(@"				""calculationSpecification"": {");
            sb.AppendLine(@"					""id"": ""a7eb5876-a447-4629-a731-5607f8662fcf"",");
            sb.AppendLine(@"					""name"": ""F4_2 High Needs 1619""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""allocationLine"": {");
            sb.AppendLine(@"					""id"": ""YPA16"",");
            sb.AppendLine(@"					""name"": ""16-19 High Needs Element 2""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""policies"": [");
            sb.AppendLine(@"					{");
            sb.AppendLine(@"						""id"": ""72bea276-b034-4931-a69b-2e168a3e310b"",");
            sb.AppendLine(@"						""name"": ""1619 High Needs""");
            sb.AppendLine(@"					}");
            sb.AppendLine(@"				],");
            sb.AppendLine(@"				""specification"": {");
            sb.AppendLine(@"					""period"": null,");
            sb.AppendLine(@"					""fundingStream"": null,");
            sb.AppendLine(@"					""id"": ""82b15316-eb4b-4167-9dee-57bab61f8a58"",");
            sb.AppendLine(@"					""name"": ""YP 201718 16-19 Learner Responsive""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""period"": {");
            sb.AppendLine(@"					""id"": ""1718"",");
            sb.AppendLine(@"					""name"": ""2017/18""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""fundingStream"": {");
            sb.AppendLine(@"					""id"": ""YPLRA"",");
            sb.AppendLine(@"					""name"": ""16-19 Learner Responsive""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""buildProjectId"": ""2f2432ff-3b75-4622-a1b0-7f6bc3d30096"",");
            sb.AppendLine(@"				""current"": {");
            sb.AppendLine(@"					""decimalPlaces"": 6,");
            sb.AppendLine(@"					""sourceCode"": ""Return 42"",");
            sb.AppendLine(@"					""version"": 3,");
            sb.AppendLine(@"					""date"": ""2018-02-22T12:32:59.6357163Z"",");
            sb.AppendLine(@"					""author"": {");
            sb.AppendLine(@"						""id"": ""matt.hammond@education.gov.uk"",");
            sb.AppendLine(@"						""name"": ""Matt Hammond""");
            sb.AppendLine(@"					},");
            sb.AppendLine(@"					""comment"": null,");
            sb.AppendLine(@"					""publishStatus"": ""Draft""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""published"": null,");
            sb.AppendLine(@"				""history"": [");
            sb.AppendLine(@"					{");
            sb.AppendLine(@"						""decimalPlaces"": 6,");
            sb.AppendLine(@"						""sourceCode"": ""' --- Providers ---- '\r\n\r\n' Provider fields can be accessed from the 'Provider' property:\r\n' Dim yearOpened = Provider.DateOpened.Year \r\n\r\n' --- Datasets ---- '\r\n' Referenced dataset fields can be accessed via the 'Datasets' property:\r\n' Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis\r\n\r\n' --- Caclulations ---- '\r\n' Other calculations within the same specification can be referred to directly:\r\n'  Dim rate = P004_PriRate()\r\n\r\n' For backwards compatability legacy Store functions and properties are available\r\n 'LAToProv()\r\n' Exclude()\r\n' Print()\r\n' IIf()\r\n' currentScenario \r\n' rid \r\n \r\n' If Decimal.MinValue is returned it will exclude the result of this calculation for this provider\r\n\r\nReturn Decimal.MinValue\r\n"",");
            sb.AppendLine(@"						""version"": 1,");
            sb.AppendLine(@"						""date"": ""2018-01-30T17:51:12.5501489Z"",");
            sb.AppendLine(@"						""author"": {");
            sb.AppendLine(@"							""id"": ""b001af14-3754-4cb1-9980-359e850700a8"",");
            sb.AppendLine(@"							""name"": ""testuser""");
            sb.AppendLine(@"						},");
            sb.AppendLine(@"						""comment"": null,");
            sb.AppendLine(@"						""publishStatus"": ""Draft""");
            sb.AppendLine(@"					},");
            sb.AppendLine(@"					{");
            sb.AppendLine(@"						""decimalPlaces"": 6,");
            sb.AppendLine(@"						""sourceCode"": ""Return 42"",");
            sb.AppendLine(@"						""version"": 2,");
            sb.AppendLine(@"						""date"": ""2018-02-22T11:42:47.3473941Z"",");
            sb.AppendLine(@"						""author"": {");
            sb.AppendLine(@"							""id"": ""matt.hammond@education.gov.uk"",");
            sb.AppendLine(@"							""name"": ""Matt Hammond""");
            sb.AppendLine(@"						},");
            sb.AppendLine(@"						""comment"": null,");
            sb.AppendLine(@"						""publishStatus"": ""Draft""");
            sb.AppendLine(@"					},");
            sb.AppendLine(@"					{");
            sb.AppendLine(@"						""decimalPlaces"": 6,");
            sb.AppendLine(@"						""sourceCode"": ""Return 42"",");
            sb.AppendLine(@"						""version"": 3,");
            sb.AppendLine(@"						""date"": ""2018-02-22T12:32:59.6357163Z"",");
            sb.AppendLine(@"						""author"": {");
            sb.AppendLine(@"							""id"": ""matt.hammond@education.gov.uk"",");
            sb.AppendLine(@"							""name"": ""Matt Hammond""");
            sb.AppendLine(@"						},");
            sb.AppendLine(@"						""comment"": null,");
            sb.AppendLine(@"						""publishStatus"": ""Draft""");
            sb.AppendLine(@"					}");
            sb.AppendLine(@"				],");
            sb.AppendLine(@"				""id"": ""f4dd989f-c9f4-4dae-88df-2e1cc0fad091"",");
            sb.AppendLine(@"				""name"": ""F4_2 High Needs 1619""");
            sb.AppendLine(@"			},");
            sb.AppendLine(@"			{");
            sb.AppendLine(@"				""calculationSpecification"": {");
            sb.AppendLine(@"					""id"": ""7d18bb00-fc99-4a30-9e13-48c38b5ff613"",");
            sb.AppendLine(@"					""name"": ""F4_3 Bursary Fund 1619""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""allocationLine"": {");
            sb.AppendLine(@"					""id"": ""YPA12"",");
            sb.AppendLine(@"					""name"": ""16-19 Bursary Funds""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""policies"": [");
            sb.AppendLine(@"					{");
            sb.AppendLine(@"						""id"": ""6c139cec-b0e4-4bfb-8920-6528f69fb539"",");
            sb.AppendLine(@"						""name"": ""1619 Learner Support""");
            sb.AppendLine(@"					}");
            sb.AppendLine(@"				],");
            sb.AppendLine(@"				""specification"": {");
            sb.AppendLine(@"					""period"": null,");
            sb.AppendLine(@"					""fundingStream"": null,");
            sb.AppendLine(@"					""id"": ""82b15316-eb4b-4167-9dee-57bab61f8a58"",");
            sb.AppendLine(@"					""name"": ""YP 201718 16-19 Learner Responsive""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""period"": {");
            sb.AppendLine(@"					""id"": ""1718"",");
            sb.AppendLine(@"					""name"": ""2017/18""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""fundingStream"": {");
            sb.AppendLine(@"					""id"": ""YPLRA"",");
            sb.AppendLine(@"					""name"": ""16-19 Learner Responsive""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""buildProjectId"": ""27ac1ecf-41c2-44e4-9ca3-6780b3d0cf05"",");
            sb.AppendLine(@"				""current"": {");
            sb.AppendLine(@"					""decimalPlaces"": 6,");
            sb.AppendLine(@"					""sourceCode"": ""' --- Providers ---- '\r\n\r\n' Provider fields can be accessed from the 'Provider' property:\r\n' Dim yearOpened = Provider.DateOpened.Year \r\n\r\n' --- Datasets ---- '\r\n' Referenced dataset fields can be accessed via the 'Datasets' property:\r\n' Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis\r\n\r\n' --- Caclulations ---- '\r\n' Other calculations within the same specification can be referred to directly:\r\n'  Dim rate = P004_PriRate()\r\n\r\n' For backwards compatability legacy Store functions and properties are available\r\n 'LAToProv()\r\n' Exclude()\r\n' Print()\r\n' IIf()\r\n' currentScenario \r\n' rid \r\n \r\n' If Decimal.MinValue is returned it will exclude the result of this calculation for this provider\r\n\r\nReturn Decimal.MinValue\r\n"",");
            sb.AppendLine(@"					""version"": 1,");
            sb.AppendLine(@"					""date"": ""2018-01-30T17:51:40.5041881Z"",");
            sb.AppendLine(@"					""author"": {");
            sb.AppendLine(@"						""id"": ""b001af14-3754-4cb1-9980-359e850700a8"",");
            sb.AppendLine(@"						""name"": ""testuser""");
            sb.AppendLine(@"					},");
            sb.AppendLine(@"					""comment"": null,");
            sb.AppendLine(@"					""publishStatus"": ""Draft""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""published"": null,");
            sb.AppendLine(@"				""history"": [");
            sb.AppendLine(@"					{");
            sb.AppendLine(@"						""decimalPlaces"": 6,");
            sb.AppendLine(@"						""sourceCode"": ""' --- Providers ---- '\r\n\r\n' Provider fields can be accessed from the 'Provider' property:\r\n' Dim yearOpened = Provider.DateOpened.Year \r\n\r\n' --- Datasets ---- '\r\n' Referenced dataset fields can be accessed via the 'Datasets' property:\r\n' Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis\r\n\r\n' --- Caclulations ---- '\r\n' Other calculations within the same specification can be referred to directly:\r\n'  Dim rate = P004_PriRate()\r\n\r\n' For backwards compatability legacy Store functions and properties are available\r\n 'LAToProv()\r\n' Exclude()\r\n' Print()\r\n' IIf()\r\n' currentScenario \r\n' rid \r\n \r\n' If Decimal.MinValue is returned it will exclude the result of this calculation for this provider\r\n\r\nReturn Decimal.MinValue\r\n"",");
            sb.AppendLine(@"						""version"": 1,");
            sb.AppendLine(@"						""date"": ""2018-01-30T17:51:40.5041948Z"",");
            sb.AppendLine(@"						""author"": {");
            sb.AppendLine(@"							""id"": ""b001af14-3754-4cb1-9980-359e850700a8"",");
            sb.AppendLine(@"							""name"": ""testuser""");
            sb.AppendLine(@"						},");
            sb.AppendLine(@"						""comment"": null,");
            sb.AppendLine(@"						""publishStatus"": ""Draft""");
            sb.AppendLine(@"					}");
            sb.AppendLine(@"				],");
            sb.AppendLine(@"				""id"": ""b9964b73-120c-4908-beb6-e6f8e8d42d84"",");
            sb.AppendLine(@"				""name"": ""F4_3 Bursary Fund 1619""");
            sb.AppendLine(@"			},");
            sb.AppendLine(@"			{");
            sb.AppendLine(@"				""calculationSpecification"": {");
            sb.AppendLine(@"					""id"": ""8e1a24b7-9405-472e-b38a-a236ec56715c"",");
            sb.AppendLine(@"					""name"": ""F4_3d Free Meals Funding 1619""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""allocationLine"": {");
            sb.AppendLine(@"					""id"": ""YPA23"",");
            sb.AppendLine(@"					""name"": ""16-19 Free Meals in FE""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""policies"": [");
            sb.AppendLine(@"					{");
            sb.AppendLine(@"						""id"": ""5b6105fa-522a-4f0a-92ee-86f45be17e3f"",");
            sb.AppendLine(@"						""name"": ""1619 Free Meals Funding""");
            sb.AppendLine(@"					}");
            sb.AppendLine(@"				],");
            sb.AppendLine(@"				""specification"": {");
            sb.AppendLine(@"					""period"": null,");
            sb.AppendLine(@"					""fundingStream"": null,");
            sb.AppendLine(@"					""id"": ""82b15316-eb4b-4167-9dee-57bab61f8a58"",");
            sb.AppendLine(@"					""name"": ""YP 201718 16-19 Learner Responsive""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""period"": {");
            sb.AppendLine(@"					""id"": ""1718"",");
            sb.AppendLine(@"					""name"": ""2017/18""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""fundingStream"": {");
            sb.AppendLine(@"					""id"": ""YPLRA"",");
            sb.AppendLine(@"					""name"": ""16-19 Learner Responsive""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""buildProjectId"": ""85575c70-5e71-472d-8e57-fede79198cf4"",");
            sb.AppendLine(@"				""current"": {");
            sb.AppendLine(@"					""decimalPlaces"": 6,");
            sb.AppendLine(@"					""sourceCode"": ""' --- Providers ---- '\r\n\r\n' Provider fields can be accessed from the 'Provider' property:\r\n' Dim yearOpened = Provider.DateOpened.Year \r\n\r\n' --- Datasets ---- '\r\n' Referenced dataset fields can be accessed via the 'Datasets' property:\r\n' Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis\r\n\r\n' --- Caclulations ---- '\r\n' Other calculations within the same specification can be referred to directly:\r\n'  Dim rate = P004_PriRate()\r\n\r\n' For backwards compatability legacy Store functions and properties are available\r\n 'LAToProv()\r\n' Exclude()\r\n' Print()\r\n' IIf()\r\n' currentScenario \r\n' rid \r\n \r\n' If Decimal.MinValue is returned it will exclude the result of this calculation for this provider\r\n\r\nReturn Decimal.MinValue\r\n"",");
            sb.AppendLine(@"					""version"": 1,");
            sb.AppendLine(@"					""date"": ""2018-01-30T17:52:49.9891104Z"",");
            sb.AppendLine(@"					""author"": {");
            sb.AppendLine(@"						""id"": ""b001af14-3754-4cb1-9980-359e850700a8"",");
            sb.AppendLine(@"						""name"": ""testuser""");
            sb.AppendLine(@"					},");
            sb.AppendLine(@"					""comment"": null,");
            sb.AppendLine(@"					""publishStatus"": ""Draft""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""published"": null,");
            sb.AppendLine(@"				""history"": [");
            sb.AppendLine(@"					{");
            sb.AppendLine(@"						""decimalPlaces"": 6,");
            sb.AppendLine(@"						""sourceCode"": ""' --- Providers ---- '\r\n\r\n' Provider fields can be accessed from the 'Provider' property:\r\n' Dim yearOpened = Provider.DateOpened.Year \r\n\r\n' --- Datasets ---- '\r\n' Referenced dataset fields can be accessed via the 'Datasets' property:\r\n' Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis\r\n\r\n' --- Caclulations ---- '\r\n' Other calculations within the same specification can be referred to directly:\r\n'  Dim rate = P004_PriRate()\r\n\r\n' For backwards compatability legacy Store functions and properties are available\r\n 'LAToProv()\r\n' Exclude()\r\n' Print()\r\n' IIf()\r\n' currentScenario \r\n' rid \r\n \r\n' If Decimal.MinValue is returned it will exclude the result of this calculation for this provider\r\n\r\nReturn Decimal.MinValue\r\n"",");
            sb.AppendLine(@"						""version"": 1,");
            sb.AppendLine(@"						""date"": ""2018-01-30T17:52:49.9891362Z"",");
            sb.AppendLine(@"						""author"": {");
            sb.AppendLine(@"							""id"": ""b001af14-3754-4cb1-9980-359e850700a8"",");
            sb.AppendLine(@"							""name"": ""testuser""");
            sb.AppendLine(@"						},");
            sb.AppendLine(@"						""comment"": null,");
            sb.AppendLine(@"						""publishStatus"": ""Draft""");
            sb.AppendLine(@"					}");
            sb.AppendLine(@"				],");
            sb.AppendLine(@"				""id"": ""e41718df-54ac-4e83-b2fc-6455c61d88d0"",");
            sb.AppendLine(@"				""name"": ""F4_3d Free Meals Funding 1619""");
            sb.AppendLine(@"			},");
            sb.AppendLine(@"			{");
            sb.AppendLine(@"				""calculationSpecification"": {");
            sb.AppendLine(@"					""id"": ""164d62b0-0d7f-4b2a-b091-cbb5980fc18a"",");
            sb.AppendLine(@"					""name"": ""F4_5 Formula Protection Funding 1619""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""allocationLine"": {");
            sb.AppendLine(@"					""id"": ""YPA15"",");
            sb.AppendLine(@"					""name"": ""16-19 Formula Protection Funding""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""policies"": [");
            sb.AppendLine(@"					{");
            sb.AppendLine(@"						""id"": ""3dec3fd9-32b3-483d-8e6e-050c87bdd805"",");
            sb.AppendLine(@"						""name"": ""FPF 1619""");
            sb.AppendLine(@"					}");
            sb.AppendLine(@"				],");
            sb.AppendLine(@"				""specification"": {");
            sb.AppendLine(@"					""period"": null,");
            sb.AppendLine(@"					""fundingStream"": null,");
            sb.AppendLine(@"					""id"": ""82b15316-eb4b-4167-9dee-57bab61f8a58"",");
            sb.AppendLine(@"					""name"": ""YP 201718 16-19 Learner Responsive""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""period"": {");
            sb.AppendLine(@"					""id"": ""1718"",");
            sb.AppendLine(@"					""name"": ""2017/18""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""fundingStream"": {");
            sb.AppendLine(@"					""id"": ""YPLRA"",");
            sb.AppendLine(@"					""name"": ""16-19 Learner Responsive""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""buildProjectId"": ""dfcdf315-2dad-40ea-92ee-7445e383a9d9"",");
            sb.AppendLine(@"				""current"": {");
            sb.AppendLine(@"					""decimalPlaces"": 6,");
            sb.AppendLine(@"					""sourceCode"": ""' --- Providers ---- '\r\n\r\n' Provider fields can be accessed from the 'Provider' property:\r\n' Dim yearOpened = Provider.DateOpened.Year \r\n\r\n' --- Datasets ---- '\r\n' Referenced dataset fields can be accessed via the 'Datasets' property:\r\n' Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis\r\n\r\n' --- Caclulations ---- '\r\n' Other calculations within the same specification can be referred to directly:\r\n'  Dim rate = P004_PriRate()\r\n\r\n' For backwards compatability legacy Store functions and properties are available\r\n 'LAToProv()\r\n' Exclude()\r\n' Print()\r\n' IIf()\r\n' currentScenario \r\n' rid \r\n \r\n' If Decimal.MinValue is returned it will exclude the result of this calculation for this provider\r\n\r\nReturn Decimal.MinValue\r\n"",");
            sb.AppendLine(@"					""version"": 1,");
            sb.AppendLine(@"					""date"": ""2018-01-30T17:53:04.1881514Z"",");
            sb.AppendLine(@"					""author"": {");
            sb.AppendLine(@"						""id"": ""b001af14-3754-4cb1-9980-359e850700a8"",");
            sb.AppendLine(@"						""name"": ""testuser""");
            sb.AppendLine(@"					},");
            sb.AppendLine(@"					""comment"": null,");
            sb.AppendLine(@"					""publishStatus"": ""Draft""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""published"": null,");
            sb.AppendLine(@"				""history"": [");
            sb.AppendLine(@"					{");
            sb.AppendLine(@"						""decimalPlaces"": 6,");
            sb.AppendLine(@"						""sourceCode"": ""' --- Providers ---- '\r\n\r\n' Provider fields can be accessed from the 'Provider' property:\r\n' Dim yearOpened = Provider.DateOpened.Year \r\n\r\n' --- Datasets ---- '\r\n' Referenced dataset fields can be accessed via the 'Datasets' property:\r\n' Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis\r\n\r\n' --- Caclulations ---- '\r\n' Other calculations within the same specification can be referred to directly:\r\n'  Dim rate = P004_PriRate()\r\n\r\n' For backwards compatability legacy Store functions and properties are available\r\n 'LAToProv()\r\n' Exclude()\r\n' Print()\r\n' IIf()\r\n' currentScenario \r\n' rid \r\n \r\n' If Decimal.MinValue is returned it will exclude the result of this calculation for this provider\r\n\r\nReturn Decimal.MinValue\r\n"",");
            sb.AppendLine(@"						""version"": 1,");
            sb.AppendLine(@"						""date"": ""2018-01-30T17:53:04.1881579Z"",");
            sb.AppendLine(@"						""author"": {");
            sb.AppendLine(@"							""id"": ""b001af14-3754-4cb1-9980-359e850700a8"",");
            sb.AppendLine(@"							""name"": ""testuser""");
            sb.AppendLine(@"						},");
            sb.AppendLine(@"						""comment"": null,");
            sb.AppendLine(@"						""publishStatus"": ""Draft""");
            sb.AppendLine(@"					}");
            sb.AppendLine(@"				],");
            sb.AppendLine(@"				""id"": ""f28952e6-0865-4587-8bb2-f4db89942eb3"",");
            sb.AppendLine(@"				""name"": ""F4_5 Formula Protection Funding 1619""");
            sb.AppendLine(@"			},");
            sb.AppendLine(@"			{");
            sb.AppendLine(@"				""calculationSpecification"": {");
            sb.AppendLine(@"					""id"": ""a1eed0af-85bd-48f4-85d5-60f68ea0c5a1"",");
            sb.AppendLine(@"					""name"": ""V1_9g 1617 16_19 students inc exceptions BC""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""allocationLine"": {");
            sb.AppendLine(@"					""id"": ""YPA14"",");
            sb.AppendLine(@"					""name"": ""16-19 Total Programme Funding""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""policies"": [");
            sb.AppendLine(@"					{");
            sb.AppendLine(@"						""id"": ""17ff10cd-1823-44d2-88ba-f98b051876a0"",");
            sb.AppendLine(@"						""name"": ""1619 R04 Volumes""");
            sb.AppendLine(@"					}");
            sb.AppendLine(@"				],");
            sb.AppendLine(@"				""specification"": {");
            sb.AppendLine(@"					""period"": null,");
            sb.AppendLine(@"					""fundingStream"": null,");
            sb.AppendLine(@"					""id"": ""82b15316-eb4b-4167-9dee-57bab61f8a58"",");
            sb.AppendLine(@"					""name"": ""YP 201718 16-19 Learner Responsive""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""period"": {");
            sb.AppendLine(@"					""id"": ""1718"",");
            sb.AppendLine(@"					""name"": ""2017/18""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""fundingStream"": {");
            sb.AppendLine(@"					""id"": ""YPLRA"",");
            sb.AppendLine(@"					""name"": ""16-19 Learner Responsive""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""buildProjectId"": ""417f24a3-fb8b-4174-bd12-9b643e45a4af"",");
            sb.AppendLine(@"				""current"": {");
            sb.AppendLine(@"					""decimalPlaces"": 6,");
            sb.AppendLine(@"					""sourceCode"": ""' --- Providers ---- '\r\n\r\n' Provider fields can be accessed from the 'Provider' property:\r\n' Dim yearOpened = Provider.DateOpened.Year \r\n\r\n' --- Datasets ---- '\r\n' Referenced dataset fields can be accessed via the 'Datasets' property:\r\n' Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis\r\n\r\n' --- Caclulations ---- '\r\n' Other calculations within the same specification can be referred to directly:\r\n'  Dim rate = P004_PriRate()\r\n\r\n' For backwards compatability legacy Store functions and properties are available\r\n 'LAToProv()\r\n' Exclude()\r\n' Print()\r\n' IIf()\r\n' currentScenario \r\n' rid \r\n \r\n' If Decimal.MinValue is returned it will exclude the result of this calculation for this provider\r\n\r\nReturn Decimal.MinValue\r\n"",");
            sb.AppendLine(@"					""version"": 1,");
            sb.AppendLine(@"					""date"": ""2018-01-30T17:53:31.0896134Z"",");
            sb.AppendLine(@"					""author"": {");
            sb.AppendLine(@"						""id"": ""b001af14-3754-4cb1-9980-359e850700a8"",");
            sb.AppendLine(@"						""name"": ""testuser""");
            sb.AppendLine(@"					},");
            sb.AppendLine(@"					""comment"": null,");
            sb.AppendLine(@"					""publishStatus"": ""Draft""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""published"": null,");
            sb.AppendLine(@"				""history"": [");
            sb.AppendLine(@"					{");
            sb.AppendLine(@"						""decimalPlaces"": 6,");
            sb.AppendLine(@"						""sourceCode"": ""' --- Providers ---- '\r\n\r\n' Provider fields can be accessed from the 'Provider' property:\r\n' Dim yearOpened = Provider.DateOpened.Year \r\n\r\n' --- Datasets ---- '\r\n' Referenced dataset fields can be accessed via the 'Datasets' property:\r\n' Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis\r\n\r\n' --- Caclulations ---- '\r\n' Other calculations within the same specification can be referred to directly:\r\n'  Dim rate = P004_PriRate()\r\n\r\n' For backwards compatability legacy Store functions and properties are available\r\n 'LAToProv()\r\n' Exclude()\r\n' Print()\r\n' IIf()\r\n' currentScenario \r\n' rid \r\n \r\n' If Decimal.MinValue is returned it will exclude the result of this calculation for this provider\r\n\r\nReturn Decimal.MinValue\r\n"",");
            sb.AppendLine(@"						""version"": 1,");
            sb.AppendLine(@"						""date"": ""2018-01-30T17:53:31.0896202Z"",");
            sb.AppendLine(@"						""author"": {");
            sb.AppendLine(@"							""id"": ""b001af14-3754-4cb1-9980-359e850700a8"",");
            sb.AppendLine(@"							""name"": ""testuser""");
            sb.AppendLine(@"						},");
            sb.AppendLine(@"						""comment"": null,");
            sb.AppendLine(@"						""publishStatus"": ""Draft""");
            sb.AppendLine(@"					}");
            sb.AppendLine(@"				],");
            sb.AppendLine(@"				""id"": ""01ca2c20-9a66-4770-94d5-c35d97b1fff0"",");
            sb.AppendLine(@"				""name"": ""V1_9g 1617 16_19 students inc exceptions BC""");
            sb.AppendLine(@"			},");
            sb.AppendLine(@"			{");
            sb.AppendLine(@"				""calculationSpecification"": {");
            sb.AppendLine(@"					""id"": ""84a973dd-a7af-4121-bdda-3a6b3fd036b1"",");
            sb.AppendLine(@"					""name"": ""F1_25 19 plus discretionary bursary fund""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""allocationLine"": {");
            sb.AppendLine(@"					""id"": ""YPA12"",");
            sb.AppendLine(@"					""name"": ""16-19 Bursary Funds""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""policies"": [");
            sb.AppendLine(@"					{");
            sb.AppendLine(@"						""id"": ""10a9acc4-bfda-428a-b029-626ef34fb303"",");
            sb.AppendLine(@"						""name"": ""19 plus continuing""");
            sb.AppendLine(@"					}");
            sb.AppendLine(@"				],");
            sb.AppendLine(@"				""specification"": {");
            sb.AppendLine(@"					""period"": null,");
            sb.AppendLine(@"					""fundingStream"": null,");
            sb.AppendLine(@"					""id"": ""82b15316-eb4b-4167-9dee-57bab61f8a58"",");
            sb.AppendLine(@"					""name"": ""YP 201718 16-19 Learner Responsive""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""period"": {");
            sb.AppendLine(@"					""id"": ""1718"",");
            sb.AppendLine(@"					""name"": ""2017/18""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""fundingStream"": {");
            sb.AppendLine(@"					""id"": ""YPLRA"",");
            sb.AppendLine(@"					""name"": ""16-19 Learner Responsive""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""buildProjectId"": ""1cf03fb4-1baa-408b-886e-7b1c83df3776"",");
            sb.AppendLine(@"				""current"": {");
            sb.AppendLine(@"					""decimalPlaces"": 6,");
            sb.AppendLine(@"					""sourceCode"": ""' --- Providers ---- '\r\n\r\n' Provider fields can be accessed from the 'Provider' property:\r\n' Dim yearOpened = Provider.DateOpened.Year \r\n\r\n' --- Datasets ---- '\r\n' Referenced dataset fields can be accessed via the 'Datasets' property:\r\n' Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis\r\n\r\n' --- Caclulations ---- '\r\n' Other calculations within the same specification can be referred to directly:\r\n'  Dim rate = P004_PriRate()\r\n\r\n' For backwards compatability legacy Store functions and properties are available\r\n 'LAToProv()\r\n' Exclude()\r\n' Print()\r\n' IIf()\r\n' currentScenario \r\n' rid \r\n \r\n' If Decimal.MinValue is returned it will exclude the result of this calculation for this provider\r\n\r\nReturn Decimal.MinValue\r\n"",");
            sb.AppendLine(@"					""version"": 1,");
            sb.AppendLine(@"					""date"": ""2018-01-30T17:53:42.7751286Z"",");
            sb.AppendLine(@"					""author"": {");
            sb.AppendLine(@"						""id"": ""b001af14-3754-4cb1-9980-359e850700a8"",");
            sb.AppendLine(@"						""name"": ""testuser""");
            sb.AppendLine(@"					},");
            sb.AppendLine(@"					""comment"": null,");
            sb.AppendLine(@"					""publishStatus"": ""Draft""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""published"": null,");
            sb.AppendLine(@"				""history"": [");
            sb.AppendLine(@"					{");
            sb.AppendLine(@"						""decimalPlaces"": 6,");
            sb.AppendLine(@"						""sourceCode"": ""' --- Providers ---- '\r\n\r\n' Provider fields can be accessed from the 'Provider' property:\r\n' Dim yearOpened = Provider.DateOpened.Year \r\n\r\n' --- Datasets ---- '\r\n' Referenced dataset fields can be accessed via the 'Datasets' property:\r\n' Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis\r\n\r\n' --- Caclulations ---- '\r\n' Other calculations within the same specification can be referred to directly:\r\n'  Dim rate = P004_PriRate()\r\n\r\n' For backwards compatability legacy Store functions and properties are available\r\n 'LAToProv()\r\n' Exclude()\r\n' Print()\r\n' IIf()\r\n' currentScenario \r\n' rid \r\n \r\n' If Decimal.MinValue is returned it will exclude the result of this calculation for this provider\r\n\r\nReturn Decimal.MinValue\r\n"",");
            sb.AppendLine(@"						""version"": 1,");
            sb.AppendLine(@"						""date"": ""2018-01-30T17:53:42.7751394Z"",");
            sb.AppendLine(@"						""author"": {");
            sb.AppendLine(@"							""id"": ""b001af14-3754-4cb1-9980-359e850700a8"",");
            sb.AppendLine(@"							""name"": ""testuser""");
            sb.AppendLine(@"						},");
            sb.AppendLine(@"						""comment"": null,");
            sb.AppendLine(@"						""publishStatus"": ""Draft""");
            sb.AppendLine(@"					}");
            sb.AppendLine(@"				],");
            sb.AppendLine(@"				""id"": ""53f8931c-d7e4-4492-bb3c-bb32c6a9cec0"",");
            sb.AppendLine(@"				""name"": ""F1_25 19 plus discretionary bursary fund""");
            sb.AppendLine(@"			},");
            sb.AppendLine(@"			{");
            sb.AppendLine(@"				""calculationSpecification"": {");
            sb.AppendLine(@"					""id"": ""290bb168-3490-4e02-b981-21069a105b3c"",");
            sb.AppendLine(@"					""name"": ""F1_26a 19 plus Free Meals inc BC excl de minimus""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""allocationLine"": {");
            sb.AppendLine(@"					""id"": ""YPA23"",");
            sb.AppendLine(@"					""name"": ""16-19 Free Meals in FE""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""policies"": [");
            sb.AppendLine(@"					{");
            sb.AppendLine(@"						""id"": ""d635fe27-a008-488b-8ab6-d1bfee8110b2"",");
            sb.AppendLine(@"						""name"": ""Continuing Students Free Meals Funding""");
            sb.AppendLine(@"					}");
            sb.AppendLine(@"				],");
            sb.AppendLine(@"				""specification"": {");
            sb.AppendLine(@"					""period"": null,");
            sb.AppendLine(@"					""fundingStream"": null,");
            sb.AppendLine(@"					""id"": ""82b15316-eb4b-4167-9dee-57bab61f8a58"",");
            sb.AppendLine(@"					""name"": ""YP 201718 16-19 Learner Responsive""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""period"": {");
            sb.AppendLine(@"					""id"": ""1718"",");
            sb.AppendLine(@"					""name"": ""2017/18""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""fundingStream"": {");
            sb.AppendLine(@"					""id"": ""YPLRA"",");
            sb.AppendLine(@"					""name"": ""16-19 Learner Responsive""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""buildProjectId"": ""ec8b88a3-8a59-421e-bb13-0b158b7bc9ff"",");
            sb.AppendLine(@"				""current"": {");
            sb.AppendLine(@"					""decimalPlaces"": 6,");
            sb.AppendLine(@"					""sourceCode"": ""' --- Providers ---- '\r\n\r\n' Provider fields can be accessed from the 'Provider' property:\r\n' Dim yearOpened = Provider.DateOpened.Year \r\n\r\n' --- Datasets ---- '\r\n' Referenced dataset fields can be accessed via the 'Datasets' property:\r\n' Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis\r\n\r\n' --- Caclulations ---- '\r\n' Other calculations within the same specification can be referred to directly:\r\n'  Dim rate = P004_PriRate()\r\n\r\n' For backwards compatability legacy Store functions and properties are available\r\n 'LAToProv()\r\n' Exclude()\r\n' Print()\r\n' IIf()\r\n' currentScenario \r\n' rid \r\n \r\n' If Decimal.MinValue is returned it will exclude the result of this calculation for this provider\r\n\r\nReturn Decimal.MinValue\r\n"",");
            sb.AppendLine(@"					""version"": 1,");
            sb.AppendLine(@"					""date"": ""2018-01-30T17:53:52.2132201Z"",");
            sb.AppendLine(@"					""author"": {");
            sb.AppendLine(@"						""id"": ""b001af14-3754-4cb1-9980-359e850700a8"",");
            sb.AppendLine(@"						""name"": ""testuser""");
            sb.AppendLine(@"					},");
            sb.AppendLine(@"					""comment"": null,");
            sb.AppendLine(@"					""publishStatus"": ""Draft""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""published"": null,");
            sb.AppendLine(@"				""history"": [");
            sb.AppendLine(@"					{");
            sb.AppendLine(@"						""decimalPlaces"": 6,");
            sb.AppendLine(@"						""sourceCode"": ""' --- Providers ---- '\r\n\r\n' Provider fields can be accessed from the 'Provider' property:\r\n' Dim yearOpened = Provider.DateOpened.Year \r\n\r\n' --- Datasets ---- '\r\n' Referenced dataset fields can be accessed via the 'Datasets' property:\r\n' Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis\r\n\r\n' --- Caclulations ---- '\r\n' Other calculations within the same specification can be referred to directly:\r\n'  Dim rate = P004_PriRate()\r\n\r\n' For backwards compatability legacy Store functions and properties are available\r\n 'LAToProv()\r\n' Exclude()\r\n' Print()\r\n' IIf()\r\n' currentScenario \r\n' rid \r\n \r\n' If Decimal.MinValue is returned it will exclude the result of this calculation for this provider\r\n\r\nReturn Decimal.MinValue\r\n"",");
            sb.AppendLine(@"						""version"": 1,");
            sb.AppendLine(@"						""date"": ""2018-01-30T17:53:52.2132353Z"",");
            sb.AppendLine(@"						""author"": {");
            sb.AppendLine(@"							""id"": ""b001af14-3754-4cb1-9980-359e850700a8"",");
            sb.AppendLine(@"							""name"": ""testuser""");
            sb.AppendLine(@"						},");
            sb.AppendLine(@"						""comment"": null,");
            sb.AppendLine(@"						""publishStatus"": ""Draft""");
            sb.AppendLine(@"					}");
            sb.AppendLine(@"				],");
            sb.AppendLine(@"				""id"": ""d19bd996-8a59-4155-9aa9-58442c4cb8d1"",");
            sb.AppendLine(@"				""name"": ""F1_26a 19 plus Free Meals inc BC excl de minimus""");
            sb.AppendLine(@"			},");
            sb.AppendLine(@"			{");
            sb.AppendLine(@"				""calculationSpecification"": {");
            sb.AppendLine(@"					""id"": ""d38017ba-5191-4214-8e69-6a134be0d47d"",");
            sb.AppendLine(@"					""name"": ""F4_1d Programme funding _ excluding SPIs""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""allocationLine"": {");
            sb.AppendLine(@"					""id"": ""YPA14"",");
            sb.AppendLine(@"					""name"": ""16-19 Total Programme Funding""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""policies"": [");
            sb.AppendLine(@"					{");
            sb.AppendLine(@"						""id"": ""7149b126-7ecf-4f63-a3d6-c48920a8fc21"",");
            sb.AppendLine(@"						""name"": ""1619FE Programme Funding""");
            sb.AppendLine(@"					}");
            sb.AppendLine(@"				],");
            sb.AppendLine(@"				""specification"": {");
            sb.AppendLine(@"					""period"": null,");
            sb.AppendLine(@"					""fundingStream"": null,");
            sb.AppendLine(@"					""id"": ""82b15316-eb4b-4167-9dee-57bab61f8a58"",");
            sb.AppendLine(@"					""name"": ""YP 201718 16-19 Learner Responsive""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""period"": {");
            sb.AppendLine(@"					""id"": ""1718"",");
            sb.AppendLine(@"					""name"": ""2017/18""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""fundingStream"": {");
            sb.AppendLine(@"					""id"": ""YPLRA"",");
            sb.AppendLine(@"					""name"": ""16-19 Learner Responsive""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""buildProjectId"": ""41c8a8ca-d952-447e-b859-b298e72c3721"",");
            sb.AppendLine(@"				""current"": {");
            sb.AppendLine(@"					""decimalPlaces"": 6,");
            sb.AppendLine(@"					""sourceCode"": ""' --- Providers ---- '\r\n\r\n' Provider fields can be accessed from the 'Provider' property:\r\n' Dim yearOpened = Provider.DateOpened.Year \r\n\r\n' --- Datasets ---- '\r\n' Referenced dataset fields can be accessed via the 'Datasets' property:\r\n' Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis\r\n\r\n' --- Caclulations ---- '\r\n' Other calculations within the same specification can be referred to directly:\r\n'  Dim rate = P004_PriRate()\r\n\r\n' For backwards compatability legacy Store functions and properties are available\r\n 'LAToProv()\r\n' Exclude()\r\n' Print()\r\n' IIf()\r\n' currentScenario \r\n' rid \r\n \r\n' If Decimal.MinValue is returned it will exclude the result of this calculation for this provider\r\n\r\nReturn Decimal.MinValue\r\n"",");
            sb.AppendLine(@"					""version"": 1,");
            sb.AppendLine(@"					""date"": ""2018-01-30T17:53:54.9566398Z"",");
            sb.AppendLine(@"					""author"": {");
            sb.AppendLine(@"						""id"": ""b001af14-3754-4cb1-9980-359e850700a8"",");
            sb.AppendLine(@"						""name"": ""testuser""");
            sb.AppendLine(@"					},");
            sb.AppendLine(@"					""comment"": null,");
            sb.AppendLine(@"					""publishStatus"": ""Draft""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""published"": null,");
            sb.AppendLine(@"				""history"": [");
            sb.AppendLine(@"					{");
            sb.AppendLine(@"						""decimalPlaces"": 6,");
            sb.AppendLine(@"						""sourceCode"": ""' --- Providers ---- '\r\n\r\n' Provider fields can be accessed from the 'Provider' property:\r\n' Dim yearOpened = Provider.DateOpened.Year \r\n\r\n' --- Datasets ---- '\r\n' Referenced dataset fields can be accessed via the 'Datasets' property:\r\n' Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis\r\n\r\n' --- Caclulations ---- '\r\n' Other calculations within the same specification can be referred to directly:\r\n'  Dim rate = P004_PriRate()\r\n\r\n' For backwards compatability legacy Store functions and properties are available\r\n 'LAToProv()\r\n' Exclude()\r\n' Print()\r\n' IIf()\r\n' currentScenario \r\n' rid \r\n \r\n' If Decimal.MinValue is returned it will exclude the result of this calculation for this provider\r\n\r\nReturn Decimal.MinValue\r\n"",");
            sb.AppendLine(@"						""version"": 1,");
            sb.AppendLine(@"						""date"": ""2018-01-30T17:53:54.9566551Z"",");
            sb.AppendLine(@"						""author"": {");
            sb.AppendLine(@"							""id"": ""b001af14-3754-4cb1-9980-359e850700a8"",");
            sb.AppendLine(@"							""name"": ""testuser""");
            sb.AppendLine(@"						},");
            sb.AppendLine(@"						""comment"": null,");
            sb.AppendLine(@"						""publishStatus"": ""Draft""");
            sb.AppendLine(@"					}");
            sb.AppendLine(@"				],");
            sb.AppendLine(@"				""id"": ""a8d2c2c4-bfc6-4349-8de5-99acb8ecc12f"",");
            sb.AppendLine(@"				""name"": ""F4_1d Programme funding _ excluding SPIs""");
            sb.AppendLine(@"			},");
            sb.AppendLine(@"			{");
            sb.AppendLine(@"				""calculationSpecification"": {");
            sb.AppendLine(@"					""id"": ""bf453c4e-957e-4586-974f-b2a27ea0e621"",");
            sb.AppendLine(@"					""name"": ""F4_1e Programme funding _ SPI only""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""allocationLine"": {");
            sb.AppendLine(@"					""id"": ""YPA24"",");
            sb.AppendLine(@"					""name"": ""16-19 SPI Element 1""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""policies"": [");
            sb.AppendLine(@"					{");
            sb.AppendLine(@"						""id"": ""7149b126-7ecf-4f63-a3d6-c48920a8fc21"",");
            sb.AppendLine(@"						""name"": ""1619FE Programme Funding""");
            sb.AppendLine(@"					}");
            sb.AppendLine(@"				],");
            sb.AppendLine(@"				""specification"": {");
            sb.AppendLine(@"					""period"": null,");
            sb.AppendLine(@"					""fundingStream"": null,");
            sb.AppendLine(@"					""id"": ""82b15316-eb4b-4167-9dee-57bab61f8a58"",");
            sb.AppendLine(@"					""name"": ""YP 201718 16-19 Learner Responsive""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""period"": {");
            sb.AppendLine(@"					""id"": ""1718"",");
            sb.AppendLine(@"					""name"": ""2017/18""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""fundingStream"": {");
            sb.AppendLine(@"					""id"": ""YPLRA"",");
            sb.AppendLine(@"					""name"": ""16-19 Learner Responsive""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""buildProjectId"": ""ab1c093b-13e9-4a7b-af8b-d60661a7981e"",");
            sb.AppendLine(@"				""current"": {");
            sb.AppendLine(@"					""decimalPlaces"": 6,");
            sb.AppendLine(@"					""sourceCode"": ""' --- Providers ---- '\r\n\r\n' Provider fields can be accessed from the 'Provider' property:\r\n' Dim yearOpened = Provider.DateOpened.Year \r\n\r\n' --- Datasets ---- '\r\n' Referenced dataset fields can be accessed via the 'Datasets' property:\r\n' Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis\r\n\r\n' --- Caclulations ---- '\r\n' Other calculations within the same specification can be referred to directly:\r\n'  Dim rate = P004_PriRate()\r\n\r\n' For backwards compatability legacy Store functions and properties are available\r\n 'LAToProv()\r\n' Exclude()\r\n' Print()\r\n' IIf()\r\n' currentScenario \r\n' rid \r\n \r\n' If Decimal.MinValue is returned it will exclude the result of this calculation for this provider\r\n\r\nReturn Decimal.MinValue\r\n"",");
            sb.AppendLine(@"					""version"": 1,");
            sb.AppendLine(@"					""date"": ""2018-01-30T17:53:56.3508737Z"",");
            sb.AppendLine(@"					""author"": {");
            sb.AppendLine(@"						""id"": ""b001af14-3754-4cb1-9980-359e850700a8"",");
            sb.AppendLine(@"						""name"": ""testuser""");
            sb.AppendLine(@"					},");
            sb.AppendLine(@"					""comment"": null,");
            sb.AppendLine(@"					""publishStatus"": ""Draft""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""published"": null,");
            sb.AppendLine(@"				""history"": [");
            sb.AppendLine(@"					{");
            sb.AppendLine(@"						""decimalPlaces"": 6,");
            sb.AppendLine(@"						""sourceCode"": ""' --- Providers ---- '\r\n\r\n' Provider fields can be accessed from the 'Provider' property:\r\n' Dim yearOpened = Provider.DateOpened.Year \r\n\r\n' --- Datasets ---- '\r\n' Referenced dataset fields can be accessed via the 'Datasets' property:\r\n' Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis\r\n\r\n' --- Caclulations ---- '\r\n' Other calculations within the same specification can be referred to directly:\r\n'  Dim rate = P004_PriRate()\r\n\r\n' For backwards compatability legacy Store functions and properties are available\r\n 'LAToProv()\r\n' Exclude()\r\n' Print()\r\n' IIf()\r\n' currentScenario \r\n' rid \r\n \r\n' If Decimal.MinValue is returned it will exclude the result of this calculation for this provider\r\n\r\nReturn Decimal.MinValue\r\n"",");
            sb.AppendLine(@"						""version"": 1,");
            sb.AppendLine(@"						""date"": ""2018-01-30T17:53:56.350881Z"",");
            sb.AppendLine(@"						""author"": {");
            sb.AppendLine(@"							""id"": ""b001af14-3754-4cb1-9980-359e850700a8"",");
            sb.AppendLine(@"							""name"": ""testuser""");
            sb.AppendLine(@"						},");
            sb.AppendLine(@"						""comment"": null,");
            sb.AppendLine(@"						""publishStatus"": ""Draft""");
            sb.AppendLine(@"					}");
            sb.AppendLine(@"				],");
            sb.AppendLine(@"				""id"": ""4368c77d-2899-49f0-9d53-78a09ff91309"",");
            sb.AppendLine(@"				""name"": ""F4_1e Programme funding _ SPI only""");
            sb.AppendLine(@"			},");
            sb.AppendLine(@"			{");
            sb.AppendLine(@"				""calculationSpecification"": {");
            sb.AppendLine(@"					""id"": ""16b2eb9e-6991-437b-a49a-c0537ccae8ed"",");
            sb.AppendLine(@"					""name"": ""F1_7_14to16_PP Service Child""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""allocationLine"": {");
            sb.AppendLine(@"					""id"": ""YPA22"",");
            sb.AppendLine(@"					""name"": ""14-16 service child premium""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""policies"": [");
            sb.AppendLine(@"					{");
            sb.AppendLine(@"						""id"": ""d566166e-ed6a-4601-ae5d-cd79d458667d"",");
            sb.AppendLine(@"						""name"": ""1416 Funding""");
            sb.AppendLine(@"					}");
            sb.AppendLine(@"				],");
            sb.AppendLine(@"				""specification"": {");
            sb.AppendLine(@"					""period"": null,");
            sb.AppendLine(@"					""fundingStream"": null,");
            sb.AppendLine(@"					""id"": ""82b15316-eb4b-4167-9dee-57bab61f8a58"",");
            sb.AppendLine(@"					""name"": ""YP 201718 16-19 Learner Responsive""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""period"": {");
            sb.AppendLine(@"					""id"": ""1718"",");
            sb.AppendLine(@"					""name"": ""2017/18""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""fundingStream"": {");
            sb.AppendLine(@"					""id"": ""YPLRA"",");
            sb.AppendLine(@"					""name"": ""16-19 Learner Responsive""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""buildProjectId"": ""52a77c16-0422-4e83-89a7-03af020ea665"",");
            sb.AppendLine(@"				""current"": {");
            sb.AppendLine(@"					""decimalPlaces"": 6,");
            sb.AppendLine(@"					""sourceCode"": ""' --- Providers ---- '\r\n\r\n' Provider fields can be accessed from the 'Provider' property:\r\n' Dim yearOpened = Provider.DateOpened.Year \r\n\r\n' --- Datasets ---- '\r\n' Referenced dataset fields can be accessed via the 'Datasets' property:\r\n' Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis\r\n\r\n' --- Caclulations ---- '\r\n' Other calculations within the same specification can be referred to directly:\r\n'  Dim rate = P004_PriRate()\r\n\r\n' For backwards compatability legacy Store functions and properties are available\r\n 'LAToProv()\r\n' Exclude()\r\n' Print()\r\n' IIf()\r\n' currentScenario \r\n' rid \r\n \r\n' If Decimal.MinValue is returned it will exclude the result of this calculation for this provider\r\n\r\nReturn Decimal.MinValue\r\n"",");
            sb.AppendLine(@"					""version"": 1,");
            sb.AppendLine(@"					""date"": ""2018-01-30T17:54:05.5999891Z"",");
            sb.AppendLine(@"					""author"": {");
            sb.AppendLine(@"						""id"": ""b001af14-3754-4cb1-9980-359e850700a8"",");
            sb.AppendLine(@"						""name"": ""testuser""");
            sb.AppendLine(@"					},");
            sb.AppendLine(@"					""comment"": null,");
            sb.AppendLine(@"					""publishStatus"": ""Draft""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""published"": null,");
            sb.AppendLine(@"				""history"": [");
            sb.AppendLine(@"					{");
            sb.AppendLine(@"						""decimalPlaces"": 6,");
            sb.AppendLine(@"						""sourceCode"": ""' --- Providers ---- '\r\n\r\n' Provider fields can be accessed from the 'Provider' property:\r\n' Dim yearOpened = Provider.DateOpened.Year \r\n\r\n' --- Datasets ---- '\r\n' Referenced dataset fields can be accessed via the 'Datasets' property:\r\n' Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis\r\n\r\n' --- Caclulations ---- '\r\n' Other calculations within the same specification can be referred to directly:\r\n'  Dim rate = P004_PriRate()\r\n\r\n' For backwards compatability legacy Store functions and properties are available\r\n 'LAToProv()\r\n' Exclude()\r\n' Print()\r\n' IIf()\r\n' currentScenario \r\n' rid \r\n \r\n' If Decimal.MinValue is returned it will exclude the result of this calculation for this provider\r\n\r\nReturn Decimal.MinValue\r\n"",");
            sb.AppendLine(@"						""version"": 1,");
            sb.AppendLine(@"						""date"": ""2018-01-30T17:54:05.6000075Z"",");
            sb.AppendLine(@"						""author"": {");
            sb.AppendLine(@"							""id"": ""b001af14-3754-4cb1-9980-359e850700a8"",");
            sb.AppendLine(@"							""name"": ""testuser""");
            sb.AppendLine(@"						},");
            sb.AppendLine(@"						""comment"": null,");
            sb.AppendLine(@"						""publishStatus"": ""Draft""");
            sb.AppendLine(@"					}");
            sb.AppendLine(@"				],");
            sb.AppendLine(@"				""id"": ""3ed13a77-58e1-4220-8740-2678520ed265"",");
            sb.AppendLine(@"				""name"": ""F1_7_14to16_PP Service Child""");
            sb.AppendLine(@"			},");
            sb.AppendLine(@"			{");
            sb.AppendLine(@"				""calculationSpecification"": {");
            sb.AppendLine(@"					""id"": ""1e803ae3-7fb5-4201-8da7-9e65ce1d0b8b"",");
            sb.AppendLine(@"					""name"": ""F1_6_14to16_PP Free Meals Funding""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""allocationLine"": {");
            sb.AppendLine(@"					""id"": ""YPA21"",");
            sb.AppendLine(@"					""name"": ""14-16 Pupil Premium""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""policies"": [");
            sb.AppendLine(@"					{");
            sb.AppendLine(@"						""id"": ""d566166e-ed6a-4601-ae5d-cd79d458667d"",");
            sb.AppendLine(@"						""name"": ""1416 Funding""");
            sb.AppendLine(@"					}");
            sb.AppendLine(@"				],");
            sb.AppendLine(@"				""specification"": {");
            sb.AppendLine(@"					""period"": null,");
            sb.AppendLine(@"					""fundingStream"": null,");
            sb.AppendLine(@"					""id"": ""82b15316-eb4b-4167-9dee-57bab61f8a58"",");
            sb.AppendLine(@"					""name"": ""YP 201718 16-19 Learner Responsive""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""period"": {");
            sb.AppendLine(@"					""id"": ""1718"",");
            sb.AppendLine(@"					""name"": ""2017/18""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""fundingStream"": {");
            sb.AppendLine(@"					""id"": ""YPLRA"",");
            sb.AppendLine(@"					""name"": ""16-19 Learner Responsive""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""buildProjectId"": ""2876ae8d-76e1-429f-bcce-838c9edf39f3"",");
            sb.AppendLine(@"				""current"": {");
            sb.AppendLine(@"					""decimalPlaces"": 6,");
            sb.AppendLine(@"					""sourceCode"": ""' --- Providers ---- '\r\n\r\n' Provider fields can be accessed from the 'Provider' property:\r\n' Dim yearOpened = Provider.DateOpened.Year \r\n\r\n' --- Datasets ---- '\r\n' Referenced dataset fields can be accessed via the 'Datasets' property:\r\n' Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis\r\n\r\n' --- Caclulations ---- '\r\n' Other calculations within the same specification can be referred to directly:\r\n'  Dim rate = P004_PriRate()\r\n\r\n' For backwards compatability legacy Store functions and properties are available\r\n 'LAToProv()\r\n' Exclude()\r\n' Print()\r\n' IIf()\r\n' currentScenario \r\n' rid \r\n \r\n' If Decimal.MinValue is returned it will exclude the result of this calculation for this provider\r\n\r\nReturn Decimal.MinValue\r\n"",");
            sb.AppendLine(@"					""version"": 1,");
            sb.AppendLine(@"					""date"": ""2018-01-30T17:54:07.135306Z"",");
            sb.AppendLine(@"					""author"": {");
            sb.AppendLine(@"						""id"": ""b001af14-3754-4cb1-9980-359e850700a8"",");
            sb.AppendLine(@"						""name"": ""testuser""");
            sb.AppendLine(@"					},");
            sb.AppendLine(@"					""comment"": null,");
            sb.AppendLine(@"					""publishStatus"": ""Draft""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""published"": null,");
            sb.AppendLine(@"				""history"": [");
            sb.AppendLine(@"					{");
            sb.AppendLine(@"						""decimalPlaces"": 6,");
            sb.AppendLine(@"						""sourceCode"": ""' --- Providers ---- '\r\n\r\n' Provider fields can be accessed from the 'Provider' property:\r\n' Dim yearOpened = Provider.DateOpened.Year \r\n\r\n' --- Datasets ---- '\r\n' Referenced dataset fields can be accessed via the 'Datasets' property:\r\n' Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis\r\n\r\n' --- Caclulations ---- '\r\n' Other calculations within the same specification can be referred to directly:\r\n'  Dim rate = P004_PriRate()\r\n\r\n' For backwards compatability legacy Store functions and properties are available\r\n 'LAToProv()\r\n' Exclude()\r\n' Print()\r\n' IIf()\r\n' currentScenario \r\n' rid \r\n \r\n' If Decimal.MinValue is returned it will exclude the result of this calculation for this provider\r\n\r\nReturn Decimal.MinValue\r\n"",");
            sb.AppendLine(@"						""version"": 1,");
            sb.AppendLine(@"						""date"": ""2018-01-30T17:54:07.1353216Z"",");
            sb.AppendLine(@"						""author"": {");
            sb.AppendLine(@"							""id"": ""b001af14-3754-4cb1-9980-359e850700a8"",");
            sb.AppendLine(@"							""name"": ""testuser""");
            sb.AppendLine(@"						},");
            sb.AppendLine(@"						""comment"": null,");
            sb.AppendLine(@"						""publishStatus"": ""Draft""");
            sb.AppendLine(@"					}");
            sb.AppendLine(@"				],");
            sb.AppendLine(@"				""id"": ""7f5b6ee5-680d-4706-a16e-64d898b7ab01"",");
            sb.AppendLine(@"				""name"": ""F1_6_14to16_PP Free Meals Funding""");
            sb.AppendLine(@"			},");
            sb.AppendLine(@"			{");
            sb.AppendLine(@"				""calculationSpecification"": {");
            sb.AppendLine(@"					""id"": ""b074faae-fd23-439a-add2-826df5904769"",");
            sb.AppendLine(@"					""name"": ""F1_5_14to16_PP Care Funding""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""allocationLine"": {");
            sb.AppendLine(@"					""id"": ""YPA21"",");
            sb.AppendLine(@"					""name"": ""14-16 Pupil Premium""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""policies"": [");
            sb.AppendLine(@"					{");
            sb.AppendLine(@"						""id"": ""d566166e-ed6a-4601-ae5d-cd79d458667d"",");
            sb.AppendLine(@"						""name"": ""1416 Funding""");
            sb.AppendLine(@"					}");
            sb.AppendLine(@"				],");
            sb.AppendLine(@"				""specification"": {");
            sb.AppendLine(@"					""period"": null,");
            sb.AppendLine(@"					""fundingStream"": null,");
            sb.AppendLine(@"					""id"": ""82b15316-eb4b-4167-9dee-57bab61f8a58"",");
            sb.AppendLine(@"					""name"": ""YP 201718 16-19 Learner Responsive""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""period"": {");
            sb.AppendLine(@"					""id"": ""1718"",");
            sb.AppendLine(@"					""name"": ""2017/18""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""fundingStream"": {");
            sb.AppendLine(@"					""id"": ""YPLRA"",");
            sb.AppendLine(@"					""name"": ""16-19 Learner Responsive""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""buildProjectId"": ""735dae21-0b28-4144-8a9e-4c2d794dfd7a"",");
            sb.AppendLine(@"				""current"": {");
            sb.AppendLine(@"					""decimalPlaces"": 6,");
            sb.AppendLine(@"					""sourceCode"": ""' --- Providers ---- '\r\n\r\n' Provider fields can be accessed from the 'Provider' property:\r\n' Dim yearOpened = Provider.DateOpened.Year \r\n\r\n' --- Datasets ---- '\r\n' Referenced dataset fields can be accessed via the 'Datasets' property:\r\n' Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis\r\n\r\n' --- Caclulations ---- '\r\n' Other calculations within the same specification can be referred to directly:\r\n'  Dim rate = P004_PriRate()\r\n\r\n' For backwards compatability legacy Store functions and properties are available\r\n 'LAToProv()\r\n' Exclude()\r\n' Print()\r\n' IIf()\r\n' currentScenario \r\n' rid \r\n \r\n' If Decimal.MinValue is returned it will exclude the result of this calculation for this provider\r\n\r\nReturn Decimal.MinValue\r\n"",");
            sb.AppendLine(@"					""version"": 1,");
            sb.AppendLine(@"					""date"": ""2018-01-30T17:54:08.2976614Z"",");
            sb.AppendLine(@"					""author"": {");
            sb.AppendLine(@"						""id"": ""b001af14-3754-4cb1-9980-359e850700a8"",");
            sb.AppendLine(@"						""name"": ""testuser""");
            sb.AppendLine(@"					},");
            sb.AppendLine(@"					""comment"": null,");
            sb.AppendLine(@"					""publishStatus"": ""Draft""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""published"": null,");
            sb.AppendLine(@"				""history"": [");
            sb.AppendLine(@"					{");
            sb.AppendLine(@"						""decimalPlaces"": 6,");
            sb.AppendLine(@"						""sourceCode"": ""' --- Providers ---- '\r\n\r\n' Provider fields can be accessed from the 'Provider' property:\r\n' Dim yearOpened = Provider.DateOpened.Year \r\n\r\n' --- Datasets ---- '\r\n' Referenced dataset fields can be accessed via the 'Datasets' property:\r\n' Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis\r\n\r\n' --- Caclulations ---- '\r\n' Other calculations within the same specification can be referred to directly:\r\n'  Dim rate = P004_PriRate()\r\n\r\n' For backwards compatability legacy Store functions and properties are available\r\n 'LAToProv()\r\n' Exclude()\r\n' Print()\r\n' IIf()\r\n' currentScenario \r\n' rid \r\n \r\n' If Decimal.MinValue is returned it will exclude the result of this calculation for this provider\r\n\r\nReturn Decimal.MinValue\r\n"",");
            sb.AppendLine(@"						""version"": 1,");
            sb.AppendLine(@"						""date"": ""2018-01-30T17:54:08.297677Z"",");
            sb.AppendLine(@"						""author"": {");
            sb.AppendLine(@"							""id"": ""b001af14-3754-4cb1-9980-359e850700a8"",");
            sb.AppendLine(@"							""name"": ""testuser""");
            sb.AppendLine(@"						},");
            sb.AppendLine(@"						""comment"": null,");
            sb.AppendLine(@"						""publishStatus"": ""Draft""");
            sb.AppendLine(@"					}");
            sb.AppendLine(@"				],");
            sb.AppendLine(@"				""id"": ""641635ec-f3f5-48e7-9146-daf129c334bb"",");
            sb.AppendLine(@"				""name"": ""F1_5_14to16_PP Care Funding""");
            sb.AppendLine(@"			},");
            sb.AppendLine(@"			{");
            sb.AppendLine(@"				""calculationSpecification"": {");
            sb.AppendLine(@"					""id"": ""40926e4d-3ae5-4b9d-91e2-903dddf022a7"",");
            sb.AppendLine(@"					""name"": ""F1_10_14to16 Total Programme Funding""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""allocationLine"": {");
            sb.AppendLine(@"					""id"": ""YPA20"",");
            sb.AppendLine(@"					""name"": ""14-16 Programme Funding""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""policies"": [");
            sb.AppendLine(@"					{");
            sb.AppendLine(@"						""id"": ""d566166e-ed6a-4601-ae5d-cd79d458667d"",");
            sb.AppendLine(@"						""name"": ""1416 Funding""");
            sb.AppendLine(@"					}");
            sb.AppendLine(@"				],");
            sb.AppendLine(@"				""specification"": {");
            sb.AppendLine(@"					""period"": null,");
            sb.AppendLine(@"					""fundingStream"": null,");
            sb.AppendLine(@"					""id"": ""82b15316-eb4b-4167-9dee-57bab61f8a58"",");
            sb.AppendLine(@"					""name"": ""YP 201718 16-19 Learner Responsive""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""period"": {");
            sb.AppendLine(@"					""id"": ""1718"",");
            sb.AppendLine(@"					""name"": ""2017/18""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""fundingStream"": {");
            sb.AppendLine(@"					""id"": ""YPLRA"",");
            sb.AppendLine(@"					""name"": ""16-19 Learner Responsive""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""buildProjectId"": ""dc5d4eb9-4310-4e5b-b1c4-05f986ad7d02"",");
            sb.AppendLine(@"				""current"": {");
            sb.AppendLine(@"					""decimalPlaces"": 6,");
            sb.AppendLine(@"					""sourceCode"": ""' --- Providers ---- '\r\n\r\n' Provider fields can be accessed from the 'Provider' property:\r\n' Dim yearOpened = Provider.DateOpened.Year \r\n\r\n' --- Datasets ---- '\r\n' Referenced dataset fields can be accessed via the 'Datasets' property:\r\n' Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis\r\n\r\n' --- Caclulations ---- '\r\n' Other calculations within the same specification can be referred to directly:\r\n'  Dim rate = P004_PriRate()\r\n\r\n' For backwards compatability legacy Store functions and properties are available\r\n 'LAToProv()\r\n' Exclude()\r\n' Print()\r\n' IIf()\r\n' currentScenario \r\n' rid \r\n \r\n' If Decimal.MinValue is returned it will exclude the result of this calculation for this provider\r\n\r\nReturn Decimal.MinValue\r\n"",");
            sb.AppendLine(@"					""version"": 1,");
            sb.AppendLine(@"					""date"": ""2018-01-30T17:54:10.1180539Z"",");
            sb.AppendLine(@"					""author"": {");
            sb.AppendLine(@"						""id"": ""b001af14-3754-4cb1-9980-359e850700a8"",");
            sb.AppendLine(@"						""name"": ""testuser""");
            sb.AppendLine(@"					},");
            sb.AppendLine(@"					""comment"": null,");
            sb.AppendLine(@"					""publishStatus"": ""Draft""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""published"": null,");
            sb.AppendLine(@"				""history"": [");
            sb.AppendLine(@"					{");
            sb.AppendLine(@"						""decimalPlaces"": 6,");
            sb.AppendLine(@"						""sourceCode"": ""' --- Providers ---- '\r\n\r\n' Provider fields can be accessed from the 'Provider' property:\r\n' Dim yearOpened = Provider.DateOpened.Year \r\n\r\n' --- Datasets ---- '\r\n' Referenced dataset fields can be accessed via the 'Datasets' property:\r\n' Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis\r\n\r\n' --- Caclulations ---- '\r\n' Other calculations within the same specification can be referred to directly:\r\n'  Dim rate = P004_PriRate()\r\n\r\n' For backwards compatability legacy Store functions and properties are available\r\n 'LAToProv()\r\n' Exclude()\r\n' Print()\r\n' IIf()\r\n' currentScenario \r\n' rid \r\n \r\n' If Decimal.MinValue is returned it will exclude the result of this calculation for this provider\r\n\r\nReturn Decimal.MinValue\r\n"",");
            sb.AppendLine(@"						""version"": 1,");
            sb.AppendLine(@"						""date"": ""2018-01-30T17:54:10.1180691Z"",");
            sb.AppendLine(@"						""author"": {");
            sb.AppendLine(@"							""id"": ""b001af14-3754-4cb1-9980-359e850700a8"",");
            sb.AppendLine(@"							""name"": ""testuser""");
            sb.AppendLine(@"						},");
            sb.AppendLine(@"						""comment"": null,");
            sb.AppendLine(@"						""publishStatus"": ""Draft""");
            sb.AppendLine(@"					}");
            sb.AppendLine(@"				],");
            sb.AppendLine(@"				""id"": ""8fa3ac3e-8c86-4d67-91f2-9be47e942a19"",");
            sb.AppendLine(@"				""name"": ""F1_10_14to16 Total Programme Funding""");
            sb.AppendLine(@"			},");
            sb.AppendLine(@"			{");
            sb.AppendLine(@"				""calculationSpecification"": {");
            sb.AppendLine(@"					""id"": ""d924d55d-0b97-461c-990c-9142fb520b77"",");
            sb.AppendLine(@"					""name"": ""Test Calc 124""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""allocationLine"": {");
            sb.AppendLine(@"					""id"": ""YPA01"",");
            sb.AppendLine(@"					""name"": ""16 -19 Low Level Learners Programme funding""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""policies"": [");
            sb.AppendLine(@"					{");
            sb.AppendLine(@"						""id"": ""72bea276-b034-4931-a69b-2e168a3e310b"",");
            sb.AppendLine(@"						""name"": ""1619 High Needs""");
            sb.AppendLine(@"					}");
            sb.AppendLine(@"				],");
            sb.AppendLine(@"				""specification"": {");
            sb.AppendLine(@"					""period"": null,");
            sb.AppendLine(@"					""fundingStream"": null,");
            sb.AppendLine(@"					""id"": ""82b15316-eb4b-4167-9dee-57bab61f8a58"",");
            sb.AppendLine(@"					""name"": ""YP 201718 16-19 Learner Responsive""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""period"": {");
            sb.AppendLine(@"					""id"": ""1718"",");
            sb.AppendLine(@"					""name"": ""2017/18""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""fundingStream"": {");
            sb.AppendLine(@"					""id"": ""YPLRA"",");
            sb.AppendLine(@"					""name"": ""16-19 Learner Responsive""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""buildProjectId"": ""a03f41ec-dddf-458c-9ea1-4684934ce389"",");
            sb.AppendLine(@"				""current"": {");
            sb.AppendLine(@"					""decimalPlaces"": 6,");
            sb.AppendLine(@"					""sourceCode"": ""' --- Providers ---- '\r\n\r\n' Provider fields can be accessed from the 'Provider' property:\r\n' Dim yearOpened = Provider.DateOpened.Year \r\n\r\n' --- Datasets ---- '\r\n' Referenced dataset fields can be accessed via the 'Datasets' property:\r\n' Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis\r\n\r\n' --- Caclulations ---- '\r\n' Other calculations within the same specification can be referred to directly:\r\n'  Dim rate = P004_PriRate()\r\n\r\n' For backwards compatability legacy Store functions and properties are available\r\n 'LAToProv()\r\n' Exclude()\r\n' Print()\r\n' IIf()\r\n' currentScenario \r\n' rid \r\n \r\n' If Decimal.MinValue is returned it will exclude the result of this calculation for this provider\r\n\r\nReturn 43.40\r\n"",");
            sb.AppendLine(@"					""version"": 2,");
            sb.AppendLine(@"					""date"": ""2018-03-14T08:50:11.9786506Z"",");
            sb.AppendLine(@"					""author"": {");
            sb.AppendLine(@"						""id"": ""b001af14-3754-4cb1-9980-359e850700a8"",");
            sb.AppendLine(@"						""name"": ""testuser""");
            sb.AppendLine(@"					},");
            sb.AppendLine(@"					""comment"": null,");
            sb.AppendLine(@"					""publishStatus"": ""Draft""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				""published"": null,");
            sb.AppendLine(@"				""history"": [");
            sb.AppendLine(@"					{");
            sb.AppendLine(@"						""decimalPlaces"": 6,");
            sb.AppendLine(@"						""sourceCode"": ""' --- Providers ---- '\r\n\r\n' Provider fields can be accessed from the 'Provider' property:\r\n' Dim yearOpened = Provider.DateOpened.Year \r\n\r\n' --- Datasets ---- '\r\n' Referenced dataset fields can be accessed via the 'Datasets' property:\r\n' Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis\r\n\r\n' --- Caclulations ---- '\r\n' Other calculations within the same specification can be referred to directly:\r\n'  Dim rate = P004_PriRate()\r\n\r\n' For backwards compatability legacy Store functions and properties are available\r\n 'LAToProv()\r\n' Exclude()\r\n' Print()\r\n' IIf()\r\n' currentScenario \r\n' rid \r\n \r\n' If Decimal.MinValue is returned it will exclude the result of this calculation for this provider\r\n\r\nReturn Decimal.MinValue\r\n"",");
            sb.AppendLine(@"						""version"": 1,");
            sb.AppendLine(@"						""date"": ""2018-01-31T14:53:32.2120693Z"",");
            sb.AppendLine(@"						""author"": {");
            sb.AppendLine(@"							""id"": ""b001af14-3754-4cb1-9980-359e850700a8"",");
            sb.AppendLine(@"							""name"": ""testuser""");
            sb.AppendLine(@"						},");
            sb.AppendLine(@"						""comment"": null,");
            sb.AppendLine(@"						""publishStatus"": ""Draft""");
            sb.AppendLine(@"					},");
            sb.AppendLine(@"					{");
            sb.AppendLine(@"						""decimalPlaces"": 6,");
            sb.AppendLine(@"						""sourceCode"": ""' --- Providers ---- '\r\n\r\n' Provider fields can be accessed from the 'Provider' property:\r\n' Dim yearOpened = Provider.DateOpened.Year \r\n\r\n' --- Datasets ---- '\r\n' Referenced dataset fields can be accessed via the 'Datasets' property:\r\n' Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis\r\n\r\n' --- Caclulations ---- '\r\n' Other calculations within the same specification can be referred to directly:\r\n'  Dim rate = P004_PriRate()\r\n\r\n' For backwards compatability legacy Store functions and properties are available\r\n 'LAToProv()\r\n' Exclude()\r\n' Print()\r\n' IIf()\r\n' currentScenario \r\n' rid \r\n \r\n' If Decimal.MinValue is returned it will exclude the result of this calculation for this provider\r\n\r\nReturn 43.40\r\n"",");
            sb.AppendLine(@"						""version"": 2,");
            sb.AppendLine(@"						""date"": ""2018-03-14T08:50:11.9786506Z"",");
            sb.AppendLine(@"						""author"": {");
            sb.AppendLine(@"							""id"": ""b001af14-3754-4cb1-9980-359e850700a8"",");
            sb.AppendLine(@"							""name"": ""testuser""");
            sb.AppendLine(@"						},");
            sb.AppendLine(@"						""comment"": null,");
            sb.AppendLine(@"						""publishStatus"": ""Draft""");
            sb.AppendLine(@"					}");
            sb.AppendLine(@"				],");
            sb.AppendLine(@"				""id"": ""724950e7-7df8-48dd-8d80-bb30e04feb6c"",");
            sb.AppendLine(@"				""name"": ""Test Calc 124""");
            sb.AppendLine(@"			}");
            sb.AppendLine(@"		],");
            sb.AppendLine(@"		""datasetRelationships"": null,");
            sb.AppendLine(@"		""build"": {");
            sb.AppendLine(@"			""success"": true,");
            sb.AppendLine(@"			""compilerMessages"": [],");
            sb.AppendLine(@"			""sourceFiles"": [");
            sb.AppendLine(@"				{");
            sb.AppendLine(@"					""FileName"": ""Common\\Attributes\\FieldAttribute.vb"",");
            sb.AppendLine(@"					""SourceCode"": ""Imports System\r\n<AttributeUsage(AttributeTargets.Property)> Class FieldAttribute\r\n    Inherits System.Attribute\r\n\r\n    Public Property Id() As String\r\n    Public Property Name() As String\r\n\r\nEnd Class""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				{");
            sb.AppendLine(@"					""FileName"": ""Common\\Attributes\\DatasetRelationshipAttribute.vb"",");
            sb.AppendLine(@"					""SourceCode"": ""Imports System\r\n<AttributeUsage(AttributeTargets.Property)> Class DatasetRelationshipAttribute\r\n    Inherits System.Attribute\r\n    Public Property Id() As String\r\n    Public Property Name() As String\r\n\r\nEnd Class""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				{");
            sb.AppendLine(@"					""FileName"": ""Common\\Attributes\\AllocationLineAttribute.vb"",");
            sb.AppendLine(@"					""SourceCode"": ""Imports System\r\n<AttributeUsage(AttributeTargets.Method)> Class AllocationLineAttribute\r\n    Inherits  System.Attribute\r\n\r\n    Public Property Id() As String\r\n    Public Property Name() As String\r\n\r\nEnd Class""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				{");
            sb.AppendLine(@"					""FileName"": ""Common\\Scenario.vb"",");
            sb.AppendLine(@"					""SourceCode"": ""Public Class Scenario\r\n\r\n    Public Property periodid() As Integer\r\n\r\nEnd Class""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				{");
            sb.AppendLine(@"					""FileName"": ""Common\\Provider.vb"",");
            sb.AppendLine(@"					""SourceCode"": ""\r\nPublic Class Provider\r\n\r\n    Public Property Id() As String\r\n    Public Property DateOpened() As Date\r\n    Public Property ConvertDate() As Date\r\n    Public Property ProviderType() As String\r\n    Public Property ProviderSubType() As string\r\n\r\nEnd Class""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				{");
            sb.AppendLine(@"					""FileName"": ""Common\\BaseCalculation.vb"",");
            sb.AppendLine(@"					""SourceCode"": ""Public Class BaseCalculation\r\n\r\n    Public Property Provider() as Provider\r\n\r\n#Region \""Legacy Store Support\""\r\n\r\n    public Property rid as String\r\n    Public Property currentscenario As Scenario\r\n\r\n    Public Sub Print(Of T) (value As T, name As String, rid As String)\r\n        \r\n    End Sub\r\n\r\n    public Function LAToProv(Of T)(value as T) As T\r\n        Return value\r\n    End Function\r\n\r\n    public Function IIf(Of T)(value as T, one As Boolean, two as Boolean) As T\r\n        Return value\r\n    End Function\r\n\r\n    Public Sub Exclude (rid As String)\r\n        \r\n    End Sub\r\n\r\n#End Region\r\n\r\nEnd Class""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				{");
            sb.AppendLine(@"					""FileName"": ""Common\\Attributes\\CalculationAttribute.vb"",");
            sb.AppendLine(@"					""SourceCode"": ""Imports System\r\n<AttributeUsage(AttributeTargets.Method)> Class CalculationAttribute\r\n    Inherits  System.Attribute\r\n\r\n    Public Property Id() As String\r\n    Public Property Name() As String\r\n\r\nEnd Class""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				{");
            sb.AppendLine(@"					""FileName"": ""Common\\Attributes\\CalculationSpecificationAttribute.vb"",");
            sb.AppendLine(@"					""SourceCode"": ""Imports System\r\n<AttributeUsage(AttributeTargets.Method)> Class CalculationSpecificationAttribute\r\n    Inherits  System.Attribute\r\n\r\n    Public Property Id() As String\r\n    Public Property Name() As String\r\n\r\n \r\nEnd Class""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				{");
            sb.AppendLine(@"					""FileName"": ""Common\\Attributes\\PolicySpecificationAttribute.vb"",");
            sb.AppendLine(@"					""SourceCode"": ""Imports System\r\n\r\n<AttributeUsage(AttributeTargets.Method, AllowMultiple := True)> Class PolicySpecificationAttribute\r\n    Inherits Attribute\r\n\r\n    Public Property Id() As String\r\n    Public Property Name() As String\r\n \r\n\r\nEnd Class""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				{");
            sb.AppendLine(@"					""FileName"": ""CalculateFunding.vbproj"",");
            sb.AppendLine(@"					""SourceCode"": ""<Project Sdk=\""Microsoft.NET.Sdk\"">\r\n\r\n  <PropertyGroup>\r\n    <TargetFramework>netcoreapp2.0</TargetFramework>\r\n  </PropertyGroup>\r\n\r\n</Project>\r\n""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				{");
            sb.AppendLine(@"					""FileName"": ""Datasets/Datasets.vb"",");
            sb.AppendLine(@"					""SourceCode"": ""Public Class Datasets\r\nEnd Class""");
            sb.AppendLine(@"				},");
            sb.AppendLine(@"				{");
            sb.AppendLine(@"					""FileName"": ""Calculations.vb"",");
            sb.AppendLine(@"					""SourceCode"": ""Imports System\r\nImports System.Collections.Generic\r\n\r\nPublic Class Calculations\r\n    Inherits BaseCalculation\r\n\r\n    Public Property Datasets As Datasets\r\n\r\n    <Calculation(Id:=\""f4dd989f-c9f4-4dae-88df-2e1cc0fad091\"", Name:=\""F4_2 High Needs 1619\"")>\r\n    <CalculationSpecification(Id:=\""a7eb5876-a447-4629-a731-5607f8662fcf\"", Name:=\""F4_2 High Needs 1619\"")>\r\n    <AllocationLine(Id:=\""YPA16\"", Name:=\""16-19 High Needs Element 2\"")>\r\n    <PolicySpecification(Id:=\""72bea276-b034-4931-a69b-2e168a3e310b\"", Name:=\""1619 High Needs\"")>\r\n    Public Function F4_2HighNeeds1619 As Decimal\r\n#ExternalSource(\""f4dd989f-c9f4-4dae-88df-2e1cc0fad091|F4_2 High Needs 1619\"", 1)\r\n        Return 42\r\n#End ExternalSource\r\n    End Function\r\n\r\n    <Calculation(Id:=\""b9964b73-120c-4908-beb6-e6f8e8d42d84\"", Name:=\""F4_3 Bursary Fund 1619\"")>\r\n    <CalculationSpecification(Id:=\""7d18bb00-fc99-4a30-9e13-48c38b5ff613\"", Name:=\""F4_3 Bursary Fund 1619\"")>\r\n    <AllocationLine(Id:=\""YPA12\"", Name:=\""16-19 Bursary Funds\"")>\r\n    <PolicySpecification(Id:=\""6c139cec-b0e4-4bfb-8920-6528f69fb539\"", Name:=\""1619 Learner Support\"")>\r\n    Public Function F4_3BursaryFund1619 As Decimal\r\n#ExternalSource(\""b9964b73-120c-4908-beb6-e6f8e8d42d84|F4_3 Bursary Fund 1619\"", 1)\r\n        ' --- Providers ---- '\r\n        ' Provider fields can be accessed from the 'Provider' property:\r\n        ' Dim yearOpened = Provider.DateOpened.Year \r\n        ' --- Datasets ---- '\r\n        ' Referenced dataset fields can be accessed via the 'Datasets' property:\r\n        ' Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis\r\n        ' --- Caclulations ---- '\r\n        ' Other calculations within the same specification can be referred to directly:\r\n        '  Dim rate = P004_PriRate()\r\n        ' For backwards compatability legacy Store functions and properties are available\r\n        'LAToProv()\r\n        ' Exclude()\r\n        ' Print()\r\n        ' IIf()\r\n        ' currentScenario \r\n        ' rid \r\n        ' If Decimal.MinValue is returned it will exclude the result of this calculation for this provider\r\n        Return Decimal.MinValue\r\n#End ExternalSource\r\n    End Function\r\n\r\n    <Calculation(Id:=\""e41718df-54ac-4e83-b2fc-6455c61d88d0\"", Name:=\""F4_3d Free Meals Funding 1619\"")>\r\n    <CalculationSpecification(Id:=\""8e1a24b7-9405-472e-b38a-a236ec56715c\"", Name:=\""F4_3d Free Meals Funding 1619\"")>\r\n    <AllocationLine(Id:=\""YPA23\"", Name:=\""16-19 Free Meals in FE\"")>\r\n    <PolicySpecification(Id:=\""5b6105fa-522a-4f0a-92ee-86f45be17e3f\"", Name:=\""1619 Free Meals Funding\"")>\r\n    Public Function F4_3dFreeMealsFunding1619 As Decimal\r\n#ExternalSource(\""e41718df-54ac-4e83-b2fc-6455c61d88d0|F4_3d Free Meals Funding 1619\"", 1)\r\n        ' --- Providers ---- '\r\n        ' Provider fields can be accessed from the 'Provider' property:\r\n        ' Dim yearOpened = Provider.DateOpened.Year \r\n        ' --- Datasets ---- '\r\n        ' Referenced dataset fields can be accessed via the 'Datasets' property:\r\n        ' Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis\r\n        ' --- Caclulations ---- '\r\n        ' Other calculations within the same specification can be referred to directly:\r\n        '  Dim rate = P004_PriRate()\r\n        ' For backwards compatability legacy Store functions and properties are available\r\n        'LAToProv()\r\n        ' Exclude()\r\n        ' Print()\r\n        ' IIf()\r\n        ' currentScenario \r\n        ' rid \r\n        ' If Decimal.MinValue is returned it will exclude the result of this calculation for this provider\r\n        Return Decimal.MinValue\r\n#End ExternalSource\r\n    End Function\r\n\r\n    <Calculation(Id:=\""f28952e6-0865-4587-8bb2-f4db89942eb3\"", Name:=\""F4_5 Formula Protection Funding 1619\"")>\r\n    <CalculationSpecification(Id:=\""164d62b0-0d7f-4b2a-b091-cbb5980fc18a\"", Name:=\""F4_5 Formula Protection Funding 1619\"")>\r\n    <AllocationLine(Id:=\""YPA15\"", Name:=\""16-19 Formula Protection Funding\"")>\r\n    <PolicySpecification(Id:=\""3dec3fd9-32b3-483d-8e6e-050c87bdd805\"", Name:=\""FPF 1619\"")>\r\n    Public Function F4_5FormulaProtectionFunding1619 As Decimal\r\n#ExternalSource(\""f28952e6-0865-4587-8bb2-f4db89942eb3|F4_5 Formula Protection Funding 1619\"", 1)\r\n        ' --- Providers ---- '\r\n        ' Provider fields can be accessed from the 'Provider' property:\r\n        ' Dim yearOpened = Provider.DateOpened.Year \r\n        ' --- Datasets ---- '\r\n        ' Referenced dataset fields can be accessed via the 'Datasets' property:\r\n        ' Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis\r\n        ' --- Caclulations ---- '\r\n        ' Other calculations within the same specification can be referred to directly:\r\n        '  Dim rate = P004_PriRate()\r\n        ' For backwards compatability legacy Store functions and properties are available\r\n        'LAToProv()\r\n        ' Exclude()\r\n        ' Print()\r\n        ' IIf()\r\n        ' currentScenario \r\n        ' rid \r\n        ' If Decimal.MinValue is returned it will exclude the result of this calculation for this provider\r\n        Return Decimal.MinValue\r\n#End ExternalSource\r\n    End Function\r\n\r\n    <Calculation(Id:=\""01ca2c20-9a66-4770-94d5-c35d97b1fff0\"", Name:=\""V1_9g 1617 16_19 students inc exceptions BC\"")>\r\n    <CalculationSpecification(Id:=\""a1eed0af-85bd-48f4-85d5-60f68ea0c5a1\"", Name:=\""V1_9g 1617 16_19 students inc exceptions BC\"")>\r\n    <AllocationLine(Id:=\""YPA14\"", Name:=\""16-19 Total Programme Funding\"")>\r\n    <PolicySpecification(Id:=\""17ff10cd-1823-44d2-88ba-f98b051876a0\"", Name:=\""1619 R04 Volumes\"")>\r\n    Public Function V1_9g161716_19studentsincexceptionsBC As Decimal\r\n#ExternalSource(\""01ca2c20-9a66-4770-94d5-c35d97b1fff0|V1_9g 1617 16_19 students inc exceptions BC\"", 1)\r\n        ' --- Providers ---- '\r\n        ' Provider fields can be accessed from the 'Provider' property:\r\n        ' Dim yearOpened = Provider.DateOpened.Year \r\n        ' --- Datasets ---- '\r\n        ' Referenced dataset fields can be accessed via the 'Datasets' property:\r\n        ' Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis\r\n        ' --- Caclulations ---- '\r\n        ' Other calculations within the same specification can be referred to directly:\r\n        '  Dim rate = P004_PriRate()\r\n        ' For backwards compatability legacy Store functions and properties are available\r\n        'LAToProv()\r\n        ' Exclude()\r\n        ' Print()\r\n        ' IIf()\r\n        ' currentScenario \r\n        ' rid \r\n        ' If Decimal.MinValue is returned it will exclude the result of this calculation for this provider\r\n        Return Decimal.MinValue\r\n#End ExternalSource\r\n    End Function\r\n\r\n    <Calculation(Id:=\""53f8931c-d7e4-4492-bb3c-bb32c6a9cec0\"", Name:=\""F1_25 19 plus discretionary bursary fund\"")>\r\n    <CalculationSpecification(Id:=\""84a973dd-a7af-4121-bdda-3a6b3fd036b1\"", Name:=\""F1_25 19 plus discretionary bursary fund\"")>\r\n    <AllocationLine(Id:=\""YPA12\"", Name:=\""16-19 Bursary Funds\"")>\r\n    <PolicySpecification(Id:=\""10a9acc4-bfda-428a-b029-626ef34fb303\"", Name:=\""19 plus continuing\"")>\r\n    Public Function F1_2519plusdiscretionarybursaryfund As Decimal\r\n#ExternalSource(\""53f8931c-d7e4-4492-bb3c-bb32c6a9cec0|F1_25 19 plus discretionary bursary fund\"", 1)\r\n        ' --- Providers ---- '\r\n        ' Provider fields can be accessed from the 'Provider' property:\r\n        ' Dim yearOpened = Provider.DateOpened.Year \r\n        ' --- Datasets ---- '\r\n        ' Referenced dataset fields can be accessed via the 'Datasets' property:\r\n        ' Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis\r\n        ' --- Caclulations ---- '\r\n        ' Other calculations within the same specification can be referred to directly:\r\n        '  Dim rate = P004_PriRate()\r\n        ' For backwards compatability legacy Store functions and properties are available\r\n        'LAToProv()\r\n        ' Exclude()\r\n        ' Print()\r\n        ' IIf()\r\n        ' currentScenario \r\n        ' rid \r\n        ' If Decimal.MinValue is returned it will exclude the result of this calculation for this provider\r\n        Return Decimal.MinValue\r\n#End ExternalSource\r\n    End Function\r\n\r\n    <Calculation(Id:=\""d19bd996-8a59-4155-9aa9-58442c4cb8d1\"", Name:=\""F1_26a 19 plus Free Meals inc BC excl de minimus\"")>\r\n    <CalculationSpecification(Id:=\""290bb168-3490-4e02-b981-21069a105b3c\"", Name:=\""F1_26a 19 plus Free Meals inc BC excl de minimus\"")>\r\n    <AllocationLine(Id:=\""YPA23\"", Name:=\""16-19 Free Meals in FE\"")>\r\n    <PolicySpecification(Id:=\""d635fe27-a008-488b-8ab6-d1bfee8110b2\"", Name:=\""Continuing Students Free Meals Funding\"")>\r\n    Public Function F1_26a19plusFreeMealsincBCexcldeminimus As Decimal\r\n#ExternalSource(\""d19bd996-8a59-4155-9aa9-58442c4cb8d1|F1_26a 19 plus Free Meals inc BC excl de minimus\"", 1)\r\n        ' --- Providers ---- '\r\n        ' Provider fields can be accessed from the 'Provider' property:\r\n        ' Dim yearOpened = Provider.DateOpened.Year \r\n        ' --- Datasets ---- '\r\n        ' Referenced dataset fields can be accessed via the 'Datasets' property:\r\n        ' Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis\r\n        ' --- Caclulations ---- '\r\n        ' Other calculations within the same specification can be referred to directly:\r\n        '  Dim rate = P004_PriRate()\r\n        ' For backwards compatability legacy Store functions and properties are available\r\n        'LAToProv()\r\n        ' Exclude()\r\n        ' Print()\r\n        ' IIf()\r\n        ' currentScenario \r\n        ' rid \r\n        ' If Decimal.MinValue is returned it will exclude the result of this calculation for this provider\r\n        Return Decimal.MinValue\r\n#End ExternalSource\r\n    End Function\r\n\r\n    <Calculation(Id:=\""a8d2c2c4-bfc6-4349-8de5-99acb8ecc12f\"", Name:=\""F4_1d Programme funding _ excluding SPIs\"")>\r\n    <CalculationSpecification(Id:=\""d38017ba-5191-4214-8e69-6a134be0d47d\"", Name:=\""F4_1d Programme funding _ excluding SPIs\"")>\r\n    <AllocationLine(Id:=\""YPA14\"", Name:=\""16-19 Total Programme Funding\"")>\r\n    <PolicySpecification(Id:=\""7149b126-7ecf-4f63-a3d6-c48920a8fc21\"", Name:=\""1619FE Programme Funding\"")>\r\n    Public Function F4_1dProgrammefunding_excludingSPIs As Decimal\r\n#ExternalSource(\""a8d2c2c4-bfc6-4349-8de5-99acb8ecc12f|F4_1d Programme funding _ excluding SPIs\"", 1)\r\n        ' --- Providers ---- '\r\n        ' Provider fields can be accessed from the 'Provider' property:\r\n        ' Dim yearOpened = Provider.DateOpened.Year \r\n        ' --- Datasets ---- '\r\n        ' Referenced dataset fields can be accessed via the 'Datasets' property:\r\n        ' Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis\r\n        ' --- Caclulations ---- '\r\n        ' Other calculations within the same specification can be referred to directly:\r\n        '  Dim rate = P004_PriRate()\r\n        ' For backwards compatability legacy Store functions and properties are available\r\n        'LAToProv()\r\n        ' Exclude()\r\n        ' Print()\r\n        ' IIf()\r\n        ' currentScenario \r\n        ' rid \r\n        ' If Decimal.MinValue is returned it will exclude the result of this calculation for this provider\r\n        Return Decimal.MinValue\r\n#End ExternalSource\r\n    End Function\r\n\r\n    <Calculation(Id:=\""4368c77d-2899-49f0-9d53-78a09ff91309\"", Name:=\""F4_1e Programme funding _ SPI only\"")>\r\n    <CalculationSpecification(Id:=\""bf453c4e-957e-4586-974f-b2a27ea0e621\"", Name:=\""F4_1e Programme funding _ SPI only\"")>\r\n    <AllocationLine(Id:=\""YPA24\"", Name:=\""16-19 SPI Element 1\"")>\r\n    <PolicySpecification(Id:=\""7149b126-7ecf-4f63-a3d6-c48920a8fc21\"", Name:=\""1619FE Programme Funding\"")>\r\n    Public Function F4_1eProgrammefunding_SPIonly As Decimal\r\n#ExternalSource(\""4368c77d-2899-49f0-9d53-78a09ff91309|F4_1e Programme funding _ SPI only\"", 1)\r\n        ' --- Providers ---- '\r\n        ' Provider fields can be accessed from the 'Provider' property:\r\n        ' Dim yearOpened = Provider.DateOpened.Year \r\n        ' --- Datasets ---- '\r\n        ' Referenced dataset fields can be accessed via the 'Datasets' property:\r\n        ' Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis\r\n        ' --- Caclulations ---- '\r\n        ' Other calculations within the same specification can be referred to directly:\r\n        '  Dim rate = P004_PriRate()\r\n        ' For backwards compatability legacy Store functions and properties are available\r\n        'LAToProv()\r\n        ' Exclude()\r\n        ' Print()\r\n        ' IIf()\r\n        ' currentScenario \r\n        ' rid \r\n        ' If Decimal.MinValue is returned it will exclude the result of this calculation for this provider\r\n        Return Decimal.MinValue\r\n#End ExternalSource\r\n    End Function\r\n\r\n    <Calculation(Id:=\""3ed13a77-58e1-4220-8740-2678520ed265\"", Name:=\""F1_7_14to16_PP Service Child\"")>\r\n    <CalculationSpecification(Id:=\""16b2eb9e-6991-437b-a49a-c0537ccae8ed\"", Name:=\""F1_7_14to16_PP Service Child\"")>\r\n    <AllocationLine(Id:=\""YPA22\"", Name:=\""14-16 service child premium\"")>\r\n    <PolicySpecification(Id:=\""d566166e-ed6a-4601-ae5d-cd79d458667d\"", Name:=\""1416 Funding\"")>\r\n    Public Function F1_7_14to16_PPServiceChild As Decimal\r\n#ExternalSource(\""3ed13a77-58e1-4220-8740-2678520ed265|F1_7_14to16_PP Service Child\"", 1)\r\n        ' --- Providers ---- '\r\n        ' Provider fields can be accessed from the 'Provider' property:\r\n        ' Dim yearOpened = Provider.DateOpened.Year \r\n        ' --- Datasets ---- '\r\n        ' Referenced dataset fields can be accessed via the 'Datasets' property:\r\n        ' Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis\r\n        ' --- Caclulations ---- '\r\n        ' Other calculations within the same specification can be referred to directly:\r\n        '  Dim rate = P004_PriRate()\r\n        ' For backwards compatability legacy Store functions and properties are available\r\n        'LAToProv()\r\n        ' Exclude()\r\n        ' Print()\r\n        ' IIf()\r\n        ' currentScenario \r\n        ' rid \r\n        ' If Decimal.MinValue is returned it will exclude the result of this calculation for this provider\r\n        Return Decimal.MinValue\r\n#End ExternalSource\r\n    End Function\r\n\r\n    <Calculation(Id:=\""7f5b6ee5-680d-4706-a16e-64d898b7ab01\"", Name:=\""F1_6_14to16_PP Free Meals Funding\"")>\r\n    <CalculationSpecification(Id:=\""1e803ae3-7fb5-4201-8da7-9e65ce1d0b8b\"", Name:=\""F1_6_14to16_PP Free Meals Funding\"")>\r\n    <AllocationLine(Id:=\""YPA21\"", Name:=\""14-16 Pupil Premium\"")>\r\n    <PolicySpecification(Id:=\""d566166e-ed6a-4601-ae5d-cd79d458667d\"", Name:=\""1416 Funding\"")>\r\n    Public Function F1_6_14to16_PPFreeMealsFunding As Decimal\r\n#ExternalSource(\""7f5b6ee5-680d-4706-a16e-64d898b7ab01|F1_6_14to16_PP Free Meals Funding\"", 1)\r\n        ' --- Providers ---- '\r\n        ' Provider fields can be accessed from the 'Provider' property:\r\n        ' Dim yearOpened = Provider.DateOpened.Year \r\n        ' --- Datasets ---- '\r\n        ' Referenced dataset fields can be accessed via the 'Datasets' property:\r\n        ' Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis\r\n        ' --- Caclulations ---- '\r\n        ' Other calculations within the same specification can be referred to directly:\r\n        '  Dim rate = P004_PriRate()\r\n        ' For backwards compatability legacy Store functions and properties are available\r\n        'LAToProv()\r\n        ' Exclude()\r\n        ' Print()\r\n        ' IIf()\r\n        ' currentScenario \r\n        ' rid \r\n        ' If Decimal.MinValue is returned it will exclude the result of this calculation for this provider\r\n        Return Decimal.MinValue\r\n#End ExternalSource\r\n    End Function\r\n\r\n    <Calculation(Id:=\""641635ec-f3f5-48e7-9146-daf129c334bb\"", Name:=\""F1_5_14to16_PP Care Funding\"")>\r\n    <CalculationSpecification(Id:=\""b074faae-fd23-439a-add2-826df5904769\"", Name:=\""F1_5_14to16_PP Care Funding\"")>\r\n    <AllocationLine(Id:=\""YPA21\"", Name:=\""14-16 Pupil Premium\"")>\r\n    <PolicySpecification(Id:=\""d566166e-ed6a-4601-ae5d-cd79d458667d\"", Name:=\""1416 Funding\"")>\r\n    Public Function F1_5_14to16_PPCareFunding As Decimal\r\n#ExternalSource(\""641635ec-f3f5-48e7-9146-daf129c334bb|F1_5_14to16_PP Care Funding\"", 1)\r\n        ' --- Providers ---- '\r\n        ' Provider fields can be accessed from the 'Provider' property:\r\n        ' Dim yearOpened = Provider.DateOpened.Year \r\n        ' --- Datasets ---- '\r\n        ' Referenced dataset fields can be accessed via the 'Datasets' property:\r\n        ' Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis\r\n        ' --- Caclulations ---- '\r\n        ' Other calculations within the same specification can be referred to directly:\r\n        '  Dim rate = P004_PriRate()\r\n        ' For backwards compatability legacy Store functions and properties are available\r\n        'LAToProv()\r\n        ' Exclude()\r\n        ' Print()\r\n        ' IIf()\r\n        ' currentScenario \r\n        ' rid \r\n        ' If Decimal.MinValue is returned it will exclude the result of this calculation for this provider\r\n        Return Decimal.MinValue\r\n#End ExternalSource\r\n    End Function\r\n\r\n    <Calculation(Id:=\""8fa3ac3e-8c86-4d67-91f2-9be47e942a19\"", Name:=\""F1_10_14to16 Total Programme Funding\"")>\r\n    <CalculationSpecification(Id:=\""40926e4d-3ae5-4b9d-91e2-903dddf022a7\"", Name:=\""F1_10_14to16 Total Programme Funding\"")>\r\n    <AllocationLine(Id:=\""YPA20\"", Name:=\""14-16 Programme Funding\"")>\r\n    <PolicySpecification(Id:=\""d566166e-ed6a-4601-ae5d-cd79d458667d\"", Name:=\""1416 Funding\"")>\r\n    Public Function F1_10_14to16TotalProgrammeFunding As Decimal\r\n#ExternalSource(\""8fa3ac3e-8c86-4d67-91f2-9be47e942a19|F1_10_14to16 Total Programme Funding\"", 1)\r\n        ' --- Providers ---- '\r\n        ' Provider fields can be accessed from the 'Provider' property:\r\n        ' Dim yearOpened = Provider.DateOpened.Year \r\n        ' --- Datasets ---- '\r\n        ' Referenced dataset fields can be accessed via the 'Datasets' property:\r\n        ' Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis\r\n        ' --- Caclulations ---- '\r\n        ' Other calculations within the same specification can be referred to directly:\r\n        '  Dim rate = P004_PriRate()\r\n        ' For backwards compatability legacy Store functions and properties are available\r\n        'LAToProv()\r\n        ' Exclude()\r\n        ' Print()\r\n        ' IIf()\r\n        ' currentScenario \r\n        ' rid \r\n        ' If Decimal.MinValue is returned it will exclude the result of this calculation for this provider\r\n        Return Decimal.MinValue\r\n#End ExternalSource\r\n    End Function\r\n\r\n    <Calculation(Id:=\""724950e7-7df8-48dd-8d80-bb30e04feb6c\"", Name:=\""Test Calc 124\"")>\r\n    <CalculationSpecification(Id:=\""d924d55d-0b97-461c-990c-9142fb520b77\"", Name:=\""Test Calc 124\"")>\r\n    <AllocationLine(Id:=\""YPA01\"", Name:=\""16 -19 Low Level Learners Programme funding\"")>\r\n    <PolicySpecification(Id:=\""72bea276-b034-4931-a69b-2e168a3e310b\"", Name:=\""1619 High Needs\"")>\r\n    Public Function TestCalc124 As Decimal\r\n#ExternalSource(\""724950e7-7df8-48dd-8d80-bb30e04feb6c|Test Calc 124\"", 1)\r\n        ' --- Providers ---- '\r\n        ' Provider fields can be accessed from the 'Provider' property:\r\n        ' Dim yearOpened = Provider.DateOpened.Year \r\n        ' --- Datasets ---- '\r\n        ' Referenced dataset fields can be accessed via the 'Datasets' property:\r\n        ' Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis\r\n        ' --- Caclulations ---- '\r\n        ' Other calculations within the same specification can be referred to directly:\r\n        '  Dim rate = P004_PriRate()\r\n        ' For backwards compatability legacy Store functions and properties are available\r\n        'LAToProv()\r\n        ' Exclude()\r\n        ' Print()\r\n        ' IIf()\r\n        ' currentScenario \r\n        ' rid \r\n        ' If Decimal.MinValue is returned it will exclude the result of this calculation for this provider\r\n        Return 43.40\r\n#End ExternalSource\r\n    End Function\r\nEnd Class\r\n""");
            sb.AppendLine(@"				}");
            sb.AppendLine(@"			]");
            sb.AppendLine(@"		},");
            sb.AppendLine(@"		""id"": ""2f2432ff-3b75-4622-a1b0-7f6bc3d30096"",");
            sb.AppendLine(@"		""name"": ""YP 201718 16-19 Learner Responsive""");
            sb.AppendLine(@"	}");

            return sb.ToString();
        }

        public static byte[] GetMockAssembly()
        {
            string assemblyAsString = "TVqQAAMAAAAEAAAA//8AALgAAAAAAAAAQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAgAAAAA4fug4AtAnNIbgBTM0hVGhpcyBwcm9ncmFtIGNhbm5vdCBiZSBydW4gaW4gRE9TIG1vZGUuDQ0KJAAAAAAAAABQRQAATAECAETiqFoAAAAAAAAAAOAAIiALAVAAACYAAAACAAAAAAAAqkUAAAAgAAAAYAAAAAAAEAAgAAAAAgAABAAAAAAAAAAEAAAAAAAAAACAAAAAAgAAAAAAAAMAQIUAABAAABAAAAAAEAAAEAAAAAAAABAAAAAAAAAAAAAAAFhFAABPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAGAAAAwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAIAAACAAAAAAAAAAAAAAACCAAAEgAAAAAAAAAAAAAAC50ZXh0AAAAsCUAAAAgAAAAJgAAAAIAAAAAAAAAAAAAAAAAACAAAGAucmVsb2MAAAwAAAAAYAAAAAIAAAAoAAAAAAAAAAAAAAAAAABAAABCAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACMRQAAAAAAAEgAAAACAAUA9CMAAGQhAAABAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAB4CKAcAAAoqJgJ7AQAABCsAKiICA30BAAAEKiYCewIAAAQrACoiAgN9AgAABComAnsDAAAEKwAqIgIDfQMAAAQqJgJ7BAAABCsAKiICA30EAAAEKiYCewUAAAQrACoiAgN9BQAABComAnsGAAAEKwAqIgIDfQYAAAQqHgIoCAAACiomAnsHAAAEKwAqIgIDfQcAAAQqJgJ7CAAABCsAKiICA30IAAAEKiYCewkAAAQrACoiAgN9CQAABComAnsKAAAEKwAqIgIDfQoAAAQqJgJ7CwAABCsAKiICA30LAAAEKiYCewwAAAQrACoiAgN9DAAABComAnsNAAAEKwAqIgIDfQ0AAAQqJgJ7DgAABCsAKiICA30OAAAEKiYCew8AAAQrACoiAgN9DwAABCoKACoTMAEABwAAAAEAABEAAworAAYqABMwAQAHAAAAAQAAEQADCisABiomAnsQAAAEKwAqIgIDfRAAAAQqJgJ7EQAABCsAKiICA30RAAAEKiYCexIAAAQrACoiAgN9EgAABComAnsTAAAEKwAqIgIDfRMAAAQqJgJ7FAAABCsAKiICA30UAAAEKiYCexUAAAQrACoiAgN9FQAABCoeAigeAAAGKiYCexYAAAQrACoiAgN9FgAABCoTMAIADwAAAAIAABEAEgAfKmooCQAACisABioAEzAGABEAAAACAAARABIAFRUVFxYoCgAACisABioAAAATMAYAEQAAAAIAABEAEgAVFRUXFigKAAAKKwAGKgAAABMwBgARAAAAAgAAEQASABUVFRcWKAoAAAorAAYqAAAAEzAGABEAAAACAAARABIAFRUVFxYoCgAACisABioAAAATMAYAEQAAAAIAABEAEgAVFRUXFigKAAAKKwAGKgAAABMwBgARAAAAAgAAEQASABUVFRcWKAoAAAorAAYqAAAAEzAGABEAAAACAAARABIAFRUVFxYoCgAACisABioAAAATMAYAEQAAAAIAABEAEgAVFRUXFigKAAAKKwAGKgAAABMwBgARAAAAAgAAEQASABUVFRcWKAoAAAorAAYqAAAAEzAGABEAAAACAAARABIAFRUVFxYoCgAACisABioAAAATMAYAEQAAAAIAABEAEgAVFRUXFigKAAAKKwAGKgAAABMwBgARAAAAAgAAEQASABUVFRcWKAoAAAorAAYqAAAAEzAGABUAAAACAAARABIAILIBAAAWFhYXKAoAAAorAAYqAAAAQlNKQgEAAQAAAAAADAAAAHY0LjAuMzAzMTkAAAAABQBsAAAAWAwAACN+AADEDAAAQAUAACNTdHJpbmdzAAAAAAQSAAAEAAAAI1VTAAgSAAAQAAAAI0dVSUQAAAAYEgAATA8AACNCbG9iAAAAAAAAAAIAAAFXFaIBCQQAAAD6ATMAFgAAAQAAAA0AAAAMAAAAFgAAAEkAAAAeAAAACgAAAJkAAAACAAAACgAAABYAAAAsAAAAAQAAAAEAAAADAAAAAACvAwEAAAAAAAYA8gJ8BAYAEgN8BAYAOgJpBA8AnAQAAAYAuATZAwYAIgLZAwYAJgPZAwYA+AF8BAYA4QFpBAYATgJpBAYACwXZAwYAagHZAwYApwPZAwAAAABuAAAAAAABAAEAAAAAABMCAAAdAAEAAQAAAAAA1QIAAB0AAwAGAAAAAABpAgAAHQAFAAsAAQAAAPADAAAtAAcAEAABAAAANgQAAC0ACAATAAEAAADgAwAALQANAB4AAAAAAMACAAAdABAAKQAAAAAAgQIAAB0AEgAuAAAAAACjAgAAHQAUADMAAQAAANoEAAAtABYAOAABAAAAqwQAABwAFgA5AAEAwAA9AAEAXwE9AAEAwAA9AAEAXwE9AAEAwAA9AAEAXwE9AAEA8gBAAAEAwAA9AAEA1gBDAAEA1AFDAAEAswE9AAEAjgE9AAEANQRHAAEABwE9AAEAEARLAAEAwAA9AAEAXwE9AAEAwAA9AAEAXwE9AAEAwAA9AAEAXwE9AAEA2QRPAFAgAAAAAAYYPwQGAAEAWCAAAAAABgi2AFMAAQBiIAAAAAAGCL0AVwABAGsgAAAAAAYIUwFTAAIAdSAAAAAABghcAVcAAgBQIAAAAAAGGD8EBgADAH4gAAAAAAYItgBTAAMAiCAAAAAABgi9AFcAAwCRIAAAAAAGCFMBUwAEAJsgAAAAAAYIXAFXAAQAUCAAAAAABhg/BAYABQCkIAAAAAAGCLYAUwAFAK4gAAAAAAYIvQBXAAUAtyAAAAAABghTAVMABgDBIAAAAAAGCFwBVwAGAMogAAAAAAYYPwQGAAcA0iAAAAAABgjiAFwABwDcIAAAAAAGCO8AAQAHAMogAAAAAAYYPwQGAAgA5SAAAAAABgi2AFMACADvIAAAAAAGCL0AVwAIAPggAAAAAAYIxABgAAkAAiEAAAAABgjTAGUACQALIQAAAAAGCMEBYAAKABUhAAAAAAYI0QFlAAoAHiEAAAAABgifAVMACwAoIQAAAAAGCLABVwALADEhAAAAAAYIdwFTAAwAOyEAAAAABgiLAVcADADKIAAAAAAGGD8EBgANAEQhAAAAAAYIJQRrAA0ATiEAAAAABggyBHAADQBXIQAAAAAGCPwAUwAOAGEhAAAAAAYIBAFXAA4AaiEAAAAABgj5A3YADwB0IQAAAAAGCA0EewAPAH0hAAAAAAYAEgWBABAAgCEAAAAABgAYBYoAEwCUIQAAAAAGAEgDkgAUAH0hAAAAAAYASwFXABcAUCAAAAAABhg/BAYAGACnIQAAAAAGCLYAUwAYALEhAAAAAAYIvQBXABgAuiEAAAAABghTAVMAGQDEIQAAAAAGCFwBVwAZAFAgAAAAAAYYPwQGABoAzSEAAAAABgi2AFMAGgDXIQAAAAAGCL0AVwAaAOAhAAAAAAYIUwFTABsA6iEAAAAABghcAVcAGwBQIAAAAAAGGD8EBgAcAPMhAAAAAAYItgBTABwA/SEAAAAABgi9AFcAHAAGIgAAAAAGCFMBUwAdABAiAAAAAAYIXAFXAB0AyiAAAAAABhg/BAYAHgAZIgAAAAAGGD8EBgAeACEiAAAAAAYIyQScAB4AKyIAAAAABgjWBKEAHgA0IgAAAAAGAFwApwAfAFAiAAAAAAYADQCnAB8AcCIAAAAABgBCAKcAHwCQIgAAAAAGACEApwAfALAiAAAAAAYAdwCnAB8A0CIAAAAABgAnAacAHwDwIgAAAAAGAOMEpwAfABAjAAAAAAYARQSnAB8AMCMAAAAABgAhBacAHwBQIwAAAAAGAAwBpwAfAHAjAAAAAAYAiAOnAB8AkCMAAAAABgBuA6cAHwCwIwAAAAAGAEwDpwAfANAjAAAAAAYAAQCnAB8AAAABADADAAABADADAAABADADAAABADADAAABADADAAABADADAAABADADAAABADADAAABADADAAABADADAAABADADAAABADADAAABADADAAABADADAAABADADAAABAEIDAAACAGUBAAADAAgBAAABAEIDAAABAEIDAAACAHMBAAADACEEAAABAAgBAAABADADAAABADADAAABADADAAABADADAAABADADAAABADADAAABADADCQA/BAEAEQA/BAYAGQA/BAoAMQA/BBAAQQA/BAYAUQA/BBYAOQA/BAYAWQA/BAYAaQA/BCYAaQA/BCsAIQArAPkAIQAzABQPLgALAMgALgATANEALgAbAPAAQAArAPkAQQArAPkAQQAzABQPQwAjAB0PYAArAPkAYQArAPkAYQAzABQPYwAjAB0PgAArAPkAgQArAPkAgQAzABQPgwAjACYPoAArAPkAoQArAPkAoQAzABQPwQArAPkAwQAzABQP4AArAPkA4QArAPkA4QAzABQPAAErAPkAAQErAPkAAQEzABQPAwEjACYPIAErAPkAIQErAPkAIQEzABQPIwEjACYPQAErAPkAQQErAPkAQQEzABQPQwEjAC8PYQErAPkAYQEzABQPgAErAPkAgQErAPkAgQEzABQPoAErAPkAoQErAPkAoQEzABQPwAErAPkAwQErAPkAwQEzABQP4AErAPkA4QErAPkA4QEzABQPAQIrAPkAAQIzABQPIAIrAPkAIQIrAPkAIQIzABQPQAIrAPkAQQIrAPkAQQIzABQPYQIrAPkAYQIzABQPgAIrAPkAgQIrAPkAgQIzABQPoAIrAPkAoQIrAPkAoQIzABQPwAIrAPkAwQIrAPkAwQIzABQP4AIrAPkAAAMrAPkAIAMrAPkAQAMrAPkAYAMrAPkAgAMrAPkAoAMrAPkA4AMrAPkAAAQrAPkAIAQrAPkAQAQrAPkAYAQrAPkAgAQrAPkAQAUrAPkAYAUrAPkAgAUrAPkAoAUrAPkA4AUrAPkAAAYrAPkAIAYrAPkAQAYrAPkAgAYrAPkAoAYrAPkAwAYrAPkA4AYrAPkAQAcrAPkAYAcrAPkAgAdKAf4AgAdyAUkBgAdaAJQBgAeaAcYBoAdKAQwCoAdyAVkCoAdaAKYCoAeaAdECwAdKARwDwAdyAXADwAdaAMQDwAeaAfID4AdKAUAE4AdyAZsE4AdaAPYE4AeaAS4FAAhKAW0FAAhyAc8FAAhaADEGAAiaAWYGIAhKAa0GIAhyAQwHIAhaAKYCIAiaAWsHQAhKAbQHQAhyARsIQAhaAMQDQAiaAYIIYAhKAd8IYAhyAT4JYAhaADEGYAiaAZ0JgAhKAewJgAhyAUUKgAhaAJ4KgAiaAZ0JoAhKAckKoAhyARwLoAhaAG8LoAiaAaILwAhKAeULwAhyAT0MwAhaAJUMwAiaAaIL4AhKAcAM4AhyARIN4AhaAJUM4AiaAaILAAlKAWQNAAlyAb8NAAlaABoOAAmaAaILIAlKAUkOIAlyAY0OIAlaANEOIAmaAcYBHAAhAAIAAQADAAMABAAFAAUABwAGAAgABwANAAgAEAAJABIACgAUAAwAFgAAAMEArAAAAGABrAAAAMEArAAAAGABrAAAAMEArAAAAGABrAAAAPMAsAAAAMEArAAAANcAtAAAANUBtAAAALQBrAAAAI8BrAAAADYEuQAAAAgBrAAAABEEvgAAAMEArAAAAGABrAAAAMEArAAAAGABrAAAAMEArAAAAGABrAAAANoEwwACAAIAAwABAAMAAwACAAQABQABAAUABQACAAcABwABAAgABwACAAkACQABAAoACQACAAwACwABAA0ACwACAA4ADQABAA8ADQACABEADwABABIADwACABQAEQABABUAEQACABYAEwABABcAEwACABgAFQABABkAFQACABoAFwABABsAFwACABwAGQABAB0AGQACAB8AGwABACAAGwACACEAHQABACIAHQACACMAHwABACQAHwACACoAIQABACsAIQACACwAIwABAC0AIwACAC8AJQABADAAJQACADEAJwABADIAJwACADQAKQABADUAKQACADYAKwABADcAKwACADoALQABADsALQAEgAAAAAAAAAAAAAAAAAAAAADGAwAABAAAAAAAAAAAAAAANACfAAAAAAAAAAAASwCdAAAAAABNAJ0AAAAAAE8AnQAAAABUZXN0Q2FsYzEyNABGNF8zQnVyc2FyeUZ1bmQxNjE5AEY0XzVGb3JtdWxhUHJvdGVjdGlvbkZ1bmRpbmcxNjE5AEY0XzNkRnJlZU1lYWxzRnVuZGluZzE2MTkARjRfMkhpZ2hOZWVkczE2MTkAPE1vZHVsZT4AVjFfOWcxNjE3MTZfMTlzdHVkZW50c2luY2V4Y2VwdGlvbnNCQwBUAFN5c3RlbS5Qcml2YXRlLkNvcmVMaWIAZ2V0X0lkAHNldF9JZABnZXRfRGF0ZU9wZW5lZABzZXRfRGF0ZU9wZW5lZABnZXRfcGVyaW9kaWQAc2V0X3BlcmlvZGlkAGdldF9yaWQAc2V0X3JpZABGMV83XzE0dG8xNl9QUFNlcnZpY2VDaGlsZABGMV8yNTE5cGx1c2Rpc2NyZXRpb25hcnlidXJzYXJ5ZnVuZABFeGNsdWRlAGdldF9OYW1lAHNldF9OYW1lAG5hbWUARGF0ZVRpbWUAb25lAGdldF9Qcm92aWRlclN1YlR5cGUAc2V0X1Byb3ZpZGVyU3ViVHlwZQBnZXRfUHJvdmlkZXJUeXBlAHNldF9Qcm92aWRlclR5cGUAZ2V0X0NvbnZlcnREYXRlAHNldF9Db252ZXJ0RGF0ZQBEZWJ1Z2dlckJyb3dzYWJsZVN0YXRlAENvbXBpbGVyR2VuZXJhdGVkQXR0cmlidXRlAEZpZWxkQXR0cmlidXRlAEF0dHJpYnV0ZVVzYWdlQXR0cmlidXRlAERlYnVnZ2FibGVBdHRyaWJ1dGUARGVidWdnZXJCcm93c2FibGVBdHRyaWJ1dGUAQWxsb2NhdGlvbkxpbmVBdHRyaWJ1dGUAQ2FsY3VsYXRpb25TcGVjaWZpY2F0aW9uQXR0cmlidXRlAFBvbGljeVNwZWNpZmljYXRpb25BdHRyaWJ1dGUAQ2FsY3VsYXRpb25BdHRyaWJ1dGUARGF0YXNldFJlbGF0aW9uc2hpcEF0dHJpYnV0ZQBDb21waWxhdGlvblJlbGF4YXRpb25zQXR0cmlidXRlAFJ1bnRpbWVDb21wYXRpYmlsaXR5QXR0cmlidXRlAEF1dG9Qcm9wZXJ0eVZhbHVlAHZhbHVlAElJZgBGMV8xMF8xNHRvMTZUb3RhbFByb2dyYW1tZUZ1bmRpbmcARjFfNV8xNHRvMTZfUFBDYXJlRnVuZGluZwBGMV82XzE0dG8xNl9QUEZyZWVNZWFsc0Z1bmRpbmcARGVjaW1hbABpbXBsZW1lbnRhdGlvbi5kbGwuZGxsAGltcGxlbWVudGF0aW9uLmRsbABTeXN0ZW0AQmFzZUNhbGN1bGF0aW9uAFNjZW5hcmlvAGdldF9jdXJyZW50c2NlbmFyaW8Ac2V0X2N1cnJlbnRzY2VuYXJpbwB0d28AZ2V0X1Byb3ZpZGVyAHNldF9Qcm92aWRlcgAuY3RvcgBGNF8xZFByb2dyYW1tZWZ1bmRpbmdfZXhjbHVkaW5nU1BJcwBTeXN0ZW0uRGlhZ25vc3RpY3MAU3lzdGVtLlJ1bnRpbWUuQ29tcGlsZXJTZXJ2aWNlcwBEZWJ1Z2dpbmdNb2RlcwBDYWxjdWxhdGlvbnMAQXR0cmlidXRlVGFyZ2V0cwBnZXRfRGF0YXNldHMAc2V0X0RhdGFzZXRzAEYxXzI2YTE5cGx1c0ZyZWVNZWFsc2luY0JDZXhjbGRlbWluaW11cwBPYmplY3QAUHJpbnQATEFUb1Byb3YARjRfMWVQcm9ncmFtbWVmdW5kaW5nX1NQSW9ubHkAAAAAAAAC6vjVNwQQQ4vPtCcIUlGtAAQgAQEIAyAAAQUgAQEREQUgAQERFQUgAQERJQQHAR4ABAcBETUEIAEBCgggBQEICAgCBQh87IXXvqd5jgIGDgIGCAMGETEDBhIYAwYSFAMGEiwDIAAOBCABAQ4DIAAIBCAAETEFIAEBETEEIAASGAUgAQESGAQgABIUBSABARIUCDABAwEeAA4OBzABAR4AHgAJMAEDHgAeAAICBCAAEiwFIAEBEiwEIAARNQMoAA4DKAAIBCgAETEEKAASGAQoABIUBCgAEiwIAQAIAAAAAAAeAQABAFQCFldyYXBOb25FeGNlcHRpb25UaHJvd3MBCAEABwEAAAAABAEAAABKAQACAFQOAklkJGY0ZGQ5ODlmLWM5ZjQtNGRhZS04OGRmLTJlMWNjMGZhZDA5MVQOBE5hbWUURjRfMiBIaWdoIE5lZWRzIDE2MTlKAQACAFQOAklkJGE3ZWI1ODc2LWE0NDctNDYyOS1hNzMxLTU2MDdmODY2MmZjZlQOBE5hbWUURjRfMiBIaWdoIE5lZWRzIDE2MTkxAQACAFQOAklkBVlQQTE2VA4ETmFtZRoxNi0xOSBIaWdoIE5lZWRzIEVsZW1lbnQgMkUBAAIAVA4CSWQkNzJiZWEyNzYtYjAzNC00OTMxLWE2OWItMmUxNjhhM2UzMTBiVA4ETmFtZQ8xNjE5IEhpZ2ggTmVlZHNMAQACAFQOAklkJGI5OTY0YjczLTEyMGMtNDkwOC1iZWI2LWU2ZjhlOGQ0MmQ4NFQOBE5hbWUWRjRfMyBCdXJzYXJ5IEZ1bmQgMTYxOUwBAAIAVA4CSWQkN2QxOGJiMDAtZmM5OS00YTMwLTllMTMtNDhjMzhiNWZmNjEzVA4ETmFtZRZGNF8zIEJ1cnNhcnkgRnVuZCAxNjE5KgEAAgBUDgJJZAVZUEExMlQOBE5hbWUTMTYtMTkgQnVyc2FyeSBGdW5kc0oBAAIAVA4CSWQkNmMxMzljZWMtYjBlNC00YmZiLTg5MjAtNjUyOGY2OWZiNTM5VA4ETmFtZRQxNjE5IExlYXJuZXIgU3VwcG9ydFMBAAIAVA4CSWQkZTQxNzE4ZGYtNTRhYy00ZTgzLWIyZmMtNjQ1NWM2MWQ4OGQwVA4ETmFtZR1GNF8zZCBGcmVlIE1lYWxzIEZ1bmRpbmcgMTYxOVMBAAIAVA4CSWQkOGUxYTI0YjctOTQwNS00NzJlLWIzOGEtYTIzNmVjNTY3MTVjVA4ETmFtZR1GNF8zZCBGcmVlIE1lYWxzIEZ1bmRpbmcgMTYxOS0BAAIAVA4CSWQFWVBBMjNUDgROYW1lFjE2LTE5IEZyZWUgTWVhbHMgaW4gRkVNAQACAFQOAklkJDViNjEwNWZhLTUyMmEtNGYwYS05MmVlLTg2ZjQ1YmUxN2UzZlQOBE5hbWUXMTYxOSBGcmVlIE1lYWxzIEZ1bmRpbmdaAQACAFQOAklkJGYyODk1MmU2LTA4NjUtNDU4Ny04YmIyLWY0ZGI4OTk0MmViM1QOBE5hbWUkRjRfNSBGb3JtdWxhIFByb3RlY3Rpb24gRnVuZGluZyAxNjE5WgEAAgBUDgJJZCQxNjRkNjJiMC0wZDdmLTRiMmEtYjA5MS1jYmI1OTgwZmMxOGFUDgROYW1lJEY0XzUgRm9ybXVsYSBQcm90ZWN0aW9uIEZ1bmRpbmcgMTYxOTcBAAIAVA4CSWQFWVBBMTVUDgROYW1lIDE2LTE5IEZvcm11bGEgUHJvdGVjdGlvbiBGdW5kaW5nPgEAAgBUDgJJZCQzZGVjM2ZkOS0zMmIzLTQ4M2QtOGU2ZS0wNTBjODdiZGQ4MDVUDgROYW1lCEZQRiAxNjE5YQEAAgBUDgJJZCQwMWNhMmMyMC05YTY2LTQ3NzAtOTRkNS1jMzVkOTdiMWZmZjBUDgROYW1lK1YxXzlnIDE2MTcgMTZfMTkgc3R1ZGVudHMgaW5jIGV4Y2VwdGlvbnMgQkNhAQACAFQOAklkJGExZWVkMGFmLTg1YmQtNDhmNC04NWQ1LTYwZjY4ZWEwYzVhMVQOBE5hbWUrVjFfOWcgMTYxNyAxNl8xOSBzdHVkZW50cyBpbmMgZXhjZXB0aW9ucyBCQzQBAAIAVA4CSWQFWVBBMTRUDgROYW1lHTE2LTE5IFRvdGFsIFByb2dyYW1tZSBGdW5kaW5nRgEAAgBUDgJJZCQxN2ZmMTBjZC0xODIzLTQ0ZDItODhiYS1mOThiMDUxODc2YTBUDgROYW1lEDE2MTkgUjA0IFZvbHVtZXNeAQACAFQOAklkJDUzZjg5MzFjLWQ3ZTQtNDQ5Mi1iYjNjLWJiMzJjNmE5Y2VjMFQOBE5hbWUoRjFfMjUgMTkgcGx1cyBkaXNjcmV0aW9uYXJ5IGJ1cnNhcnkgZnVuZF4BAAIAVA4CSWQkODRhOTczZGQtYTdhZi00MTIxLWJkZGEtM2E2YjNmZDAzNmIxVA4ETmFtZShGMV8yNSAxOSBwbHVzIGRpc2NyZXRpb25hcnkgYnVyc2FyeSBmdW5kSAEAAgBUDgJJZCQxMGE5YWNjNC1iZmRhLTQyOGEtYjAyOS02MjZlZjM0ZmIzMDNUDgROYW1lEjE5IHBsdXMgY29udGludWluZ2YBAAIAVA4CSWQkZDE5YmQ5OTYtOGE1OS00MTU1LTlhYTktNTg0NDJjNGNiOGQxVA4ETmFtZTBGMV8yNmEgMTkgcGx1cyBGcmVlIE1lYWxzIGluYyBCQyBleGNsIGRlIG1pbmltdXNmAQACAFQOAklkJDI5MGJiMTY4LTM0OTAtNGUwMi1iOTgxLTIxMDY5YTEwNWIzY1QOBE5hbWUwRjFfMjZhIDE5IHBsdXMgRnJlZSBNZWFscyBpbmMgQkMgZXhjbCBkZSBtaW5pbXVzXAEAAgBUDgJJZCRkNjM1ZmUyNy1hMDA4LTQ4OGItOGFiNi1kMWJmZWU4MTEwYjJUDgROYW1lJkNvbnRpbnVpbmcgU3R1ZGVudHMgRnJlZSBNZWFscyBGdW5kaW5nXgEAAgBUDgJJZCRhOGQyYzJjNC1iZmM2LTQzNDktOGRlNS05OWFjYjhlY2MxMmZUDgROYW1lKEY0XzFkIFByb2dyYW1tZSBmdW5kaW5nIF8gZXhjbHVkaW5nIFNQSXNeAQACAFQOAklkJGQzODAxN2JhLTUxOTEtNDIxNC04ZTY5LTZhMTM0YmUwZDQ3ZFQOBE5hbWUoRjRfMWQgUHJvZ3JhbW1lIGZ1bmRpbmcgXyBleGNsdWRpbmcgU1BJc04BAAIAVA4CSWQkNzE0OWIxMjYtN2VjZi00ZjYzLWEzZDYtYzQ4OTIwYThmYzIxVA4ETmFtZRgxNjE5RkUgUHJvZ3JhbW1lIEZ1bmRpbmdYAQACAFQOAklkJDQzNjhjNzdkLTI4OTktNDlmMC05ZDUzLTc4YTA5ZmY5MTMwOVQOBE5hbWUiRjRfMWUgUHJvZ3JhbW1lIGZ1bmRpbmcgXyBTUEkgb25seVgBAAIAVA4CSWQkYmY0NTNjNGUtOTU3ZS00NTg2LTk3NGYtYjJhMjdlYTBlNjIxVA4ETmFtZSJGNF8xZSBQcm9ncmFtbWUgZnVuZGluZyBfIFNQSSBvbmx5KgEAAgBUDgJJZAVZUEEyNFQOBE5hbWUTMTYtMTkgU1BJIEVsZW1lbnQgMVIBAAIAVA4CSWQkM2VkMTNhNzctNThlMS00MjIwLTg3NDAtMjY3ODUyMGVkMjY1VA4ETmFtZRxGMV83XzE0dG8xNl9QUCBTZXJ2aWNlIENoaWxkUgEAAgBUDgJJZCQxNmIyZWI5ZS02OTkxLTQzN2ItYTQ5YS1jMDUzN2NjYWU4ZWRUDgROYW1lHEYxXzdfMTR0bzE2X1BQIFNlcnZpY2UgQ2hpbGQyAQACAFQOAklkBVlQQTIyVA4ETmFtZRsxNC0xNiBzZXJ2aWNlIGNoaWxkIHByZW1pdW1CAQACAFQOAklkJGQ1NjYxNjZlLWVkNmEtNDYwMS1hZTVkLWNkNzlkNDU4NjY3ZFQOBE5hbWUMMTQxNiBGdW5kaW5nVwEAAgBUDgJJZCQ3ZjViNmVlNS02ODBkLTQ3MDYtYTE2ZS02NGQ4OThiN2FiMDFUDgROYW1lIUYxXzZfMTR0bzE2X1BQIEZyZWUgTWVhbHMgRnVuZGluZ1cBAAIAVA4CSWQkMWU4MDNhZTMtN2ZiNS00MjAxLThkYTctOWU2NWNlMWQwYjhiVA4ETmFtZSFGMV82XzE0dG8xNl9QUCBGcmVlIE1lYWxzIEZ1bmRpbmcqAQACAFQOAklkBVlQQTIxVA4ETmFtZRMxNC0xNiBQdXBpbCBQcmVtaXVtUQEAAgBUDgJJZCQ2NDE2MzVlYy1mM2Y1LTQ4ZTctOTE0Ni1kYWYxMjljMzM0YmJUDgROYW1lG0YxXzVfMTR0bzE2X1BQIENhcmUgRnVuZGluZ1EBAAIAVA4CSWQkYjA3NGZhYWUtZmQyMy00MzlhLWFkZDItODI2ZGY1OTA0NzY5VA4ETmFtZRtGMV81XzE0dG8xNl9QUCBDYXJlIEZ1bmRpbmdaAQACAFQOAklkJDhmYTNhYzNlLThjODYtNGQ2Ny05MWYyLTliZTQ3ZTk0MmExOVQOBE5hbWUkRjFfMTBfMTR0bzE2IFRvdGFsIFByb2dyYW1tZSBGdW5kaW5nWgEAAgBUDgJJZCQ0MDkyNmU0ZC0zYWU1LTRiOWQtOTFlMi05MDNkZGRmMDIyYTdUDgROYW1lJEYxXzEwXzE0dG8xNiBUb3RhbCBQcm9ncmFtbWUgRnVuZGluZy4BAAIAVA4CSWQFWVBBMjBUDgROYW1lFzE0LTE2IFByb2dyYW1tZSBGdW5kaW5nQwEAAgBUDgJJZCQ3MjQ5NTBlNy03ZGY4LTQ4ZGQtOGQ4MC1iYjMwZTA0ZmViNmNUDgROYW1lDVRlc3QgQ2FsYyAxMjRDAQACAFQOAklkJGQ5MjRkNTVkLTBiOTctNDYxYy05OTBjLTkxNDJmYjUyMGI3N1QOBE5hbWUNVGVzdCBDYWxjIDEyNEIBAAIAVA4CSWQFWVBBMDFUDgROYW1lKzE2IC0xOSBMb3cgTGV2ZWwgTGVhcm5lcnMgUHJvZ3JhbW1lIGZ1bmRpbmcIAQAAAAAAAAAIAQCAAAAAAAAIAQBAAAAAAAAZAQBAAAAAAQBUAg1BbGxvd011bHRpcGxlAQAAAIBFAAAAAAAAAAAAAJpFAAAAIAAAAAAAAAAAAAAAAAAAAAAAAAAAAACMRQAAAAAAAAAAAAAAAF9Db3JEbGxNYWluAG1zY29yZWUuZGxsAAAAAAD/JQAgABAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABAAAAMAAAArDUAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

            return Convert.FromBase64String(assemblyAsString);
        }

        public static IList<ProviderSummary> GetDummyProviders(int count)
        {
            var summaries = new List<ProviderSummary>
            {
                new ProviderSummary
                {
                    Authority = "Sheffield",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "3732109",
                    Id = "107013",
                    LACode = null,
                    LegalName = null,
                    Name = "Sharrow Nursery and Infant School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Community",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "107013"
                },
                new ProviderSummary
                {
                    Authority = "Sheffield",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "3732114",
                    Id = "107016",
                    LACode = null,
                    LegalName = null,
                    Name = "Southey Green Junior School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Community",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "107016"
                },
                new ProviderSummary
                {
                    Authority = "East Sussex",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "8453015",
                    Id = "114496",
                    LACode = null,
                    LegalName = null,
                    Name = "Cross-in-Hand Church of England Primary School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Voluntary",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "114496"
                },
                new ProviderSummary
                {
                    Authority = "Leicestershire",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "8552148",
                    Id = "119966",
                    LACode = null,
                    LegalName = null,
                    Name = "Linden Community Junior School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Community",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "119966"
                },
                new ProviderSummary
                {
                    Authority = "Bolton",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "3502013",
                    Id = "105154",
                    LACode = null,
                    LegalName = null,
                    Name = "Devonshire Road Primary School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Community",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "105154"
                },
                new ProviderSummary
                {
                    Authority = "Trafford",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "3582045",
                    Id = "106319",
                    LACode = null,
                    LegalName = null,
                    Name = "Flixton Infant School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Community",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "106319"
                },
                new ProviderSummary
                {
                    Authority = "Dorset",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "8353696",
                    Id = "113851",
                    LACode = null,
                    LegalName = null,
                    Name = "St Michael's Church of England Voluntary Aided Primary School, Lyme Regis",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Voluntary",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "113851"
                },
                new ProviderSummary
                {
                    Authority = "Lancashire",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "8883403",
                    Id = "119470",
                    LACode = null,
                    LegalName = null,
                    Name = "Coppull Parish Church of England Primary School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Voluntary",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "119470"
                },
                new ProviderSummary
                {
                    Authority = "Wirral",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "3443335",
                    Id = "105074",
                    LACode = null,
                    LegalName = null,
                    Name = "Sacred Heart Catholic Primary School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Voluntary",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "105074"
                },
                new ProviderSummary
                {
                    Authority = "Lancashire",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "8883586",
                    Id = "119570",
                    LACode = null,
                    LegalName = null,
                    Name = "Hoole St Michael CofE Primary School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Voluntary",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "119570"
                },
                new ProviderSummary
                {
                    Authority = "Trafford",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "3583314",
                    Id = "106350",
                    LACode = null,
                    LegalName = null,
                    Name = "St Michael's CofE (Aided) Primary School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Voluntary",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "106350"
                },
                new ProviderSummary
                {
                    Authority = "Leicester",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "8562323",
                    Id = "120053",
                    LACode = null,
                    LegalName = null,
                    Name = "Woodstock Primary School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Community",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "120053"
                },
                new ProviderSummary
                {
                    Authority = "Wirral",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "3442223",
                    Id = "105024",
                    LACode = null,
                    LegalName = null,
                    Name = "Pensby Junior School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Community",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "105024"
                },
                new ProviderSummary
                {
                    Authority = "Newcastle upon Tyne",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "3914492",
                    Id = "108529",
                    LACode = null,
                    LegalName = null,
                    Name = "Denton Park Middle School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Community",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "108529"
                },
                new ProviderSummary
                {
                    Authority = "Leicestershire",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "8552015",
                    Id = "119908",
                    LACode = null,
                    LegalName = null,
                    Name = "Holmfield Primary School Leicester Forest East",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Community",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "119908"
                },
                new ProviderSummary
                {
                    Authority = "Gateshead",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "3902167",
                    Id = "108339",
                    LACode = null,
                    LegalName = null,
                    Name = "Emmaville Primary School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Community",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "108339"
                },
                new ProviderSummary
                {
                    Authority = "Doncaster",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "3712187",
                    Id = "106749",
                    LACode = null,
                    LegalName = null,
                    Name = "Conisbrough Station Road Primary School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Community",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "106749"
                },
                new ProviderSummary
                {
                    Authority = "Devon",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "8786025",
                    Id = "113592",
                    LACode = null,
                    LegalName = null,
                    Name = "Marland School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Independent",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "113592"
                },
                new ProviderSummary
                {
                    Authority = "East Sussex",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "8452078",
                    Id = "114410",
                    LACode = null,
                    LegalName = null,
                    Name = "Plumpton Primary School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Community",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "114410"
                },
                new ProviderSummary
                {
                    Authority = "Sheffield",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "3737012",
                    Id = "107170",
                    LACode = null,
                    LegalName = null,
                    Name = "Tapton Mount School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Community",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "107170"
                },
                new ProviderSummary
                {
                    Authority = "Dorset",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "8352053",
                    Id = "113685",
                    LACode = null,
                    LegalName = null,
                    Name = "Oakhurst Community First School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Community",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "113685"
                },
                new ProviderSummary
                {
                    Authority = "Brighton and Hove",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "8462079",
                    Id = "114411",
                    LACode = null,
                    LegalName = null,
                    Name = "St Peter's Community Primary School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Community",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "114411"
                },
                new ProviderSummary
                {
                    Authority = "Doncaster",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "3716009",
                    Id = "106815",
                    LACode = null,
                    LegalName = null,
                    Name = "Hill House St Mary's School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Independent",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "106815"
                },
                new ProviderSummary
                {
                    Authority = "East Sussex",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "8453063",
                    Id = "114524",
                    LACode = null,
                    LegalName = null,
                    Name = "Ticehurst and Flimwell Church of England Primary School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Voluntary",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "114524"
                },
                new ProviderSummary
                {
                    Authority = "Essex",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "8817047",
                    Id = "115463",
                    LACode = null,
                    LegalName = null,
                    Name = "The Leas School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Community",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "115463"
                },
                new ProviderSummary
                {
                    Authority = "Devon",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "8782248",
                    Id = "113171",
                    LACode = null,
                    LegalName = null,
                    Name = "Umberleigh Community Primary School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Community",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "113171"
                },
                new ProviderSummary
                {
                    Authority = "Gateshead",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "3902233",
                    Id = "108380",
                    LACode = null,
                    LegalName = null,
                    Name = "Caedmon Community Primary School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Community",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "108380"
                },
                new ProviderSummary
                {
                    Authority = "Devon",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "8789902",
                    Id = "113089",
                    LACode = null,
                    LegalName = null,
                    Name = "Topsham First School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Community",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "113089"
                },
                new ProviderSummary
                {
                    Authority = "Portsmouth",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "8512688",
                    Id = "116202",
                    LACode = null,
                    LegalName = null,
                    Name = "Arundel Court Infant School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Community",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "116202"
                },
                new ProviderSummary
                {
                    Authority = "Dorset",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "8352027",
                    Id = "113672",
                    LACode = null,
                    LegalName = null,
                    Name = "Milborne St Andrew First School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Community",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "113672"
                },
                new ProviderSummary
                {
                    Authority = "Devon",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "8782039",
                    Id = "113092",
                    LACode = null,
                    LegalName = null,
                    Name = "Whipton Barton Infants and Nursery School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Foundation",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "113092"
                },
                new ProviderSummary
                {
                    Authority = "Devon",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "8782053",
                    Id = "113102",
                    LACode = null,
                    LegalName = null,
                    Name = "Musbury Primary School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Community",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "113102"
                },
                new ProviderSummary
                {
                    Authority = "Wakefield",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "3843006",
                    Id = "108245",
                    LACode = null,
                    LegalName = null,
                    Name = "Horbury Bridge Church of England Voluntary Controlled Junior and Infant School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Voluntary",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "108245"
                },
                new ProviderSummary
                {
                    Authority = "Wirral",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "3442216",
                    Id = "105019",
                    LACode = null,
                    LegalName = null,
                    Name = "Mill Park Infant School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Community",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "105019"
                },
                new ProviderSummary
                {
                    Authority = "Sheffield",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "3732109",
                    Id = "107013",
                    LACode = null,
                    LegalName = null,
                    Name = "Sharrow Nursery and Infant School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Community",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "107013"
                },
                new ProviderSummary
                {
                    Authority = "Sheffield",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "3732114",
                    Id = "107016",
                    LACode = null,
                    LegalName = null,
                    Name = "Southey Green Junior School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Community",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "107016"
                },
                new ProviderSummary
                {
                    Authority = "East Sussex",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "8453015",
                    Id = "114496",
                    LACode = null,
                    LegalName = null,
                    Name = "Cross-in-Hand Church of England Primary School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Voluntary",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "114496"
                },
                new ProviderSummary
                {
                    Authority = "Leicestershire",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "8552148",
                    Id = "119966",
                    LACode = null,
                    LegalName = null,
                    Name = "Linden Community Junior School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Community",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "119966"
                },
                new ProviderSummary
                {
                    Authority = "Bolton",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "3502013",
                    Id = "105154",
                    LACode = null,
                    LegalName = null,
                    Name = "Devonshire Road Primary School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Community",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "105154"
                },
                new ProviderSummary
                {
                    Authority = "Trafford",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "3582045",
                    Id = "106319",
                    LACode = null,
                    LegalName = null,
                    Name = "Flixton Infant School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Community",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "106319"
                },
                new ProviderSummary
                {
                    Authority = "Dorset",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "8353696",
                    Id = "113851",
                    LACode = null,
                    LegalName = null,
                    Name = "St Michael's Church of England Voluntary Aided Primary School, Lyme Regis",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Voluntary",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "113851"
                },
                new ProviderSummary
                {
                    Authority = "Lancashire",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "8883403",
                    Id = "119470",
                    LACode = null,
                    LegalName = null,
                    Name = "Coppull Parish Church of England Primary School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Voluntary",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "119470"
                },
                new ProviderSummary
                {
                    Authority = "Wirral",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "3443335",
                    Id = "105074",
                    LACode = null,
                    LegalName = null,
                    Name = "Sacred Heart Catholic Primary School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Voluntary",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "105074"
                },
                new ProviderSummary
                {
                    Authority = "Lancashire",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "8883586",
                    Id = "119570",
                    LACode = null,
                    LegalName = null,
                    Name = "Hoole St Michael CofE Primary School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Voluntary",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "119570"
                },
                new ProviderSummary
                {
                    Authority = "Trafford",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "3583314",
                    Id = "106350",
                    LACode = null,
                    LegalName = null,
                    Name = "St Michael's CofE (Aided) Primary School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Voluntary",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "106350"
                },
                new ProviderSummary
                {
                    Authority = "Leicester",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "8562323",
                    Id = "120053",
                    LACode = null,
                    LegalName = null,
                    Name = "Woodstock Primary School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Community",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "120053"
                },
                new ProviderSummary
                {
                    Authority = "Wirral",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "3442223",
                    Id = "105024",
                    LACode = null,
                    LegalName = null,
                    Name = "Pensby Junior School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Community",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "105024"
                },
                new ProviderSummary
                {
                    Authority = "Newcastle upon Tyne",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "3914492",
                    Id = "108529",
                    LACode = null,
                    LegalName = null,
                    Name = "Denton Park Middle School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Community",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "108529"
                },
                new ProviderSummary
                {
                    Authority = "Leicestershire",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "8552015",
                    Id = "119908",
                    LACode = null,
                    LegalName = null,
                    Name = "Holmfield Primary School Leicester Forest East",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Community",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "119908"
                },
                new ProviderSummary
                {
                    Authority = "Gateshead",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "3902167",
                    Id = "108339",
                    LACode = null,
                    LegalName = null,
                    Name = "Emmaville Primary School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Community",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "108339"
                },
                new ProviderSummary
                {
                    Authority = "Doncaster",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "3712187",
                    Id = "106749",
                    LACode = null,
                    LegalName = null,
                    Name = "Conisbrough Station Road Primary School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Community",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "106749"
                },
                new ProviderSummary
                {
                    Authority = "Devon",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "8786025",
                    Id = "113592",
                    LACode = null,
                    LegalName = null,
                    Name = "Marland School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Independent",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "113592"
                },
                new ProviderSummary
                {
                    Authority = "East Sussex",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "8452078",
                    Id = "114410",
                    LACode = null,
                    LegalName = null,
                    Name = "Plumpton Primary School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Community",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "114410"
                },
                new ProviderSummary
                {
                    Authority = "Sheffield",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "3737012",
                    Id = "107170",
                    LACode = null,
                    LegalName = null,
                    Name = "Tapton Mount School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Community",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "107170"
                },
                new ProviderSummary
                {
                    Authority = "Dorset",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "8352053",
                    Id = "113685",
                    LACode = null,
                    LegalName = null,
                    Name = "Oakhurst Community First School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Community",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "113685"
                },
                new ProviderSummary
                {
                    Authority = "Brighton and Hove",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "8462079",
                    Id = "114411",
                    LACode = null,
                    LegalName = null,
                    Name = "St Peter's Community Primary School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Community",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "114411"
                },
                new ProviderSummary
                {
                    Authority = "Doncaster",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "3716009",
                    Id = "106815",
                    LACode = null,
                    LegalName = null,
                    Name = "Hill House St Mary's School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Independent",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "106815"
                },
                new ProviderSummary
                {
                    Authority = "East Sussex",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "8453063",
                    Id = "114524",
                    LACode = null,
                    LegalName = null,
                    Name = "Ticehurst and Flimwell Church of England Primary School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Voluntary",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "114524"
                },
                new ProviderSummary
                {
                    Authority = "Essex",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "8817047",
                    Id = "115463",
                    LACode = null,
                    LegalName = null,
                    Name = "The Leas School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Community",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "115463"
                },
                new ProviderSummary
                {
                    Authority = "Devon",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "8782248",
                    Id = "113171",
                    LACode = null,
                    LegalName = null,
                    Name = "Umberleigh Community Primary School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Community",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "113171"
                },
                new ProviderSummary
                {
                    Authority = "Gateshead",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "3902233",
                    Id = "108380",
                    LACode = null,
                    LegalName = null,
                    Name = "Caedmon Community Primary School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Community",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "108380"
                },
                new ProviderSummary
                {
                    Authority = "Devon",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "8789902",
                    Id = "113089",
                    LACode = null,
                    LegalName = null,
                    Name = "Topsham First School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Community",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "113089"
                },
                new ProviderSummary
                {
                    Authority = "Portsmouth",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "8512688",
                    Id = "116202",
                    LACode = null,
                    LegalName = null,
                    Name = "Arundel Court Infant School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Community",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "116202"
                },
                new ProviderSummary
                {
                    Authority = "Dorset",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "8352027",
                    Id = "113672",
                    LACode = null,
                    LegalName = null,
                    Name = "Milborne St Andrew First School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Community",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "113672"
                },
                new ProviderSummary
                {
                    Authority = "Devon",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "8782039",
                    Id = "113092",
                    LACode = null,
                    LegalName = null,
                    Name = "Whipton Barton Infants and Nursery School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Foundation",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "113092"
                },
                new ProviderSummary
                {
                    Authority = "Devon",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "8782053",
                    Id = "113102",
                    LACode = null,
                    LegalName = null,
                    Name = "Musbury Primary School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Community",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "113102"
                },
                new ProviderSummary
                {
                    Authority = "Wakefield",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "3843006",
                    Id = "108245",
                    LACode = null,
                    LegalName = null,
                    Name = "Horbury Bridge Church of England Voluntary Controlled Junior and Infant School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Voluntary",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "108245"
                },
                new ProviderSummary
                {
                    Authority = "Wirral",
                    CrmAccountId = null,
                    DateOpened = null,
                    EstablishmentNumber = "3442216",
                    Id = "105019",
                    LACode = null,
                    LegalName = null,
                    Name = "Mill Park Infant School",
                    NavVendorNo = null,
                    ProviderProfileIdType = null,
                    ProviderSubType = "Community",
                    ProviderType = "School",
                    UKPRN = "",
                    UPIN = "",
                    URN = "105019"
                }
            };

            if (count > summaries.Count)
            {
                count = summaries.Count;
            }

            return summaries.Take(count).ToList();
        }

        public static SpecificationSummary CreateSpecificationSummary(string specificationId = null)
        {
            return new SpecificationSummary()
            {
                Id = specificationId ?? new RandomString(),
                DataDefinitionRelationshipIds = new List<string>()
                {
                    new RandomString(),
                    new RandomString(),
                    new RandomString()
                }
            };
        }
    }
}