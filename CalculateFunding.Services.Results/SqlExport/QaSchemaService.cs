using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.TemplateMetadata;
using CalculateFunding.Common.TemplateMetadata.Enums;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Results.Interfaces;
using CalculateFunding.Services.SqlExport;
using CalculateFunding.Services.SqlExport.Models;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CalcsApiCalculation = CalculateFunding.Common.ApiClient.Calcs.Models.Calculation;
using CalcsApiCalculationValueType = CalculateFunding.Common.ApiClient.Calcs.Models.CalculationValueType;
using CalcsApiCalculationType = CalculateFunding.Common.ApiClient.Calcs.Models.CalculationType;


namespace CalculateFunding.Services.Results.SqlExport
{
    public class QaSchemaService : IQaSchemaService
    {
        private readonly ISpecificationsApiClient _specifications;
        private readonly IQaRepository _qaRepository;
        private readonly ISqlSchemaGenerator _schemaGenerator;
        private readonly ISqlNameGenerator _sqlNames;
        private readonly IPoliciesApiClient _policies;
        private readonly ITemplateMetadataResolver _templateMetadataResolver;
        private readonly ICalculationsApiClient _calculationsApiClient;

        private readonly AsyncPolicy _specificationResilience;
        private readonly AsyncPolicy _policiesResilience;
        private readonly AsyncPolicy _calcsResilience;

