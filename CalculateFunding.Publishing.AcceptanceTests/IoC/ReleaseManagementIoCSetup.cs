using AutoMapper;
using BoDi;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.FundingDataZone;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Profiling;
using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Storage;
using CalculateFunding.Generators.OrganisationGroup;
using CalculateFunding.Generators.OrganisationGroup.Interfaces;
using CalculateFunding.Generators.Schema10;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Publishing.AcceptanceTests.Contexts;
using CalculateFunding.Publishing.AcceptanceTests.Extensions;
using CalculateFunding.Publishing.AcceptanceTests.Repositories;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Core.Interfaces.Threading;
using CalculateFunding.Services.Core.Services;
using CalculateFunding.Services.Core.Threading;
using CalculateFunding.Services.Publishing;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Providers;
using CalculateFunding.Services.Publishing.Reporting;
using CalculateFunding.Services.Publishing.Reporting.PublishedProviderEstate;
using CalculateFunding.Services.Publishing.Specifications;
using CalculateFunding.Services.Publishing.Validators;
using CalculateFunding.Services.Publishing.Variations;
using CalculateFunding.Tests.Common;
using FluentAssertions.Common;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;
using Polly;
using Serilog;
using System;
using System.Linq;
using TechTalk.SpecFlow;
using PublishedFundingContentsGenerator10 = CalculateFunding.Generators.Schema10.PublishedFundingContentsGenerator;
using PublishedFundingContentsGenerator11 = CalculateFunding.Generators.Schema11.PublishedFundingContentsGenerator;
using PublishedFundingContentsGenerator12 = CalculateFunding.Generators.Schema12.PublishedFundingContentsGenerator;
using PublishedProviderContentsGenerator10 = CalculateFunding.Generators.Schema10.PublishedProviderContentsGenerator;
using PublishedProviderContentsGenerator11 = CalculateFunding.Generators.Schema11.PublishedProviderContentsGenerator;
using PublishedProviderContentsGenerator12 = CalculateFunding.Generators.Schema12.PublishedProviderContentsGenerator;
using PublishingResiliencePolicies = CalculateFunding.Services.Publishing.ResiliencePolicies;

namespace CalculateFunding.Publishing.AcceptanceTests.IoC
{
    [Binding]
    public class ReleaseManagementIoCSetup : SetupBase
    {

        public ReleaseManagementIoCSetup(IObjectContainer objectContainer) : base(objectContainer)
        {
        }

        [BeforeScenario(new[] { "releasemanagement" })]
        public void SetupReleaseManagement()
        {
            RegisterDependentMicroserviceAccessors();

            RegisterInfastructureServices();

            RegisterRepositories();

            RegisterReleaseManagementServices();

            RegisterPublishingServices();

            RegisterStepContexts();

            RegisterBlobStorageClientsAndStepContexts();

            RegisterValidators();

            RegisterPrereqChecker();
        }

        private void RegisterBlobStorageClientsAndStepContexts()
        {
            RegisterFactoryAs<IReleaseManagementBlobStepContext>((container) =>
            {
                return new ReleaseManagementBlobStepContext
                {
                    PublishedProvidersClient = new InMemoryAzureBlobClient(),
                    FundingGroupsClient = new InMemoryBlobClient(),
                    PublishedFundingClient = new InMemoryBlobClient(),
                    ReleasedProvidersClient = new InMemoryBlobClient()
                };
            });
        }

        private void RegisterPrereqChecker()
        {
            RegisterTypeAs<CalculationPrerequisiteCheckerService, ICalculationPrerequisiteCheckerService>();
            RegisterTypeAs<SpecificationFundingStatusService, ISpecificationFundingStatusService>();
            RegisterTypeAs<CalculationPrerequisiteCheckerService, ICalculationPrerequisiteCheckerService>();
            RegisterTypeAs<RefreshPrerequisiteChecker, IPrerequisiteChecker>();
            RegisterTypeAs<PublishAllPrerequisiteChecker, IPrerequisiteChecker>();
            RegisterTypeAs<PublishProviderToChannelsPrerequisiteChecker, IPrerequisiteChecker>();
            RegisterTypeAs<PublishBatchPrerequisiteChecker, IPrerequisiteChecker>();
            RegisterTypeAs<ApproveAllProvidersPrerequisiteChecker, IPrerequisiteChecker>();
            RegisterTypeAs<ApproveBatchProvidersPrerequisiteChecker, IPrerequisiteChecker>();
            RegisterTypeAs<UndoPublishingPrerequisiteChecker, IPrerequisiteChecker>();

            IPrerequisiteChecker[] prerequisiteCheckers = typeof(IPrerequisiteChecker).Assembly.GetTypes()
                            .Where(_ => _.Implements(typeof(IPrerequisiteChecker)))
                            .Select(_ => (IPrerequisiteChecker)_objectContainer.Resolve(_))
                            .ToArray();
            RegisterInstanceAs<IPrerequisiteCheckerLocator>(new PrerequisiteCheckerLocator(prerequisiteCheckers));
        }

