using Microsoft.VisualStudio.TestTools.UnitTesting;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.DataImporter;
using Microsoft.Azure.Storage.Blob;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.DataImporter.Models;
using FluentAssertions;
using Moq;
using OfficeOpenXml;
using Serilog.Core;
using CalculateFunding.Models.Datasets;

namespace CalculateFunding.Services.Datasets.Services
{
    [TestClass]
    public class DatasetDataMergeServiceTests
    {
        private Mock<IBlobClient> _blobClient;
        
        private DatasetDataMergeService _service;

        [TestInitialize]
        public void SetUp()
        {
            _blobClient = new Mock<IBlobClient>();

            _service = new DatasetDataMergeService(_blobClient.Object, 
                Logger.None, 
                new ExcelDatasetReader(), 
                new DataDefinitionExcelWriter(), 
                DatasetsResilienceTestHelper.GenerateTestPolicies());
        }

        [DataTestMethod]
        [DataRow("PE_and_Sports_Grant_Data_v1.xlsx", "PE_and_Sports_Grant_Data_v2.xlsx", "PE_and_Sports_Grant_Data_v2.xlsx", "PE_and_Sports_Grant_Data_result.xlsx", "TestDatasetDefinition_PSG.json", DatasetEmptyFieldEvaluationOption.NA)]
        [DataRow("DSG_Rate_and_Baselines_data_V1.xlsx", "DSG_Rate_and_Baselines_data_V2.xlsx", "DSG_Rate_and_Baselines_data_V2.xlsx", "DSG_Rate_and_Baselines_data_result.xlsx", "TestDatasetDefinition_DSG.json", DatasetEmptyFieldEvaluationOption.NA)]
        [DataRow("PE_and_Sports_Grant_Data_Ignore_v1.xlsx", "PE_and_Sports_Grant_Data_Ignore_v2.xlsx", "PE_and_Sports_Grant_Data_Ignore_v2.xlsx", "PE_and_Sports_Grant_Data_Ignore_result.xlsx", "TestDatasetDefinition_PSG.json", DatasetEmptyFieldEvaluationOption.Ignore)]
        [DataRow("PE_and_Sports_Grant_Data_AsNull_v1.xlsx", "PE_and_Sports_Grant_Data_AsNull_v2.xlsx", "PE_and_Sports_Grant_Data_AsNull_v2.xlsx", "PE_and_Sports_Grant_Data_AsNull_result.xlsx", "TestDatasetDefinition_PSG.json", DatasetEmptyFieldEvaluationOption.AsNull)]
        public async Task Merge_ShouldGetTheNewAndUpdatedData(
            string latestBlobFileName, 
            string blobFileNameToMerge, 
            string blobFileNameFinal, 
            string resultsFile, 
            string definitionFileName,
            DatasetEmptyFieldEvaluationOption datasetEmptyFieldEvaluationOption)
        {
            DatasetDefinition datasetDefinition = GetDatasetDefinitionByName(definitionFileName);

            await using Stream latestDatasetStream = File.OpenRead($"TestItems{Path.DirectorySeparatorChar}{latestBlobFileName}");

            Mock<ICloudBlob> latestFileBlob = new Mock<ICloudBlob>();
            
            _blobClient.Setup(_ => _  
                .GetBlobReferenceFromServerAsync(latestBlobFileName))
                .ReturnsAsync(latestFileBlob.Object);
            
            _blobClient.Setup(_ => _
                .DownloadToStreamAsync(latestFileBlob.Object))
                .ReturnsAsync(latestDatasetStream);

            await using Stream fileToMergeDatasetStream = File.OpenRead($"TestItems{Path.DirectorySeparatorChar}{blobFileNameToMerge}");

            Mock<ICloudBlob> fileToMergeBlob = new Mock<ICloudBlob>();
            _blobClient.Setup(_ => _
                .GetBlobReferenceFromServerAsync(blobFileNameToMerge))
                .ReturnsAsync(fileToMergeBlob.Object);
            _blobClient.Setup(_ => _
                .DownloadToStreamAsync(fileToMergeBlob.Object))
                .ReturnsAsync(fileToMergeDatasetStream);

            await using MemoryStream uploadedStream = new MemoryStream();

            fileToMergeBlob.Setup(_ => _.UploadFromStreamAsync(It.IsAny<Stream>()))
                .Callback<Stream>(_ =>
                {
                    _?.Seek(0, SeekOrigin.Begin);
                    // ReSharper disable once AccessToDisposedClosure
                    _?.CopyTo(uploadedStream);
                });

            DatasetDataMergeResult result = await _service.Merge(datasetDefinition, latestBlobFileName, blobFileNameFinal, datasetEmptyFieldEvaluationOption);

            result.TablesMergeResults.Count().Should().Be(1);

            await using Stream expectedResultStream = File.OpenRead($"TestItems{Path.DirectorySeparatorChar}{resultsFile}");

            using ExcelPackage expected = new ExcelPackage(expectedResultStream);
            using ExcelPackage actual = new ExcelPackage(uploadedStream);

            ExcelWorksheet expectedWorksheet = expected.Workbook.Worksheets[1];
            ExcelWorksheet actualWorksheet = expected.Workbook.Worksheets[1];

            for (int i = 1; i <= expectedWorksheet.Dimension.Rows; i++)
            {
                for (int j = 1; j <= expectedWorksheet.Dimension.Columns; j++)
                {
                    actualWorksheet.Cells[i, j].Value.Should().Be(expectedWorksheet.Cells[i, j].Value);
                }
            }
        }

        private static DatasetDefinition GetDatasetDefinitionByName(string datasetDefinitionName)
            => File.ReadAllText($"DatasetDefinitions{Path.DirectorySeparatorChar}{datasetDefinitionName}")
                .AsPoco<DatasetDefinition>();
    }
}
