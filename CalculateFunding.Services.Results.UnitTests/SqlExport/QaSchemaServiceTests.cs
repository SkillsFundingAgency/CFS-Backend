using CalculateFunding.Services.Results.SqlExport;
using CalculateFunding.Services.SqlExport;
using CalculateFunding.Services.SqlExport.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalcsApiCalculation = CalculateFunding.Common.ApiClient.Calcs.Models.Calculation;
using CalcsApiCalculationIdentifier = CalculateFunding.Common.ApiClient.Calcs.Models.CalculationIdentifier;

namespace CalculateFunding.Services.Results.UnitTests.SqlExport
{
    [TestClass]
    public class QaSchemaServiceTests : QaSchemaTemplateTest
    {
        private Mock<ISqlNameGenerator> _sqlNameGenerator;
        private Mock<ISqlSchemaGenerator> _sqlSchemaGenerator;
        private Mock<IQaRepository> _qa;

        private QaSchemaService _schemaService;

        [TestInitialize]
        public void SetUp()
        {
            _sqlNameGenerator = new Mock<ISqlNameGenerator>();
            _sqlSchemaGenerator = new Mock<ISqlSchemaGenerator>();
            _qa = new Mock<IQaRepository>();

            _sqlNameGenerator.Setup(_ => _.GenerateIdentifier(It.IsAny<string>()))
                .Returns<string>(input => input);

            _schemaService = new QaSchemaService(
                new ResiliencePolicies
                {
                    SpecificationsApiClient = Policy.NoOpAsync(),
                    PoliciesApiClient = Policy.NoOpAsync(),
                    CalculationsApiClient = Policy.NoOpAsync()
                },
                Specifications.Object,
                _qa.Object,
                _sqlSchemaGenerator.Object,
                _sqlNameGenerator.Object,
                Policies.Object,
                TemplateMetadataResolver.Object,
                Calculations.Object);
        }

        [TestMethod]
        public async Task CreatesDDLMatchingSpecificationAndReCreatesSchemaObjectsInQaRepository()
        {
            string specificationId = NewRandomString();
            string specificationName = NewRandomString();
            string specificationGeneratedIdentifierName = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string templateVersion = NewRandomString();
            string schemaVersion = NewRandomString();
            string fundingTemplateContents = NewRandomString();

            string specificationTablePrefix = specificationGeneratedIdentifierName;

            uint fundingLineOneTemplateLineId = NewRandomUInt();
            uint fundingLineTwoTemplateLineId = NewRandomUInt();
            string fundingLineOneName = NewRandomString();
            string fundingLineTwoName = NewRandomString();

            string calculationOneName = NewRandomString();

            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _
                .WithId(specificationId)
                .WithName(specificationName)
                .WithFundingStreamIds(fundingStreamId)
                .WithFundingPeriodId(fundingPeriodId)
                .WithTemplateIds((fundingStreamId, templateVersion)));
            FundingTemplateContents fundingTemplate = NewFundingTemplateContents(_ => _.WithSchemaVersion(schemaVersion)
                .WithTemplateFileContents(fundingTemplateContents));
            TemplateMetadataContents templateMetadataContents = NewTemplateMetadataContents(_ => _
                .WithFundingLines(
                    NewFundingLine(fl => fl
                        .WithName(fundingLineOneName)
                        .WithTemplateLineId(fundingLineOneTemplateLineId)
                        .WithCalculations(
                            NewTemplateMetadataCalculation(tmc => tmc.WithName(calculationOneName)))),
                    NewFundingLine(fl => fl
                        .WithName(fundingLineTwoName)
                        .WithTemplateLineId(fundingLineTwoTemplateLineId))));

            IEnumerable<CalcsApiCalculation> calculations = new List<CalcsApiCalculation>()
            {
                NewApiCalculation(_ => _.WithType(Common.ApiClient.Calcs.Models.CalculationType.Template).WithName(calculationOneName)),
                NewApiCalculation(_ => _.WithType(Common.ApiClient.Calcs.Models.CalculationType.Additional))
            };

            CalcsApiCalculationIdentifier calcsApiCalculationIdentifier = new CalcsApiCalculationIdentifier
            {
                Name = specificationName,
                SourceCodeName = specificationGeneratedIdentifierName
            };

            GivenTheSpecification(specificationId, specificationSummary);
            AndTheCalculationsForSpecification(specificationId, calculations);
            AndTheGenerateCalculationIdentifier(specificationName, calcsApiCalculationIdentifier);
            AndTheFundingTemplate(fundingStreamId, fundingPeriodId, templateVersion, fundingTemplate);
            AndTheTemplateMetadataContents(schemaVersion, fundingTemplateContents, templateMetadataContents);

            await WhenTheSchemaIsRecreated(specificationId);

