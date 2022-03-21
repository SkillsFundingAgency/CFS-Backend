using AutoMapper;
using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.DataImporter;
using CalculateFunding.Services.DataImporter.Validators.Models;
using CalculateFunding.Services.Datasets.Interfaces;
using CalculateFunding.Services.Datasets.MappingProfiles;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Azure.Storage.Blob;
using NSubstitute;
using OfficeOpenXml;
using Serilog;
using PoliciesApiModels = CalculateFunding.Common.ApiClient.Policies.Models;
using System;
using System.Collections.Generic;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Services.Datasets.UnitTests.Builders;
using CalculateFunding.Services.Datasets.Excel;
using CalculateFunding.Common.ApiClient.Graph;

namespace CalculateFunding.Services.Datasets.Services
{
    public abstract class DatasetServiceTestsBase
    {
        protected const string DatasetName = "test-dataset";
        protected const string Username = "test-user";
        protected const string UserId = "33d7a71b-f570-4425-801b-250b9129f3d3";
        protected const string DataDefinitionId = "45d7a71b-f570-4425-801b-250b9129f124";
        protected const string SpecificationId = "d557a71b-f570-4425-801b-250b9129f111";
        protected const string BuildProjectId = "d557a71b-f570-4425-801b-250b9129f111";
        protected const string DatasetId = "e557a71b-f570-4436-801b-250b9129f999";
        protected const string DatasetRelationshipId = "r557a71b-f570-4436-801b-250b9129f999";
        protected const string ProviderVersionId = "f557a71b-f570-4436-801b-250b9129f999";
        protected const string FundingStreamId = "test-funding-stream-id";
        protected const string FundingStreamName = "test-funding-stream-name";
        protected const string JobId = "job1";

        protected DatasetService CreateDatasetService(
            IBlobClient blobClient = null,
            ILogger logger = null,
            IDatasetRepository datasetRepository = null,
            IVersionRepository<DatasetVersion> datasetVersionRepository = null,
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
            IProvidersApiClient providersApiClient = null,
            IJobManagement jobManagement = null,
            IProviderSourceDatasetsRepository providerSourceDatasetsRepository = null,
            ISpecificationsApiClient specificationsApiClient = null,
            IPolicyRepository policyRepository = null,
            IDatasetDataMergeService datasetDataMergeService = null,
            IRelationshipDataExcelWriter relationshipDataExcelWriter = null,
            IGraphApiClient graphApiClient = null,
            IConverterDataMergeService converterDataMergeService = null)
        {
            return new DatasetService(
                blobClient ?? CreateBlobClient(),
                logger ?? CreateLogger(),
                datasetRepository ?? CreateDatasetsRepository(),
                datasetVersionRepository ?? CreateDatasetsVersionRepository(),
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
                datasetVersionIndex ?? CreateDatasetVersionIndexRepository(),
                providersApiClient ?? CreateProvidersApiClient(),
                jobManagement ?? CreateJobManagement(),
                providerSourceDatasetsRepository ?? CreateProviderSourceDatasetsRepository(),
                specificationsApiClient ?? CreateSpecificationsApiClient(),
                policyRepository ?? CreatePolicyRepository(),
                calcsRepository ?? CreateCalcsRepository(),
                datasetDataMergeService ?? CreateDatasetDataMergeService(),
                relationshipDataExcelWriter ?? CreateRelationshipDataExcelWriter(),
                graphApiClient ?? CreateGraphClient(),
                converterDataMergeService ?? CreateConverterDataMergeService());
        }

        protected IDatasetDataMergeService CreateDatasetDataMergeService()
        {
            return Substitute.For<IDatasetDataMergeService>();
        }

