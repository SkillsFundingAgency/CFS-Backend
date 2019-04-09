using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.DataImporter;
using CalculateFunding.Services.Datasets.Interfaces;
using CalculateFunding.Services.Providers.Interfaces;
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
            ILogger logger = null,
            IDatasetRepository datasetRepository = null,
            IExcelDatasetReader excelDatasetReader = null,
            ICacheProvider cacheProvider = null,
            ICalcsRepository calcsRepository = null,
            IProviderService providerService = null,
            IResultsRepository resultsRepository = null,
            IProvidersResultsRepository providerResultsRepository = null,
            ITelemetry telemetry = null,
            IDatasetsResiliencePolicies datasetsResiliencePolicies = null,
            IVersionRepository<ProviderSourceDatasetVersion> versionRepository = null,
            IDatasetsAggregationsRepository datasetsAggregationsRepository = null,
            IFeatureToggle featureToggle = null,
            IJobsApiClient jobsApiClient = null)
        {

            return new ProcessDatasetService(
                datasetRepository ?? CreateDatasetsRepository(),
                excelDatasetReader ?? CreateExcelDatasetReader(),
                cacheProvider ?? CreateCacheProvider(),
                calcsRepository ?? CreateCalcsRepository(),
                blobClient ?? CreateBlobClient(),
                providerResultsRepository ?? CreateProviderResultsRepository(),
                resultsRepository ?? CreateResultsRepository(),
                providerService ?? CreateProviderService(),
                versionRepository ?? CreateVersionRepository(),
                logger ?? CreateLogger(),
                telemetry ?? CreateTelemetry(),
                datasetsResiliencePolicies ?? DatasetsResilienceTestHelper.GenerateTestPolicies(),
                datasetsAggregationsRepository ?? CreateDatasetsAggregationsRepository(),
                featureToggle ?? CreateFeatureToggle(),
                jobsApiClient ?? CreateJobsApiClient());
        }

        protected static IFeatureToggle CreateFeatureToggle()
        {
            IFeatureToggle featureToggle = Substitute.For<IFeatureToggle>();
            featureToggle
                .IsAggregateSupportInCalculationsEnabled()
                .Returns(false);

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

        protected static IProviderService CreateProviderService()
        {
            return Substitute.For<IProviderService>();
        }

        protected static IProvidersResultsRepository CreateProviderResultsRepository()
        {
            return Substitute.For<IProvidersResultsRepository>();
        }

        protected static IResultsRepository CreateResultsRepository()
        {
            return Substitute.For<IResultsRepository>();
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

        protected static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        protected static IDatasetRepository CreateDatasetsRepository()
        {
            return Substitute.For<IDatasetRepository>();
        }
    }
}
