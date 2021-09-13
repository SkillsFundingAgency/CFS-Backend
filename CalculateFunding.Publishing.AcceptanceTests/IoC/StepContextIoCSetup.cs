using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Reflection;
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
using CalculateFunding.Common.ServiceBus;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Storage;
using CalculateFunding.Generators.OrganisationGroup;
using CalculateFunding.Generators.OrganisationGroup.Interfaces;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Publishing.AcceptanceTests.Contexts;
using CalculateFunding.Publishing.AcceptanceTests.Repositories;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Core.Interfaces.Threading;
using CalculateFunding.Services.Core.Threading;
using CalculateFunding.Services.Publishing;
using CalculateFunding.Services.Publishing.Errors;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Profiling;
using CalculateFunding.Services.Publishing.Providers;
using CalculateFunding.Services.Publishing.Reporting;
using CalculateFunding.Services.Publishing.Reporting.PublishedProviderEstate;
using CalculateFunding.Services.Publishing.Specifications;
using CalculateFunding.Services.Publishing.Variations;
using CalculateFunding.Services.Publishing.Variations.Errors;
using CalculateFunding.Services.Publishing.Variations.Strategies;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions.Common;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Microsoft.FeatureManagement;
using Polly;
using Serilog;
using TechTalk.SpecFlow;
using PublishingResiliencePolicies = CalculateFunding.Services.Publishing.ResiliencePolicies;
using PublishedProviderContentsGenerator10 = CalculateFunding.Generators.Schema10.PublishedProviderContentsGenerator;
using PublishedProviderContentsGenerator11 = CalculateFunding.Generators.Schema11.PublishedProviderContentsGenerator;
using PublishedProviderContentsGenerator12 = CalculateFunding.Generators.Schema12.PublishedProviderContentsGenerator;
using PublishedFundingContentsGenerator10 = CalculateFunding.Generators.Schema10.PublishedFundingContentsGenerator;
using PublishedFundingContentsGenerator11 = CalculateFunding.Generators.Schema11.PublishedFundingContentsGenerator;
using PublishedFundingContentsGenerator12 = CalculateFunding.Generators.Schema12.PublishedFundingContentsGenerator;
using CalculateFunding.Generators.Schema10;

namespace CalculateFunding.Publishing.AcceptanceTests.IoC
{
    [Binding]
    public class StepContextIoCSetup
    {
        private readonly IObjectContainer _objectContainer;

        public StepContextIoCSetup(IObjectContainer objectContainer)
        {
            _objectContainer = objectContainer;
        }

        private void RegisterTypeAs<TType, TInterface>()
            where TType : class, TInterface
            where TInterface : class
        {
            _objectContainer.RegisterTypeAs<TType, TInterface>();
        }

        private void RegisterInstanceAs<TType>(TType instance)
            where TType : class
        {
            _objectContainer.RegisterInstanceAs(instance);
        }

        private TType ResolveInstance<TType>()
            where TType : class
        {
            return _objectContainer.Resolve<TType>();
        }

        [BeforeScenario]
        public void SetupStepContexts()
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

            RegisterInstanceAs<ILogger>(new LoggerConfiguration().CreateLogger());
            RegisterInstanceAs<IPublishingResiliencePolicies>(publishingResiliencePolicies);
            RegisterInstanceAs<IConfiguration>(new ConfigurationBuilder().Build());

            RegisterTypeAs<InMemoryBlobClient, IBlobClient>();
            RegisterTypeAs<InMemoryAzureBlobClient, Services.Core.Interfaces.AzureStorage.IBlobClient>();
            RegisterTypeAs<InMemoryCosmosRepository, ICosmosRepository>();
            RegisterTypeAs<SpecificationInMemoryRepository, ISpecificationService>();
            RegisterTypeAs<JobsInMemoryRepository, IJobsApiClient>();
            RegisterTypeAs<InMemoryFeatureManagerSnapshot, IFeatureManagerSnapshot>();
            RegisterTypeAs<PublishedProviderInMemorySearchRepository, ISearchRepository<PublishedProviderIndex>>();
            RegisterTypeAs<ReApplyCustomProfiles, IReApplyCustomProfiles>();
            RegisterTypeAs<ReProfilingResponseMapper, IReProfilingResponseMapper>();
            RegisterTypeAs<ReProfilingRequestBuilder, IReProfilingRequestBuilder>();
            RegisterTypeAs<InMemoryCalculationsService, ICalculationsService>();

