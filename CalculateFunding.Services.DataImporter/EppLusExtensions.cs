using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using CalculateFunding.Models;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.Specs;
using Microsoft.Azure.Documents.SystemFunctions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using OfficeOpenXml;
using OfficeOpenXml.FormulaParsing.Excel.Functions.RefAndLookup;
using FieldType = CalculateFunding.Models.Datasets.Schema.FieldType;

namespace CalculateFunding.Services.DataImporter
{

    public class TableLoadResult
    {
        [JsonProperty("tableDefinition")]
        public TableDefinition TableDefinition { get; set; }

        [JsonProperty("globalErrors")]
        public List<DatasetValidationError> GlobalErrors { get; set; }

        [JsonProperty("rows")]
        public List<RowLoadResult> Rows { get; set; }
    }

	public class RowLoadResult
	{
	    [JsonProperty("identifier")]
        public string Identifier { get; set; }
	    [JsonProperty("identifierFieldType")]
        public IdentifierFieldType IdentifierFieldType { get; set; }
	    [JsonProperty("fields")]
        public Dictionary<string, object> Fields { get; set; }
	    [JsonProperty("validationErrors")]
        public List<DatasetValidationError> ValidationErrors { get; set; }
	}

    public static class EPPlusExtensions
    {

        public static TableLoadResult ConvertSheetToObjects(this ExcelWorksheet worksheet,TableDefinition tableDefinition) 
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

            var start = worksheet.Dimension.Start;
            var end = worksheet.Dimension.End;

            var headerDictionary = new Dictionary<string, int>();
            for (int col = start.Column; col <= end.Column; col++)
            {
                var val = worksheet.Cells[1, col];
                var valAsString = val.GetValue<string>()?.ToLowerInvariant();
                if (valAsString != null)
                {
                    var column = tableDefinition.FieldDefinitions.FirstOrDefault(x => MatchColumn(x, valAsString));
                    if (column != null)
                    {
                        if (!headerDictionary.ContainsKey(column.Name))
                        {
                            headerDictionary.Add(column.Name, col);
                        }

                    }


                }
                
            }

            foreach (var fieldDefinition in tableDefinition.FieldDefinitions)
            {
                if (fieldDefinition.Required && !headerDictionary.ContainsKey(fieldDefinition.Name))
                {
                    result.GlobalErrors.Add(new DatasetValidationError(fieldDefinition, 0, $"Required column '{fieldDefinition.Name}' cannot be found"));
                }
            }


            foreach (var row in rows.Skip(1))
            {
                var rowResult = new RowLoadResult{Fields = new Dictionary<string, object>(), ValidationErrors = new List<DatasetValidationError>()};

                foreach (var fieldDefinition in tableDefinition.FieldDefinitions)
                {
                    if (headerDictionary.ContainsKey(fieldDefinition.Name))
                    {
                        var dataCell = worksheet.Cells[row, headerDictionary[fieldDefinition.Name]];
                        if (dataCell.GetValue<object>() == null && fieldDefinition.Required)
                        {
                            rowResult.ValidationErrors.Add(new DatasetValidationError(fieldDefinition, row, $"Required field {fieldDefinition.Name} is null"));
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
                                    rowResult.Fields.Add(fieldDefinition.Name, dataCell.GetValue<DateTime>());
                                    break;
                                default:
                                    rowResult.Fields.Add(fieldDefinition.Name, dataCell.GetValue<string>());
                                    break;
                            }

                            if (fieldDefinition.IdentifierFieldType.HasValue 
                                && fieldDefinition.IdentifierFieldType.Value != IdentifierFieldType.None 
                                &&  string.IsNullOrWhiteSpace(rowResult.Identifier))
                            {
                                rowResult.Identifier = rowResult.Fields[fieldDefinition.Name] as string;
                                rowResult.IdentifierFieldType = fieldDefinition.IdentifierFieldType.Value;
                            }
                        }

                    }
                }

                result.Rows.Add(rowResult);
            }

            return result;
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
