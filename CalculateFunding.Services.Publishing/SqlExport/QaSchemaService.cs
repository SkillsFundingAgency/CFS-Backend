using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Profiling;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.TemplateMetadata;
using CalculateFunding.Common.TemplateMetadata.Enums;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.SqlExport;
using CalculateFunding.Services.SqlExport.Models;
using Polly;

namespace CalculateFunding.Services.Publishing.SqlExport
{
    public class QaSchemaService : IQaSchemaService
    {
        private readonly IPoliciesApiClient _policies;
        private readonly ISpecificationsApiClient _specifications;
        private readonly ITemplateMetadataResolver _templateMetadataResolver;
        private readonly ISqlSchemaGenerator _schemaGenerator;
        private readonly IQaRepository _qaRepository;
        private readonly IProfilingApiClient _profilingClient;
        private readonly ISqlNameGenerator _sqlNames;
        private readonly AsyncPolicy _specificationResilience;
        private readonly AsyncPolicy _policiesResilience;

        public QaSchemaService(IPoliciesApiClient policies,
            ISpecificationsApiClient specificationsApiClient,
            ITemplateMetadataResolver templateMetadataResolver,
            ISqlSchemaGenerator schemaGenerator,
            IQaRepository qaRepository,
            IProfilingApiClient profilingClient,
            ISqlNameGenerator sqlNames,
            IPublishingResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(policies, nameof(policies));
            Guard.ArgumentNotNull(specificationsApiClient, nameof(specificationsApiClient));
            Guard.ArgumentNotNull(templateMetadataResolver, nameof(templateMetadataResolver));
            Guard.ArgumentNotNull(schemaGenerator, nameof(schemaGenerator));
            Guard.ArgumentNotNull(qaRepository, nameof(qaRepository));
            Guard.ArgumentNotNull(profilingClient, nameof(profilingClient));
            Guard.ArgumentNotNull(sqlNames, nameof(sqlNames));

            _policies = policies;
            _specifications = specificationsApiClient;
            _templateMetadataResolver = templateMetadataResolver;
            _schemaGenerator = schemaGenerator;
            _qaRepository = qaRepository;
            _profilingClient = profilingClient;
            _sqlNames = sqlNames;
            _specificationResilience = resiliencePolicies.SpecificationsApiClient;
            _policiesResilience = resiliencePolicies.PoliciesApiClient;

            //TODO; extract all of the different table builders so that this can more easily tested
            //at the moment it needs a god test with too much setup to make much sense to anyone
        }

        public async Task<SchemaContext> ReCreateTablesForSpecificationAndFundingStream(string specificationId,
            string fundingStreamId)
        {
            ApiResponse<SpecificationSummary> specificationResponse = await _specificationResilience.ExecuteAsync(()
                => _specifications.GetSpecificationSummaryById(specificationId));

            SpecificationSummary specification = specificationResponse?.Content;

            if (specification == null)
            {
                throw new NonRetriableException(
                    $"Did not locate a specification {specificationId}. Unable to complete Qa Schema Generation");
            }

            SchemaContext schemaContext = new SchemaContext();

            await EnsureTablesForFundingStream(specification, fundingStreamId, schemaContext);

            return schemaContext;
        }

        public async Task<SchemaContext> EnsureSqlTablesForSpecification(string specificationId)
        {
            ApiResponse<SpecificationSummary> specificationResponse = await _specificationResilience.ExecuteAsync(()
                => _specifications.GetSpecificationSummaryById(specificationId));

            SpecificationSummary specification = specificationResponse.Content;

            SchemaContext schemaContext = new SchemaContext();

            foreach (Reference fundingStream in specification.FundingStreams) await EnsureTablesForFundingStream(specification, fundingStream.Id, schemaContext);

            return schemaContext;
        }

