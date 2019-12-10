using BoDi;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Publishing.AcceptanceTests.Contexts;
using CalculateFunding.Publishing.AcceptanceTests.Repositories;
using CalculateFunding.Services.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;
using Polly;
using Serilog;
using TechTalk.SpecFlow;
using PublishingResiliencePolicies = CalculateFunding.Services.Publishing.ResiliencePolicies;

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

        [BeforeScenario]
        public void SetupStepContexts()
        {
            IConfigurationRoot config = new ConfigurationBuilder().Build();
            _objectContainer.RegisterInstanceAs<IConfiguration>(config);

            LoggerConfiguration loggerConfiguration = new LoggerConfiguration();

            ILogger logger = loggerConfiguration.CreateLogger();

            PublishingResiliencePolicies publishingResiliencePolicies = new PublishingResiliencePolicies()
            {
                BlobClient = Policy.NoOpAsync(),
                CalculationsApiClient = Policy.NoOpAsync(),
                FundingFeedSearchRepository = Policy.NoOpAsync(),
                JobsApiClient = Policy.NoOpAsync(),
                PoliciesApiClient = Policy.NoOpAsync(),
                ProvidersApiClient = Policy.NoOpAsync(),
                PublishedFundingBlobRepository = Policy.NoOpAsync(),
                PublishedFundingRepository = Policy.NoOpAsync(),
                PublishedProviderVersionRepository = Policy.NoOpAsync(),
                CalculationResultsRepository = Policy.NoOpAsync(),
                SpecificationsRepositoryPolicy = Policy.NoOpAsync(),
            };

            InMemoryBlobClient inMemoryBlobClient = new InMemoryBlobClient();
            _objectContainer.RegisterInstanceAs<InMemoryBlobClient>(inMemoryBlobClient);

            InMemoryAzureBlobClient inMemoryAzureBlobClient = new InMemoryAzureBlobClient();
            _objectContainer.RegisterInstanceAs<InMemoryAzureBlobClient>(inMemoryAzureBlobClient);

            InMemoryCosmosRepository inMemoryCosmosRepository = new InMemoryCosmosRepository();
            _objectContainer.RegisterInstanceAs<ICosmosRepository>(inMemoryCosmosRepository);

            SpecificationInMemoryRepository specificationInMemoryRepository = new SpecificationInMemoryRepository();
            _objectContainer.RegisterInstanceAs<ISpecificationService>(specificationInMemoryRepository);

            JobsInMemoryRepository jobsInMemoryRepository = new JobsInMemoryRepository();
            _objectContainer.RegisterInstanceAs<IJobsApiClient>(jobsInMemoryRepository);

            InMemoryFeatureManagerSnapshot inMemoryFeatureManagerSnapshot = new InMemoryFeatureManagerSnapshot();
            _objectContainer.RegisterInstanceAs<IFeatureManagerSnapshot>(inMemoryFeatureManagerSnapshot);

            PublishingFeatureFlag publishingFeatureFlag = new PublishingFeatureFlag(inMemoryFeatureManagerSnapshot);
            _objectContainer.RegisterInstanceAs<IPublishingFeatureFlag>(publishingFeatureFlag);

            JobManagementResiliencePolicies jobManagementResiliencePolicies = new JobManagementResiliencePolicies()
            {
                JobsApiClient = Policy.NoOpAsync(),
            };

            JobManagement jobManagement = new JobManagement(jobsApiClient: jobsInMemoryRepository,
                logger: logger,
                jobManagementResiliencePolicies: jobManagementResiliencePolicies);
            _objectContainer.RegisterInstanceAs<IJobManagement>(jobManagement);

            InMemoryPublishedFundingRepository inMemoryPublishedFundingRepository = new InMemoryPublishedFundingRepository(inMemoryCosmosRepository);
            _objectContainer.RegisterInstanceAs<IPublishedFundingRepository>(inMemoryPublishedFundingRepository);

            PublishedProviderInMemorySearchRepository publishedProviderInMemorySearchRepository = new PublishedProviderInMemorySearchRepository();

            PoliciesInMemoryRepository policiesInMemoryRepository = new PoliciesInMemoryRepository(logger);
            ProvidersInMemoryClient providersInMemoryClient = new ProvidersInMemoryClient();
            ProviderService providerService = new ProviderService(providersInMemoryClient, publishingResiliencePolicies);

            SpecificationsInMemoryClient specificationsInMemoryClient = new SpecificationsInMemoryClient();
            PublishedFundingDateService publishedFundingDateService = new PublishedFundingDateService(specificationsInMemoryClient, publishingResiliencePolicies);

            _objectContainer.RegisterTypeAs<PublishServiceAcceptanceStepContext, IPublishFundingStepContext>();

            CurrentSpecificationStepContext currentSpecificationStepContext = new CurrentSpecificationStepContext();
            currentSpecificationStepContext.Repo = specificationInMemoryRepository;

            _objectContainer.RegisterInstanceAs<ICurrentSpecificationStepContext>(currentSpecificationStepContext);

            JobStepContext jobStepContext = new JobStepContext()
            {
                InMemoryRepo = jobsInMemoryRepository,
                JobsClient = jobsInMemoryRepository,
            };

            _objectContainer.RegisterInstanceAs<IJobStepContext>(jobStepContext);

            CurrentJobStepContext currentJobStepContext = new CurrentJobStepContext();
            _objectContainer.RegisterInstanceAs<ICurrentJobStepContext>(currentJobStepContext);

            PublishedFundingRepositoryStepContext publishedFundingRepositoryStepContext = new PublishedFundingRepositoryStepContext()
            {
                Repo = inMemoryPublishedFundingRepository,
                CosmosRepo = inMemoryCosmosRepository,
                BlobRepo = inMemoryBlobClient,
            };
            _objectContainer.RegisterInstanceAs<IPublishedFundingRepositoryStepContext>(publishedFundingRepositoryStepContext);

            PoliciesStepContext policiesStepContext = new PoliciesStepContext()
            {
                Client = policiesInMemoryRepository,
                Repo = policiesInMemoryRepository,
            };

            _objectContainer.RegisterInstanceAs<IPoliciesStepContext>(policiesStepContext);

            LoggerStepContext loggerStepContext = new LoggerStepContext()
            {
                Logger = logger,
            };
            _objectContainer.RegisterInstanceAs<ILoggerStepContext>(loggerStepContext);

            ProvidersStepContext providersStepContext = new ProvidersStepContext()
            {
                Client = providersInMemoryClient,
                EmulatedClient = providersInMemoryClient,
                Service = providerService,
                BlobRepo = inMemoryAzureBlobClient,
            };
            _objectContainer.RegisterInstanceAs<IProvidersStepContext>(providersStepContext);

            PublishingDatesStepContext publishingDatesStepContext = new PublishingDatesStepContext()
            {
                EmulatedService = publishedFundingDateService,
                Service = publishedFundingDateService,
                EmulatedClient = specificationsInMemoryClient
            };
            _objectContainer.RegisterInstanceAs<IPublishingDatesStepContext>(publishingDatesStepContext);

            PublishedFundingResultStepContext publishedFundingResultStepContext = new PublishedFundingResultStepContext();
            _objectContainer.RegisterInstanceAs<IPublishedFundingResultStepContext>(publishedFundingResultStepContext);

            PublishedProviderStepContext publishedProviderStepContext = new PublishedProviderStepContext()
            {
                Client = providersInMemoryClient,
                EmulatedClient = providersInMemoryClient,
                Service = providerService,
                BlobRepo = inMemoryAzureBlobClient,
                SearchRepo = publishedProviderInMemorySearchRepository,
            };
            _objectContainer.RegisterInstanceAs<IPublishedProviderStepContext>(publishedProviderStepContext);
        }
    }
}
