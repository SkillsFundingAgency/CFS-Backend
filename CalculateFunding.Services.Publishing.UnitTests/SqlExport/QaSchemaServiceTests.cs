using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Profiling;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.SqlExport;
using CalculateFunding.Services.SqlExport;
using CalculateFunding.Services.SqlExport.Models;
using FluentAssertions;
using Microsoft.FeatureManagement;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Publishing.UnitTests.SqlExport
{
    [TestClass]
    public class QaSchemaServiceTests : QaSchemaTemplateTest
    {
        private Mock<ISqlNameGenerator> _sqlNameGenerator;
        private Mock<ISqlSchemaGenerator> _sqlSchemaGenerator;
        private Mock<IQaRepository> _qa;
        private Mock<IQaRepositoryLocator> _qaRepoLocator;
        private Mock<IProfilingApiClient> _profiling;
        private Mock<IFeatureManagerSnapshot> _featureManagerSnapshot;
        private Mock<IPrerequisiteCheckerLocator> _prerequisiteCheckerLocator;
        private Mock<IJobsRunning> _jobRunning;
        private Mock<IJobManagement> _jobManagement;
        private Mock<ILogger> _logger;

        private QaSchemaService _schemaService;

        [TestInitialize]
        public void SetUp()
        {
            _sqlNameGenerator = new Mock<ISqlNameGenerator>();
            _sqlSchemaGenerator = new Mock<ISqlSchemaGenerator>();
            _profiling = new Mock<IProfilingApiClient>();
            _qa = new Mock<IQaRepository>();
            _qaRepoLocator = new Mock<IQaRepositoryLocator>();
            _prerequisiteCheckerLocator = new Mock<IPrerequisiteCheckerLocator>();
            _qaRepoLocator.Setup(_ => _.GetService(It.IsAny<SqlExportSource>())).Returns(_qa.Object);

            _featureManagerSnapshot = new Mock<IFeatureManagerSnapshot>();
            _jobRunning = new Mock<IJobsRunning>();
            _jobManagement = new Mock<IJobManagement>();
            _logger = new Mock<ILogger>();

            _prerequisiteCheckerLocator.Setup(_ => _.GetPreReqChecker(CalculateFunding.Models.Publishing.PrerequisiteCheckerType.SqlImport))
                .Returns(new SqlImportPreRequisiteChecker(_jobRunning.Object,
                    _jobManagement.Object,
                    _logger.Object));

            _sqlNameGenerator.Setup(_ => _.GenerateIdentifier(It.IsAny<string>()))
                .Returns<string>(input => input);
            
            _schemaService = new QaSchemaService(Policies.Object,
                Specifications.Object,
                TemplateMetadataResolver.Object,
                _sqlSchemaGenerator.Object,
                _qaRepoLocator.Object,
                _profiling.Object,
                _sqlNameGenerator.Object,
                new ResiliencePolicies
                {
                    SpecificationsApiClient = Policy.NoOpAsync(),
                    PoliciesApiClient = Policy.NoOpAsync()
                },
                ReleaseManagementRepository.Object,
                _featureManagerSnapshot.Object,
                _prerequisiteCheckerLocator.Object);
        }

        [TestMethod]
        public void FailsPrequisiteCheckIfUndoPublishingJobRunning()
        {
            string specificationId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string jobId = NewRandomString();
            string templateVersion = NewRandomString();

            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _.WithId(specificationId)
                .WithFundingStreamIds(fundingStreamId)
                .WithFundingPeriodId(fundingPeriodId)
                .WithTemplateIds((fundingStreamId, templateVersion)));

            _jobRunning.Setup(_ => _.GetJobTypes(specificationId,
                    It.Is<IEnumerable<string>>(jobTypes => jobTypes.First() == JobConstants.DefinitionNames.PublishedFundingUndoJob)))
                .ReturnsAsync(new string[] { JobConstants.DefinitionNames.PublishedFundingUndoJob });

            GivenTheSpecification(specificationId, specificationSummary);

            Func<Task> invocation = () => WhenTheSchemaIsRecreated(specificationId, fundingStreamId, jobId, SqlExportSource.CurrentPublishedProviderVersion);
            
            invocation
                .Should()
                .ThrowExactly<NonRetriableException>()
                .WithMessage($"Sql Import with specification id: '{specificationId}' has prerequisites which aren't complete.");

            _jobManagement.Verify(_ => _.UpdateJobStatus(jobId, 0, false, "PublishedFundingUndoJob is still running"));
        }

        [DataTestMethod]
        [DataRow(SqlExportSource.CurrentPublishedProviderVersion, true)]
        [DataRow(SqlExportSource.ReleasedPublishedProviderVersion, true)]
        [DataRow(SqlExportSource.CurrentPublishedProviderVersion, false)]
        [DataRow(SqlExportSource.ReleasedPublishedProviderVersion, false)]
        public async Task CreatesDDLMatchingSpecificationFundingAndReCreatesSchemaObjectsInQaRepository(
            SqlExportSource sqlExportSource,
            bool latestReleasedVersionChannelPopulationEnabled)
        {
            string specificationId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string jobId = NewRandomString();
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

            string channelNameOne = NewRandomString();

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

            FundingConfigurationChannel fundingConfigurationChannel
                = NewFundingConfigurationChannel(_ => _.WithChannelCode(channelNameOne));
            FundingConfiguration fundingConfiguration
                = NewFundingConfiguration(_ => _.WithReleaseChannels(fundingConfigurationChannel));
            IEnumerable<Channel> channels = new[] { NewChannel(_ => _.WithChannelCode(channelNameOne).WithChannelId(1)) };

            GivenTheSpecification(specificationId, specificationSummary);
            AndTheFundingConfiguration(fundingStreamId, fundingPeriodId, fundingConfiguration);
            AndTheChannels(channels);
            AndTheFundingTemplate(fundingStreamId, fundingPeriodId, templateVersion, fundingTemplate);
            AndTheTemplateMetadataContents(schemaVersion, fundingTemplateContents, templateMetadataContents);
            AndTheProfiling(fundingStreamId, fundingPeriodId, profilePatternOne, profilePatternTwo);
            AndTheFeature("EnableLatestReleasedVersionChannelPopulation", latestReleasedVersionChannelPopulationEnabled);

            await WhenTheSchemaIsRecreated(specificationId, fundingStreamId, jobId, sqlExportSource);

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
                },
                new SqlColumnDefinition
                {
                    Name = "TrustStatus",
                    Type = "[varchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "UPIN",
                    Type = "[varchar](32)",
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
                    Name = "ProviderProfileIdType",
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
                    Name = "LondonRegionCode",
                    Type = "[varchar](256)",
                    AllowNulls = true
                },
                new SqlColumnDefinition
                {
                    Name = "LondonRegionName",
                    Type = "[nvarchar](256)",
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
                }
            }.ToDictionary(_ => _.Name);

            List<SqlColumnDefinition> fundingColumns = new List<SqlColumnDefinition>() {
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
            };

            if (sqlExportSource == SqlExportSource.CurrentPublishedProviderVersion && latestReleasedVersionChannelPopulationEnabled)
            {
                fundingColumns.Add(
                    new SqlColumnDefinition
                    {
                        Name = $"Latest{channelNameOne}ReleaseVersion",
                        Type = "[varchar](8)",
                        AllowNulls = false
                    });
            }

            Dictionary<string, SqlColumnDefinition> fundingColumnsDictionary = fundingColumns.ToDictionary(_ => _.Name);

            ThenTheGenerateCreateTableSqlWasCalled($"{fundingStreamTablePrefix}_Providers", fundingStreamId, fundingPeriodId, providerColumns);

            ThenTheGenerateCreateTableSqlWasCalled($"{fundingStreamTablePrefix}_Funding", fundingStreamId, fundingPeriodId, fundingColumnsDictionary);

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

        private void AndTheFeature(string featureName,
            bool featureStatus)
            => _featureManagerSnapshot.Setup(_ => _.IsEnabledAsync(featureName))
                .ReturnsAsync(featureStatus);

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

        private async Task WhenTheSchemaIsRecreated(
            string specificationId,
            string fundingStreamId,
            string jobId,
            SqlExportSource sqlExportSource)
            => await _schemaService.ReCreateTablesForSpecificationAndFundingStream(specificationId, fundingStreamId, jobId, sqlExportSource);

        private FundingStreamPeriodProfilePattern NewFundingStreamPeriodProfilePattern(Action<FundingStreamPeriodProfilePatternBuilder> setUp = null)
        {
            FundingStreamPeriodProfilePatternBuilder fundingStreamPeriodProfilePatternBuilder = new FundingStreamPeriodProfilePatternBuilder();

            setUp?.Invoke(fundingStreamPeriodProfilePatternBuilder);
            
            return fundingStreamPeriodProfilePatternBuilder.Build();
        }

        private static ProviderVersionInChannel NewProviderVersionInChannel(Action<ProviderVersionInChannelBuilder> setUp = null)
        {
            ProviderVersionInChannelBuilder providerVersionInChannelBuilder = new ProviderVersionInChannelBuilder();

            setUp?.Invoke(providerVersionInChannelBuilder);

            return providerVersionInChannelBuilder.Build();
        }
    }
}