        private async Task EnsureTablesForFundingStream(SpecificationSummary specification, string fundingStreamId, SchemaContext schemaContext)
        {
            // TODO: handle multiple version of the template for fields....
            string fundingPeriodId = specification.FundingPeriod.Id;

            await DropExistingTables(fundingStreamId, fundingPeriodId);
            
            UniqueTemplateContents templateMetadata = await GetTemplateData(specification, fundingStreamId);
            Dictionary<string, IEnumerable<SqlColumnDefinition>> profilingTables =
                await GenerateProfiling(fundingStreamId, fundingPeriodId, templateMetadata, schemaContext);

            string fundingStreamTablePrefix = $"{fundingStreamId}_{fundingPeriodId}";

            EnsureTable($"{fundingStreamTablePrefix}_Funding", GetSqlColumnDefinitionsForFunding(), fundingStreamId, fundingPeriodId);
            EnsureTable($"{fundingStreamTablePrefix}_Providers", GetSqlColumnDefinitionsForProviderInformation(), fundingStreamId, fundingPeriodId);

            (IEnumerable<SqlColumnDefinition> informationFundingLineFields,
                IEnumerable<SqlColumnDefinition> paymentFundingLineFields,
                IEnumerable<SqlColumnDefinition> calculationFields) = GetUniqueFundingLinesAndCalculationsForFundingStream(templateMetadata);

            foreach (KeyValuePair<string, IEnumerable<SqlColumnDefinition>> profileTable in profilingTables)
                EnsureTable($"{fundingStreamTablePrefix}_Profiles_{profileTable.Key}", profileTable.Value, fundingStreamId, fundingPeriodId);

            EnsureTable($"{fundingStreamTablePrefix}_InformationFundingLines", informationFundingLineFields, fundingStreamId, fundingPeriodId);
            EnsureTable($"{fundingStreamTablePrefix}_PaymentFundingLines", paymentFundingLineFields, fundingStreamId, fundingPeriodId);
            EnsureTable($"{fundingStreamTablePrefix}_Calculations", calculationFields, fundingStreamId, fundingPeriodId);
        }

        private async Task DropExistingTables(string fundingStreamId,
            string fundingPeriodId)
        {
            IEnumerable<TableForStreamAndPeriod> tablesToDrop = await _qaRepository.GetTablesForFundingStreamAndPeriod(fundingStreamId, fundingPeriodId);

            StringBuilder dropSql = new StringBuilder();

            foreach (TableForStreamAndPeriod table in tablesToDrop)
            {
                dropSql.AppendLine($"DROP TABLE [dbo].[{table.Name}];");
            }

            if (dropSql.Length == 0)
            {
                return;
            }
            
            _qaRepository.ExecuteSql(dropSql.ToString());
        }

        private async Task<Dictionary<string, IEnumerable<SqlColumnDefinition>>> GenerateProfiling(string fundingStreamId,
            string fundingPeriodId, 
            UniqueTemplateContents templateMetadata,
            SchemaContext schemaContext)
        {
            ApiResponse<IEnumerable<FundingStreamPeriodProfilePattern>> profilePatternResults =
                await _profilingClient.GetProfilePatternsForFundingStreamAndFundingPeriod(fundingStreamId, fundingPeriodId);

            IEnumerable<FundingStreamPeriodProfilePattern> profilePatterns = profilePatternResults?.Content?.ToArray();

            if (profilePatterns == null)
            {
                throw new NonRetriableException(
                    $"Did not locate any profile patterns for funding stream {fundingStreamId} and funding period {fundingPeriodId}. Unable to continue with Qa Schema Generation");
            }

            Dictionary<string, IEnumerable<SqlColumnDefinition>> profiling = new Dictionary<string, IEnumerable<SqlColumnDefinition>>();

            foreach (FundingLine fundingLine in templateMetadata.FundingLines.Where(f => f.Type == FundingLineType.Payment))
            {
                string fundingLineCode = fundingLine.FundingLineCode;
                
                IEnumerable<ProfilePeriodPattern> allPatterns = profilePatterns
                    .Where(p => p.FundingLineId == fundingLineCode)
                    .SelectMany(p => p.ProfilePattern)
                    .ToArray();

                IEnumerable<ProfilePeriodPattern> uniqueProfilePatterns = Enumerable.DistinctBy(allPatterns, _ => new
                {
                    _.Occurrence,
                    _.Period,
                    _.PeriodType,
                    _.PeriodYear
                });
                
                schemaContext.AddFundingLineProfilePatterns(fundingLineCode, uniqueProfilePatterns.ToArray());

                List<SqlColumnDefinition> fundingSqlFields = new List<SqlColumnDefinition>();

                foreach (ProfilePeriodPattern pattern in uniqueProfilePatterns)
                {
                    string patternPrefix = $"{pattern.Period}_{pattern.PeriodType}_{pattern.PeriodYear}_{pattern.Occurrence}";
                    fundingSqlFields.Add(new SqlColumnDefinition
                    {
                        Name = $"{patternPrefix}_Period",
                        Type = "[varchar](64)",
                        AllowNulls = true
                    });
                    fundingSqlFields.Add(new SqlColumnDefinition
                    {
                        Name = $"{patternPrefix}_PeriodType",
                        Type = "[varchar](64)",
                        AllowNulls = true
                    });
                    fundingSqlFields.Add(new SqlColumnDefinition
                    {
                        Name = $"{patternPrefix}_Year",
                        Type = "[varchar](64)",
                        AllowNulls = true
                    });
                    fundingSqlFields.Add(new SqlColumnDefinition
                    {
                        Name = $"{patternPrefix}_Occurrence",
                        Type = "[varchar](64)",
                        AllowNulls = true
                    });
                    fundingSqlFields.Add(new SqlColumnDefinition
                    {
                        Name = $"{patternPrefix}_DistributionPeriod",
                        Type = "[varchar](64)",
                        AllowNulls = true
                    });
                    fundingSqlFields.Add(new SqlColumnDefinition
                    {
                        Name = $"{patternPrefix}_Value",
                        Type = "[decimal](30, 2)",
                        AllowNulls = true
                    });
                }

                profiling.Add(fundingLineCode, fundingSqlFields);
            }

            return profiling;
        }
        
