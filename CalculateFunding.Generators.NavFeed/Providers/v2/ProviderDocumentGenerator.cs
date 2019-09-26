using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.ApiClient.Providers;
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

namespace CalculateFunding.Generators.NavFeed.Providers.v2
{
    public class ProviderDocumentGenerator : IProviderDocumentGenerator
    {
        private readonly IPublishedProviderContentsGenerator _publishedProviderContentsGenerator;
        private readonly IPublishedFundingContentsGenerator _publishedFundingContentsGenerator;
        private readonly IOrganisationGroupTargetProviderLookup organisationGroupTargetProviderLookup;
        private readonly ILogger _logger;

        // For this sample always creating 1 sample of below template objects
        private const uint TemplateLineId = 1;
        private const uint TemplateCalculationId = 1;
        private const uint TemplateReferenceId = 1;

        private const int PupilCount = 10;

        private readonly DateTime hardcodedStatusDate = new DateTime(2019, 9, 16);

        public ProviderDocumentGenerator(IPublishedProviderContentsGenerator publishedProviderContentsGenerator, IPublishedFundingContentsGenerator publishedFundingContentsGenerator,
            IProvidersApiClient providersApiClient, IOrganisationGroupResiliencePolicies organisationGroupResiliencePolicies, ILogger logger)
        {
            _publishedProviderContentsGenerator = publishedProviderContentsGenerator;
            _publishedFundingContentsGenerator = publishedFundingContentsGenerator;
            _logger = logger;

            organisationGroupTargetProviderLookup = new OrganisationGroupTargetProviderLookup(providersApiClient, organisationGroupResiliencePolicies);
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
            foreach (IGrouping<(string, string, string), Provider> groupingKey in records.GroupBy(x => (x.LACode, x.MajorVersionNo, x.AllocationID)))
            {
                OrganisationGroupLookupParameters organisationGroupLookupParameters = new OrganisationGroupLookupParameters
                {
                    OrganisationGroupTypeCode = Common.ApiClient.Policies.Models.OrganisationGroupTypeCode.LocalAuthority,
                    IdentifierValue = groupingKey.Key.Item1,
                    GroupTypeIdentifier = Common.ApiClient.Policies.Models.OrganisationGroupTypeIdentifier.LACode,
                    ProviderVersionId = options.ProviderVersion
                };
                IEnumerable<ProviderApiClient> apiClientProviders = GetApiClientProviders(groupingKey);
                TargetOrganisationGroup targetOrganisationGroup = null;

                try
                {
                    targetOrganisationGroup = await organisationGroupTargetProviderLookup.GetTargetProviderDetails(organisationGroupLookupParameters, Common.ApiClient.Policies.Models.GroupingReason.Payment, apiClientProviders);
                }
                catch (Exception ex)
                {
                    string message = $"Could not find provider with ID:{organisationGroupLookupParameters.IdentifierValue} with error message {ex}";
                    _logger.Error(message);
                    Console.WriteLine(message);

                    continue;
                }

                PublishedFundingVersion publishedFundingVersion = GetPublishedFundingVersion(groupingKey, targetOrganisationGroup, fundingIndex);
                Common.TemplateMetadata.Models.TemplateMetadataContents templateMetadataContents = GetFundingTemplateMetadataContents();
                string generatedFundingDocumentContent = _publishedFundingContentsGenerator.GenerateContents(publishedFundingVersion, templateMetadataContents);

                PublishFundingDocument(options, publishedFundingVersion, generatedFundingDocumentContent);

                fundingIndex++;
            }

            _logger.Information("NAV Data generation completed.");

            return 1;
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

        private PublishedFundingVersion GetPublishedFundingVersion(IGrouping<(string, string, string), Provider> groupingKey, TargetOrganisationGroup targetOrganisationGroup, int fundingIndex)
        {
            Provider anyProvider = groupingKey.FirstOrDefault();
            PublishedFundingVersion publishedFundingVersion = new PublishedFundingVersion
            {
                FundingId = $"{groupingKey.Key.Item3}-{(PublishedFundingPeriodType)Enum.Parse(typeof(PublishedFundingPeriodType), anyProvider?.PeriodTypeID)}-{anyProvider?.PeriodID}-{GroupingReason.Payment.ToString()}-{Common.ApiClient.Policies.Models.OrganisationGroupTypeCode.LocalAuthority.ToString()}-{targetOrganisationGroup?.Identifier}-{groupingKey.Key.Item2}_{0}",
                SchemaVersion = "1.0",
                TemplateVersion = "1.0",
                MajorVersion = int.Parse(groupingKey.Key.Item1),
                MinorVersion = 0,
                ProviderFundings = groupingKey.Select(x => GetFundingFundingId(x, groupingKey.Key.Item3)),
                GroupingReason = GroupingReason.Payment,
                FundingStreamId = anyProvider?.FundingStreamID,
                FundingStreamName = anyProvider?.FundingStreamName,
                FundingPeriod = new PublishedFundingPeriod
                {
                    Type = (PublishedFundingPeriodType)Enum.Parse(typeof(PublishedFundingPeriodType), anyProvider?.PeriodTypeID),
                    Period = anyProvider?.PeriodID,
                    Name = anyProvider?.PeriodTypeName,
                    StartDate = new DateTime(int.Parse(anyProvider?.StartYear), int.Parse(anyProvider?.StartMonth), int.Parse(anyProvider?.StartDay)),
                    EndDate = new DateTime(int.Parse(anyProvider?.EndYear), int.Parse(anyProvider?.EndMonth), int.Parse(anyProvider?.EndDay))
                },
                OrganisationGroupTypeIdentifier = targetOrganisationGroup?.Identifier,
                OrganisationGroupName = targetOrganisationGroup?.Name,
                OrganisationGroupIdentifiers = targetOrganisationGroup?.Identifiers?.Select(x => new PublishedOrganisationGroupTypeIdentifier
                {
                    Value = x.Value,
                    Type = Enum.GetName(typeof(OrganisationGroupTypeIdentifier), x.Type)
                }),
                OrganisationGroupTypeCategory = "LegalEntity",
                OrganisationGroupIdentifierValue = targetOrganisationGroup?.Identifiers?.Where(x => x.Type == OrganisationGroupTypeIdentifier.UKPRN).FirstOrDefault()?.Value,
                OrganisationGroupSearchableName = Helpers.SanitiseName(targetOrganisationGroup?.Name),
                OrganisationGroupTypeCode = groupingKey.Key.Item3,
                FundingLines = new List<FundingLine>
                        {
                            new FundingLine
                            {
                                TemplateLineId = TemplateLineId,
                                Value = decimal.ToInt64(groupingKey.Sum(x=>decimal.Parse(x.AllocationAmount))),
                                DistributionPeriods = groupingKey
                                                        .GroupBy(la => la.PeriodID)
                                                        .Select(dp => new DistributionPeriod{
                                                            DistributionPeriodId = dp.Key,
                                                            Value = dp.Sum(p => decimal.Parse(p.AllocationAmount)),
                                                            ProfilePeriods = new List<ProfilePeriod>
                                                            {
                                                                new ProfilePeriod
                                                                {
                                                                    Type = ProfilePeriodType.CalendarMonth,
                                                                    TypeValue = dp.FirstOrDefault()?.EndMonth,
                                                                    Year = int.Parse(dp.FirstOrDefault()?.EndYear),
                                                                    Occurrence = 1,
                                                                    ProfiledValue = dp.Sum(p => decimal.Parse(p.AllocationAmount)),
                                                                    DistributionPeriodId = dp.Key
                                                                }
                                                            }
                                                        })
                            }
                        },
                StatusChangedDate = hardcodedStatusDate.AddMinutes(fundingIndex),
                ExternalPublicationDate = hardcodedStatusDate.AddMinutes(fundingIndex),
                EarliestPaymentAvailableDate = hardcodedStatusDate.AddMinutes(fundingIndex)
            };

            publishedFundingVersion.TotalFunding = decimal.ToInt32(publishedFundingVersion.FundingLines.Sum(x => x.Value));
            return publishedFundingVersion;
        }

        private IEnumerable<ProviderApiClient> GetApiClientProviders(IEnumerable<Provider> providers)
        {
            return providers.Select(x => new ProviderApiClient
            {
                Authority = x.LocalAuthority,
                CensusWardCode = null,
                CensusWardName = null,
                CompaniesHouseNumber = null,
                CountryCode = null,
                CountryName = null,
                CrmAccountId = null,
                DateClosed = null,
                DateOpened = null,
                DfeEstablishmentNumber = x.DFEEstablishNo,
                DistrictCode = null,
                DistrictName = null,
                EstablishmentNumber = x.EstablishmentNo,
                GovernmentOfficeRegionCode = null,
                GovernmentOfficeRegionName = null,
                GroupIdNumber = null,
                LACode = x.LACode,
                LegalName = null,
                Name = x.ProviderName,
                LocalAuthorityName = null,
                LowerSuperOutputAreaCode = null,
                LowerSuperOutputAreaName = null,
                MiddleSuperOutputAreaCode = null,
                MiddleSuperOutputAreaName = null,
                NavVendorNo = x.NAVVendorNo,
                ParliamentaryConstituencyCode = null,
                ParliamentaryConstituencyName = null,
                PhaseOfEducation = null,
                Postcode = null,
                ProviderId = x.ID,
                ProviderProfileIdType = null,
                TrustCode = null,
                TrustName = null,
                ReasonEstablishmentClosed = x.CloseReason,
                ReasonEstablishmentOpened = x.OpenReason,
                RscRegionCode = null,
                RscRegionName = null,
                Town = null,
                Successor = x.Successors,
                WardCode = null,
                WardName = null,
                ProviderType = x.Type,
                ProviderSubType = x.Type,
                UKPRN = x.UKPRN,
                UPIN = x.UPIN,
                URN = x.URN,
                Status = x.ProviderStatus,
                ProviderVersionId = "psg-1",
                ProviderVersionIdProviderId = null,
                TrustStatus = Common.ApiClient.Providers.Models.TrustStatus.NotApplicable
            });
        }

        private PublishedProviderVersion GetPublishedProviderVersion(Provider input)
        {
            return new PublishedProviderVersion
            {
                MajorVersion = int.Parse(input.MajorVersionNo),
                MinorVersion = int.Parse(input.MinorVersionNo),
                ProviderId = input.UKPRN,
                Provider = new Models.Publishing.Provider
                {
                    Name = input.ProviderName,
                    URN = input.URN,
                    UKPRN = input.UKPRN,
                    LACode = input.LACode,
                    UPIN = input.UPIN,
                    DfeEstablishmentNumber = input.DFEEstablishNo,
                    ProviderVersionId = "psg-1",
                    ProviderType = input.Type,
                    ProviderSubType = input.SubType,
                    DateOpened = null,
                    DateClosed = null,
                    Status = input.ProviderStatus,
                    PhaseOfEducation = null,
                    ReasonEstablishmentOpened = input.OpenReason,
                    ReasonEstablishmentClosed = input.CloseReason,
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
                    Successor = input.Successors
                },
                TotalFunding = (int)decimal.Parse(input.AllocationAmount),
                FundingStreamId = input.FundingStreamID,
                FundingPeriodId = input.PeriodID,
                VariationReasons = string.IsNullOrEmpty(input.VariationReasons.Trim()) ? 
                    null : 
                    input.VariationReasons.Trim()
                        .Split(' ')
                        .Select(x=> x == "LACodeField" ? "LACodeFieldUpdated" : x)
                        .Where(x=> Enum.TryParse<VariationReason>(x, out _))
                        .Select(x => Enum.Parse<VariationReason>(x))
                        .ToList(),
                Predecessors = string.IsNullOrEmpty(input.Precessors) ? null : new List<string> { input.Precessors }
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

        private GeneratedProviderResult GetGeneratedProviderResult(Provider input)
        {
            return new GeneratedProviderResult
            {
                FundingLines = new List<FundingLine>
                {
                    new FundingLine
                    {
                        TemplateLineId = TemplateLineId,
                        Value = decimal.Parse(input.AllocationAmount),
                        DistributionPeriods = new List<DistributionPeriod>
                        {
                            new DistributionPeriod
                            {
                                Value = decimal.Parse(input.AllocationAmount),
                                DistributionPeriodId = $"{input.PeriodTypeID}{input.PeriodID}",
                                ProfilePeriods = new List<ProfilePeriod>
                                {
                                    new ProfilePeriod
                                    {
                                        Type = ProfilePeriodType.CalendarMonth,
                                        TypeValue = input.EndMonth,
                                        Year = int.Parse(input.EndYear),
                                        Occurrence = 1,
                                        ProfiledValue = decimal.Parse(input.AllocationAmount),
                                        DistributionPeriodId = $"{input.PeriodTypeID}{input.PeriodID}"
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
                        Value = PupilCount
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

        private static string GetFundingFundingId(Provider input, string allocationLineId)
        {
            return $"{allocationLineId}-{input.PeriodID}-{input.UKPRN}-{input.MajorVersionNo}_{input.MinorVersionNo}";
        }

        private IEnumerable<Provider> GetRecords(string filePath)
        {
            using (StreamReader reader = new StreamReader(filePath))
            {
                using (CsvReader csv = new CsvReader(reader))
                {
                    csv.Configuration.RegisterClassMap<ProviderMap>();
                    return csv
                        .GetRecords<Provider>()
                        .Where(x=> !string.IsNullOrEmpty(x.UKPRN))
                        .ToList();
                }
            }
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
    }
}