            Dictionary<string, SqlColumnDefinition> calculationRunColumns = GetExpectedCalculationRunColumns().ToDictionary(_ => _.Name);
            Dictionary<string, SqlColumnDefinition> providerColumns = GetExpectedProviderColumns().ToDictionary(_ => _.Name);

            ThenTheGenerateCreateTableSqlWasCalled($"{specificationTablePrefix}_CalculationRun", specificationId, calculationRunColumns, isSpecificationTable: true);
            ThenTheGenerateCreateTableSqlWasCalled($"{specificationTablePrefix}_Providers", specificationId, providerColumns);

            ThenTheTotalNumberOfDDLScriptsExecutedWas(6);
        }

        [TestMethod]
        public async Task CreatesDDLMatchingSpecificationAndReCreatesSchemaObjectsInQaRepositoryWithoutAdditionalCalculations()
        {
            string specificationId = NewRandomString();
            string specificationName = NewRandomString();
            string specificationGeneratedIdentifierName = NewRandomString();

            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string templateVersion = NewRandomString();
            string schemaVersion = NewRandomString();
            string fundingTemplateContents = NewRandomString();

            string specificationTablePrefix = specificationGeneratedIdentifierName;

            uint fundingLineOneTemplateLineId = NewRandomUInt();
            uint fundingLineTwoTemplateLineId = NewRandomUInt();
            string fundingLineOneName = NewRandomString();
            string fundingLineTwoName = NewRandomString();

            string calculationOneName = NewRandomString();

            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _
                .WithId(specificationId)
                .WithName(specificationName)
                .WithFundingStreamIds(fundingStreamId)
                .WithFundingPeriodId(fundingPeriodId)
                .WithTemplateIds((fundingStreamId, templateVersion)));
            FundingTemplateContents fundingTemplate = NewFundingTemplateContents(_ => _.WithSchemaVersion(schemaVersion)
                .WithTemplateFileContents(fundingTemplateContents));
            TemplateMetadataContents templateMetadataContents = NewTemplateMetadataContents(_ => _.WithFundingLines(
                NewFundingLine(fl => fl
                    .WithName(fundingLineOneName)
                    .WithTemplateLineId(fundingLineOneTemplateLineId)
                    .WithCalculations(
                            NewTemplateMetadataCalculation(tmc => tmc.WithName(calculationOneName)))),
                NewFundingLine(fl => fl
                    .WithName(fundingLineTwoName)
                    .WithTemplateLineId(fundingLineTwoTemplateLineId))
                ));

            IEnumerable<CalcsApiCalculation> calculations = new List<CalcsApiCalculation>()
            {
                NewApiCalculation(_ => _.WithType(Common.ApiClient.Calcs.Models.CalculationType.Template).WithName(calculationOneName)),
            };

            CalcsApiCalculationIdentifier calcsApiCalculationIdentifier = new CalcsApiCalculationIdentifier
            {
                Name = specificationName,
                SourceCodeName = specificationGeneratedIdentifierName
            };

            GivenTheSpecification(specificationId, specificationSummary);
            AndTheCalculationsForSpecification(specificationId, calculations);
            AndTheGenerateCalculationIdentifier(specificationName, calcsApiCalculationIdentifier);
            AndTheFundingTemplate(fundingStreamId, fundingPeriodId, templateVersion, fundingTemplate);
            AndTheTemplateMetadataContents(schemaVersion, fundingTemplateContents, templateMetadataContents);

            await WhenTheSchemaIsRecreated(specificationId);

            Dictionary<string, SqlColumnDefinition> calculationRunColumns = GetExpectedCalculationRunColumns().ToDictionary(_ => _.Name);
            Dictionary<string, SqlColumnDefinition> providerColumns = GetExpectedProviderColumns().ToDictionary(_ => _.Name);

            ThenTheGenerateCreateTableSqlWasCalled($"{specificationTablePrefix}_CalculationRun", specificationId, calculationRunColumns, isSpecificationTable: true);
            ThenTheGenerateCreateTableSqlWasCalled($"{specificationTablePrefix}_Providers", specificationId, providerColumns);

