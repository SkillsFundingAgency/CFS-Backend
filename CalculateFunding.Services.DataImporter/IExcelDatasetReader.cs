using System.Collections.Generic;
using System.IO;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Services.DataImporter.Validators.Models;
using OfficeOpenXml;

namespace CalculateFunding.Services.DataImporter
{
    public interface IExcelDatasetReader
    {
        IEnumerable<TableLoadResult> Read(Stream stream, DatasetDefinition datasetDefinition);

	    TableLoadResultWithHeaders Read(ExcelPackage excelPackage, DatasetDefinition datasetDefinition);
    }
}