        private void EnsureTable(string tableName, IEnumerable<SqlColumnDefinition> fields,
            string fundingStreamId, 
            string fundingPeriodId)
        {
            string sql = _schemaGenerator.GenerateCreateTableSql(tableName, fundingStreamId, fundingPeriodId, fields);

            _qaRepository.ExecuteSql(sql);
        }

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
                                                             ?? new FundingLine[0];

            IEnumerable<FundingLine> uniqueFundingLines = flattenedFundingLines.GroupBy(x => x.TemplateLineId)
                .Select(f => f.First());

            IEnumerable<Calculation> flattenedCalculations =
                flattenedFundingLines.SelectMany(_ => _.Calculations.Flatten(cal => cal.Calculations)) ?? new Calculation[0];

            IEnumerable<Calculation> uniqueFlattenedCalculations =
                flattenedCalculations.GroupBy(x => x.TemplateCalculationId)
                    .Select(x => x.FirstOrDefault());

            uniqueTemplateContents.FundingLines = uniqueFundingLines;
            uniqueTemplateContents.Calculations = uniqueFlattenedCalculations;

            return uniqueTemplateContents;
        }

        private (IEnumerable<SqlColumnDefinition> informationFundingLineFields,
            IEnumerable<SqlColumnDefinition> paymentFundingLineFields,
                IEnumerable<SqlColumnDefinition> calculationFields) GetUniqueFundingLinesAndCalculationsForFundingStream(
                UniqueTemplateContents templateContents)
        {
            IEnumerable<SqlColumnDefinition> informationFundingLineFields =
                GetSqlColumnDefinitionsForFundingLines(templateContents.FundingLines.Where(f => f.Type == FundingLineType.Information));
            IEnumerable<SqlColumnDefinition> paymentFundingLineFields =
                GetSqlColumnDefinitionsForFundingLines(templateContents.FundingLines.Where(f => f.Type == FundingLineType.Payment));
            IEnumerable<SqlColumnDefinition> calculationFields = GetSqlColumnDefinitionsForCalculations(templateContents.Calculations);

            return (informationFundingLineFields, paymentFundingLineFields, calculationFields);
        }

        private IEnumerable<SqlColumnDefinition> GetSqlColumnDefinitionsForFunding()
        {
            //TODO: these column sizes will most likely need some tuning as just doing
            //the import spike threw up some issues with truncation errors etc.
            return new List<SqlColumnDefinition>
            {
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
        }

        private IEnumerable<SqlColumnDefinition> GetSqlColumnDefinitionsForProviderInformation()
        {
            //TODO: these column sizes will most likely need some tuning as just doing
            //the import spike threw up some issues with truncation errors etc.
            return new List<SqlColumnDefinition>
            {
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
            };
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

        private IEnumerable<SqlColumnDefinition> GetSqlColumnDefinitionsForCalculations(IEnumerable<Calculation> calculations)
        {
            List<SqlColumnDefinition> fields = new List<SqlColumnDefinition>(calculations.Count());

            foreach (Calculation calculation in calculations.OrderBy(c => c.TemplateCalculationId))
                fields.Add(new SqlColumnDefinition
                {
                    Name = $"Calc_{calculation.TemplateCalculationId}_{_sqlNames.GenerateIdentifier(calculation.Name)}",
                    Type = GetSqlDatatypeForCalculation(calculation.ValueFormat),
                    AllowNulls = true
                });

            return fields;
        }

        private string GetSqlDatatypeForCalculation(CalculationValueFormat valueFormat)
        {
            return valueFormat switch
            {
                var format when format == CalculationValueFormat.Currency ||
                                format == CalculationValueFormat.Number ||
                                format == CalculationValueFormat.Percentage
                    => "[decimal](30, 18)",
                CalculationValueFormat.Boolean => "[bit]",
                CalculationValueFormat.String => "[varchar](128)",
                _ => throw new InvalidOperationException("Unknown value format")
            };
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
    }
}