        private void RegisterPublishingServices()
        {
            PublishedFundingIdGeneratorResolver idGeneratorResolver = new PublishedFundingIdGeneratorResolver();
            PublishedFundingIdGenerator publishedFundingIdGenerator = new PublishedFundingIdGenerator();
            idGeneratorResolver.Register("1.0", publishedFundingIdGenerator);
            idGeneratorResolver.Register("1.1", publishedFundingIdGenerator);
            idGeneratorResolver.Register("1.2", publishedFundingIdGenerator);
            RegisterTypeAs<PublishedFundingGenerator, IPublishedFundingGenerator>();

            RegisterInstanceAs<IPublishedFundingIdGeneratorResolver>(idGeneratorResolver);

            RegisterTypeAs<PublishingEngineOptions, IPublishingEngineOptions>();

            RegisterTypeAs<ProfilingService, IProfilingService>();

            RegisterTypeAs<JobsRunning, IJobsRunning>();
            RegisterTypeAs<PostReleaseJobCreationService, IPostReleaseJobCreationService>();

            RegisterTypeAs<InMemoryFundingStreamPaymentDatesRepository, IFundingStreamPaymentDatesRepository>();

            RegisterTypeAs<InMemoryCalculationsService, ICalculationsService>();
            RegisterTypeAs<PublishedFundingChangeDetectorService, IPublishedFundingChangeDetectorService>();

            PublishedProviderContentsGeneratorResolver providerContentsGeneratorResolver = new PublishedProviderContentsGeneratorResolver();
            providerContentsGeneratorResolver.Register("1.0", new PublishedProviderContentsGenerator10());
            providerContentsGeneratorResolver.Register("1.1", new PublishedProviderContentsGenerator11());
            providerContentsGeneratorResolver.Register("1.2", new PublishedProviderContentsGenerator12());
            RegisterInstanceAs<IPublishedProviderContentsGeneratorResolver>(providerContentsGeneratorResolver);

            PublishedFundingContentsGeneratorResolver publishedFundingContentsGeneratorResolver = new PublishedFundingContentsGeneratorResolver();
            IPublishedFundingContentsGenerator v10Generator = new PublishedFundingContentsGenerator10();
            IPublishedFundingContentsGenerator v11Generator = new PublishedFundingContentsGenerator11();
            IPublishedFundingContentsGenerator v12Generator = new PublishedFundingContentsGenerator12();
            publishedFundingContentsGeneratorResolver.Register("1.0", v10Generator);
            publishedFundingContentsGeneratorResolver.Register("1.1", v11Generator);
            publishedFundingContentsGeneratorResolver.Register("1.2", v12Generator);

            RegisterInstanceAs<IPublishedFundingContentsGeneratorResolver>(publishedFundingContentsGeneratorResolver);

            RegisterTypeAs<PublishedProviderContentPersistenceService, IPublishedProviderContentPersistenceService>();

            RegisterFactoryAs<IPublishedFundingContentsPersistenceService>((ctx =>
            {
                IPublishedFundingContentsGeneratorResolver contentsGeneratorResolver = ctx.Resolve<IPublishedFundingContentsGeneratorResolver>();
                IPublishingResiliencePolicies publishingResiliencePolicies = ctx.Resolve<IPublishingResiliencePolicies>();
                IPublishingEngineOptions publishingEngineOptions = ctx.Resolve<IPublishingEngineOptions>();

                IReleaseManagementBlobStepContext releaseManagementBlobStepContext = ctx.Resolve<IReleaseManagementBlobStepContext>();
                ReleaseManagementBlobStepContext stepContext = (ReleaseManagementBlobStepContext)releaseManagementBlobStepContext;

                return new PublishedFundingContentsPersistenceService(contentsGeneratorResolver,
                    stepContext.PublishedFundingClient,
                    publishingResiliencePolicies,
                    publishingEngineOptions);
            }));

            RegisterFactoryAs<IPublishedProviderVersionService>((svc) =>
            {
                ILogger logger = svc.Resolve<ILogger>();
                IReleaseManagementBlobStepContext releaseManagementBlobStepContext = svc.Resolve<IReleaseManagementBlobStepContext>();
                ReleaseManagementBlobStepContext stepContext = (ReleaseManagementBlobStepContext)releaseManagementBlobStepContext;

                IPublishingResiliencePolicies publishingResiliencePolicies = svc.Resolve<IPublishingResiliencePolicies>();
                IJobManagement jobManagement = svc.Resolve<IJobManagement>();

                return new PublishedProviderVersionService(logger,
                    stepContext.PublishedProvidersClient,
                    publishingResiliencePolicies,
                    jobManagement
                    );
            });


            RegisterTypeAs<PublishedProviderVersioningService, IPublishedProviderVersioningService>();
            RegisterTypeAs<PublishedProviderIndexerService, IPublishedProviderIndexerService>();
            RegisterTypeAs<PublishedProviderStatusUpdateService, IPublishedProviderStatusUpdateService>();
            RegisterTypeAs<PublishedProviderStatusUpdateSettings, IPublishedProviderStatusUpdateSettings>();
            RegisterTypeAs<ProviderService, IProviderService>();
            RegisterTypeAs<PublishedFundingDataService, IPublishedFundingDataService>();

            RegisterTypeAs<TransactionFactory, ITransactionFactory>();

            RegisterInstanceAs<ITransactionResiliencePolicies>(new TransactionResiliencePolicies
            {
                TransactionPolicy = Policy.NoOpAsync()
            });

            RegisterTypeAs<PublishedFundingService, IPublishedFundingService>();

            RegisterTypeAs<OrganisationGroupGenerator, IOrganisationGroupGenerator>();
            RegisterTypeAs<OrganisationGroupTargetProviderLookup, IOrganisationGroupTargetProviderLookup>();
            RegisterInstanceAs<IOrganisationGroupResiliencePolicies>(new OrganisationGroupResiliencePolicies
            {
                ProvidersApiClient = Policy.NoOpAsync()
            });

            RegisterTypeAs<ProviderFilter, IProviderFilter>();
            RegisterTypeAs<PublishedFundingDateService, IPublishedFundingDateService>();

            RegisterTypeAs<PublishIntegrityCheckJobCreation, ICreatePublishIntegrityJob>();
            RegisterTypeAs<PublishingDatasetsDataCopyJobCreation, ICreatePublishDatasetsDataCopyJob>();
            RegisterTypeAs<PublishedFundingCsvJobsService, IPublishedFundingCsvJobsService>();

            RegisterTypeAs<GeneratePublishedFundingCsvJobCreation, ICreateGeneratePublishedFundingCsvJobs>();
            RegisterTypeAs<CreateGeneratePublishedProviderEstateCsvJobs, ICreateGeneratePublishedProviderEstateCsvJobs>();
            RegisterTypeAs<ProcessDatasetObsoleteItemsJobCreation, ICreateProcessDatasetObsoleteItemsJob>();

            IGeneratePublishedFundingCsvJobsCreation[] generatePublishedFundingCsvJobsCreations =
                typeof(IGeneratePublishedFundingCsvJobsCreation).Assembly.GetTypes()
                .Where(_ => _.Implements(typeof(IGeneratePublishedFundingCsvJobsCreation)) &&
                            !_.IsAbstract)
                .Select(_ => (IGeneratePublishedFundingCsvJobsCreation)Activator.CreateInstance(_,
                    ResolveInstance<ICreateGeneratePublishedFundingCsvJobs>(),
                    ResolveInstance<ICreateGeneratePublishedProviderEstateCsvJobs>()))
                .ToArray();
            RegisterInstanceAs<IGeneratePublishedFundingCsvJobsCreationLocator>(new GeneratePublishedFundingCsvJobsCreationLocator(generatePublishedFundingCsvJobsCreations));


            IVariationStrategy[] variationStrategies = typeof(IVariationStrategy).Assembly.GetTypes()
                .Where(_ => !_.IsAbstract && _.Implements(typeof(IVariationStrategy)))
                .Select(_ => (IVariationStrategy)_objectContainer.Resolve(_))
                .ToArray();

            RegisterInstanceAs<IVariationStrategyServiceLocator>(new VariationStrategyServiceLocator(variationStrategies));
        }

