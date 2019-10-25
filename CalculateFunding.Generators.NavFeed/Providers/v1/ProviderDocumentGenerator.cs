using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Generators.Funding;
using CalculateFunding.Generators.NavFeed.Options;
using CalculateFunding.Generators.OrganisationGroup;
using CalculateFunding.Generators.OrganisationGroup.Enums;
using CalculateFunding.Generators.OrganisationGroup.Interfaces;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CsvHelper;
using Serilog;
using ProviderApiClient = CalculateFunding.Common.ApiClient.Providers.Models.Provider;

namespace CalculateFunding.Generators.NavFeed.Providers.v1
{
    public class ProviderDocumentGenerator : IProviderDocumentGenerator
    {
        private readonly IPublishedProviderContentsGenerator _publishedProviderContentsGenerator;
        private readonly IPublishedFundingContentsGenerator _publishedFundingContentsGenerator;
        private readonly IOrganisationGroupTargetProviderLookup organisationGroupTargetProviderLookup;
        private readonly ILogger _logger;

        private const int MajorVersion = 1;
        private const int MinorVersion = 0;

        private const uint TemplateLineId = 1;
        private const uint TemplateCalculationId = 1;
        private const uint TemplateReferenceId = 1;

        private readonly DateTime hardcodedStatusDate = new DateTime(2019, 9, 16);

        public ProviderDocumentGenerator(IPublishedProviderContentsGenerator publishedProviderContentsGenerator, IPublishedFundingContentsGenerator publishedFundingContentsGenerator,
            IProvidersApiClient providersApiClient, IOrganisationGroupResiliencePolicies organisationGroupResiliencePolicies, ILogger logger)
        {
            _publishedProviderContentsGenerator = publishedProviderContentsGenerator;
            _publishedFundingContentsGenerator = publishedFundingContentsGenerator;

            organisationGroupTargetProviderLookup = new OrganisationGroupTargetProviderLookup(providersApiClient, organisationGroupResiliencePolicies);

            _logger = logger;
        }

        public async Task<int> Generate(FeedOptions options)
        {
            IEnumerable<Provider> records = GetRecords(options.InputFilePath);

            foreach (Provider provider in records)
            {
                PublishedProviderVersion publishedProviderVersion = GetPublishedProviderVersion(provider);
                Common.TemplateMetadata.Models.TemplateMetadataContents templateMetadataContents = GetProviderTemplateMetadataContents();
                TemplateMapping templateMapping = GetTemplateMapping();
                GeneratedProviderResult generatedProviderResult = GetGeneratedProviderResult(provider);

                string generatedProviderDocumentContent = _publishedProviderContentsGenerator.GenerateContents(publishedProviderVersion, templateMetadataContents, templateMapping, generatedProviderResult);
                PublishProviderDocument(options, publishedProviderVersion, generatedProviderDocumentContent);
            }

            int fundingIndex = 0;
            foreach (IGrouping<string, Provider> laGroup in records.GroupBy(x => x.ProviderLaCode))
            {
                OrganisationGroupLookupParameters organisationGroupLookupParameters = new OrganisationGroupLookupParameters
                {
                    OrganisationGroupTypeCode = Common.ApiClient.Policies.Models.OrganisationGroupTypeCode.LocalAuthority,
                    IdentifierValue = laGroup.Key,
                    GroupTypeIdentifier = Common.ApiClient.Policies.Models.OrganisationGroupTypeIdentifier.LACode,
                    ProviderVersionId = options.ProviderVersion
                };
                IEnumerable<ProviderApiClient> apiClientProviders = GetApiClientProviders(laGroup);
                TargetOrganisationGroup targetOrganisationGroup = await organisationGroupTargetProviderLookup.GetTargetProviderDetails(organisationGroupLookupParameters, Common.ApiClient.Policies.Models.GroupingReason.Payment, apiClientProviders);

                PublishedFundingVersion publishedFundingVersion = GetPublishedFundingVersion(laGroup, targetOrganisationGroup, fundingIndex);
                Common.TemplateMetadata.Models.TemplateMetadataContents templateMetadataContents = GetFundingTemplateMetadataContents();
                string generatedFundingDocumentContent = _publishedFundingContentsGenerator.GenerateContents(publishedFundingVersion, templateMetadataContents);

                PublishFundingDocument(options, publishedFundingVersion, generatedFundingDocumentContent);

                fundingIndex++;
            }

            _logger.Information("NAV Data generation completed.");

            return 1;
        }

