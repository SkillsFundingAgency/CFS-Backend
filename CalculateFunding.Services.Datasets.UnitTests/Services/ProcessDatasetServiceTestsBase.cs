using CalculateFunding.Models.Results;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.DataImporter;
using CalculateFunding.Services.Datasets.Interfaces;
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
            IMessengerService messengerService = null,
            IExcelDatasetReader excelDatasetReader = null,
            ICacheProvider cacheProvider = null,
            ICalcsRepository calcsRepository = null,
            IProviderRepository providerRepository = null,
            IProvidersResultsRepository providerResultsRepository = null,
            ITelemetry telemetry = null,
            IDatasetsResiliencePolicies datasetsResiliencePolicies = null,
            IVersionRepository<ProviderSourceDatasetVersion> versionRepository = null)
        {

            return new ProcessDatasetService(
                datasetRepository ?? CreateDatasetsRepository(),
                messengerService ?? CreateMessengerService(),
                excelDatasetReader ?? CreateExcelDatasetReader(),
                cacheProvider ?? CreateCacheProvider(),
                calcsRepository ?? CreateCalcsRepository(),
                blobClient ?? CreateBlobClient(),
                providerResultsRepository ?? CreateProviderResultsRepository(),
                providerRepository ?? CreateProviderRepository(),
                versionRepository ?? CreateVersionRepository(),
                logger ?? CreateLogger(),
                telemetry ?? CreateTelemetry(),
                datasetsResiliencePolicies ?? DatasetsResilienceTestHelper.GenerateTestPolicies());
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

        protected static IProviderRepository CreateProviderRepository()
        {
            return Substitute.For<IProviderRepository>();
        }

        protected static IProvidersResultsRepository CreateProviderResultsRepository()
        {
            return Substitute.For<IProvidersResultsRepository>();
        }

        protected static IExcelDatasetReader CreateExcelDatasetReader()
        {
            return Substitute.For<IExcelDatasetReader>();
        }

        protected static IMessengerService CreateMessengerService()
        {
            return Substitute.For<IMessengerService>();
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