            IMapper mapper = new MapperConfiguration(c =>
            {
                c.AddProfile<PublishingServiceMappingProfile>();
            }).CreateMapper();

            RegisterInstanceAs(mapper);

            JobManagementResiliencePolicies jobManagementResiliencePolicies = new JobManagementResiliencePolicies()
            {
                JobsApiClient = Policy.NoOpAsync(),
            };

            RegisterInstanceAs<IJobManagementResiliencePolicies>(jobManagementResiliencePolicies);
            RegisterTypeAs<JobTracker, IJobTracker>();
            RegisterTypeAs<JobManagement, IJobManagement>();
            RegisterTypeAs<InMemoryPublishedFundingRepository, IPublishedFundingRepository>();
            RegisterTypeAs<InMemoryPublishedFundingBulkRepository, IPublishedFundingBulkRepository>();
            RegisterTypeAs<PoliciesInMemoryRepository, IPoliciesApiClient>();
            RegisterTypeAs<InMemoryCacheProvider, ICacheProvider>();
            RegisterTypeAs<InMemoryMessengerService, IMessengerService>();
            RegisterTypeAs<InMemoryFundingStreamPaymentDatesRepository, IFundingStreamPaymentDatesRepository>();

            ProvidersInMemoryClient providersInMemoryClient = new ProvidersInMemoryClient(mapper);
            
            RegisterInstanceAs<IProvidersApiClient>(providersInMemoryClient);
            RegisterTypeAs<ProviderService, IProviderService>();
            RegisterTypeAs<PublishedFundingService, IPublishedFundingService>();
            RegisterTypeAs<PoliciesService, IPoliciesService>();
            RegisterTypeAs<VariationService, IVariationService>();
            RegisterTypeAs<PublishedFundingDateService, IPublishedFundingDateService>();
            RegisterTypeAs<PublishServiceAcceptanceStepContext, IPublishFundingStepContext>();
            RegisterTypeAs<CurrentSpecificationStepContext, ICurrentSpecificationStepContext>();
            RegisterTypeAs<JobStepContext, IJobStepContext>();
            RegisterTypeAs<CurrentJobStepContext, ICurrentJobStepContext>();
            RegisterTypeAs<PublishedFundingRepositoryStepContext, IPublishedFundingRepositoryStepContext>();
            RegisterTypeAs<PoliciesStepContext, IPoliciesStepContext>();
            RegisterTypeAs<LoggerStepContext, ILoggerStepContext>();
            RegisterTypeAs<ProvidersStepContext, IProvidersStepContext>();
            RegisterTypeAs<PublishingDatesStepContext, IPublishingDatesStepContext>();
            RegisterTypeAs<PublishedFundingResultStepContext, IPublishedFundingResultStepContext>();
            RegisterTypeAs<PublishedProviderStepContext, IPublishedProviderStepContext>();
            RegisterTypeAs<ProfilingService, IProfilingService>();
            RegisterTypeAs<ProfilingInMemoryClient, IProfilingApiClient>();
            RegisterTypeAs<FDZInMemoryClient, IFundingDataZoneApiClient>();
            RegisterTypeAs<PublishedFundingDataService, IPublishedFundingDataService>();
            RegisterTypeAs<PublishedFundingVersionDataService, IPublishedFundingVersionDataService>();
            RegisterTypeAs<CalculationResultsService, ICalculationResultsService>();
            RegisterTypeAs<PublishingEngineOptions, IPublishingEngineOptions>();
            RegisterTypeAs<PublishedProviderVersionInMemoryRepository, IVersionRepository<PublishedProviderVersion>>();
            RegisterTypeAs<PublishedProviderVersionBulkInMemoryRepository, IVersionBulkRepository<PublishedProviderVersion>>();
            RegisterTypeAs<PublishedFundingVersionInMemoryRepository, IVersionRepository<PublishedFundingVersion>>();
            RegisterTypeAs<PublishedFundingVersionBulkInMemoryRepository, IVersionBulkRepository<PublishedFundingVersion>>();
            RegisterTypeAs<PublishedProviderDataGenerator, IPublishedProviderDataGenerator>();
            RegisterTypeAs<PublishedFundingStatusUpdateService, IPublishedFundingStatusUpdateService>();
            RegisterTypeAs<SpecificationFundingStatusService, ISpecificationFundingStatusService>();

