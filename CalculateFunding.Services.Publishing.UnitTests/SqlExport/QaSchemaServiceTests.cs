using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Profiling;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Services.Publishing.SqlExport;
using CalculateFunding.Services.SqlExport;
using CalculateFunding.Services.SqlExport.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;

namespace CalculateFunding.Services.Publishing.UnitTests.SqlExport
{
    [TestClass]
    public class QaSchemaServiceTests : QaSchemaTemplateTest
    {
        private Mock<ISqlNameGenerator> _sqlNameGenerator;
        private Mock<ISqlSchemaGenerator> _sqlSchemaGenerator;
        private Mock<IQaRepository> _qa;
        private Mock<IProfilingApiClient> _profiling;
        
        private QaSchemaService _schemaService;

        [TestInitialize]
        public void SetUp()
        {
            _sqlNameGenerator = new Mock<ISqlNameGenerator>();
            _sqlSchemaGenerator = new Mock<ISqlSchemaGenerator>();
            _profiling = new Mock<IProfilingApiClient>();
            _qa = new Mock<IQaRepository>();

            _sqlNameGenerator.Setup(_ => _.GenerateIdentifier(It.IsAny<string>()))
                .Returns<string>(input => input);
            
            _schemaService = new QaSchemaService(Policies.Object,
                Specifications.Object,
                TemplateMetadataResolver.Object,
                _sqlSchemaGenerator.Object,
                _qa.Object,
                _profiling.Object,
                _sqlNameGenerator.Object,
                new ResiliencePolicies
                {
                    SpecificationsApiClient = Policy.NoOpAsync(),
                    PoliciesApiClient = Policy.NoOpAsync()
                });
        }

        [TestMethod]
        public async Task CreatesDDLMatchingSpecificationFundingAndReCreatesSchemaObjectsInQaRepository()
        {
            string specificationId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string templateVersion = NewRandomString();
            string schemaVersion = NewRandomString();
            string fundingTemplateContents = NewRandomString();

            string fundingStreamTablePrefix = $"{fundingStreamId}_{fundingPeriodId}";

            Calculation calculationOne = NewCalculation();
            Calculation calculationTwo = NewCalculation();
            Calculation calculationThree = NewCalculation();
            Calculation calculationFour = NewCalculation(_ => _.WithCalculations(calculationOne));
            Calculation calculationFive = NewCalculation(_ => _.WithCalculations(calculationTwo));

            string fundingLineOneCode = NewRandomString();
            string fundingLineTwoCode = NewRandomString();
            
            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _.WithId(specificationId)
                .WithFundingStreamIds(fundingStreamId)
                .WithFundingPeriodId(fundingPeriodId)
                .WithTemplateIds((fundingStreamId, templateVersion)));
            FundingTemplateContents fundingTemplate = NewFundingTemplateContents(_ => _.WithSchemaVersion(schemaVersion)
                .WithTemplateFileContents(fundingTemplateContents));
            TemplateMetadataContents templateMetadataContents = NewTemplateMetadataContents(_ => _.WithFundingLines(
                NewFundingLine(fl => fl.WithCalculations(calculationFour,
                        calculationFive)
                    .WithFundingLineCode(fundingLineOneCode)
                    .WithFundingLines(NewFundingLine(fl1 => fl1.WithCalculations(calculationThree)
                        .WithFundingLineCode(fundingLineTwoCode)))
                )));

            FundingStreamPeriodProfilePattern profilePatternOne = NewFundingStreamPeriodProfilePattern(_ => 
                _.WithFundingPeriodId(fundingPeriodId)
                    .WithFundingStreamId(fundingStreamId)
                    .WithFundingLineId(fundingLineOneCode));
            FundingStreamPeriodProfilePattern profilePatternTwo = NewFundingStreamPeriodProfilePattern(_ => 
                _.WithFundingPeriodId(fundingPeriodId)
                    .WithFundingStreamId(fundingStreamId)
                    .WithFundingLineId(fundingLineTwoCode));
            
            GivenTheSpecification(specificationId, specificationSummary);
            AndTheFundingTemplate(fundingStreamId, fundingPeriodId, templateVersion, fundingTemplate);
            AndTheTemplateMetadataContents(schemaVersion, fundingTemplateContents, templateMetadataContents);
            AndTheProfiling(fundingStreamId, fundingPeriodId, profilePatternOne, profilePatternTwo);
            
            await WhenTheSchemaIsRecreated(specificationId, fundingStreamId);

            Dictionary<string, SqlColumnDefinition> providerColumns = new List<SqlColumnDefinition>() {
                new SqlColumnDefinition
                {
                    Name = "ProviderId",
                    Type = "[varchar](32)",
                    AllowNulls = false
                },
                new SqlColumnDefinition
                {
                    Name = "ProviderName",
                    Type = "[nvarchar](256)",
                    AllowNulls = false
                },
                new SqlColumnDefinition
                {
                    Name = "UKPRN",
                    Type = "[varchar](32)",
                    AllowNulls = false
                },
                new SqlColumnDefinition
                {
                    Name = "URN",
                    Type = "[varchar](32)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "Authority",
                    Type = "[nvarchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "ProviderType",
                    Type = "[varchar](128)",
                    AllowNulls = false
                },
                new SqlColumnDefinition
                {
                    Name = "ProviderSubtype",
                    Type = "[varchar](128)",
                    AllowNulls = false
                },
                new SqlColumnDefinition
                {
                    Name = "DateOpened",
                    Type = "datetime",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "DateClosed",
                    Type = "datetime",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "LaCode",
                    Type = "[varchar](32)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "Status",
                    Type = "[varchar](64)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "Successor",
                    Type = "[varchar](128)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "TrustCode",
                    Type = "[varchar](32)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "TrustName",
                    Type = "[nvarchar](128)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "PaymentOrganisationIdentifier",
                    Type = "[varchar](32)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "PaymentOrganisationName",
                    Type = "[nvarchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "IsIndicative",
                    Type = "[bit]",
                    AllowNulls = false
                }
            }.ToDictionary(_ => _.Name);