        private PublishedFundingVersion GetPublishedFundingVersion(IGrouping<string, Provider> laGroup, TargetOrganisationGroup targetOrganisationGroup, int fundingIndex)
        {
            Provider anyProvider = laGroup.FirstOrDefault();
            PublishedFundingVersion publishedFundingVersion = new PublishedFundingVersion
            {
                FundingId = $"{anyProvider?.FundingStreamId}-{(PublishedFundingPeriodType)Enum.Parse(typeof(PublishedFundingPeriodType), anyProvider?.FundingStreamPeriodTypeId)}-{anyProvider?.FundingPeriodId}-{GroupingReason.Payment.ToString()}-{Common.ApiClient.Policies.Models.OrganisationGroupTypeCode.LocalAuthority.ToString()}-{targetOrganisationGroup?.Identifier}-{MajorVersion}_{MinorVersion}",
                SchemaVersion = "1.0",
                TemplateVersion = "1.0",
                MajorVersion = MajorVersion,
                MinorVersion = MinorVersion,
                ProviderFundings = laGroup.Select(x => GetFundingId(x)),
                GroupingReason = GroupingReason.Payment,
                FundingStreamId = anyProvider?.FundingStreamId,
                FundingStreamName = anyProvider?.FundingStreamName,
                FundingPeriod = new PublishedFundingPeriod
                {
                    Type = (PublishedFundingPeriodType)Enum.Parse(typeof(PublishedFundingPeriodType), anyProvider?.FundingStreamPeriodTypeId),
                    Period = anyProvider?.FundingPeriodId,
                    Name = anyProvider?.FundingStreamPeriodTypeName,
                    StartDate = new DateTime(2000 + int.Parse(anyProvider?.FundingPeriodId.Substring(0, 2)), int.Parse(anyProvider?.FundingStreamPeriodTypeStartMonth), int.Parse(anyProvider?.FundingStreamPeriodTypeStartDay)),
                    EndDate = new DateTime(2000 + int.Parse(anyProvider?.FundingPeriodId.Substring(2, 2)), int.Parse(anyProvider?.FundingStreamPeriodTypeEndMoth), int.Parse(anyProvider?.FundingStreamPeriodTypeEndDay))
                },
                OrganisationGroupTypeIdentifier = targetOrganisationGroup?.Identifier,
                OrganisationGroupName = targetOrganisationGroup?.Name,
                OrganisationGroupIdentifiers = targetOrganisationGroup?.Identifiers?.Select(x => new PublishedOrganisationGroupTypeIdentifier
                {
                    Value = x.Value,
                    Type = Enum.GetName(typeof(OrganisationGroupTypeIdentifier), x.Type)
                }),
                OrganisationGroupTypeClassification = "LegalEntity",
                OrganisationGroupIdentifierValue = targetOrganisationGroup?.Identifiers?.Where(x => x.Type == OrganisationGroupTypeIdentifier.UKPRN).FirstOrDefault()?.Value,
                OrganisationGroupSearchableName = Helpers.SanitiseName(targetOrganisationGroup?.Name),
                FundingLines = new List<FundingLine>
                        {
                            new FundingLine
                            {
                                TemplateLineId = TemplateLineId,
                                Value = decimal.ToInt64(laGroup.Sum(x=>decimal.Parse(x.OctoberProfileValue) + decimal.Parse(x.AprilProfileValue))),
                                DistributionPeriods = laGroup
                                                        .GroupBy(la => la.OctoberDistributionPeriod)
                                                        .Select(dp => new DistributionPeriod{
                                                            DistributionPeriodId = dp.Key,
                                                            Value = dp.Sum(p => decimal.Parse(p.OctoberProfileValue)),
                                                            ProfilePeriods = new List<ProfilePeriod>
                                                            {
                                                                new ProfilePeriod
                                                                {
                                                                    Type = ProfilePeriodType.CalendarMonth,
                                                                    TypeValue = dp.FirstOrDefault()?.OctoberPeriod,
                                                                    Year = int.Parse(dp.FirstOrDefault()?.OctoberPeriodYear),
                                                                    Occurrence = 1,
                                                                    ProfiledValue = dp.Sum(p => decimal.Parse(p.OctoberProfileValue)),
                                                                    DistributionPeriodId = dp.Key
                                                                }
                                                            }
                                                        })
                                                        .Concat(laGroup
                                                            .GroupBy(la => la.AprilDistributionPeriod)
                                                            .Select(dp => new DistributionPeriod{
                                                                DistributionPeriodId = dp.Key,
                                                                Value = dp.Sum(p => decimal.Parse(p.AprilProfileValue)),
                                                                ProfilePeriods = new List<ProfilePeriod>
                                                                {
                                                                    new ProfilePeriod
                                                                    {
                                                                        Type = ProfilePeriodType.CalendarMonth,
                                                                        TypeValue = dp.FirstOrDefault()?.AprilPeriod,
                                                                        Year = int.Parse(dp.FirstOrDefault()?.AprilPeriodYear),
                                                                        Occurrence = 1,
                                                                        ProfiledValue = dp.Sum(p => decimal.Parse(p.AprilProfileValue)),
                                                                        DistributionPeriodId = dp.Key
                                                                    }
                                                                }
                                                            })
                                                        )
                            }
                        },
                StatusChangedDate = hardcodedStatusDate.AddMinutes(fundingIndex),
                ExternalPublicationDate = hardcodedStatusDate.AddMinutes(fundingIndex),
                EarliestPaymentAvailableDate = hardcodedStatusDate.AddMinutes(fundingIndex)
            };

            if (publishedFundingVersion.FundingLines != null)
            {
                publishedFundingVersion.TotalFunding = publishedFundingVersion.FundingLines
                    .Where(fundingLine => fundingLine != null)
                    .Aggregate(
                        (decimal?)null,
                        (current, fundingLineTotal) => current.AddValueIfNotNull(fundingLineTotal.Value));
            }

            return publishedFundingVersion;
        }

