using System.Text;
using CalculateFunding.Api.External.V1.Models;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Examples;

namespace CalculateFunding.Api.External.V1.Controllers
{
    public class ProviderResultSummaryExamples : IExamplesProvider
    {
        public object GetExamples()
        {
            string sampleData = GetSampleData();

            ProviderResultSummary providerResultSummary = JsonConvert.DeserializeObject<ProviderResultSummary>(sampleData);

            return providerResultSummary;
        }

        public string GetSampleData()
        {
            var sb = new StringBuilder();

            sb.AppendLine(@"{");
            sb.AppendLine(@"	""TotalAmount"": 1194008.0,");
            sb.AppendLine(@"	""Provider"": {");
            sb.AppendLine(@"		""Ukprn"": ""106319"",");
            sb.AppendLine(@"		""Upin"": null,");
            sb.AppendLine(@"		""ProviderOpenDate"": null,");
            sb.AppendLine(@"		""LegalName"": ""FLIXTON INFANT SCHOOL""");
            sb.AppendLine(@"	},");
            sb.AppendLine(@"	""FundingPeriodResults"": [");
            sb.AppendLine(@"		{");
            sb.AppendLine(@"			""Period"": {");
            sb.AppendLine(@"				""PeriodType"": ""AY"",");
            sb.AppendLine(@"				""PeriodId"": ""AY2017181"",");
            sb.AppendLine(@"				""StartDate"": ""2017-08-31T23:00:00+00:00"",");
            sb.AppendLine(@"				""EndDate"": ""2018-08-30T23:00:00+00:00""");
            sb.AppendLine(@"			},");
            sb.AppendLine(@"			""FundingStreamResults"": [");
            sb.AppendLine(@"				{");
            sb.AppendLine(@"					""FundingStream"": {");
            sb.AppendLine(@"						""FundingStreamCode"": ""YPLRD"",");
            sb.AppendLine(@"						""FundingStreamName"": ""Academy Programme Funds"",");
            sb.AppendLine(@"						""AllocationLines"": null");
            sb.AppendLine(@"					},");
            sb.AppendLine(@"					""TotalAmount"": 1193993.0,");
            sb.AppendLine(@"					""Allocations"": [");
            sb.AppendLine(@"						{");
            sb.AppendLine(@"							""AllocationLine"": {");
            sb.AppendLine(@"								""AllocationLineCode"": ""YPD01"",");
            sb.AppendLine(@"								""AllocationLineName"": ""Academy Programme Funds""");
            sb.AppendLine(@"							},");
            sb.AppendLine(@"							""AllocationVersionNumber"": 2,");
            sb.AppendLine(@"							""AllocationStatus"": ""Approved"",");
            sb.AppendLine(@"							""AllocationAmount"": 1193993.0,");
            sb.AppendLine(@"							""AllocationLearnerCount"": null");
            sb.AppendLine(@"						}");
            sb.AppendLine(@"					],");
            sb.AppendLine(@"					""Policies"": [");
            sb.AppendLine(@"						{");
            sb.AppendLine(@"							""Policy"": {");
            sb.AppendLine(@"								""PolicyId"": ""5a806c89-493e-4b98-8a25-d623a6e06de5"",");
            sb.AppendLine(@"								""PolicyName"": ""AB Test Policy 2007-001"",");
            sb.AppendLine(@"								""PolicyDescription"": ""test""");
            sb.AppendLine(@"							},");
            sb.AppendLine(@"							""TotalAmount"": 1193993.0,");
            sb.AppendLine(@"							""Calculations"": [");
            sb.AppendLine(@"								{");
            sb.AppendLine(@"									""CalculationName"": ""AB Test Calc 2007-001"",");
            sb.AppendLine(@"									""CalculationVersionNumber"": 1,");
            sb.AppendLine(@"									""CalculationType"": ""Funding"",");
            sb.AppendLine(@"									""CalculationAmount"": 1193993.0");
            sb.AppendLine(@"								},");
            sb.AppendLine(@"								{");
            sb.AppendLine(@"									""CalculationName"": ""Ab Number 0708-001"",");
            sb.AppendLine(@"									""CalculationVersionNumber"": 1,");
            sb.AppendLine(@"									""CalculationType"": ""Number"",");
            sb.AppendLine(@"									""CalculationAmount"": 990.0");
            sb.AppendLine(@"								}");
            sb.AppendLine(@"							],");
            sb.AppendLine(@"							""SubPolicyResults"": [");
            sb.AppendLine(@"								{");
            sb.AppendLine(@"									""Policy"": {");
            sb.AppendLine(@"										""PolicyId"": ""5a806c89-493e-4b98-8a25-d623a6e06de5"",");
            sb.AppendLine(@"										""PolicyName"": ""AB Test Policy 2007-001"",");
            sb.AppendLine(@"										""PolicyDescription"": ""test""");
            sb.AppendLine(@"									},");
            sb.AppendLine(@"									""TotalAmount"": 0.0,");
            sb.AppendLine(@"									""Calculations"": [");
            sb.AppendLine(@"										{");
            sb.AppendLine(@"											""CalculationName"": ""AB Calc 0808-001"",");
            sb.AppendLine(@"											""CalculationVersionNumber"": 1,");
            sb.AppendLine(@"											""CalculationType"": ""Number"",");
            sb.AppendLine(@"											""CalculationAmount"": 52.0");
            sb.AppendLine(@"										}");
            sb.AppendLine(@"									],");
            sb.AppendLine(@"									""SubPolicyResults"": []");
            sb.AppendLine(@"								}");
            sb.AppendLine(@"							]");
            sb.AppendLine(@"						}");
            sb.AppendLine(@"					]");
            sb.AppendLine(@"				}");
            sb.AppendLine(@"			]");
            sb.AppendLine(@"		},");
            sb.AppendLine(@"		{");
            sb.AppendLine(@"			""Period"": {");
            sb.AppendLine(@"				""PeriodType"": ""FY"",");
            sb.AppendLine(@"				""PeriodId"": ""FY2017181"",");
            sb.AppendLine(@"				""StartDate"": ""2017-03-31T23:00:00+00:00"",");
            sb.AppendLine(@"				""EndDate"": ""2018-03-30T23:00:00+00:00""");
            sb.AppendLine(@"			},");
            sb.AppendLine(@"			""FundingStreamResults"": [");
            sb.AppendLine(@"				{");
            sb.AppendLine(@"					""FundingStream"": {");
            sb.AppendLine(@"						""FundingStreamCode"": ""YPLRN"",");
            sb.AppendLine(@"						""FundingStreamName"": ""SSF - SEN Funding"",");
            sb.AppendLine(@"						""AllocationLines"": null");
            sb.AppendLine(@"					},");
            sb.AppendLine(@"					""TotalAmount"": 15.0,");
            sb.AppendLine(@"					""Allocations"": [");
            sb.AppendLine(@"						{");
            sb.AppendLine(@"							""AllocationLine"": {");
            sb.AppendLine(@"								""AllocationLineCode"": ""YPN01"",");
            sb.AppendLine(@"								""AllocationLineName"": ""SSF - SEN Funding""");
            sb.AppendLine(@"							},");
            sb.AppendLine(@"							""AllocationVersionNumber"": 2,");
            sb.AppendLine(@"							""AllocationStatus"": ""Approved"",");
            sb.AppendLine(@"							""AllocationAmount"": 15.0,");
            sb.AppendLine(@"							""AllocationLearnerCount"": null");
            sb.AppendLine(@"						}");
            sb.AppendLine(@"					],");
            sb.AppendLine(@"					""Policies"": [");
            sb.AppendLine(@"						{");
            sb.AppendLine(@"							""Policy"": {");
            sb.AppendLine(@"								""PolicyId"": ""239c8b47-89e6-4906-a3bf-866bd11da2f4"",");
            sb.AppendLine(@"								""PolicyName"": ""Ab Test Policy 0908-001"",");
            sb.AppendLine(@"								""PolicyDescription"": ""This is another etst policy created 9th August 2018""");
            sb.AppendLine(@"							},");
            sb.AppendLine(@"							""TotalAmount"": 315.0,");
            sb.AppendLine(@"							""Calculations"": [");
            sb.AppendLine(@"								{");
            sb.AppendLine(@"									""CalculationName"": ""Learner Count"",");
            sb.AppendLine(@"									""CalculationVersionNumber"": 1,");
            sb.AppendLine(@"									""CalculationType"": ""Number"",");
            sb.AppendLine(@"									""CalculationAmount"": 1003.0");
            sb.AppendLine(@"								},");
            sb.AppendLine(@"								{");
            sb.AppendLine(@"									""CalculationName"": ""AB Test Calc 0908-001"",");
            sb.AppendLine(@"									""CalculationVersionNumber"": 1,");
            sb.AppendLine(@"									""CalculationType"": ""Funding"",");
            sb.AppendLine(@"									""CalculationAmount"": 300.0");
            sb.AppendLine(@"								},");
            sb.AppendLine(@"								{");
            sb.AppendLine(@"									""CalculationName"": ""AB Test Calc 0908-002"",");
            sb.AppendLine(@"									""CalculationVersionNumber"": 1,");
            sb.AppendLine(@"									""CalculationType"": ""Funding"",");
            sb.AppendLine(@"									""CalculationAmount"": 15.0");
            sb.AppendLine(@"								}");
            sb.AppendLine(@"							],");
            sb.AppendLine(@"							""SubPolicyResults"": [");
            sb.AppendLine(@"								{");
            sb.AppendLine(@"									""Policy"": {");
            sb.AppendLine(@"										""PolicyId"": ""239c8b47-89e6-4906-a3bf-866bd11da2f4"",");
            sb.AppendLine(@"										""PolicyName"": ""Ab Test Policy 0908-001"",");
            sb.AppendLine(@"										""PolicyDescription"": ""This is another etst policy created 9th August 2018""");
            sb.AppendLine(@"									},");
            sb.AppendLine(@"									""TotalAmount"": 0.0,");
            sb.AppendLine(@"									""Calculations"": [],");
            sb.AppendLine(@"									""SubPolicyResults"": []");
            sb.AppendLine(@"								}");
            sb.AppendLine(@"							]");
            sb.AppendLine(@"						}");
            sb.AppendLine(@"					]");
            sb.AppendLine(@"				}");
            sb.AppendLine(@"			]");
            sb.AppendLine(@"		}");
            sb.AppendLine(@"	]");
            sb.AppendLine(@"}");

            return sb.ToString();
        }
    }
}