        private void RegisterInfastructureServices()
        {
            // RegisterInstanceAs<ILogger>(new LoggerConfiguration().CreateLogger());
            RegisterTypeAs<SpecFlowSerilogLogger, ILogger>();

            IMapper mapper = new MapperConfiguration(c =>
            {
                c.AddProfile<PublishingServiceMappingProfile>();
            }).CreateMapper();

            RegisterInstanceAs<IMapper>(mapper);

            RegisterInstanceAs<IConfiguration>(new ConfigurationBuilder().Build());
            RegisterTypeAs<InMemoryFeatureManagerSnapshot, IFeatureManagerSnapshot>();

            StaticDateTimeService staticDateTimeService = new StaticDateTimeService();

            RegisterInstanceAs<StaticDateTimeService>(staticDateTimeService);
            RegisterInstanceAs<ICurrentDateTime>(staticDateTimeService);
        }

        private void RegisterValidators()
        {
            RegisterTypeAs<ChannelModelValidator, IValidator<ChannelRequest>>();
        }

        private void RegisterStepContexts()
        {
            RegisterTypeAs<ReleaseProvidersToChannelsContext, IReleaseProvidersToChannelsContext>();
            RegisterTypeAs<CurrentSpecificationStepContext, ICurrentSpecificationStepContext>();
            RegisterTypeAs<PublishServiceAcceptanceStepContext, IPublishFundingStepContext>();
            RegisterTypeAs<PublishedFundingRepositoryStepContext, IPublishedFundingRepositoryStepContext>();
            RegisterTypeAs<ProvidersStepContext, IProvidersStepContext>();
            RegisterTypeAs<JobStepContext, IJobStepContext>();
            RegisterTypeAs<CurrentJobStepContext, ICurrentJobStepContext>();
            RegisterTypeAs<ReleaseManagementIdentifierGeneratorStepContext, IReleaseManagementIdentifierGeneratorStepContext>();
        }