            RegisterTypeAs<OrganisationGroupGenerator, IOrganisationGroupGenerator>();
            RegisterTypeAs<PublishedFundingChangeDetectorService, IPublishedFundingChangeDetectorService>();
            RegisterTypeAs<PublishedProviderVersionService, IPublishedProviderVersionService>();
            RegisterTypeAs<PublishedFundingInMemorySearchRepository, ISearchRepository<PublishedFundingIndex>>();

            RegisterTypeAs<GeneratePublishedFundingCsvJobCreation, ICreateGeneratePublishedFundingCsvJobs>();
            RegisterTypeAs<CreateGeneratePublishedProviderEstateCsvJobs, ICreateGeneratePublishedProviderEstateCsvJobs>();
            RegisterTypeAs<PublishIntegrityCheckJobCreation, ICreatePublishIntegrityJob>();
            RegisterTypeAs<PublishingDatasetsDataCopyJobCreation, ICreatePublishDatasetsDataCopyJob>();
            RegisterTypeAs<CurrentCorrelationStepContext, ICurrentCorrelationStepContext>();
            RegisterTypeAs<ProcessDatasetObsoleteItemsJobCreation, ICreateProcessDatasetObsoleteItemsJob>();
            RegisterTypeAs<ProviderFilter, IProviderFilter>();

            IGeneratePublishedFundingCsvJobsCreation[] generatePublishedFundingCsvJobsCreations = 
                typeof(IGeneratePublishedFundingCsvJobsCreation).Assembly.GetTypes()
                .Where(_ => _.Implements(typeof(IGeneratePublishedFundingCsvJobsCreation)) &&
                            !_.IsAbstract)
                .Select(_ => (IGeneratePublishedFundingCsvJobsCreation)Activator.CreateInstance(_, 
                    ResolveInstance<ICreateGeneratePublishedFundingCsvJobs>(), 
                    ResolveInstance<ICreateGeneratePublishedProviderEstateCsvJobs>()))
                .ToArray();
            RegisterInstanceAs<IGeneratePublishedFundingCsvJobsCreationLocator>(new GeneratePublishedFundingCsvJobsCreationLocator(generatePublishedFundingCsvJobsCreations));


            RegisterInstanceAs<IOrganisationGroupResiliencePolicies>(new OrganisationGroupResiliencePolicies
            {
                ProvidersApiClient = Policy.NoOpAsync()
            });

            RegisterTypeAs<OrganisationGroupTargetProviderLookup, IOrganisationGroupTargetProviderLookup>();
            RegisterInstanceAs<IFundingLineRoundingSettings>(new RoundingSettingsStub());

            IDetectPublishedProviderErrors[] detectorStrategies = typeof(IDetectPublishedProviderErrors).Assembly.GetTypes()
                .Where(_ => _.Implements(typeof(PublishedProviderErrorDetector)) && !_.IsAbstract)
                .Select(_ => (IDetectPublishedProviderErrors)_objectContainer.Resolve(_))
                .ToArray();

            RegisterInstanceAs<IErrorDetectionStrategyLocator>(new ErrorDetectionStrategyLocator(detectorStrategies));
            RegisterTypeAs<PublishedProviderErrorDetection, IPublishedProviderErrorDetection>();