        protected IRelationshipDataExcelWriter CreateRelationshipDataExcelWriter()
        {
            return Substitute.For<IRelationshipDataExcelWriter>();
        }
        protected IGraphApiClient CreateGraphClient()
        {
            return Substitute.For<IGraphApiClient>();
        }
        protected IConverterDataMergeService CreateConverterDataMergeService()
        {
            return Substitute.For<IConverterDataMergeService>();
        }
        protected IPolicyRepository CreatePolicyRepository()
        {
            return Substitute.For<IPolicyRepository>();
        }

        protected ICalcsRepository CreateCalcsRepository()
        {
            return Substitute.For<ICalcsRepository>();
        }

        protected ISpecificationsApiClient CreateSpecificationsApiClient()
        {
            return Substitute.For<ISpecificationsApiClient>();
        }

        private IProviderSourceDatasetsRepository CreateProviderSourceDatasetsRepository()
        {
            return Substitute.For<IProviderSourceDatasetsRepository>();
        }

        protected IJobManagement CreateJobManagement()
        {
            IJobManagement jobManagement = Substitute.For<IJobManagement>();

            jobManagement
                .RetrieveJobAndCheckCanBeProcessed(JobId)
                .Returns(new JobViewModel { Id = JobId });

            return jobManagement;
        }

        protected IVersionRepository<ProviderSourceDatasetVersion> CreateVersionRepository()
        {
            return Substitute.For<IVersionRepository<ProviderSourceDatasetVersion>>();
        }

        protected IProviderSourceDatasetsRepository CreateProviderResultsRepository()
        {
            return Substitute.For<IProviderSourceDatasetsRepository>();
        }

        protected IExcelDatasetReader CreateExcelDatasetReader()
        {
            return Substitute.For<IExcelDatasetReader>();
        }

        protected ISearchRepository<DatasetIndex> CreateSearchRepository()
        {
            return Substitute.For<ISearchRepository<DatasetIndex>>();
        }

        protected ISearchRepository<DatasetVersionIndex> CreateDatasetVersionIndexRepository()
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
                .ValidateAsync(Arg.Any<DatasetUploadValidationModel>())
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

        protected IVersionRepository<DatasetVersion> CreateDatasetsVersionRepository()
        {
            return Substitute.For<IVersionRepository<DatasetVersion>>();
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

        protected IEnumerable<PoliciesApiModels.FundingStream> NewFundingStreams() =>
            new List<PoliciesApiModels.FundingStream>
            {
                NewApiFundingStream(_ => _.WithId(FundingStreamId).WithName(FundingStreamName))
            };

        protected SpecificationSummary NewSpecificationSummary(
            Action<SpecificationSummaryBuilder> setUp = null)
        {
            SpecificationSummaryBuilder specificationSummaryBuilder = new SpecificationSummaryBuilder();

            setUp?.Invoke(specificationSummaryBuilder);

            return specificationSummaryBuilder.Build();
        }

        protected PoliciesApiModels.FundingStream NewApiFundingStream(
            Action<PolicyFundingStreamBuilder> setUp = null)
        {
            PolicyFundingStreamBuilder fundingStreamBuilder = new PolicyFundingStreamBuilder();

            setUp?.Invoke(fundingStreamBuilder);

            return fundingStreamBuilder.Build();
        }

        protected IProvidersApiClient CreateProvidersApiClient()
        {
            return Substitute.For<IProvidersApiClient>();
        }

        protected DefinitionSpecificationRelationship NewDefinitionSpecificationRelationship(Action<DefinitionSpecificationRelationshipBuilder> setUp = null)
        {
            DefinitionSpecificationRelationshipBuilder builder = new DefinitionSpecificationRelationshipBuilder();
            setUp?.Invoke(builder);
            return builder.Build();
        }

        protected DefinitionSpecificationRelationshipVersion NewDefinitionSpecificationRelationshipVersion(Action<DefinitionSpecificationRelationshipVersionBuilder> setUp = null)
        {
            DefinitionSpecificationRelationshipVersionBuilder builder = new DefinitionSpecificationRelationshipVersionBuilder();
            setUp?.Invoke(builder);
            return builder.Build();
        }
    }
}