        private Common.TemplateMetadata.Models.TemplateMetadataContents GetFundingTemplateMetadataContents()
        {
            return new Common.TemplateMetadata.Models.TemplateMetadataContents
            {
                RootFundingLines = new List<Common.TemplateMetadata.Models.FundingLine>
                {
                    new Common.TemplateMetadata.Models.FundingLine
                    {
                        Name = "Total funding line",
                        FundingLineCode = "TotalFundingLine",
                        TemplateLineId = TemplateLineId,
                        Type = Common.TemplateMetadata.Enums.FundingLineType.Payment,
                        Calculations = null
                    }
                }
            };
        }

        private IEnumerable<ProviderApiClient> GetApiClientProviders(IEnumerable<Provider> providers)
        {
            return providers.Select(x => new ProviderApiClient
            {
                Authority = x.ProviderAuthority,
                CensusWardCode = null,
                CensusWardName = null,
                CompaniesHouseNumber = null,
                CountryCode = null,
                CountryName = null,
                CrmAccountId = x.ProviderCrmAccountId,
                DateClosed = string.IsNullOrEmpty(x.ProviderDateClosed) ? (DateTimeOffset?)null : DateTimeOffset.Parse(x.ProviderDateClosed),
                DateOpened = string.IsNullOrEmpty(x.ProviderDateOpened) ? (DateTimeOffset?)null : DateTimeOffset.Parse(x.ProviderDateOpened),
                DfeEstablishmentNumber = x.ProviderDfeEstablishmentNumber,
                DistrictCode = null,
                DistrictName = null,
                EstablishmentNumber = x.ProviderEstablishmentNumber,
                GovernmentOfficeRegionCode = null,
                GovernmentOfficeRegionName = null,
                GroupIdNumber = null,
                LACode = x.ProviderLaCode,
                LegalName = null,
                Name = x.ProviderName,
                LocalAuthorityName = null,
                LowerSuperOutputAreaCode = null,
                LowerSuperOutputAreaName = null,
                MiddleSuperOutputAreaCode = null,
                MiddleSuperOutputAreaName = null,
                NavVendorNo = x.ProviderNavVendorNumber,
                ParliamentaryConstituencyCode = null,
                ParliamentaryConstituencyName = null,
                PhaseOfEducation = x.ProviderPhaseOfEducation,
                Postcode = null,
                ProviderId = x.providerId,
                ProviderProfileIdType = null,
                TrustCode = null,
                TrustName = null,
                ReasonEstablishmentClosed = null,
                ReasonEstablishmentOpened = null,
                RscRegionCode = null,
                RscRegionName = null,
                Town = null,
                Successor = null,
                WardCode = null,
                WardName = null,
                ProviderType = x.ProviderProviderType,
                ProviderSubType = x.ProviderProviderSubType,
                UKPRN = null,
                UPIN = x.ProviderUpin,
                URN = x.ProviderUrn,
                Status = x.ProviderStatus,
                ProviderVersionId = null,
                ProviderVersionIdProviderId = null,
                TrustStatus = Common.ApiClient.Providers.Models.TrustStatus.NotApplicable
            });
        }