            PublishedProviderContentsGeneratorResolver providerContentsGeneratorResolver = new PublishedProviderContentsGeneratorResolver();
            providerContentsGeneratorResolver.Register("1.0", new PublishedProviderContentsGenerator10());
            providerContentsGeneratorResolver.Register("1.1", new PublishedProviderContentsGenerator11());
            providerContentsGeneratorResolver.Register("1.2", new PublishedProviderContentsGenerator12());
            RegisterInstanceAs<IPublishedProviderContentsGeneratorResolver>(providerContentsGeneratorResolver);

            RegisterTypeAs<FundingLineTotalAggregator, IFundingLineTotalAggregator>();
            RegisterTypeAs<CalculationInMemoryRepository, ICalculationResultsRepository>();
            RegisterTypeAs<PublishedProviderStatusUpdateService, IPublishedProviderStatusUpdateService>();
            RegisterTypeAs<PublishedProviderStatusUpdateSettings, IPublishedProviderStatusUpdateSettings>();

            IVariationStrategy[] variationStrategies = typeof(IVariationStrategy).Assembly.GetTypes()
                .Where(_ => _.Implements(typeof(IVariationStrategy)))
                .Select(_ => (IVariationStrategy)_objectContainer.Resolve(_))
                .ToArray();
            
            RegisterInstanceAs<IVariationStrategyServiceLocator>(new VariationStrategyServiceLocator(variationStrategies));

            RegisterTypeAs<ProviderVariationsDetection, IDetectProviderVariations>();
            RegisterTypeAs<SpecificationsInMemoryClient, ISpecificationsApiClient>();
            RegisterTypeAs<ProviderVariationsApplication, IApplyProviderVariations>();
            RegisterTypeAs<VariationServiceStepContext, IVariationServiceStepContext>();
            RegisterTypeAs<CalculationsInMemoryClient, ICalculationsApiClient>();
            RegisterTypeAs<CalculationPrerequisiteCheckerService, ICalculationPrerequisiteCheckerService>();
            RegisterTypeAs<JobsRunning, IJobsRunning>();
            RegisterTypeAs<SpecificationFundingStatusService, ISpecificationFundingStatusService>();
            RegisterTypeAs<CalculationPrerequisiteCheckerService, ICalculationPrerequisiteCheckerService>();
            RegisterTypeAs<RefreshPrerequisiteChecker, IPrerequisiteChecker>();
            RegisterTypeAs<PublishAllPrerequisiteChecker, IPrerequisiteChecker>();
            RegisterTypeAs<PublishProviderToChannelsPrerequisiteChecker, IPrerequisiteChecker>();
            RegisterTypeAs<PublishBatchPrerequisiteChecker, IPrerequisiteChecker>();
            RegisterTypeAs<ApproveAllProvidersPrerequisiteChecker, IPrerequisiteChecker>();
            RegisterTypeAs<ApproveBatchProvidersPrerequisiteChecker, IPrerequisiteChecker>();

            IPrerequisiteChecker[] prerequisiteCheckers = typeof(IPrerequisiteChecker).Assembly.GetTypes()
                .Where(_ => _.Implements(typeof(IPrerequisiteChecker)))
                .Select(_ => (IPrerequisiteChecker)_objectContainer.Resolve(_))
                .ToArray();

            RegisterInstanceAs<IPrerequisiteCheckerLocator>(new PrerequisiteCheckerLocator(prerequisiteCheckers));

            RegisterTypeAs<GenerateCsvJobsInMemoryClient, IGeneratePublishedFundingCsvJobsCreation>();

            RegisterTypeAs<RefreshStateService, IRefreshStateService>();

            PublishedFundingIdGeneratorResolver idGeneratorResolver = new PublishedFundingIdGeneratorResolver();
            PublishedFundingIdGenerator publishedFundingIdGenerator = new PublishedFundingIdGenerator();
            idGeneratorResolver.Register("1.0", publishedFundingIdGenerator);
            idGeneratorResolver.Register("1.1", publishedFundingIdGenerator);
            idGeneratorResolver.Register("1.2", publishedFundingIdGenerator);
            RegisterTypeAs<PublishedFundingGenerator, IPublishedFundingGenerator>();

