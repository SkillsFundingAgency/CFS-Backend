using Microsoft.VisualStudio.TestTools.UnitTesting;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.DataImporter;
using Microsoft.Azure.Storage.Blob;
using Serilog;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NSubstitute;
using Newtonsoft.Json.Linq;
using CalculateFunding.Services.Datasets.Interfaces;
using FluentAssertions;
using OfficeOpenXml;

namespace CalculateFunding.Services.Datasets.Services
{
    [TestClass]
    public class DatasetDataMergeServiceTests
    {
        private IDatasetDataMergeService _service;
        private IBlobClient _blobClient;
        private ILogger _logger;
        private IExcelDatasetReader _excelDatasetReader;

        public DatasetDataMergeServiceTests()
        {
            _blobClient = Substitute.For<IBlobClient>();
            _logger = Substitute.For<ILogger>();
            _excelDatasetReader = new ExcelDatasetReader();
            IExcelDatasetWriter excelDatasetWriter = new DataDefinitionExcelWriter();
            IDatasetsResiliencePolicies datasetsResiliencePolicies = DatasetsResilienceTestHelper.GenerateTestPolicies();

            _service = new DatasetDataMergeService(_blobClient, _logger, _excelDatasetReader, excelDatasetWriter, datasetsResiliencePolicies);
        }

        [DataTestMethod]
        [DataRow("PE_and_Sports_Grant_Data_v1.xlsx", "PE_and_Sports_Grant_Data_v2.xlsx", "PE_and_Sports_Grant_Data_result.xlsx", "TestDatasetDefinition_PSG.json")]
        [DataRow("DSG_Rate_and_Baselines_data_V1.xlsx", "DSG_Rate_and_Baselines_data_V2.xlsx", "DSG_Rate_and_Baselines_data_result.xlsx", "TestDatasetDefinition_DSG.json")]
        public async Task Merge_ShouldGetTheNewAndUpdatedData(string latestBlobFileName, string blobFileNameToMerge, string resultsFile, string definitionFileName)
        {
            DatasetDefinition datasetDefinition = GetDatasetDefinitionByName(definitionFileName);

            using Stream latestDatasetStream = File.OpenRead($"TestItems{Path.DirectorySeparatorChar}{latestBlobFileName}");

            ICloudBlob latestFileBlob = Substitute.For<ICloudBlob>();
            _blobClient.GetBlobReferenceFromServerAsync(latestBlobFileName)
                .Returns(latestFileBlob);
            _blobClient.DownloadToStreamAsync(Arg.Is(latestFileBlob))
                .Returns(latestDatasetStream);

            using Stream fileToMergeDatasetStream = File.OpenRead($"TestItems{Path.DirectorySeparatorChar}{blobFileNameToMerge}");

            ICloudBlob fileToMergeBlob = Substitute.For<ICloudBlob>();
            _blobClient.GetBlobReferenceFromServerAsync(blobFileNameToMerge)
                .Returns(fileToMergeBlob);
            _blobClient.DownloadToStreamAsync(Arg.Is(fileToMergeBlob))
                .Returns(fileToMergeDatasetStream);

            string actualResultFileName = Path.ChangeExtension(Path.GetTempFileName(), "xlsx");
            await fileToMergeBlob.UploadFromStreamAsync(Arg.Do<Stream>(mergedExcelStream =>
            {
                mergedExcelStream.Should().NotBeNull();

                using var fileStream = File.Create(actualResultFileName);
                mergedExcelStream.Seek(0, SeekOrigin.Begin);
                mergedExcelStream.CopyTo(fileStream);
            }));

            var result = await _service.Merge(datasetDefinition, latestBlobFileName, blobFileNameToMerge);

            result.TablesMergeResults.Count().Should().Be(1);

            using Stream expectedResultStream = File.OpenRead($"TestItems{Path.DirectorySeparatorChar}{resultsFile}");
            using Stream actualResultStream = File.OpenRead(actualResultFileName);

            using ExcelPackage expected = new ExcelPackage(expectedResultStream);
            using ExcelPackage actual = new ExcelPackage(actualResultStream);

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
        {
            JObject datasetDefinitionJson = JObject.Parse(File.ReadAllText($"DatasetDefinitions{Path.DirectorySeparatorChar}{datasetDefinitionName}"));

            DatasetDefinition datasetDefinition = datasetDefinitionJson.ToObject<DatasetDefinition>();

            return datasetDefinition;
        }

        private static Stream GetDatasetData(string datasetSourceFileName)
        {
            return File.OpenRead($"TestItems{Path.DirectorySeparatorChar}{datasetSourceFileName}");
        }
    }
}