        private void RegisterRepositories()
        {
            PublishingResiliencePolicies publishingResiliencePolicies = new PublishingResiliencePolicies()
            {
                BlobClient = Policy.NoOpAsync(),
                CalculationsApiClient = Policy.NoOpAsync(),
                FundingFeedSearchRepository = Policy.NoOpAsync(),
                JobsApiClient = Policy.NoOpAsync(),
                PoliciesApiClient = Policy.NoOpAsync(),
                ProfilingApiClient = Policy.NoOpAsync(),
                ProvidersApiClient = Policy.NoOpAsync(),
                PublishedFundingBlobRepository = Policy.NoOpAsync(),
                PublishedFundingRepository = Policy.NoOpAsync(),
                PublishedProviderVersionRepository = Policy.NoOpAsync(),
                CalculationResultsRepository = Policy.NoOpAsync(),
                SpecificationsRepositoryPolicy = Policy.NoOpAsync(),
                SpecificationsApiClient = Policy.NoOpAsync(),
                PublishedProviderSearchRepository = Policy.NoOpAsync(),
                PublishedIndexSearchResiliencePolicy = Policy.NoOpAsync(),
                CacheProvider = Policy.NoOpAsync(),
                FundingStreamPaymentDatesRepository = Policy.NoOpAsync(),
            };

            RegisterInstanceAs<IPublishingResiliencePolicies>(publishingResiliencePolicies);
            RegisterTypeAs<InMemoryReleaseManagementRepository, IReleaseManagementRepository>();
            RegisterTypeAs<CalculationInMemoryRepository, ICalculationResultsRepository>();

            RegisterTypeAs<InMemoryPublishedFundingRepository, IPublishedFundingRepository>();
            RegisterTypeAs<InMemoryPublishedFundingBulkRepository, IPublishedFundingBulkRepository>();

            RegisterTypeAs<PublishedProviderVersionInMemoryRepository, IVersionRepository<PublishedProviderVersion>>();
            RegisterTypeAs<PublishedProviderVersionBulkInMemoryRepository, IVersionBulkRepository<PublishedProviderVersion>>();
            RegisterTypeAs<PublishedFundingVersionInMemoryRepository, IVersionRepository<PublishedFundingVersion>>();
            RegisterTypeAs<PublishedFundingVersionBulkInMemoryRepository, IVersionBulkRepository<PublishedFundingVersion>>();

            RegisterTypeAs<InMemoryCosmosRepository, ICosmosRepository>();

            RegisterTypeAs<InMemoryCacheProvider, ICacheProvider>();
            RegisterTypeAs<InMemoryMessengerService, IMessengerService>();

            // TODO: possibly register different blob clients for asserts
            RegisterTypeAs<InMemoryBlobClient, IBlobClient>();
            RegisterTypeAs<InMemoryAzureBlobClient, Services.Core.Interfaces.AzureStorage.IBlobClient>();


            RegisterTypeAs<PublishedProviderInMemorySearchRepository, ISearchRepository<PublishedProviderIndex>>();
            RegisterTypeAs<PublishedFundingInMemorySearchRepository, ISearchRepository<PublishedFundingIndex>>();


        }

