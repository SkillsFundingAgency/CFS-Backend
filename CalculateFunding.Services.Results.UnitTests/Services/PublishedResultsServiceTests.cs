using System;
using System.Collections.Generic;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Profiling;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Obsoleted;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Results.Search;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Results.Interfaces;
using CalculateFunding.Services.Results.MappingProfiles;
using CalculateFunding.Services.Results.Repositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Results.UnitTests.Services
{
    [TestClass]
    public partial class PublishedResultsServiceTests
    {
        const string providerId = "123456";
        const string specificationId = "888999";
        const string fundingStreamId = "fs-1";
        const string fundingPeriodId = "fp-1";
        const string jobId = "job-id-1";

        static PublishedResultsService CreateResultsService(ILogger logger = null,
            IMapper mapper = null,
            ITelemetry telemetry = null,
            ICalculationResultsRepository resultsRepository = null,
            ISpecificationsRepository specificationsRepository = null,
            IPoliciesApiClient policiesApiClient = null,
            IResultsResiliencePolicies resiliencePolicies = null,
            IPublishedProviderResultsAssemblerService publishedProviderResultsAssemblerService = null,
            IPublishedProviderResultsRepository publishedProviderResultsRepository = null,
            ICacheProvider cacheProvider = null,
            ISearchRepository<AllocationNotificationFeedIndex> allocationNotificationFeedSearchRepository = null,
            IProfilingApiClient profilingApiClient = null,
            IMessengerService messengerService = null,
            IVersionRepository<PublishedAllocationLineResultVersion> publishedProviderResultsVersionRepository = null,
            IPublishedAllocationLineLogicalResultVersionService publishedAllocationLineLogicalResultVersionService = null,
            IFeatureToggle featureToggle = null,
            IJobsApiClient jobsApiClient = null,
            IPublishedProviderResultsSettings publishedProviderResultsSettings = null,
            IProviderChangesRepository providerChangesRepository = null,
            IProviderVariationsService providerVariationsService = null,
            IProviderVariationsStorageRepository providerVariationsStorageRepository = null)
        {
            ISpecificationsRepository specsRepo = specificationsRepository ?? CreateSpecificationsRepository();

            return new PublishedResultsService(
                logger ?? CreateLogger(),
                mapper ?? CreateMapper(),
                telemetry ?? CreateTelemetry(),
                resultsRepository ?? CreateResultsRepository(),
                specsRepo,
                resiliencePolicies ?? ResultsResilienceTestHelper.GenerateTestPolicies(),
                publishedProviderResultsAssemblerService ?? CreateResultsAssembler(),
                publishedProviderResultsRepository ?? CreatePublishedProviderResultsRepository(),
                cacheProvider ?? CreateCacheProvider(),
                allocationNotificationFeedSearchRepository ?? CreateAllocationNotificationFeedSearchRepository(),
                profilingApiClient ?? CreateProfilingRepository(),
                messengerService ?? CreateMessengerService(),
                publishedProviderResultsVersionRepository ?? CreatePublishedProviderResultsVersionRepository(),
                publishedAllocationLineLogicalResultVersionService ?? CreatePublishedAllocationLineLogicalResultVersionService(),
                featureToggle ?? CreateFeatureToggle(),
                jobsApiClient ?? CreateJobsApiClient(),
                publishedProviderResultsSettings ?? CreatePublishedProviderResultsSettings(),
                providerChangesRepository ?? CreateProviderChangesRepository(),
                providerVariationsService ?? CreateProviderVariationsService(CreateProviderVariationAssemblerService(), policiesApiClient ?? CreatePoliciesApiClient()),
                providerVariationsStorageRepository ?? CreateProviderVariationsStorageRepository()
                );
        }

        static IProviderVariationsStorageRepository CreateProviderVariationsStorageRepository()
        {
            return Substitute.For<IProviderVariationsStorageRepository>();
        }

        static IProviderVariationsService CreateProviderVariationsService(
            IProviderVariationAssemblerService providerVariationAssemblerService,
            IPoliciesApiClient policiesApiClient = null,
            ILogger logger = null,
            IMapper mapper = null)
        {
            return new ProviderVariationsService(
                providerVariationAssemblerService,
                policiesApiClient ?? CreatePoliciesApiClient(),
                ResultsResilienceTestHelper.GenerateTestPolicies(),
                logger ?? CreateLogger(),
                mapper ?? CreateRealMapper()
                );
        }

        static IProviderVariationsService CreateProviderVariationsService()
        {
            return Substitute.For<IProviderVariationsService>();
        }

        static IProviderVariationAssemblerService CreateProviderVariationAssemblerService()
        {
            return Substitute.For<IProviderVariationAssemblerService>();
        }

        static IPublishedProviderResultsSettings CreatePublishedProviderResultsSettings()
        {
            IPublishedProviderResultsSettings publishedProviderResultsSettings = Substitute.For<IPublishedProviderResultsSettings>();
            publishedProviderResultsSettings
                .UpdateAllocationLineResultStatusBatchCount
                .Returns(100);

            return publishedProviderResultsSettings;
        }

        static IFeatureToggle CreateFeatureToggle()
        {
            return Substitute.For<IFeatureToggle>();
        }

        static IJobsApiClient CreateJobsApiClient()
        {
            return Substitute.For<IJobsApiClient>();
        }

        static IPublishedAllocationLineLogicalResultVersionService CreatePublishedAllocationLineLogicalResultVersionService()
        {
            return Substitute.For<IPublishedAllocationLineLogicalResultVersionService>();
        }

        static ICalculationResultsRepository CreateResultsRepository()
        {
            return Substitute.For<ICalculationResultsRepository>();
        }

        static IVersionRepository<PublishedAllocationLineResultVersion> CreatePublishedProviderResultsVersionRepository()
        {
            return Substitute.For<IVersionRepository<PublishedAllocationLineResultVersion>>();
        }

        static IProfilingApiClient CreateProfilingRepository()
        {
            return Substitute.For<IProfilingApiClient>();
        }

        static ISearchRepository<AllocationNotificationFeedIndex> CreateAllocationNotificationFeedSearchRepository()
        {
            return Substitute.For<ISearchRepository<AllocationNotificationFeedIndex>>();
        }

        static ICacheProvider CreateCacheProvider()
        {
            return Substitute.For<ICacheProvider>();
        }

        static IPublishedProviderResultsAssemblerService CreateResultsAssembler()
        {
            return Substitute.For<IPublishedProviderResultsAssemblerService>();
        }

        static IPublishedProviderResultsRepository CreatePublishedProviderResultsRepository()
        {
            return Substitute.For<IPublishedProviderResultsRepository>();
        }

        static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        static ITelemetry CreateTelemetry()
        {
            return Substitute.For<ITelemetry>();
        }

        static IMapper CreateMapper()
        {
            MapperConfiguration mapperConfiguration = new MapperConfiguration(c =>
            {
                c.AddProfile<ResultServiceMappingProfile>();
                c.AddProfile<PolicyMappingProfile>();
            });
            return mapperConfiguration.CreateMapper();
        }

        static IMessengerService CreateMessengerService()
        {
            return Substitute.For<IMessengerService>();
        }

        static ISpecificationsRepository CreateSpecificationsRepository()
        {
            return Substitute.For<ISpecificationsRepository>();
        }

        public static IProviderChangesRepository CreateProviderChangesRepository()
        {
            return Substitute.For<IProviderChangesRepository>();
        }

        static IPoliciesApiClient CreatePoliciesApiClient()
        {
            return Substitute.For<IPoliciesApiClient>();
        }

        static IMapper CreateRealMapper()
        {
            MapperConfiguration mapperConfiguration = new MapperConfiguration(c =>
            {
                c.AddProfile<ResultServiceMappingProfile>();
                c.AddProfile<PolicyMappingProfile>();
            });
            return mapperConfiguration.CreateMapper();
        }

        static SpecificationCurrentVersion CreateSpecification(string specificationId)
        {
            return new SpecificationCurrentVersion
            {
                Id = specificationId
            };
        }

        static DocumentEntity<ProviderResult> CreateDocumentEntity()
        {
            return new DocumentEntity<ProviderResult>
            {
                UpdatedAt = DateTime.Now,
                Content = new ProviderResult
                {
                    SpecificationId = "spec-id",
                    CalculationResults = new List<CalculationResult>
                    {
                        new CalculationResult
                        {
                            Calculation = new Reference { Id = "calc-id-1", Name = "calc name 1" },
                            Value = 123,
                            CalculationType = Models.Calcs.CalculationType.Template
                        },
                        new CalculationResult
                        {
                            Calculation = new Reference { Id = "calc-id-2", Name = "calc name 2" },
                            Value = 10,
                            CalculationType = Models.Calcs.CalculationType.Template
                        }
                    },
                    Provider = new ProviderSummary
                    {
                        Id = "prov-id",
                        Name = "prov name",
                        ProviderType = "prov type",
                        ProviderSubType = "prov sub type",
                        Authority = "authority",
                        UKPRN = "ukprn",
                        UPIN = "upin",
                        URN = "urn",
                        EstablishmentNumber = "12345",
                        LACode = "la code",
                        DateOpened = DateTime.Now.AddDays(-7)
                    }
                }
            };
        }

        static DocumentEntity<ProviderResult> CreateDocumentEntityWithNullCalculationResult()
        {
            return new DocumentEntity<ProviderResult>
            {
                UpdatedAt = DateTime.Now,
                Content = new ProviderResult
                {
                    SpecificationId = "spec-id",
                    CalculationResults = new List<CalculationResult>
                    {
                        new CalculationResult
                        {
                            Calculation = new Reference { Id = "calc-id-1", Name = "calc name 1" },
                            Value = null,
                            CalculationType = Models.Calcs.CalculationType.Template
                        }
                    },
                    Provider = new ProviderSummary
                    {
                        Id = "prov-id",
                        Name = "prov name",
                        ProviderType = "prov type",
                        ProviderSubType = "prov sub type",
                        Authority = "authority",
                        UKPRN = "ukprn",
                        UPIN = "upin",
                        URN = "urn",
                        EstablishmentNumber = "12345",
                        LACode = "la code",
                        DateOpened = DateTime.Now.AddDays(-7)
                    }
                }
            };
        }

        static IEnumerable<MasterProviderModel> CreateProviderModels()
        {
            return new[]
            {
                new MasterProviderModel { MasterUKPRN = "1234" },
                new MasterProviderModel { MasterUKPRN = "5678" },
                new MasterProviderModel { MasterUKPRN = "1122" }
            };
        }

        static IEnumerable<PublishedProviderResult> CreatePublishedProviderResults()
        {
            return new[]
            {
                new PublishedProviderResult
                {
                    SpecificationId = "spec-1",
                    ProviderId = "1111",
                    FundingStreamResult = new PublishedFundingStreamResult
                    {
                        FundingStream = new PublishedFundingStreamDefinition
                        {
                            Id = "fs-1",
                            Name = "funding stream 1"
                        },
                        AllocationLineResult = new PublishedAllocationLineResult
                        {
                            AllocationLine = new PublishedAllocationLineDefinition
                            {
                                Id = "AAAAA",
                                Name = "test allocation line 1",
                                ShortName = "tal1",
                                FundingRoute = PublishedFundingRoute.LA,
                                IsContractRequired = true
                            },
                            Current = new PublishedAllocationLineResultVersion
                            {
                                Status = AllocationLineStatus.Held,
                                Value = 50,
                                Version = 1,
                                Date = DateTimeOffset.Now,
                                PublishedProviderResultId = "res1",
                                ProviderId = "1111",
                                Provider = new ProviderSummary
                                {
                                    URN = "12345",
                                    UKPRN = "1111",
                                    UPIN = "2222",
                                    EstablishmentNumber = "es123",
                                    Authority = "London",
                                    ProviderType = "test type",
                                    ProviderSubType = "test sub type",
                                    DateOpened = DateTimeOffset.Now,
                                    ProviderProfileIdType = "UKPRN",
                                    LACode = "77777",
                                    Id = "1111",
                                    Name = "test provider name 1"
                                }
                            }
                        }
                    },
                    FundingPeriod = new Period
                    {
                        Id = "1819",
                        Name = "fp-1"
                    }
                },
                new PublishedProviderResult
                {
                    SpecificationId = "spec-1",
                    ProviderId = "1111",
                    FundingStreamResult = new PublishedFundingStreamResult
                    {
                        FundingStream = new PublishedFundingStreamDefinition
                        {
                            Id = "fs-1",
                            Name = "funding stream 1"
                        },
                        AllocationLineResult = new PublishedAllocationLineResult
                        {
                            AllocationLine = new PublishedAllocationLineDefinition
                            {
                                Id = "AAAAA",
                                Name = "test allocation line 1",
                                ShortName = "tal1",
                                FundingRoute = PublishedFundingRoute.LA,
                                IsContractRequired = true
                            },
                            Current = new PublishedAllocationLineResultVersion
                            {
                                Status = AllocationLineStatus.Held,
                                Value = 100,
                                Version = 1,
                                Date = DateTimeOffset.Now,
                                PublishedProviderResultId = "res2",
                                ProviderId = "1111",
                                Provider = new ProviderSummary
                                {
                                    URN = "12345",
                                    UKPRN = "1111",
                                    UPIN = "2222",
                                    EstablishmentNumber = "es123",
                                    Authority = "London",
                                    ProviderType = "test type",
                                    ProviderSubType = "test sub type",
                                    DateOpened = DateTimeOffset.Now,
                                    ProviderProfileIdType = "UKPRN",
                                    LACode = "77777",
                                    Id = "1111",
                                    Name = "test provider name 1"
                                }
                            }
                        }
                    },
                    FundingPeriod = new Period
                    {
                        Id = "1819",
                        Name = "fp-1"
                    }
                },
                new PublishedProviderResult
                {
                    SpecificationId = "spec-1",
                    ProviderId = "1111",
                    FundingStreamResult = new PublishedFundingStreamResult
                    {
                        FundingStream = new PublishedFundingStreamDefinition
                        {
                            Id = "fs-2",
                            Name = "funding stream 2"
                        },
                        AllocationLineResult = new PublishedAllocationLineResult
                        {
                            AllocationLine = new PublishedAllocationLineDefinition
                            {
                                Id = "AAAAA",
                                Name = "test allocation line 1",
                                ShortName = "tal1",
                                FundingRoute = PublishedFundingRoute.LA,
                                IsContractRequired = true
                            },
                            Current = new PublishedAllocationLineResultVersion
                            {
                                Status = AllocationLineStatus.Held,
                                Value = 100,
                                Version = 1,
                                Date = DateTimeOffset.Now,
                                PublishedProviderResultId = "res3",
                                ProviderId = "1111",
                                Provider = new ProviderSummary
                                {
                                    URN = "12345",
                                    UKPRN = "1111",
                                    UPIN = "2222",
                                    EstablishmentNumber = "es123",
                                    Authority = "London",
                                    ProviderType = "test type",
                                    ProviderSubType = "test sub type",
                                    DateOpened = DateTimeOffset.Now,
                                    ProviderProfileIdType = "UKPRN",
                                    LACode = "77777",
                                    Id = "1111",
                                    Name = "test provider name 1"
                                }
                            }
                        }
                    },
                    FundingPeriod = new Period
                    {
                        Id = "1819",
                        Name = "fp-1"
                    }
                }
            };
        }

        static IEnumerable<PublishedProviderResultByAllocationLineViewModel> CreatePublishedProviderResultByAllocationLineViewModel()
        {
            return new[]
            {
                new PublishedProviderResultByAllocationLineViewModel
                {
                    SpecificationId = "spec-1",
                    ProviderId = "1111",
                    FundingStreamId = "fs-1",
                    FundingStreamName = "funding stream 1",
                    AllocationLineId = "AAAAA",
                    AllocationLineName = "test allocation line 1",
                    Status = AllocationLineStatus.Held,
                    FundingAmount = 50,
                    VersionNumber = "0.1",
                    LastUpdated = DateTimeOffset.Now,
                    Ukprn = "1111",
                    Authority = "London",
                    ProviderType = "test type",
                    ProviderName = "test provider name 1",

                },
                new PublishedProviderResultByAllocationLineViewModel
                {
                    SpecificationId = "spec-1",
                    ProviderId = "1111",
                    FundingStreamId = "fs-1",
                    FundingStreamName = "funding stream 1",
                    AllocationLineId = "AAAAA",
                    AllocationLineName = "test allocation line 1",
                    Status = AllocationLineStatus.Held,
                    FundingAmount = 100,
                    VersionNumber = "0.1",
                    LastUpdated = DateTimeOffset.Now,
                    Ukprn = "1111",
                    Authority = "London",
                    ProviderType = "test type",
                    ProviderName = "test provider name 1",
                },
                new PublishedProviderResultByAllocationLineViewModel
                {
                    SpecificationId = "spec-1",
                    ProviderId = "1111",
                    FundingStreamId = "fs-2",
                    FundingStreamName = "funding stream 2",
                    AllocationLineId = "AAAAA",
                    AllocationLineName = "test allocation line 1",
                    Status = AllocationLineStatus.Held,
                    FundingAmount = 100,
                    VersionNumber = "0.1",
                    LastUpdated = DateTimeOffset.Now,
                    Ukprn = "1111",
                    Authority = "London",
                    ProviderType = "test type",
                    ProviderName = "test provider name 1",
                }
            };
        }

        static IEnumerable<PublishedProviderResult> CreatePublishedProviderResultsWithDifferentProviders()
        {
            return new[]
            {
                new PublishedProviderResult
                {
                    SpecificationId = "spec-1",
                    ProviderId = "1111",
                    FundingStreamResult = new PublishedFundingStreamResult
                    {
                        FundingStream = new PublishedFundingStreamDefinition
                        {
                            Id = "fs-1",
                            Name = "funding stream 1",
                            ShortName = "fs1",
                            PeriodType = new PublishedPeriodType
                            {
                                Id = "pt1",
                                Name = "period-type 1",
                                StartDay = 1,
                                EndDay = 31,
                                StartMonth = 8,
                                EndMonth = 7
                            }
                        },
                        AllocationLineResult = new PublishedAllocationLineResult
                        {
                            AllocationLine = new PublishedAllocationLineDefinition
                            {
                                Id = "AAAAA",
                                Name = "test allocation line 1",
                                ShortName = "tal1",
                                FundingRoute = PublishedFundingRoute.LA,
                                IsContractRequired = true
                            },
                            Current = new PublishedAllocationLineResultVersion
                            {
                                Status = AllocationLineStatus.Held,
                                Value = 50,
                                Version = 1,
                                Date = DateTimeOffset.Now,
                                PublishedProviderResultId = "c3BlYy0xMTExMUFBQUFB",
                                Major = 0,
                                Minor = 1,
                                ProviderId = "1111",
                                Provider = new ProviderSummary
                                {
                                    URN = "12345",
                                    UKPRN = "1111",
                                    UPIN = "2222",
                                    EstablishmentNumber = "es123",
                                    Authority = "London",
                                    ProviderType = "test type",
                                    ProviderSubType = "test sub type",
                                    DateOpened = DateTimeOffset.Now,
                                    ProviderProfileIdType = "UKPRN",
                                    LACode = "77777",
                                    Id = "1111",
                                    Name = "test provider name 1"
                                }
                            }
                        }
                    },
                    FundingPeriod = new Period
                    {
                        Id = "1819",
                        Name = "fp-1",
                        StartDate = DateTimeOffset.Now,
                        EndDate = DateTimeOffset.Now.AddYears(1)
                    }
                },
                new PublishedProviderResult
                {
                    SpecificationId = "spec-1",
                    ProviderId = "1111-1",
                    FundingStreamResult = new PublishedFundingStreamResult
                    {
                        FundingStream = new PublishedFundingStreamDefinition
                        {
                            Id = "fs-1",
                            Name = "funding stream 1",
                            ShortName = "fs1",
                            PeriodType = new PublishedPeriodType
                            {
                                Id = "pt1",
                                Name = "period-type 1",
                                StartDay = 1,
                                EndDay = 31,
                                StartMonth = 8,
                                EndMonth = 7
                            }
                        },
                        AllocationLineResult = new PublishedAllocationLineResult
                        {
                            AllocationLine = new PublishedAllocationLineDefinition
                            {
                                Id = "AAAAA",
                                Name = "test allocation line 1",
                                ShortName = "tal1",
                                FundingRoute = PublishedFundingRoute.LA,
                                IsContractRequired = true
                            },
                            Current = new PublishedAllocationLineResultVersion
                            {
                                ProviderId ="1111-1",
                                Status = AllocationLineStatus.Held,
                                Value = 100,
                                Version = 1,
                                Date = DateTimeOffset.Now,
                                PublishedProviderResultId = "c3BlYy0xMTExMS0xQUFBQUE=",
                                Major = 0,
                                Minor = 1,
                                Provider = new ProviderSummary
                                {
                                    URN = "12345",
                                    UKPRN = "1111-1",
                                    UPIN = "2222",
                                    EstablishmentNumber = "es123",
                                    Authority = "London",
                                    ProviderType = "test type",
                                    ProviderSubType = "test sub type",
                                    DateOpened = DateTimeOffset.Now,
                                    ProviderProfileIdType = "UKPRN",
                                    LACode = "77777",
                                    Id = "1111-1",
                                    Name = "test provider name 2"
                                }
                            }
                        }
                    },
                    FundingPeriod = new Period
                    {
                        Id = "1819",
                        Name = "fp-1",
                        StartDate = DateTimeOffset.Now,
                        EndDate = DateTimeOffset.Now.AddYears(1)
                    }
                },
                new PublishedProviderResult
                {
                    SpecificationId = "spec-1",
                    ProviderId = "1111-2",
                    FundingStreamResult = new PublishedFundingStreamResult
                    {
                        FundingStream = new PublishedFundingStreamDefinition
                        {
                            Id = "fs-1",
                            Name = "funding stream 1",
                            ShortName = "fs1",
                            PeriodType = new PublishedPeriodType
                            {
                                Id = "pt1",
                                Name = "period-type 1",
                                StartDay = 1,
                                EndDay = 31,
                                StartMonth = 8,
                                EndMonth = 7
                            }
                        },
                        AllocationLineResult = new PublishedAllocationLineResult
                        {
                             AllocationLine = new PublishedAllocationLineDefinition
                            {
                                Id = "AAAAA",
                                Name = "test allocation line 1",
                                ShortName = "tal1",
                                FundingRoute = PublishedFundingRoute.LA,
                                IsContractRequired = true
                            },
                            Current = new PublishedAllocationLineResultVersion
                            {
                                ProviderId = "1111-2",
                                Status = AllocationLineStatus.Held,
                                Value = 100,
                                Version = 1,
                                Date = DateTimeOffset.Now,
                                PublishedProviderResultId = "c3BlYy0xMTExMS0yQUFBQUE=",
                                Major = 0,
                                Minor = 1,
                                Provider = new ProviderSummary
                                {
                                    URN = "12345",
                                    UKPRN = "1111-2",
                                    UPIN = "2222",
                                    EstablishmentNumber = "es123",
                                    Authority = "London",
                                    ProviderType = "test type",
                                    ProviderSubType = "test sub type",
                                    DateOpened = DateTimeOffset.Now,
                                    ProviderProfileIdType = "UKPRN",
                                    LACode = "77777",
                                    Id = "1111-2",
                                    Name = "test provider name 3"
                                }
                            }
                        }
                    },
                    FundingPeriod = new Period
                    {
                        Id = "1819",
                        Name = "fp-1",
                        StartDate = DateTimeOffset.Now,
                        EndDate = DateTimeOffset.Now.AddYears(1)
                    }
                }
            };
        }
    }
}
