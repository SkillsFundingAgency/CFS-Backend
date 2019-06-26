using AutoMapper;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.DataImporter;
using CalculateFunding.Services.Datasets.Interfaces;
using CalculateFunding.Services.Datasets.MappingProfiles;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Datasets.Services
{
    public abstract class ProcessDatasetServiceTestsBase
    {
        protected const string DatasetName = "test-dataset";
        protected const string Username = "test-user";
        protected const string UserId = "33d7a71b-f570-4425-801b-250b9129f3d3";
        protected const string DataDefintionId = "45d7a71b-f570-4425-801b-250b9129f124";
        protected const string SpecificationId = "d557a71b-f570-4425-801b-250b9129f111";
        protected const string BuildProjectId = "d557a71b-f570-4425-801b-250b9129f111";
        protected const string DatasetId = "e557a71b-f570-4436-801b-250b9129f999";

        protected static ProcessDatasetService CreateProcessDatasetService(
            IBlobClient blobClient = null,
            IMessengerService messengerService = null,
            ILogger logger = null,
            IDatasetRepository datasetRepository = null,
            IExcelDatasetReader excelDatasetReader = null,
            ICacheProvider cacheProvider = null,
            ICalcsRepository calcsRepository = null,
            IProvidersApiClient providersApiClient = null,
            IProvidersResultsRepository providerResultsRepository = null,
            ITelemetry telemetry = null,
            IDatasetsResiliencePolicies datasetsResiliencePolicies = null,
            IVersionRepository<ProviderSourceDatasetVersion> versionRepository = null,
            IDatasetsAggregationsRepository datasetsAggregationsRepository = null,
            IFeatureToggle featureToggle = null,
            IJobsApiClient jobsApiClient = null,
            IMapper mapper = null)
        {

            return new ProcessDatasetService(
                datasetRepository ?? CreateDatasetsRepository(),
                excelDatasetReader ?? CreateExcelDatasetReader(),
                cacheProvider ?? CreateCacheProvider(),
                calcsRepository ?? CreateCalcsRepository(),
                blobClient ?? CreateBlobClient(),
                messengerService ?? CreateMessengerService(),
                providerResultsRepository ?? CreateProviderResultsRepository(),
                providersApiClient ?? CreateProvidersApiClient(),
                versionRepository ?? CreateVersionRepository(),
                logger ?? CreateLogger(),
                telemetry ?? CreateTelemetry(),
                datasetsResiliencePolicies ?? DatasetsResilienceTestHelper.GenerateTestPolicies(),
                datasetsAggregationsRepository ?? CreateDatasetsAggregationsRepository(),
                featureToggle ?? CreateFeatureToggle(),
                jobsApiClient ?? CreateJobsApiClient(),
                mapper ?? CreateMapper());
        }

        protected static IFeatureToggle CreateFeatureToggle()
        {
            IFeatureToggle featureToggle = Substitute.For<IFeatureToggle>();

            return featureToggle;
        }

        protected static IJobsApiClient CreateJobsApiClient()
        {
            return Substitute.For<IJobsApiClient>();
        }

        protected static IDatasetsAggregationsRepository CreateDatasetsAggregationsRepository()
        {
            return Substitute.For<IDatasetsAggregationsRepository>();
        }

        protected static IVersionRepository<ProviderSourceDatasetVersion> CreateVersionRepository()
        {
            return Substitute.For<IVersionRepository<ProviderSourceDatasetVersion>>();
        }

        protected static ICalcsRepository CreateCalcsRepository()
        {
            return Substitute.For<ICalcsRepository>();
        }

        static ITelemetry CreateTelemetry()
        {
            return Substitute.For<ITelemetry>();
        }

        protected static IProvidersApiClient CreateProvidersApiClient()
        {
            return Substitute.For<IProvidersApiClient>();
        }

        protected static IProvidersResultsRepository CreateProviderResultsRepository()
        {
            return Substitute.For<IProvidersResultsRepository>();
        }

        protected static IExcelDatasetReader CreateExcelDatasetReader()
        {
            return Substitute.For<IExcelDatasetReader>();
        }

        protected static ICacheProvider CreateCacheProvider()
        {
            return Substitute.For<ICacheProvider>();
        }

        protected static IBlobClient CreateBlobClient()
        {
            return Substitute.For<IBlobClient>();
        }

        protected static IMessengerService CreateMessengerService()
        {
            return Substitute.For<IMessengerService>();
        }

        protected static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        protected static IDatasetRepository CreateDatasetsRepository()
        {
            return Substitute.For<IDatasetRepository>();
        }
        protected static IMapper CreateMapper()
        {
            MapperConfiguration config = new MapperConfiguration(c =>
            {
                c.AddProfile<ProviderMappingProfile>();
            });

            return new Mapper(config);
        }
    }
}