        private void RegisterDependentMicroserviceAccessors()
        {
            RegisterTypeAs<SpecificationInMemoryRepository, ISpecificationService>();

            RegisterTypeAs<PoliciesService, IPoliciesService>();
            RegisterTypeAs<PoliciesInMemoryRepository, IPoliciesApiClient>();
            RegisterTypeAs<PoliciesStepContext, IPoliciesStepContext>();

            RegisterTypeAs<SpecificationsInMemoryClient, ISpecificationsApiClient>();
            RegisterTypeAs<CalculationsInMemoryClient, ICalculationsApiClient>();

            JobManagementResiliencePolicies jobManagementResiliencePolicies = new JobManagementResiliencePolicies()
            {
                JobsApiClient = Policy.NoOpAsync(),
            };

            RegisterInstanceAs<IJobManagementResiliencePolicies>(jobManagementResiliencePolicies);
            RegisterTypeAs<JobTracker, IJobTracker>();
            RegisterTypeAs<JobManagement, IJobManagement>();
            RegisterTypeAs<JobsInMemoryRepository, IJobsApiClient>();

            RegisterTypeAs<ProfilingInMemoryClient, IProfilingApiClient>();
            RegisterTypeAs<FDZInMemoryClient, IFundingDataZoneApiClient>();
            RegisterTypeAs<ProvidersInMemoryClient, IProvidersApiClient>();
        }