        public QaSchemaService(
            IResultsResiliencePolicies resiliencePolicies,
            ISpecificationsApiClient specificationsApiClient,
            IQaRepository qaRepository,
            ISqlSchemaGenerator schemaGenerator,
            ISqlNameGenerator sqlNames,
            IPoliciesApiClient policies,
            ITemplateMetadataResolver templateMetadataResolver,
            ICalculationsApiClient calculationsApiClient
            )
        {
            Guard.ArgumentNotNull(specificationsApiClient, nameof(specificationsApiClient));
            Guard.ArgumentNotNull(qaRepository, nameof(qaRepository));
            Guard.ArgumentNotNull(schemaGenerator, nameof(schemaGenerator));
            Guard.ArgumentNotNull(sqlNames, nameof(sqlNames));
            Guard.ArgumentNotNull(policies, nameof(policies));
            Guard.ArgumentNotNull(templateMetadataResolver, nameof(templateMetadataResolver));
            Guard.ArgumentNotNull(calculationsApiClient, nameof(calculationsApiClient));

            Guard.ArgumentNotNull(resiliencePolicies?.SpecificationsApiClient, nameof(resiliencePolicies.SpecificationsApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.PoliciesApiClient, nameof(resiliencePolicies.PoliciesApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.CalculationsApiClient, nameof(resiliencePolicies.CalculationsApiClient));

            _specifications = specificationsApiClient;
            _qaRepository = qaRepository;
            _schemaGenerator = schemaGenerator;
            _sqlNames = sqlNames;
            _policies = policies;
            _templateMetadataResolver = templateMetadataResolver;
            _calculationsApiClient = calculationsApiClient;

            _specificationResilience = resiliencePolicies.SpecificationsApiClient;
            _policiesResilience = resiliencePolicies.PoliciesApiClient;
            _calcsResilience = resiliencePolicies.CalculationsApiClient;
        }

        public async Task ReCreateTablesForSpecification(string specificationId)
        {
            ApiResponse<SpecificationSummary> specificationResponse = await _specificationResilience.ExecuteAsync(()
                => _specifications.GetSpecificationSummaryById(specificationId));

            SpecificationSummary specification = specificationResponse?.Content;

            if (specification == null)
            {
                throw new NonRetriableException(
                    $"Did not locate a specification {specificationId}. Unable to complete Qa Schema Generation");
            }

            await EnsureTablesForSpecification(specification);
        }

        public async Task EnsureSqlTablesForSpecification(string specificationId)
        {
            ApiResponse<SpecificationSummary> specificationResponse = await _specificationResilience.ExecuteAsync(()
                => _specifications.GetSpecificationSummaryById(specificationId));

            SpecificationSummary specification = specificationResponse.Content;

            if (specification == null)
            {
                throw new NonRetriableException(
                    $"Did not locate a specification {specificationId}. Unable to complete Qa Schema Generation");
            }

            await EnsureTablesForSpecification(specification);
        }

        private async Task EnsureTablesForSpecification(SpecificationSummary specification)
        {
            await DropExistingTables(specification.Id);

            string specificationTablePrefix = specification.Id;

            UniqueTemplateContents templateMetadata = await GetTemplateData(specification, specification.FundingStreams.FirstOrDefault().Id);

            ApiResponse<IEnumerable<CalcsApiCalculation>> calculationsResponse =
                await _calcsResilience.ExecuteAsync(() => _calculationsApiClient.GetCalculationsForSpecification(specification.Id));

            IEnumerable<CalcsApiCalculation> calculations = calculationsResponse.Content;

            if (calculations == null)
            {
                throw new NonRetriableException(
                    $"Did not locate calculations for {specification.Id}. Unable to complete Qa Schema Generation");
            }

            EnsureTable($"{specificationTablePrefix}_CalculationRun", GetSqlColumnDefinitionsForCalculationRun(), specification.Id, isSpecificationTable: true);
            EnsureTable($"{specificationTablePrefix}_Providers", GetSqlColumnDefinitionsForProviders(), specification.Id);

            (IEnumerable<SqlColumnDefinition> informationFundingLineFields, IEnumerable<SqlColumnDefinition> paymentFundingLineFields) 
                = GetUniqueFundingLinesForSpecification(templateMetadata.FundingLines);

            (IEnumerable<SqlColumnDefinition> templateCalculationFields, IEnumerable<SqlColumnDefinition> additionalCalculationFields) 
                = GetUniqueCalculationsForSpecification(calculations, templateMetadata.Calculations);

            EnsureTable($"{specificationTablePrefix}_PaymentFundingLines", paymentFundingLineFields, specification.Id);
            EnsureTable($"{specificationTablePrefix}_InformationFundingLines", informationFundingLineFields, specification.Id);
            EnsureTable($"{specificationTablePrefix}_TemplateCalculations", templateCalculationFields, specification.Id);

            if (additionalCalculationFields.Any())
            {
                EnsureTable($"{specificationTablePrefix}_AdditionalCalculations", additionalCalculationFields, specification.Id);
            }
        }

        private async Task DropExistingTables(string specificationId)
        {
            IEnumerable<TableForSpecification> tablesToDrop = await _qaRepository.GetTablesForSpecification(specificationId);

            StringBuilder dropSql = new StringBuilder();

            foreach (TableForSpecification table in tablesToDrop)
            {
                dropSql.AppendLine($"DROP TABLE [dbo].[{table.Name}];");
            }

            if (dropSql.Length == 0)
            {
                return;
            }

            _qaRepository.ExecuteSql(dropSql.ToString());
        }

        private void EnsureTable(
            string tableName, 
            IEnumerable<SqlColumnDefinition> fields,
            string specificationId,
            bool isSpecificationTable = false)
        {
            string sql = _schemaGenerator.GenerateCreateTableSql(tableName, specificationId, fields, isSpecificationTable: isSpecificationTable);

            _qaRepository.ExecuteSql(sql);
        }

        private IEnumerable<SqlColumnDefinition> GetSqlColumnDefinitionsForCalculationRun()
            => new List<SqlColumnDefinition>
            {
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

        private IEnumerable<SqlColumnDefinition> GetSqlColumnDefinitionsForProviders()
            => new List<SqlColumnDefinition>
            {
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
                }
            };

        private (IEnumerable<SqlColumnDefinition> informationFundingLineFields, IEnumerable<SqlColumnDefinition> paymentFundingLineFields) 
            GetUniqueFundingLinesForSpecification(
            IEnumerable<FundingLine> templateFundingLines)
        {
            IEnumerable<SqlColumnDefinition> informationFundingLineFields =
                GetSqlColumnDefinitionsForFundingLines(templateFundingLines.Where(f => f.Type == FundingLineType.Information));
            IEnumerable<SqlColumnDefinition> paymentFundingLineFields =
                GetSqlColumnDefinitionsForFundingLines(templateFundingLines.Where(f => f.Type == FundingLineType.Payment));

            return (informationFundingLineFields, paymentFundingLineFields);
        }

        private (IEnumerable<SqlColumnDefinition> templateCalculationFields, IEnumerable<SqlColumnDefinition> additionalCalculationFields)
            GetUniqueCalculationsForSpecification(
            IEnumerable<CalcsApiCalculation> calculations,
            IEnumerable<Calculation> templateCalculations)
        {
            IEnumerable<SqlColumnDefinition> templateCalculationFields = 
                GetSqlColumnDefinitionsForTemplateCalculations(calculations.Where(_ => _.CalculationType == CalcsApiCalculationType.Template), templateCalculations);
            IEnumerable<SqlColumnDefinition> additionalCalculationFields =
                GetSqlColumnDefinitionsForAdditionalCalculations(calculations.Where(_ => _.CalculationType == CalcsApiCalculationType.Additional));

            return (templateCalculationFields, additionalCalculationFields);
        }

        private IEnumerable<SqlColumnDefinition> GetSqlColumnDefinitionsForTemplateCalculations(
            IEnumerable<CalcsApiCalculation> calculations,
            IEnumerable<Calculation> templateCalculations)
        {
            List<SqlColumnDefinition> fields = new List<SqlColumnDefinition>(calculations.Count());

            foreach (CalcsApiCalculation calculation in calculations.OrderBy(c => c.Id))
            {
                Calculation templateCalculation = templateCalculations.SingleOrDefault(_ => _.Name == calculation.Name);

                fields.Add(new SqlColumnDefinition
                {
                    Name = $"Calc_{templateCalculation.TemplateCalculationId}_{_sqlNames.GenerateIdentifier(calculation.Name)}",
                    Type = GetSqlDatatypeForCalculation(calculation.ValueType),
                    AllowNulls = true
                });
            }

            return fields;
        }

        private IEnumerable<SqlColumnDefinition> GetSqlColumnDefinitionsForAdditionalCalculations(
            IEnumerable<CalcsApiCalculation> calculations)
        {
            List<SqlColumnDefinition> fields = new List<SqlColumnDefinition>(calculations.Count());

            foreach (CalcsApiCalculation calculation in calculations.OrderBy(c => c.Id))
            {
                fields.Add(new SqlColumnDefinition
                {
                    Name = $"Add_{_sqlNames.GenerateIdentifier(calculation.Name)}",
                    Type = GetSqlDatatypeForCalculation(calculation.ValueType),
                    AllowNulls = true
                });
            }

            return fields;
        }

        private string GetSqlDatatypeForCalculation(CalcsApiCalculationValueType valueType)
            => valueType switch
            {
                var format when format == CalcsApiCalculationValueType.Currency ||
                                format == CalcsApiCalculationValueType.Number ||
                                format == CalcsApiCalculationValueType.Percentage
                    => "[decimal](30, 18)",
                CalcsApiCalculationValueType.Boolean => "[bit]",
                CalcsApiCalculationValueType.String => "[varchar](256)",
                _ => throw new InvalidOperationException("Unknown value type")
            };

        private async Task<UniqueTemplateContents> GetTemplateData(SpecificationSummary specification, string fundingStreamId)
        {
            UniqueTemplateContents uniqueTemplateContents = new UniqueTemplateContents();

            TemplateMetadataContents templateMetadata = await GetTemplateMetadataContents(specification, fundingStreamId);

            if (templateMetadata == null)
            {
                throw new NonRetriableException(
                    $"Did not locate template information for specification {specification.Id} in {fundingStreamId}. Unable to complete Qa Schema Generation");
            }

            IEnumerable<FundingLine> flattenedFundingLines = templateMetadata.RootFundingLines.Flatten(_ => _.FundingLines)
                                                             ?? Array.Empty<FundingLine>();

            IEnumerable<FundingLine> uniqueFundingLines = flattenedFundingLines.GroupBy(x => x.TemplateLineId)
                .Select(f => f.First());

            IEnumerable<Calculation> flattenedCalculations =
                flattenedFundingLines.SelectMany(_ => _.Calculations.Flatten(cal => cal.Calculations)) ?? Array.Empty<Calculation>();

            IEnumerable<Calculation> uniqueFlattenedCalculations =
                flattenedCalculations.GroupBy(x => x.TemplateCalculationId)
                    .Select(x => x.FirstOrDefault());

            uniqueTemplateContents.FundingLines = uniqueFundingLines;
            uniqueTemplateContents.Calculations = uniqueFlattenedCalculations;

            return uniqueTemplateContents;
        }

        private async Task<TemplateMetadataContents> GetTemplateMetadataContents(SpecificationSummary specification, string fundingStreamId)
        {
            string templateVersion = specification.TemplateIds[fundingStreamId];

            ApiResponse<FundingTemplateContents> templateContentsRequest =
                await _policiesResilience.ExecuteAsync(() => _policies.GetFundingTemplate(fundingStreamId, specification.FundingPeriod.Id, templateVersion));

            FundingTemplateContents fundingTemplateContents = templateContentsRequest.Content;

            string schemaVersion = fundingTemplateContents?.SchemaVersion ?? fundingTemplateContents?.Metadata?.SchemaVersion;

            if (schemaVersion.IsNullOrWhitespace())
            {
                throw new NonRetriableException($"Did not locate a schema version for specification {specification.Id}");
            }

            ITemplateMetadataGenerator templateContents = _templateMetadataResolver.GetService(schemaVersion);

            if (templateContents == null)
            {
                throw new NonRetriableException(
                    $"Could not resolve template metadata generator for {schemaVersion}. Unable to complete Qa Schema Generation");
            }

            return templateContents.GetMetadata(fundingTemplateContents.TemplateFileContents);
        }

        private IEnumerable<SqlColumnDefinition> GetSqlColumnDefinitionsForFundingLines(IEnumerable<FundingLine> fundingLines)
        {
            List<SqlColumnDefinition> fundingSqlFields = new List<SqlColumnDefinition>(fundingLines.Count());

            foreach (FundingLine fundingLine in fundingLines.OrderBy(f => f.TemplateLineId))
                fundingSqlFields.Add(new SqlColumnDefinition
                {
                    Name = $"Fl_{fundingLine.TemplateLineId}_{_sqlNames.GenerateIdentifier(fundingLine.Name)}",
                    Type = "[decimal](30, 18)",
                    AllowNulls = true
                });

            return fundingSqlFields;
        }
    }
}
