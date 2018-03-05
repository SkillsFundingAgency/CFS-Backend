using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CalculateFunding.Models.Datasets.Schema;
using OfficeOpenXml;

namespace CalculateFunding.Services.DataImporter
{
    public class ExcelReader
    {


	    public IEnumerable<TableLoadResult> Read(Stream stream, DatasetDefinition datasetDefinition) 
	    {
	        ExcelPackage excel = new ExcelPackage(stream);

            // If only one table defined then always take the first
	        if (datasetDefinition.TableDefinitions.Count == 1 && excel.Workbook.Worksheets.Any())
	        {
	           yield return excel.Workbook.Worksheets.First().ConvertSheetToObjects(datasetDefinition.TableDefinitions.First());
            }
	        else
	        {
	            foreach (var tableDefinition in datasetDefinition.TableDefinitions)
	            {
	                var workSheet = excel.Workbook.Worksheets.First(x => Regex.IsMatch(x.Name, WildCardToRegular(tableDefinition.Name)));
	                yield return workSheet.ConvertSheetToObjects(tableDefinition);
	            }
            }     
	    }

        private static string WildCardToRegular(string value)
        {
            return "^" + Regex.Escape(value).Replace("\\?", ".").Replace("\\*", ".*") + "$";
        }



    }
}