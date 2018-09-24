using AutoMapper;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.DataImporter;
using CalculateFunding.Services.DataImporter.Validators.Models;
using CalculateFunding.Services.Datasets.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using NSubstitute;
using OfficeOpenXml;
using Serilog;

namespace CalculateFunding.Services.Datasets.Services
{
    public abstract class DatasetServiceTestsBase
    {
        protected const string DatasetName = "test-dataset";
        protected const string Username = "test-user";
        protected const string UserId = "33d7a71b-f570-4425-801b-250b9129f3d3";
        protected const string DataDefintionId = "45d7a71b-f570-4425-801b-250b9129f124";
        protected const string SpecificationId = "d557a71b-f570-4425-801b-250b9129f111";
        protected const string BuildProjectId = "d557a71b-f570-4425-801b-250b9129f111";
        protected const string DatasetId = "e557a71b-f570-4436-801b-250b9129f999";

        protected static DatasetService CreateDatasetService(
            IBlobClient blobClient = null,
            ILogger logger = null,
            IDatasetRepository datasetRepository = null,
            IValidator<CreateNewDatasetModel> createNewDatasetModelValidator = null,
            IValidator<DatasetVersionUpdateModel> datasetVersionUpdateModelValidator = null,
            IMapper mapper = null,
            IValidator<DatasetMetadataModel> datasetMetadataModelValidator = null,
            ISearchRepository<DatasetIndex> searchRepository = null,
            IValidator<GetDatasetBlobModel> getDatasetBlobModelValidator = null,
            ISpecificationsRepository specificationsRepository = null,
            IMessengerService messengerService = null,
            IExcelDatasetReader excelDatasetReader = null,
            ICacheProvider cacheProvider = null,
            ICalcsRepository calcsRepository = null,
            IProviderRepository providerRepository = null,
            IProvidersResultsRepository providerResultsRepository = null,
            ITelemetry telemetry = null,
            IDatasetsResiliencePolicies datasetsResiliencePolicies = null,
            IValidator<ExcelPackage> datasetWorksheetValidator = null,
            IValidator<DatasetUploadValidationModel> datasetUploadValidator = null,
            IVersionRepository<ProviderSourceDatasetVersion> versionRepository = null)
        {

            return new DatasetService(
                blobClient ?? CreateBlobClient(),
                logger ?? CreateLogger(),
                datasetRepository ?? CreateDatasetsRepository(),
                createNewDatasetModelValidator ?? CreateNewDatasetModelValidator(),
                datasetVersionUpdateModelValidator ?? CreateDatasetVersionUpdateModelValidator(),
                mapper ?? CreateMapper(),
                datasetMetadataModelValidator ?? CreateDatasetMetadataModelValidator(),
                searchRepository ?? CreateSearchRepository(),
                getDatasetBlobModelValidator ?? CreateGetDatasetBlobModelValidator(),
                specificationsRepository ?? CreateSpecificationsRepository(),
                messengerService ?? CreateMessengerService(),
                excelDatasetReader ?? CreateExcelDatasetReader(),
                cacheProvider ?? CreateCacheProvider(), calcsRepository ?? CreateCalcsRepository(),
                providerRepository ?? CreateProviderRepository(),
                providerResultsRepository ?? CreateProviderResultsRepository(),
                telemetry ?? CreateTelemetry(),
                datasetsResiliencePolicies ?? DatasetsResilienceTestHelper.GenerateTestPolicies(),
                datasetWorksheetValidator ?? CreateDataWorksheetValidator(),
                datasetUploadValidator ?? CreateDatasetUploadValidator(),
                versionRepository ?? CreateVersionRepository());
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

        protected static ISearchRepository<DatasetIndex> CreateSearchRepository()
        {
            return Substitute.For<ISearchRepository<DatasetIndex>>();
        }

        protected static ISpecificationsRepository CreateSpecificationsRepository()
        {
            return Substitute.For<ISpecificationsRepository>();
        }

        protected static IMessengerService CreateMessengerService()
        {
            return Substitute.For<IMessengerService>();
        }

        protected static ServiceBusSettings CreateEventHubSettings()
        {
            return new ServiceBusSettings();
        }

        protected static ICacheProvider CreateCacheProvider()
        {
            return Substitute.For<ICacheProvider>();
        }

        protected static IValidator<DatasetUploadValidationModel> CreateDatasetUploadValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
            {
                validationResult = new ValidationResult();
            }

            IValidator<DatasetUploadValidationModel> validator = Substitute.For<IValidator<DatasetUploadValidationModel>>();

            validator
                .Validate(Arg.Any<DatasetUploadValidationModel>())
                .Returns(validationResult);

            return validator;
        }

        protected static IValidator<CreateNewDatasetModel> CreateNewDatasetModelValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
            {
                validationResult = new ValidationResult();
            }

            IValidator<CreateNewDatasetModel> validator = Substitute.For<IValidator<CreateNewDatasetModel>>();

            validator
               .ValidateAsync(Arg.Any<CreateNewDatasetModel>())
               .Returns(validationResult);

            return validator;
        }

        protected static IValidator<DatasetVersionUpdateModel> CreateDatasetVersionUpdateModelValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
            {
                validationResult = new ValidationResult();
            }

            IValidator<DatasetVersionUpdateModel> validator = Substitute.For<IValidator<DatasetVersionUpdateModel>>();

            validator
               .ValidateAsync(Arg.Any<DatasetVersionUpdateModel>())
               .Returns(validationResult);

            return validator;
        }

        protected static IValidator<DatasetMetadataModel> CreateDatasetMetadataModelValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
            {
                validationResult = new ValidationResult();
            }

            IValidator<DatasetMetadataModel> validator = Substitute.For<IValidator<DatasetMetadataModel>>();

            validator
               .ValidateAsync(Arg.Any<DatasetMetadataModel>())
               .Returns(validationResult);

            return validator;
        }

        protected static IValidator<GetDatasetBlobModel> CreateGetDatasetBlobModelValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
            {
                validationResult = new ValidationResult();
            }

            IValidator<GetDatasetBlobModel> validator = Substitute.For<IValidator<GetDatasetBlobModel>>();

            validator
               .ValidateAsync(Arg.Any<GetDatasetBlobModel>())
               .Returns(validationResult);

            return validator;
        }

        protected static IValidator<ExcelPackage> CreateDataWorksheetValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
            {
                validationResult = new ValidationResult();
            }

            IValidator<ExcelPackage> validator = Substitute.For<IValidator<ExcelPackage>>();

            validator
               .ValidateAsync(Arg.Any<ExcelPackage>())
               .Returns(validationResult);

            return validator;
        }

        protected static IBlobClient CreateBlobClient()
        {
            return Substitute.For<IBlobClient>();
        }

        protected static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        protected static IMapper CreateMapper()
        {
            return Substitute.For<IMapper>();
        }

        protected static IMapper CreateMapperWithDatasetsConfiguration()
        {
            MapperConfiguration config = new MapperConfiguration(c =>
            {
                c.AddProfile<DatasetsMappingProfile>();
            });

            return new Mapper(config);
        }

        protected static IDatasetRepository CreateDatasetsRepository()
        {
            return Substitute.For<IDatasetRepository>();
        }

        protected static byte[] CreateTestExcelPackage()
        {
            using (ExcelPackage package = new ExcelPackage())
            {
                ExcelWorksheet workSheet = package.Workbook.Worksheets.Add("Test Worksheet");

                workSheet.Cells["A1"].Value = "1";
                workSheet.Cells["B1"].Value = "2";
                workSheet.Cells["C1"].Value = "3";

                return package.GetAsByteArray();
            }
        }
    }
}
