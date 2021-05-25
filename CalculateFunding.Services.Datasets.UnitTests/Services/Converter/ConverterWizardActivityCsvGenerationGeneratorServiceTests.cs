using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Datasets.Converter;
using CalculateFunding.Models.Datasets.ViewModels;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Datasets.Converter;
using CalculateFunding.Services.Datasets.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.Storage.Blob;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Datasets.Services.Converter
{
    [TestClass]
    public class ConverterWizardActivityCsvGenerationGeneratorServiceTests
    {
        private ConverterWizardActivityCsvGenerationGeneratorService _service;
        private Mock<ICsvUtils> _csvUtils;
        private Mock<IBlobClient> _blobClient;
        private Mock<ISpecificationsApiClient> _specsApiClient;
        private Mock<IPoliciesApiClient> _policiesApiClient;
        private Mock<ICloudBlob> _cloudBlob;
        private Mock<IConverterWizardActivityToCsvRowsTransformation> _transformation;
        private Mock<IConverterEligibleProviderService> _converterEligibleProviderService;
        private Mock<IDefinitionSpecificationRelationshipService> _definitionSpecificationRelationshipService;
        private Mock<IConverterDataMergeLogger> _converterDataMergeLogger;
        private Mock<IFileSystemAccess> _fileSystemAccess;
        private Mock<IFileSystemCacheSettings> _fileSystemCacheSettings;
        private string _rootPath;
        private BlobProperties _blobProperties;
        private Mock<IJobManagement> _jobManagement;

        private Message _message;

        [TestInitialize]
        public void SetUp()
        {
            _blobClient = new Mock<IBlobClient>();
            _csvUtils = new Mock<ICsvUtils>();
            _transformation = new Mock<IConverterWizardActivityToCsvRowsTransformation>();
            _converterEligibleProviderService = new Mock<IConverterEligibleProviderService>();
            _definitionSpecificationRelationshipService = new Mock<IDefinitionSpecificationRelationshipService>();
            _converterDataMergeLogger = new Mock<IConverterDataMergeLogger>();
            _cloudBlob = new Mock<ICloudBlob>();
            _fileSystemAccess = new Mock<IFileSystemAccess>();
            _fileSystemCacheSettings = new Mock<IFileSystemCacheSettings>();
            _jobManagement = new Mock<IJobManagement>();
            _specsApiClient = new Mock<ISpecificationsApiClient>();
            _policiesApiClient = new Mock<IPoliciesApiClient>();

            _service = new ConverterWizardActivityCsvGenerationGeneratorService(_fileSystemAccess.Object,
                _fileSystemCacheSettings.Object,
                _csvUtils.Object,
                _transformation.Object,
                _converterEligibleProviderService.Object,
                _specsApiClient.Object,
                _policiesApiClient.Object,
                _blobClient.Object,
                new DatasetsResiliencePolicies
                {
                    BlobClient = Policy.NoOpAsync(),
                    SpecificationsApiClient = Policy.NoOpAsync(),
                    PoliciesApiClient = Policy.NoOpAsync()
                },
                _definitionSpecificationRelationshipService.Object,
                _converterDataMergeLogger.Object,
                _jobManagement.Object,
                new Mock<ILogger>().Object);

            _message = new Message();
            _rootPath = NewRandomString();

            _fileSystemCacheSettings
                .Setup(_ => _.Path)
                .Returns(_rootPath);

            _fileSystemAccess
                .Setup(_ => _.Append(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _blobProperties = new BlobProperties();

            _cloudBlob
                .Setup(_ => _.Properties)
                .Returns(_blobProperties);
        }

        [TestMethod]
        public void ThrowsExceptionIfNoSpecificationIdInMessageProperties()
        {
            Func<Task> invocation = WhenTheCsvIsGenerated;

            invocation
                .Should()
                .ThrowAsync<NonRetriableException>()
                .WithMessage("Specification id missing");
        }

        [TestMethod]
        public async Task TransformConvertWizardActivityIntoCsvRowsAndCreatesCsvWithResults()
        {
            string specificationId = NewRandomString();
            string fundingStream = NewRandomString();
            string fundingPeriod = NewRandomString();
            string specificationName = NewRandomString();
            string providerVersionId = NewRandomString();
            string expectedInterimFilePath = Path.Combine(_rootPath, $"converter-wizard-activity-{specificationId}.csv");

            List<ProviderConverterDetail> providerConverterDetails = new List<ProviderConverterDetail>()
            {
                NewConverter(),
                NewConverter(),
                NewConverter()
            };

            List<ConverterDataMergeLog> converterDataMergeLogs = new List<ConverterDataMergeLog>();
            List<DatasetSpecificationRelationshipViewModel> datasetSpecificationRelationshipViewModels = new List<DatasetSpecificationRelationshipViewModel>();

            ExpandoObject[] transformedRows = {
                new ExpandoObject(),
                new ExpandoObject(),
                new ExpandoObject()
            };

            string expectedCsv = NewRandomString();

            MemoryStream incrementalFileStream = new MemoryStream();

            GivenSpecification(specificationId, specificationName, fundingStream, fundingPeriod, providerVersionId);
            GivenFundingConfiguration(fundingStream, fundingPeriod);
            GivenProviders(providerVersionId, fundingStream, fundingPeriod, providerConverterDetails);
            GivenTheCsvRowTransformation(providerConverterDetails, 
                converterDataMergeLogs,
                datasetSpecificationRelationshipViewModels,
                transformedRows, 
                expectedCsv, true);
            AndTheMessageProperties(("specification-id", specificationId));
            AndTheMessageProperties(("specification-name", specificationName));
            AndTheCloudBlobForSpecificationId(specificationId);
            AndTheFileStream(expectedInterimFilePath, incrementalFileStream);
            AndTheFileExists(expectedInterimFilePath);

            await WhenTheCsvIsGenerated();

            _fileSystemAccess
                .Verify(_ => _.Delete(expectedInterimFilePath), Times.Once);

            _fileSystemAccess
                .Verify(_ => _.Append(expectedInterimFilePath, expectedCsv, It.IsAny<CancellationToken>()), Times.Once);

            _blobClient
                .Verify(_ => _.UploadAsync(_cloudBlob.Object, incrementalFileStream), Times.Once);

            _blobClient
                .Verify(_ => _.AddMetadataAsync(
                _cloudBlob.Object,
                It.Is<IDictionary<string, string>>(_ =>
                    _["specification-id"] == specificationId &&
                    _["specification-name"] == specificationName &&
                    _["file-name"].StartsWith($"Converter wizard activity report {specificationName} {DateTimeOffset.UtcNow:yyyy-MM-dd}") &&
                    _["job-type"] == "ConverterWizardActivityCsvGenerationGenerator")
                ), Times.Once);
        }

        private void AndTheCloudBlobForSpecificationId(string specificationId)
        {
            _blobClient
                .Setup(_ => _.GetBlockBlobReference($"converter-wizard-activity-{specificationId}.csv"))
                .Returns(_cloudBlob.Object);
        }

        private void AndTheFileStream(string path, Stream stream)
        {
            _fileSystemAccess
                .Setup(_ => _.OpenRead(path))
                .Returns(stream);
        }

        private void AndTheFileExists(string path)
        {
            _fileSystemAccess
                .Setup(_ => _.Exists(path))
                .Returns(true);
        }

        private void GivenSpecification(string specificationId, string specificationName, string fundingStream, string fundingPeriod, string providerVersionId) => 
            _specsApiClient
                .Setup(_ => _.GetSpecificationSummaryById(specificationId))
                .ReturnsAsync(new ApiResponse<SpecificationSummary>(HttpStatusCode.OK, 
                    new SpecificationSummary { Id = specificationId, 
                        Name = specificationName,
                        FundingStreams = new[] { new Reference { Id = fundingStream } }, 
                        FundingPeriod = new Reference { Id = fundingPeriod },
                        ProviderVersionId = providerVersionId
                    }));

        private void GivenFundingConfiguration(string fundingStream, string fundingPeriod) => 
            _policiesApiClient
                .Setup(_ => _.GetFundingConfiguration(fundingStream, fundingPeriod))
                .ReturnsAsync(new ApiResponse<FundingConfiguration>(HttpStatusCode.OK, 
                                new FundingConfiguration { EnableConverterDataMerge = true, 
                                                        FundingStreamId = fundingStream, 
                                                        FundingPeriodId = fundingPeriod }));

        private void GivenProviders(string providerVersionId, string fundingStream, string fundingPeriod, IEnumerable<ProviderConverterDetail> providers)
        {
            _converterEligibleProviderService
                .Setup(_ => _.GetConvertersForProviderVersion(providerVersionId,
                    It.Is<FundingConfiguration>(_ => _.FundingStreamId == fundingStream && _.FundingPeriodId == fundingPeriod && _.EnableConverterDataMerge == true ),
                    null))
                .ReturnsAsync(providers);
        }
        
        private void GivenTheCsvRowTransformation(List<ProviderConverterDetail> providerConvertDetails, List<ConverterDataMergeLog> converterDataMergeLogs, List<DatasetSpecificationRelationshipViewModel> datasetSpecificationRelationshipViewModels, ExpandoObject[] transformedRows, string csv, bool outputHeaders)
        {
            _transformation
                .Setup(_ => _.TransformConvertWizardActivityIntoCsvRows(providerConvertDetails, converterDataMergeLogs, datasetSpecificationRelationshipViewModels))
                .Returns(transformedRows);

            _csvUtils
                .Setup(_ => _.AsCsv(It.Is<IEnumerable<dynamic>>(_ => _.SequenceEqual(transformedRows)), outputHeaders))
                .Returns(csv);
        }

        private static RandomString NewRandomString()
        {
            return new RandomString();
        }

        private async Task WhenTheCsvIsGenerated()
        {
            await _service.Run(_message);
        }

        private void AndTheMessageProperties(params (string, string)[] properties)
        {
            GivenTheMessageProperties(properties);
        }

        private void GivenTheMessageProperties(params (string, string)[] properties)
        {
            _message.AddUserProperties(properties);
        }

        private ProviderConverterDetail NewConverter(Action<ProviderConverterDetailBuilder> setUp = null)
        {
            ProviderConverterDetailBuilder builder = new ProviderConverterDetailBuilder();

            setUp?.Invoke(builder);

            return builder.Build();
        }
    }
}