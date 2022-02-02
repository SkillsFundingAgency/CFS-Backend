using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Services.Core.Extensions;
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
                    ExcelWorksheet workSheet = excel.Workbook.Worksheets.First(x => x.Name != "Errors");
                    if (workSheet != null)
                    {
                        yield return ConvertSheetToObjects(workSheet, tableDefinition).TableLoadResult;
                    }
	            }
            }     
	    }

        public TableLoadResultWithHeaders Read(ExcelPackage excelPackage, DatasetDefinition datasetDefinition, bool parse, bool includeUnmatchingColumn)
        {
            if (datasetDefinition.TableDefinitions.Count == 1 && excelPackage.Workbook.Worksheets.Count > 0)
            {
                return ConvertSheetToObjects(excelPackage.Workbook.Worksheets.First(), datasetDefinition.TableDefinitions.First(), parse, includeUnmatchingColumn);
            }

            return null;
        }

        private static TableLoadResultWithHeaders ConvertSheetToObjects(ExcelWorksheet worksheet, TableDefinition tableDefinition, bool parse = true, bool includeUnmatchingColumn = false)
        {
            TableLoadResultWithHeaders result = new TableLoadResultWithHeaders()
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

            (string[] rawHeaders, Dictionary<string, int> headerDictionary) columns = MatchHeaderColumns(worksheet, tableDefinition, includeUnmatchingColumn);

            foreach (int row in rows.Skip(1))
            {
                RowLoadResult rowResult = LoadRow(worksheet, tableDefinition, columns.headerDictionary, row, parse);

                result.TableLoadResult.Rows.Add(rowResult);
            }

	        if (!columns.headerDictionary.IsNullOrEmpty())
	        {
		        result.RetrievedHeaderFields = columns.headerDictionary;
	        }

	        result.RawHeaderFields = columns.rawHeaders;

	        return result;
        }

        private static RowLoadResult LoadRow(ExcelWorksheet worksheet, TableDefinition tableDefinition, Dictionary<string, int> headerDictionary, int row, bool shouldCheckType)
        {
            RowLoadResult rowResult = new RowLoadResult
            {
                Fields = new Dictionary<string, object>()
            };

            foreach (var fieldDefinition in tableDefinition.FieldDefinitions)
            {
                if (headerDictionary.ContainsKey(fieldDefinition.Name))
                {
                    ExcelRange dataCell = worksheet.Cells[row, headerDictionary[fieldDefinition.Name]];

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

        public static void PopulateField(FieldDefinition fieldDefinition, RowLoadResult rowResult, ExcelRange dataCell, bool shouldCheckType)
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
				        rowResult.Fields.Add(fieldDefinition.Name, dataCell.GetValue<int?>());
				        break;

			        case FieldType.Float:
				        rowResult.Fields.Add(fieldDefinition.Name, dataCell.GetValue<double?>());
				        break;

			        case FieldType.Decimal:
				        rowResult.Fields.Add(fieldDefinition.Name, dataCell.GetValue<decimal?>());
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

                    case FieldType.NullableOfDecimal:
                        dataCell.GetValue<string>().TryParseNullable(out decimal? parsedDecimal);
                        rowResult.Fields.Add(fieldDefinition.Name, parsedDecimal);
                        break;

                    case FieldType.NullableOfBoolean:
                        rowResult.Fields.Add(fieldDefinition.Name, dataCell.GetValue<string>().TryParseBoolean());
                        break;

                    case FieldType.NullableOfInteger:
                        dataCell.GetValue<string>().TryParseNullable(out int? parsedInt);
                        rowResult.Fields.Add(fieldDefinition.Name, parsedInt);
                        break;

                    default:
				        rowResult.Fields.Add(fieldDefinition.Name, dataCell.GetValue<string>());
				        break;
		        }
	        }
        }

        private static (string[] rawHeaders, Dictionary<string, int>) MatchHeaderColumns(ExcelWorksheet worksheet, TableDefinition tableDefinition, bool includeUnmatchingColumn)
        {
            ExcelCellAddress start = worksheet.Dimension.Start;
            ExcelCellAddress end = worksheet.Dimension.End;

            List<string> rawHeaders = new List<string>();
            
            Dictionary<string, int> headerDictionary = new Dictionary<string, int>();
            for (int col = start.Column; col <= end.Column; col++)
            {
                ExcelRange val = worksheet.Cells[1, col];
                string valAsString = val.GetValue<string>()?.ToLowerInvariant();
                if (valAsString != null)
                {
                    AddToDictionary(headerDictionary, tableDefinition, valAsString, col, includeUnmatchingColumn);
                }
                rawHeaders.Add(valAsString);
            }

            return (rawHeaders.ToArray(), headerDictionary);
        }

        public static void AddToDictionary(IDictionary<string, int> headers, TableDefinition tableDefinition, string headerCellValue, int colIndex, bool includeUnmatchingColumn)
        {
            List<FieldDefinition> columns = tableDefinition.FieldDefinitions.Where(x => MatchColumn(x, headerCellValue)).ToList();

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

            if (includeUnmatchingColumn && (columns == null || columns.Count == 0))
            {
	            if (!headers.ContainsKey(headerCellValue))
	            {
		            headers.Add(headerCellValue, colIndex);
	            }
            }
        }

        private static bool MatchColumn(FieldDefinition x, string valAsString)
        {
            return !string.IsNullOrEmpty(x.MatchExpression) ? Regex.IsMatch(valAsString, WildCardToRegular(x.MatchExpression)) : string.Equals(valAsString.Trim(),x.Name.Trim(),StringComparison.InvariantCultureIgnoreCase);
        }

        private static String WildCardToRegular(String value)
        {
            return "^" + Regex.Escape(value).Replace("\\?", ".").Replace("\\*", ".*") + "$";
        }
    }
}