            Dictionary<string, SqlColumnDefinition> fundingColumns = new List<SqlColumnDefinition>() {
                new SqlColumnDefinition
                {
                    Name = "TotalFunding",
                    Type = "[decimal](30, 18)",
                    AllowNulls = false
                },
                new SqlColumnDefinition
                {
                    Name = "ProviderId",
                    Type = "[varchar](32)",
                    AllowNulls = false
                },
                new SqlColumnDefinition
                {
                    Name = "FundingStreamId",
                    Type = "[varchar](32)",
                    AllowNulls = false
                },
                new SqlColumnDefinition
                {
                    Name = "FundingPeriodId",
                    Type = "[varchar](32)",
                    AllowNulls = false
                },
                new SqlColumnDefinition
                {
                    Name = "MajorVersion",
                    Type = "[varchar](32)",
                    AllowNulls = false
                },
                new SqlColumnDefinition
                {
                    Name = "MinorVersion",
                    Type = "[varchar](32)",
                    AllowNulls = false
                },
                new SqlColumnDefinition
                {
                    Name = "Status",
                    Type = "[varchar](32)",
                    AllowNulls = false
                },

                new SqlColumnDefinition
                {
                    Name = "LastUpdated",
                    Type = "[datetime]",
                    AllowNulls = false
                },
                new SqlColumnDefinition
                {
                    Name = "LastUpdatedBy",
                    Type = "[nvarchar](256)",
                    AllowNulls = false
                },
                new SqlColumnDefinition
                {
                    Name = "IsIndicative",
                    Type = "[bit]",
                    AllowNulls = false
                },
                new SqlColumnDefinition
                {
                    Name= "ProviderVariationReasons",
                    Type = "[nvarchar](1024)",
                    AllowNulls = false
                }
            }.ToDictionary(_ => _.Name);

            ThenTheGenerateCreateTableSqlWasCalled($"{fundingStreamTablePrefix}_Providers", fundingStreamId, fundingPeriodId, providerColumns);

            ThenTheGenerateCreateTableSqlWasCalled($"{fundingStreamTablePrefix}_Funding", fundingStreamId, fundingPeriodId, fundingColumns);

            ThenTheTotalNumberOfDDLScriptsExecutedWas(7);
        }

        private void ThenTheGenerateCreateTableSqlWasCalled(string tableName, string fundingStreamId, string fundingPeriodId, IDictionary<string, SqlColumnDefinition> columns)
        {
            _sqlSchemaGenerator.Verify(_ => _.GenerateCreateTableSql(
                    tableName, 
                    fundingStreamId, 
                    fundingPeriodId, 
                    It.Is<IEnumerable<SqlColumnDefinition>>(x => 
                        x.All(cd => columns.ContainsKey(cd.Name) && columns[cd.Name].Type == cd.Type)
                    )
                ),
                Times.Once);
        }

        private void ThenTheTotalNumberOfDDLScriptsExecutedWas(int count)
        {
            _qa.Verify(_ => _.ExecuteSql(It.IsAny<string>()),
                Times.Exactly(count));
        }

        private void AndTheProfiling(string fundingStreamId,
            string fundingPeriodId,
            params FundingStreamPeriodProfilePattern[] periodPatterns)
            => _profiling.Setup(_ => _.GetProfilePatternsForFundingStreamAndFundingPeriod(fundingStreamId, fundingPeriodId))
                .ReturnsAsync(new ApiResponse<IEnumerable<FundingStreamPeriodProfilePattern>>(HttpStatusCode.OK, periodPatterns));

        private static bool ColumnsMatch(IEnumerable<SqlColumnDefinition> actualColumns,
            IEnumerable<SqlColumnDefinition> expectedColumns)
        {
            return expectedColumns.Count() == actualColumns.Count() &&
                   expectedColumns
                .All(_ => actualColumns.Count(col =>
                    col.Name == _.Name &&
                    col.Type == _.Type &&
                    col.AllowNulls == _.AllowNulls) == 1);
        }

        private async Task WhenTheSchemaIsRecreated(string specificationId,
            string fundingStreamId)
            => await _schemaService.ReCreateTablesForSpecificationAndFundingStream(specificationId, fundingStreamId);

        private FundingStreamPeriodProfilePattern NewFundingStreamPeriodProfilePattern(Action<FundingStreamPeriodProfilePatternBuilder> setUp = null)
        {
            FundingStreamPeriodProfilePatternBuilder fundingStreamPeriodProfilePatternBuilder = new FundingStreamPeriodProfilePatternBuilder();

            setUp?.Invoke(fundingStreamPeriodProfilePatternBuilder);
            
            return fundingStreamPeriodProfilePatternBuilder.Build();
        }
    }
}