            ThenTheTotalNumberOfDDLScriptsExecutedWas(5);
        }

        private List<SqlColumnDefinition> GetExpectedCalculationRunColumns()
            => new List<SqlColumnDefinition>
                {
                    new SqlColumnDefinition
                    {
                        Name = "SpecificationId",
                        Type = "[varchar](128)",
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
                        Name = "SpecificationName",
                        Type = "[varchar](128)",
                        AllowNulls = false
                    },
                    new SqlColumnDefinition
                    {
                        Name = "TemplateVersion",
                        Type = "[varchar](32)",
                        AllowNulls = true
                    },
                    new SqlColumnDefinition
                    {
                        Name = "ProviderVersion",
                        Type = "[varchar](32)",
                        AllowNulls = true
                    },
                    new SqlColumnDefinition
                    {
                        Name = "LastUpdated",
                        Type = "[datetime]",
                        AllowNulls = true
                    },
                    new SqlColumnDefinition
                    {
                        Name = "LastUpdatedBy",
                        Type = "[nvarchar](256)",
                        AllowNulls = true
                    },
                };
        private List<SqlColumnDefinition> GetExpectedProviderColumns()
            => new List<SqlColumnDefinition>
            {
                new SqlColumnDefinition
                {
                    Name = "ProviderId",
                    Type = "[varchar](128)",
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
                    Name = "URN",
                    Type = "[varchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "UKPRN",
                    Type = "[varchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "UPIN",
                    Type = "[varchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "EstablishmentNumber",
                    Type = "[varchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "DfeEstablishmentNumber",
                    Type = "[varchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "Authority",
                    Type = "[varchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "ProviderType",
                    Type = "[varchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "ProviderSubType",
                    Type = "[varchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "DateOpened",
                    Type = "[datetime]",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "DateClosed",
                    Type = "[datetime]",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "ProviderProfileIdType",
                    Type = "[varchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "LACode",
                    Type = "[varchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "NavVendorNo",
                    Type = "[varchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "CrmAccountId",
                    Type = "[varchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "LegalName",
                    Type = "[nvarchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "Status",
                    Type = "[varchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "PhaseOfEducation",
                    Type = "[varchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "ReasonEstablishmentOpened",
                    Type = "[varchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "ReasonEstablishmentClosed",
                    Type = "[varchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "TrustStatus",
                    Type = "[varchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "TrustName",
                    Type = "[nvarchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "TrustCode",
                    Type = "[varchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "Town",
                    Type = "[nvarchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "Postcode",
                    Type = "[varchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "CompaniesHouseNumber",
                    Type = "[varchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "GroupIdNumber",
                    Type = "[varchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "RscRegionName",
                    Type = "[nvarchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "RscRegionCode",
                    Type = "[varchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "GovernmentOfficeRegionName",
                    Type = "[nvarchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "GovernmentOfficeRegionCode",
                    Type = "[varchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "DistrictName",
                    Type = "[nvarchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "DistrictCode",
                    Type = "[varchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "WardName",
                    Type = "[nvarchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "WardCode",
                    Type = "[varchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "CensusWardName",
                    Type = "[nvarchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "CensusWardCode",
                    Type = "[varchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "MiddleSuperOutputAreaName",
                    Type = "[nvarchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "MiddleSuperOutputAreaCode",
                    Type = "[varchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "LowerSuperOutputAreaName",
                    Type = "[nvarchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "LowerSuperOutputAreaCode",
                    Type = "[varchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "ParliamentaryConstituencyName",
                    Type = "[nvarchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "ParliamentaryConstituencyCode",
                    Type = "[varchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "CountryCode",
                    Type = "[varchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "CountryName",
                    Type = "[nvarchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "LocalGovernmentGroupTypeCode",
                    Type = "[varchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "LocalGovernmentGroupTypeName",
                    Type = "[nvarchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "Street",
                    Type = "[nvarchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "Locality",
                    Type = "[nvarchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "Address3",
                    Type = "[nvarchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "PaymentOrganisationIdentifier",
                    Type = "[varchar](256)",
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
                    Name = "ProviderTypeCode",
                    Type = "[varchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "ProviderSubTypeCode",
                    Type = "[varchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "PreviousLaCode",
                    Type = "[varchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "PreviousLaName",
                    Type = "[nvarchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "PreviousEstablishmentNumber",
                    Type = "[varchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "FurtherEducationTypeCode",
                    Type = "[varchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "FurtherEducationTypeName",
                    Type = "[nvarchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "Predecessors",
                    Type = "[nvarchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "Successors",
                    Type = "[nvarchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "IsIndicative",
                    Type = "[bit]",
                    AllowNulls = false
                },
                new SqlColumnDefinition
                {
                    Name = "LondonRegionName",
                    Type = "[nvarchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "LondonRegionCode",
                    Type = "[varchar](256)",
                    AllowNulls = true
                }
            };


        private void ThenTheGenerateCreateTableSqlWasCalled(string tableName, string specificationId, IDictionary<string, SqlColumnDefinition> columns, bool isSpecificationTable = false)
        {
            _sqlSchemaGenerator.Verify(_ => _.GenerateCreateTableSql(
                    tableName,
                    specificationId,
                    It.Is<IEnumerable<SqlColumnDefinition>>(x =>
                        x.All(cd => columns.ContainsKey(cd.Name) && columns[cd.Name].Type == cd.Type)
                    ),
                    isSpecificationTable
                ),
                Times.Once);
        }

        private void ThenTheTotalNumberOfDDLScriptsExecutedWas(int count)
        {
            _qa.Verify(_ => _.ExecuteSql(It.IsAny<string>()),
                Times.Exactly(count));
        }

        private async Task WhenTheSchemaIsRecreated(string specificationId)
            => await _schemaService.ReCreateTablesForSpecification(specificationId);


    }
}