        private IEnumerable<Provider> GetRecords(string filePath)
        {
            using (StreamReader reader = new StreamReader(filePath))
            {
                using (CsvReader csv = new CsvReader(reader))
                {
                    csv.Configuration.RegisterClassMap<ProviderMap>();
                    return csv.GetRecords<Provider>().ToList();
                }
            }
        }

        private PublishedProviderVersion GetPublishedProviderVersion(Provider input)
        {
            return new PublishedProviderVersion
            {
                MajorVersion = MajorVersion,
                MinorVersion = MinorVersion,
                ProviderId = input.providerId,
                Provider = new Models.Publishing.Provider
                {
                    Name = input.ProviderName,
                    URN = input.ProviderUrn,
                    UKPRN = null,
                    LACode = input.ProviderLaCode,
                    UPIN = input.ProviderUpin,
                    DfeEstablishmentNumber = input.ProviderDfeEstablishmentNumber,
                    ProviderVersionId = null,
                    ProviderType = input.ProviderProviderType,
                    ProviderSubType = input.ProviderProviderSubType,
                    DateOpened = string.IsNullOrEmpty(input.ProviderDateOpened) ? (DateTimeOffset?)null : DateTimeOffset.Parse(input.ProviderDateOpened),
                    DateClosed = string.IsNullOrEmpty(input.ProviderDateClosed) ? (DateTimeOffset?)null : DateTimeOffset.Parse(input.ProviderDateClosed),
                    Status = input.ProviderStatus,
                    PhaseOfEducation = input.ProviderPhaseOfEducation,
                    ReasonEstablishmentOpened = null,
                    ReasonEstablishmentClosed = null,
                    TrustStatus = ProviderTrustStatus.NotApplicable,
                    Town = null,
                    Postcode = null,
                    CompaniesHouseNumber = null,
                    GroupIdNumber = null,
                    RscRegionName = null,
                    RscRegionCode = null,
                    GovernmentOfficeRegionName = null,
                    GovernmentOfficeRegionCode = null,
                    DistrictName = null,
                    DistrictCode = null,
                    WardName = null,
                    WardCode = null,
                    CensusWardName = null,
                    CensusWardCode = null,
                    MiddleSuperOutputAreaName = null,
                    MiddleSuperOutputAreaCode = null,
                    LowerSuperOutputAreaName = null,
                    LowerSuperOutputAreaCode = null,
                    ParliamentaryConstituencyName = null,
                    ParliamentaryConstituencyCode = null,
                    CountryCode = null,
                    CountryName = null,
                    Successor = null
                },
                TotalFunding = decimal.ToInt32(decimal.Parse(input.OctoberProfileValue) + decimal.Parse(input.AprilProfileValue)),
                FundingStreamId = input.FundingStreamId, // JSON has it as fundingStreamCode but will be serialized as fundingStreamId
                FundingPeriodId = input.FundingPeriodId,
                VariationReasons = null,
                Predecessors = null
            };
        }
        private Common.TemplateMetadata.Models.TemplateMetadataContents GetProviderTemplateMetadataContents()
        {
            return new Common.TemplateMetadata.Models.TemplateMetadataContents
            {
                RootFundingLines = new List<Common.TemplateMetadata.Models.FundingLine>
                {
                    new Common.TemplateMetadata.Models.FundingLine
                    {
                        Name = "Total funding line",
                        FundingLineCode = "TotalFundingLine",
                        TemplateLineId = TemplateLineId,
                        Type = Common.TemplateMetadata.Enums.FundingLineType.Payment,
                        Calculations = new List<Common.TemplateMetadata.Models.Calculation>
                        {
                            new Common.TemplateMetadata.Models.Calculation
                            {
                                Name = "Number of pupils",
                                TemplateCalculationId = TemplateCalculationId,
                                ValueFormat = Common.TemplateMetadata.Enums.CalculationValueFormat.Number,
                                Type = Common.TemplateMetadata.Enums.CalculationType.PupilNumber,
                                FormulaText = "Something * something",
                                AggregationType = Common.TemplateMetadata.Enums.AggregationType.Sum,
                                ReferenceData = new List<Common.TemplateMetadata.Models.ReferenceData>
                                {
                                    new Common.TemplateMetadata.Models.ReferenceData
                                    {
                                        Name = "Academic year 2018 to 2019 pupil number on roll",
                                        TemplateReferenceId = TemplateReferenceId,
                                        Format = Common.TemplateMetadata.Enums.ReferenceDataValueFormat.Number,
                                        AggregationType = Common.TemplateMetadata.Enums.AggregationType.Sum,
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }

        private GeneratedProviderResult GetGeneratedProviderResult(Provider input)
        {
            return new GeneratedProviderResult
            {
                FundingLines = new List<FundingLine>
                {
                    new FundingLine
                    {
                        TemplateLineId = TemplateLineId,
                        Value = decimal.Parse(input.OctoberProfileValue) + decimal.Parse(input.AprilProfileValue),
                        DistributionPeriods = new List<DistributionPeriod>
                        {
                            new DistributionPeriod
                            {
                                Value = decimal.Parse(input.OctoberProfileValue),
                                DistributionPeriodId = input.OctoberDistributionPeriod,
                                ProfilePeriods = new List<ProfilePeriod>
                                {
                                    new ProfilePeriod
                                    {
                                        Type = (ProfilePeriodType) Enum.Parse(typeof(ProfilePeriodType), input.OctoberPeriodType),
                                        TypeValue = input.OctoberPeriod,
                                        Year = int.Parse(input.OctoberPeriodYear),
                                        Occurrence = int.Parse(input.OctoberOccurrence),
                                        ProfiledValue = decimal.Parse(input.OctoberProfileValue),
                                        DistributionPeriodId = input.OctoberDistributionPeriod
                                    }
                                }
                            },
                            new DistributionPeriod
                            {
                                Value = int.Parse(input.AprilProfileValue),
                                DistributionPeriodId = input.AprilDistributionPeriod,
                                ProfilePeriods = new List<ProfilePeriod>
                                {
                                    new ProfilePeriod
                                    {
                                        Type = (ProfilePeriodType) Enum.Parse(typeof(ProfilePeriodType), input.AprilPeriodType),
                                        TypeValue = input.AprilPeriod,
                                        Year = int.Parse(input.AprilPeriodYear),
                                        Occurrence = int.Parse(input.AprilOccurrence),
                                        ProfiledValue = int.Parse(input.AprilProfileValue),
                                        DistributionPeriodId = input.AprilDistributionPeriod
                                    }
                                }
                            }
                        }
                    }
                },
                Calculations = new List<FundingCalculation>
                {
                    new FundingCalculation
                    {
                        TemplateCalculationId = TemplateCalculationId,
                        Value = input.PupilCount
                    }
                },
                ReferenceData = new List<FundingReferenceData>
                {
                    new FundingReferenceData
                    {
                        TemplateReferenceId = TemplateReferenceId,
                        Value = "1"
                    }
                }
            };
        }

        private TemplateMapping GetTemplateMapping()
        {
            return new TemplateMapping
            {
                TemplateMappingItems = new List<TemplateMappingItem>
                {
                    new TemplateMappingItem
                    {
                        TemplateId = TemplateCalculationId,
                        CalculationId = "TemplateMappingCalculationId"
                    }
                }
            };
        }

        private void PublishProviderDocument(FeedOptions options, PublishedProviderVersion publishedProviderVersion, string generatedDocumentContent)
        {
            if (options.FeedStorageType == FeedStorageType.File)
            {
                File.WriteAllText(Path.Combine(options.OutputFolderPath, $"{publishedProviderVersion.FundingId}.json"), generatedDocumentContent);
            }
        }

        private void PublishFundingDocument(FeedOptions options, PublishedFundingVersion publishedFundingVersion, string generatedDocumentContent)
        {
            if (options.FeedStorageType == FeedStorageType.File)
            {
                File.WriteAllText(Path.Combine(options.OutputFolderPath, $"{publishedFundingVersion.FundingId}.json"), generatedDocumentContent);
            }
        }

        private static string GetFundingId(Provider input)
        {
            return $"PSG-{input.FundingPeriodId}-{input.providerId}-{MajorVersion}_{MinorVersion}";
        }
    }
}
