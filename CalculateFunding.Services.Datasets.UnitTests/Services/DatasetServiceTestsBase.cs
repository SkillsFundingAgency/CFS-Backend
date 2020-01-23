using AutoMapper;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.DataImporter;
using CalculateFunding.Services.DataImporter.Validators.Models;
using CalculateFunding.Services.Datasets.Interfaces;
using CalculateFunding.Services.Results.Interfaces;
using CalculateFunding.Services.Datasets.MappingProfiles;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Azure.Storage.Blob;
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

        protected DatasetService CreateDatasetService(
            IBlobClient blobClient = null,
            ILogger logger = null,
            IDatasetRepository datasetRepository = null,
            IValidator<CreateNewDatasetModel> createNewDatasetModelValidator = null,
            IValidator<DatasetVersionUpdateModel> datasetVersionUpdateModelValidator = null,
            IMapper mapper = null,
            IValidator<DatasetMetadataModel> datasetMetadataModelValidator = null,
            ISearchRepository<DatasetIndex> searchRepository = null,
            ISearchRepository<DatasetVersionIndex> datasetVersionIndex = null,
            IValidator<GetDatasetBlobModel> getDatasetBlobModelValidator = null,
            ICacheProvider cacheProvider = null,
            ICalcsRepository calcsRepository = null,
            IValidator<ExcelPackage> datasetWorksheetValidator = null,
            IValidator<DatasetUploadValidationModel> datasetUploadValidator = null,
            IJobsApiClient jobsApiClient = null,
            IProvidersApiClient providersApiClient = null,
            IJobManagement jobManagement = null,
            IProviderSourceDatasetRepository providerSourceDatasetRepository = null,
            ISpecificationsApiClient specificationsApiClient = null)
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
                cacheProvider ?? CreateCacheProvider(),
                datasetWorksheetValidator ?? CreateDataWorksheetValidator(),
                datasetUploadValidator ?? CreateDatasetUploadValidator(),
                DatasetsResilienceTestHelper.GenerateTestPolicies(),
                jobsApiClient ?? CreateJobsApiClient(),
                datasetVersionIndex ?? CreateDatasetVersionRepository(),
                providersApiClient ?? CreateProvidersApiClient(),
                jobManagement ?? CreateJobManagement(),
                providerSourceDatasetRepository ?? CreateProviderSourceDatasetRepository(),
                specificationsApiClient ?? CreateSpecificationsApiClient());
        }

        private ISpecificationsApiClient CreateSpecificationsApiClient()
        {
            return Substitute.For<ISpecificationsApiClient>();
        }

        private IProviderSourceDatasetRepository CreateProviderSourceDatasetRepository()
        {
            return Substitute.For<IProviderSourceDatasetRepository>();
        }

        protected IJobManagement CreateJobManagement()
        {
            return Substitute.For<IJobManagement>();
        }

        protected IVersionRepository<ProviderSourceDatasetVersion> CreateVersionRepository()
        {
            return Substitute.For<IVersionRepository<ProviderSourceDatasetVersion>>();
        }

        protected ICalcsRepository CreateCalcsRepository()
        {
            return Substitute.For<ICalcsRepository>();
        }

        private ITelemetry CreateTelemetry()
        {
            return Substitute.For<ITelemetry>();
        }

        protected IProvidersResultsRepository CreateProviderResultsRepository()
        {
            return Substitute.For<IProvidersResultsRepository>();
        }

        protected IExcelDatasetReader CreateExcelDatasetReader()
        {
            return Substitute.For<IExcelDatasetReader>();
        }

        protected ISearchRepository<DatasetIndex> CreateSearchRepository()
        {
            return Substitute.For<ISearchRepository<DatasetIndex>>();
        }

        protected ISearchRepository<DatasetVersionIndex> CreateDatasetVersionRepository()
        {
            return Substitute.For<ISearchRepository<DatasetVersionIndex>>();
        }

        protected ICacheProvider CreateCacheProvider()
        {
            return Substitute.For<ICacheProvider>();
        }

        protected IValidator<DatasetUploadValidationModel> CreateDatasetUploadValidator(ValidationResult validationResult = null)
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

        protected IValidator<CreateNewDatasetModel> CreateNewDatasetModelValidator(ValidationResult validationResult = null)
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

        protected IValidator<DatasetVersionUpdateModel> CreateDatasetVersionUpdateModelValidator(ValidationResult validationResult = null)
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

        protected IValidator<DatasetMetadataModel> CreateDatasetMetadataModelValidator(ValidationResult validationResult = null)
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

        protected IValidator<GetDatasetBlobModel> CreateGetDatasetBlobModelValidator(ValidationResult validationResult = null)
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

        protected IValidator<ExcelPackage> CreateDataWorksheetValidator(ValidationResult validationResult = null)
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

        protected IBlobClient CreateBlobClient()
        {
            return Substitute.For<IBlobClient>();
        }

        protected static ICloudBlob CreateBlob()
        {
            return Substitute.For<ICloudBlob>();
        }

        protected ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        protected IMapper CreateMapper()
        {
            return Substitute.For<IMapper>();
        }

        protected IMapper CreateMapperWithDatasetsConfiguration()
        {
            MapperConfiguration config = new MapperConfiguration(c =>
            {
                c.AddProfile<DatasetsMappingProfile>();
            });

            return new Mapper(config);
        }

        protected IDatasetRepository CreateDatasetsRepository()
        {
            return Substitute.For<IDatasetRepository>();
        }

        protected byte[] CreateTestExcelPackage()
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

        protected IJobsApiClient CreateJobsApiClient()
        {
            return Substitute.For<IJobsApiClient>();
        }

        protected IProvidersApiClient CreateProvidersApiClient()
        {
            return Substitute.For<IProvidersApiClient>();
        }
    }
}