            RegisterInstanceAs<IPublishedFundingIdGeneratorResolver>(idGeneratorResolver);

            PublishedProviderContentsGeneratorResolver publishedProviderContentsGeneratorResolver = new PublishedProviderContentsGeneratorResolver();
            IPublishedProviderContentsGenerator v10ProviderGenerator = new PublishedProviderContentsGenerator10();
            IPublishedProviderContentsGenerator v11ProviderGenerator = new PublishedProviderContentsGenerator11();
            IPublishedProviderContentsGenerator v12ProviderGenerator = new PublishedProviderContentsGenerator12();
            publishedProviderContentsGeneratorResolver.Register("1.0", v10ProviderGenerator);
            publishedProviderContentsGeneratorResolver.Register("1.1", v11ProviderGenerator);
            publishedProviderContentsGeneratorResolver.Register("1.2", v12ProviderGenerator);

            RegisterInstanceAs<IPublishedProviderContentsGeneratorResolver>(publishedProviderContentsGeneratorResolver);

            PublishedFundingContentsGeneratorResolver publishedFundingContentsGeneratorResolver = new PublishedFundingContentsGeneratorResolver();
            IPublishedFundingContentsGenerator v10Generator = new PublishedFundingContentsGenerator10();
            IPublishedFundingContentsGenerator v11Generator = new PublishedFundingContentsGenerator11();
            IPublishedFundingContentsGenerator v12Generator = new PublishedFundingContentsGenerator12();
            publishedFundingContentsGeneratorResolver.Register("1.0", v10Generator);
            publishedFundingContentsGeneratorResolver.Register("1.1", v11Generator);
            publishedFundingContentsGeneratorResolver.Register("1.2", v12Generator);

            RegisterInstanceAs<IPublishedFundingContentsGeneratorResolver>(publishedFundingContentsGeneratorResolver);
            
            RegisterTypeAs<TransactionFactory, ITransactionFactory>();

            RegisterInstanceAs<ITransactionResiliencePolicies>(new TransactionResiliencePolicies
            {
                TransactionPolicy = Policy.NoOpAsync()
            });

            RegisterTypeAs<PublishedProviderIndexerService, IPublishedProviderIndexerService>();
            RegisterTypeAs<FundingLineValueOverride, IFundingLineValueOverride>();
            RegisterTypeAs<PublishedProviderDataPopulator, IPublishedProviderDataPopulator>();
            RegisterTypeAs<PublishedProviderExclusionCheck, IPublishProviderExclusionCheck>();
            RegisterTypeAs<PublishedProviderVersioningService, IPublishedProviderVersioningService>();
            RegisterTypeAs<PublishedProviderContentPersistanceService, IPublishedProviderContentPersistanceService>();
            RegisterTypeAs<PublishedFundingContentsPersistanceService, IPublishedFundingContentsPersistanceService>();
            RegisterTypeAs<PublishedFundingCsvJobsService, IPublishedFundingCsvJobsService>();

            RegisterTypeAs<VariationErrorRecorder, IRecordVariationErrors>();
            RegisterTypeAs<RefreshService, IRefreshService>();

            RegisterTypeAs<ApproveService, IApproveService>();

            RegisterTypeAs<PublishService, IPublishService>();
            
            RegisterInstanceAs<IBatchProfilingOptions>(new BatchProfilingOptions(new ConfigurationStub()));
            RegisterTypeAs<BatchProfilingService, IBatchProfilingService>();
            RegisterTypeAs<ProducerConsumerFactory, IProducerConsumerFactory>();
        }

        public class RoundingSettingsStub : IFundingLineRoundingSettings
        {
            public int DecimalPlaces => 2;
        }
        

        private class ConfigurationStub : IConfiguration
        {
            public IConfigurationSection GetSection(string key) => null;

            public IEnumerable<IConfigurationSection> GetChildren() => ArraySegment<IConfigurationSection>.Empty;

            public IChangeToken GetReloadToken() => null;

            public string this[string key]
            {
                get => null;
                set{}
            }
        }
        
    }
}
