using System.Collections.Generic;
using System.IO;
using System.Linq;
using CalculateFunding.Models.Datasets.Schema;
using OfficeOpenXml;

namespace CalculateFunding.Services.DataImporter
{
    public class ExcelReader
    {


	    public IEnumerable<TTarget> Read<TTarget>(Stream stream, DatasetDefinition datasetDefinition) where TTarget : new()
	    {
		    ExcelPackage excel = new ExcelPackage(stream);
		    foreach (var tableDefinition in datasetDefinition.TableDefinitions)
		    {
				var workSheet = excel.Workbook.Worksheets.First(x => x.Name == tableDefinition.Name);
			    return workSheet.ConvertSheetToObjects<TTarget>(tableDefinition);
			}
		    return null;


	    }


		
	}
}