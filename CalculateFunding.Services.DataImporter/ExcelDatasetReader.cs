using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Services.DataImporter.Validators.Models;
using OfficeOpenXml;

namespace CalculateFunding.Services.DataImporter
{
    public class ExcelDatasetReader : IExcelDatasetReader
    {
        public IEnumerable<TableLoadResult> Read(Stream stream, DatasetDefinition datasetDefinition)
        {
            ExcelPackage excel = new ExcelPackage(stream);

            // If only one table defined in each then match it
	        if (datasetDefinition.TableDefinitions.Count == 1 && excel.Workbook.Worksheets.Count == 1)
	        {
	           yield return ConvertSheetToObjects(excel.Workbook.Worksheets.First(), datasetDefinition.TableDefinitions.First()).TableLoadResult;
            }
	        else
	        {
	            foreach (var tableDefinition in datasetDefinition.TableDefinitions)
	            {
	                var workSheet = excel.Workbook.Worksheets.First(x => Regex.IsMatch(x.Name, WildCardToRegular(tableDefinition.Name)));
	                yield return ConvertSheetToObjects(workSheet, tableDefinition).TableLoadResult;
	            }
            }     
	    }

        public TableLoadResultWithHeaders Read(ExcelPackage excelPackage, DatasetDefinition datasetDefinition, bool parse)
        {
             if (datasetDefinition.TableDefinitions.Count == 1 && excelPackage.Workbook.Worksheets.Count == 1)
            {
                return ConvertSheetToObjects(excelPackage.Workbook.Worksheets.First(), datasetDefinition.TableDefinitions.First(), parse);
            }

            return null;
        }

        private static TableLoadResultWithHeaders ConvertSheetToObjects(ExcelWorksheet worksheet, TableDefinition tableDefinition, bool parse = true)
        {
	        var result = new TableLoadResultWithHeaders()
	        {
		        TableLoadResult = new TableLoadResult
		        {
			        TableDefinition = tableDefinition,
			        Rows = new List<RowLoadResult>()
		        },
				RetrievedHeaderFields = new Dictionary<string, int>()
	        };

            IOrderedEnumerable<int> rows = worksheet.Cells
                .Select(cell => cell.Start.Row)
                .Distinct()
                .OrderBy(x => x);

            var headerDictionary = MatchHeaderColumns(worksheet, tableDefinition);

            foreach (var row in rows.Skip(1))
            {
                var rowResult = LoadRow(worksheet, tableDefinition, headerDictionary, row, parse);

                result.TableLoadResult.Rows.Add(rowResult);
            }

	        if (!headerDictionary.IsNullOrEmpty())
	        {
		        result.RetrievedHeaderFields = headerDictionary;
	        }

	        return result;
        }

        private static RowLoadResult LoadRow(ExcelWorksheet worksheet, TableDefinition tableDefinition, Dictionary<string, int> headerDictionary, int row, bool shouldCheckType)
        {
            var rowResult = new RowLoadResult
            {
                Fields = new Dictionary<string, object>()
            };

            foreach (var fieldDefinition in tableDefinition.FieldDefinitions)
            {
                if (headerDictionary.ContainsKey(fieldDefinition.Name))
                {
                    var dataCell = worksheet.Cells[row, headerDictionary[fieldDefinition.Name]];

					PopulateField(fieldDefinition, rowResult, dataCell, shouldCheckType);

					if (fieldDefinition.IdentifierFieldType.HasValue
                        && fieldDefinition.IdentifierFieldType.Value != IdentifierFieldType.None
                        && string.IsNullOrWhiteSpace(rowResult.Identifier))
                    {
                        rowResult.Identifier = rowResult.Fields[fieldDefinition.Name]?.ToString();
                        rowResult.IdentifierFieldType = fieldDefinition.IdentifierFieldType.Value;
                    }
                }
            }

            return rowResult;
        }

        static void PopulateField(FieldDefinition fieldDefinition, RowLoadResult rowResult, ExcelRange dataCell, bool shouldCheckType)
        {
	        if (!shouldCheckType)
	        {
		        rowResult.Fields.Add(fieldDefinition.Name, dataCell.Value);
			}
	        else
	        {
		        switch (fieldDefinition.Type)
		        {
			        case FieldType.Boolean:
				        rowResult.Fields.Add(fieldDefinition.Name, dataCell.GetValue<bool>());
				        break;
			        case FieldType.Integer:
				        rowResult.Fields.Add(fieldDefinition.Name, dataCell.GetValue<int>());
				        break;
			        case FieldType.Float:
				        rowResult.Fields.Add(fieldDefinition.Name, dataCell.GetValue<double>());
				        break;
			        case FieldType.Decimal:
				        rowResult.Fields.Add(fieldDefinition.Name, dataCell.GetValue<decimal>());
				        break;
			        case FieldType.DateTime:
				        try
				        {
					        string valueAsString = dataCell.GetValue<string>();
					        if (!string.IsNullOrWhiteSpace(valueAsString))
					        {
						        rowResult.Fields.Add(fieldDefinition.Name, dataCell.GetValue<DateTime>());
					        }
				        }
				        catch (InvalidCastException)
				        {

				        }

				        break;
			        default:
				        rowResult.Fields.Add(fieldDefinition.Name, dataCell.GetValue<string>());
				        break;
		        }
	        }
        }

        private static Dictionary<string, int> MatchHeaderColumns(ExcelWorksheet worksheet, TableDefinition tableDefinition)
        {
            var start = worksheet.Dimension.Start;
            var end = worksheet.Dimension.End;

            var headerDictionary = new Dictionary<string, int>();
            for (int col = start.Column; col <= end.Column; col++)
            {
                var val = worksheet.Cells[1, col];
                var valAsString = val.GetValue<string>()?.ToLowerInvariant();
                if (valAsString != null)
                {
                    AddToDictionary(headerDictionary, tableDefinition, valAsString, col);
                }
            }

            return headerDictionary;
        }

        public static void AddToDictionary(IDictionary<string, int> headers, TableDefinition tableDefinition, string headerCellValue, int colIndex)
        {
            var columns = tableDefinition.FieldDefinitions.Where(x => MatchColumn(x, headerCellValue)).ToList();
            if (columns != null)
            {
                foreach (var column in columns)
                {
                    if (!headers.ContainsKey(column.Name))
                    {
                        headers.Add(column.Name, colIndex);
                    }
                }
            }
        }

        private static bool MatchColumn(FieldDefinition x, string valAsString)
        {
            return !string.IsNullOrEmpty(x.MatchExpression) ? Regex.IsMatch(valAsString, WildCardToRegular(x.MatchExpression)) : valAsString.ToLowerInvariant().Contains(x.Name.ToLowerInvariant());
        }


        private static String WildCardToRegular(String value)
        {
            return "^" + Regex.Escape(value).Replace("\\?", ".").Replace("\\*", ".*") + "$";
        }
    }
}