        private void RegisterReleaseManagementServices()
        {
            RegisterTypeAs<ChannelsService, IChannelsService>();
            RegisterTypeAs<PublishedProvidersLoadContext, IPublishedProvidersLoadContext>();
            RegisterTypeAs<ReleaseApprovedProvidersService, IReleaseApprovedProvidersService>();
            RegisterTypeAs<PublishService, IPublishService>();
            RegisterTypeAs<PublishedFundingStatusUpdateService, IPublishedFundingStatusUpdateService>();

            RegisterTypeAs<ReleaseToChannelSqlMappingContext, IReleaseToChannelSqlMappingContext>();
            RegisterTypeAs<ReleaseManagementSpecificationService, IReleaseManagementSpecificationService>();
            RegisterTypeAs<ChannelReleaseService, IChannelReleaseService>();
            RegisterTypeAs<ProvidersForChannelFilterService, IProvidersForChannelFilterService>();
            RegisterTypeAs<PublishSpecificationValidator, ISpecificationIdServiceRequestValidator>();
            RegisterTypeAs<ProducerConsumerFactory, IProducerConsumerFactory>();
            RegisterTypeAs<PublishedProviderLookupService, IPublishedProviderLookupService>();
            RegisterTypeAs<ChannelOrganisationGroupGeneratorService, IChannelOrganisationGroupGeneratorService>();
            RegisterTypeAs<ChannelOrganisationGroupChangeDetector, IChannelOrganisationGroupChangeDetector>();
            RegisterTypeAs<ExistingReleasedProvidersForChannelFilterService, IExistingReleasedProvidersForChannelFilterService>();

            RegisterFactoryAs<IReleaseProviderPersistenceService>((svc) =>
            {
                IReleaseManagementRepository releaseManagementRepository = svc.Resolve<IReleaseManagementRepository>();
                IReleaseToChannelSqlMappingContext context = svc.Resolve<IReleaseToChannelSqlMappingContext>();
                IReleaseManagementIdentifierGeneratorStepContext releaseManagementIdentifierGeneratorStepContext = svc.Resolve<IReleaseManagementIdentifierGeneratorStepContext>();

                return new ReleaseProviderPersistenceService(context, releaseManagementRepository, releaseManagementIdentifierGeneratorStepContext.ReleasedProvider);
            });

            RegisterFactoryAs<IProviderVersionReleaseService>((svc) =>
            {
                IReleaseManagementRepository releaseManagementRepository = svc.Resolve<IReleaseManagementRepository>();
                IReleaseToChannelSqlMappingContext context = svc.Resolve<IReleaseToChannelSqlMappingContext>();
                IReleaseManagementIdentifierGeneratorStepContext releaseManagementIdentifierGeneratorStepContext = svc.Resolve<IReleaseManagementIdentifierGeneratorStepContext>();
                ILogger logger = svc.Resolve<ILogger>();

                return new ProviderVersionReleaseService(context, releaseManagementRepository, releaseManagementIdentifierGeneratorStepContext.ReleasedProviderVersion, logger);
            });

            RegisterFactoryAs<IProviderVersionToChannelReleaseService>((svc) =>
            {
                IReleaseManagementRepository releaseManagementRepository = svc.Resolve<IReleaseManagementRepository>();
                IReleaseToChannelSqlMappingContext context = svc.Resolve<IReleaseToChannelSqlMappingContext>();
                IReleaseManagementIdentifierGeneratorStepContext releaseManagementIdentifierGeneratorStepContext = svc.Resolve<IReleaseManagementIdentifierGeneratorStepContext>();
                ILogger logger = svc.Resolve<ILogger>();

                return new ProviderVersionToChannelReleaseService(releaseManagementRepository, context, releaseManagementIdentifierGeneratorStepContext.ReleasedProviderVersionChannel, logger);
            });

            RegisterTypeAs<GenerateVariationReasonsForChannelService, IGenerateVariationReasonsForChannelService>();
            RegisterTypeAs<ProviderVariationsDetection, IDetectProviderVariations>();
            RegisterTypeAs<PublishedProviderContentChannelPersistenceService, IPublishedProviderContentChannelPersistenceService>();

            RegisterFactoryAs<IFundingGroupService>((svc) =>
            {
                IReleaseManagementRepository releaseManagementRepository = svc.Resolve<IReleaseManagementRepository>();
                IReleaseToChannelSqlMappingContext context = svc.Resolve<IReleaseToChannelSqlMappingContext>();
                IReleaseManagementIdentifierGeneratorStepContext releaseManagementIdentifierGeneratorStepContext = svc.Resolve<IReleaseManagementIdentifierGeneratorStepContext>();
                ILogger logger = svc.Resolve<ILogger>();

                return new FundingGroupService(releaseManagementRepository,
                   context,
                   releaseManagementIdentifierGeneratorStepContext.FundingGroup,
                   logger);
            });

            RegisterTypeAs<FundingGroupDataGenerator, IFundingGroupDataGenerator>();
            RegisterTypeAs<PublishedProviderLoaderForFundingGroupData, IPublishedProviderLoaderForFundingGroupData>();

            RegisterFactoryAs<IFundingGroupDataPersistenceService>((svc) =>
            {
                IReleaseManagementRepository releaseManagementRepository = svc.Resolve<IReleaseManagementRepository>();
                IReleaseToChannelSqlMappingContext context = svc.Resolve<IReleaseToChannelSqlMappingContext>();
                IReleaseManagementIdentifierGeneratorStepContext releaseManagementIdentifierGeneratorStepContext = svc.Resolve<IReleaseManagementIdentifierGeneratorStepContext>();

                return new FundingGroupDataPersistenceService(releaseManagementRepository,
                   context,
                   releaseManagementIdentifierGeneratorStepContext.FundingGroupVersion,
                   releaseManagementIdentifierGeneratorStepContext.FundingGroupVersionVariationReasons);
            });

            RegisterFactoryAs<IFundingGroupProviderPersistenceService>((svc) =>
            {
                IReleaseManagementRepository releaseManagementRepository = svc.Resolve<IReleaseManagementRepository>();
                IReleaseToChannelSqlMappingContext context = svc.Resolve<IReleaseToChannelSqlMappingContext>();
                IReleaseManagementIdentifierGeneratorStepContext releaseManagementIdentifierGeneratorStepContext = svc.Resolve<IReleaseManagementIdentifierGeneratorStepContext>();
                ILogger logger= svc.Resolve<ILogger>(); 

                return new FundingGroupProviderPersistenceService(releaseManagementRepository, context, releaseManagementIdentifierGeneratorStepContext.FundingGroupProvider, logger);
            });

            RegisterFactoryAs<IProviderVariationReasonsReleaseService>((svc) =>
            {
                IReleaseManagementRepository releaseManagementRepository = svc.Resolve<IReleaseManagementRepository>();
                IReleaseToChannelSqlMappingContext context = svc.Resolve<IReleaseToChannelSqlMappingContext>();
                IReleaseManagementIdentifierGeneratorStepContext releaseManagementIdentifierGeneratorStepContext = svc.Resolve<IReleaseManagementIdentifierGeneratorStepContext>();
                ILogger logger = svc.Resolve<ILogger>();

                return new ProviderVariationReasonsReleaseService(context, releaseManagementRepository, releaseManagementIdentifierGeneratorStepContext.ProviderVariationReasons, logger);
            });

            RegisterTypeAs<ExistingReleasedProvidersLoadService, IExistingReleasedProvidersLoadService>();
            RegisterTypeAs<ExistingReleasedProviderVersionsLoadService, IExistingReleasedProviderVersionsLoadService>();

            RegisterFactoryAs<IPublishedFundingContentsChannelPersistenceService>((svc) =>
            {
                IReleaseManagementBlobStepContext releaseManagementBlobStepContextInterface = svc.Resolve<IReleaseManagementBlobStepContext>();
                ReleaseManagementBlobStepContext releaseManagementBlobStepContext = releaseManagementBlobStepContextInterface as ReleaseManagementBlobStepContext;
                ILogger logger = svc.Resolve<ILogger>();
                IPublishedFundingContentsGeneratorResolver publishedFundingContentsGeneratorResolver = svc.Resolve<IPublishedFundingContentsGeneratorResolver>();
                IPublishingResiliencePolicies publishingResiliencePolicies = svc.Resolve<IPublishingResiliencePolicies>();
                IPublishingEngineOptions publishingEngineOptions = svc.Resolve<IPublishingEngineOptions>();
                IPoliciesService policiesService = svc.Resolve<IPoliciesService>();

                return new PublishedFundingContentsChannelPersistenceService(logger,
                                    publishedFundingContentsGeneratorResolver,
                                    releaseManagementBlobStepContext.FundingGroupsClient,
                                    publishingResiliencePolicies,
                                    publishingEngineOptions,
                                    policiesService);
            });

            RegisterFactoryAs<IPublishedProviderChannelVersionService>((svc) =>
            {
                ILogger logger = svc.Resolve<ILogger>();
                IReleaseManagementBlobStepContext releaseManagementBlobStepContextInterface = svc.Resolve<IReleaseManagementBlobStepContext>();
                ReleaseManagementBlobStepContext releaseManagementBlobStepContext = releaseManagementBlobStepContextInterface as ReleaseManagementBlobStepContext;
                IPublishingResiliencePolicies publishingResiliencePolicies = svc.Resolve<IPublishingResiliencePolicies>();

                return new PublishedProviderChannelVersionService(logger,
                                                                  releaseManagementBlobStepContext.ReleasedProvidersClient,
                                                                  publishingResiliencePolicies);
            });
        }
    }
}
