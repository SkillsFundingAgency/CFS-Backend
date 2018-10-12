using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Services.DataImporter;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Datasets.Services
{
    [TestClass]
    [Ignore("This work is still underway this test and method is tests is here as a placeholder, checking in as switching to other task")]
    public class ProcessDatasetServiceTests : ProcessDatasetServiceTestsBase
    {
        [TestMethod]
        public async Task WhenGenerateAggregationsHasSumOnSingleField_ThenResultsAreCalculated()
        {
            // Arrange
            DatasetDefinition datasetDefinition = new DatasetDefinition();
            List<TableDefinition> tableDefinitions = new List<TableDefinition>();
            datasetDefinition.TableDefinitions = tableDefinitions;

            TableDefinition tableDefinition = new TableDefinition()
            {

            };

            tableDefinitions.Add(tableDefinition);



            TableLoadResult tableLoadResult = new TableLoadResult();


            ProcessDatasetService service = CreateProcessDatasetService();

            // Act

            DatasetAggregations result = await service.GenerateAggregations(datasetDefinition, tableLoadResult);

            // Assert
            result
                .Should()
                .NotBeNull();
        }
    }
}
