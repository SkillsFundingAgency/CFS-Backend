using System.Collections.Generic;
using System.IO;
using CalculateFunding.Models.Datasets.Schema;
using OfficeOpenXml;

namespace CalculateFunding.Services.DataImporter
{
    public interface IExcelDatasetReader
    {
        IEnumerable<TableLoadResult> Read(Stream stream, DatasetDefinition datasetDefinition);

        TableLoadResult Read(ExcelPackage excelPackage, DatasetDefinition datasetDefinition);
    }
}