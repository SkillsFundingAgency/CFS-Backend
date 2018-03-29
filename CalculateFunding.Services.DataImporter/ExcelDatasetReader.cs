using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using Newtonsoft.Json;
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
	           yield return ConvertSheetToObjects(excel.Workbook.Worksheets.First(), datasetDefinition.TableDefinitions.First());
            }
	        else
	        {
	            foreach (var tableDefinition in datasetDefinition.TableDefinitions)
	            {
	                var workSheet = excel.Workbook.Worksheets.First(x => Regex.IsMatch(x.Name, WildCardToRegular(tableDefinition.Name)));
	                yield return ConvertSheetToObjects(workSheet, tableDefinition);
	            }
            }     
	    }

        private static TableLoadResult ConvertSheetToObjects(ExcelWorksheet worksheet, TableDefinition tableDefinition)
        {
            var result = new TableLoadResult
            {
                GlobalErrors = new List<DatasetValidationError>(),
                TableDefinition = tableDefinition,
                Rows = new List<RowLoadResult>()
            };

            var rows = worksheet.Cells
                .Select(cell => cell.Start.Row)
                .Distinct()
                .OrderBy(x => x);

            var headerDictionary = MatchHeaderColumns(worksheet, tableDefinition);

            foreach (var fieldDefinition in tableDefinition.FieldDefinitions)
            {
                if (fieldDefinition.Required && !headerDictionary.ContainsKey(fieldDefinition.Name))
                {
                    result.GlobalErrors.Add(new DatasetValidationError(fieldDefinition, 0, $"Required column '{fieldDefinition.Name}' cannot be found"));
                }
            }

            foreach (var row in rows.Skip(1))
            {
                var rowResult = LoadRow(worksheet, tableDefinition, headerDictionary, row);

                result.Rows.Add(rowResult);
            }

            return result;
        }

        private static RowLoadResult LoadRow(ExcelWorksheet worksheet, TableDefinition tableDefinition, Dictionary<string, int> headerDictionary, int row)
        {
            var rowResult = new RowLoadResult
            {
                Fields = new Dictionary<string, object>(),
                ValidationErrors = new List<DatasetValidationError>()
            };

            foreach (var fieldDefinition in tableDefinition.FieldDefinitions)
            {
                if (headerDictionary.ContainsKey(fieldDefinition.Name))
                {
                    var dataCell = worksheet.Cells[row, headerDictionary[fieldDefinition.Name]];
                    if (dataCell.GetValue<object>() == null && fieldDefinition.Required)
                    {
                        rowResult.ValidationErrors.Add(new DatasetValidationError(fieldDefinition, row,
                            $"Required field {fieldDefinition.Name} is null"));
                    }
                    else
                    {
                        if (IsFieldValid(fieldDefinition, rowResult, dataCell))
                        {
                            PopulateField(fieldDefinition, rowResult, dataCell);

                            if (fieldDefinition.IdentifierFieldType.HasValue
                                && fieldDefinition.IdentifierFieldType.Value != IdentifierFieldType.None
                                && string.IsNullOrWhiteSpace(rowResult.Identifier))
                            {
                                rowResult.Identifier = rowResult.Fields[fieldDefinition.Name] as string;
                                rowResult.IdentifierFieldType = fieldDefinition.IdentifierFieldType.Value;
                            }
                        }

                    }
                }
            }

            return rowResult;
        }

        private static bool IsFieldValid(FieldDefinition fieldDefinition, RowLoadResult rowResult, ExcelRange dataCell)
        {
            return true; // Matt - TODO
        }

        private static void PopulateField(FieldDefinition fieldDefinition, RowLoadResult rowResult, ExcelRange dataCell)
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
                    rowResult.Fields.Add(fieldDefinition.Name, dataCell.GetValue<DateTime>());
                    break;
                default:
                    rowResult.Fields.Add(fieldDefinition.Name, dataCell.GetValue<string>());
                    break;
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