using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using CalculateFunding.Models;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.Specs;
using OfficeOpenXml;
using FieldType = CalculateFunding.Models.Datasets.Schema.FieldType;

namespace CalculateFunding.Services.DataImporter
{
	public enum ProviderIdentifierType
	{
		UKPRN,
	}
	public class ProviderData
	{
		public ProviderIdentifierType ProviderIdentifierType { get; set; }
		public string ProviderIdentifier { get; set; }
		public Reference Dataset { get; set; }
		public TableDefinition TabeDefinition { get; set; }
		public Dictionary<string, object> Values { get; set; }
	}
    public static class EppLusExtensions
    {
        public static IEnumerable<TTarget> ConvertSheetToObjects<TTarget>(this ExcelWorksheet worksheet,TableDefinition tableDefinition) where TTarget : new()
        {
        //    bool ColumnOnly(CustomAttributeData y) => y.AttributeType == typeof(SourceColumnAttribute);
        //    int i = 1;
        //    var columns = typeof(TTarget)
        //            .GetProperties()
        //            .OrderBy(x => x.MetadataToken)
        //            .Where(x => x.CustomAttributes.Any(ColumnOnly))
        //    .Select(p => new
        //    {
        //        Property = p,
        //        ColumnName = p.GetCustomAttributes<SourceColumnAttribute>().First().ColumnName.ToLowerInvariant()
        //}).ToList();




            var rows = worksheet.Cells
                .Select(cell => cell.Start.Row)
                .Distinct()
                .OrderBy(x => x);

            var headerRow = worksheet.Cells.First();
            var start = worksheet.Dimension.Start;
            var end = worksheet.Dimension.End;
            var columnNames = new HashSet<string>(tableDefinition.FieldDefinitions.Select(x => x.Name).Distinct());
            var headerDictionary = new Dictionary<string, int>();
            for (int col = start.Column; col <= end.Column; col++)
            {
                var val = worksheet.Cells[1, col];
                var valAsString = val.GetValue<string>()?.ToLowerInvariant();
                if (valAsString != null && columnNames.Contains(valAsString))
                {
                    headerDictionary.Add(valAsString, col);
                }
                
            }

			var providerRecords = new Dictionary<string, ProviderData>();

            foreach (var row in rows.Skip(1))
            {

				
	            var identifierCell = worksheet.Cells[row, headerDictionary[tableDefinition.IdentifierFieldName]];
	            var identifier = identifierCell.GetValue<string>();

	            foreach (var fieldDefinition in tableDefinition.FieldDefinitions)
	            {
		            switch (fieldDefinition.Type)
		            {
			            case FieldType.Boolean:
				            break;
			            case FieldType.Char:
				            break;
			            case FieldType.Byte:
				            break;
			            case FieldType.Integer:
				            break;
			            case FieldType.Float:
				            break;
			            case FieldType.Decimal:
				            break;
			            case FieldType.DateTime:
				            break;
			            case FieldType.String:
				            break;
			            default:
				            throw new ArgumentOutOfRangeException();
		            }
	            }

					var tnew = new TTarget();
	        //    tableDefinition.FieldDefinitions.ForEach(col =>
            //        {
                    //This is the real wrinkle to using reflection - Excel stores all numbers as double including int
              //      var val = worksheet.Cells[row, headerDictionary[col.Name]];
                    //If it is numeric it is a double since that is how excel stores all numbers
       //             if (val.Value == null)
       //                 {
	      //                  expando[""]

							//col.Property.SetValue(tnew, null);
       //                     return;
       //                 }
       //                 if (col.Property.PropertyType == typeof(Int32))
       //                 {
       //                     col.Property.SetValue(tnew, val.GetValue<int>());
       //                     return;
       //                 }
       //                 if (col.Property.PropertyType == typeof(double))
       //                 {
       //                     col.Property.SetValue(tnew, val.GetValue<double>());
       //                     return;
       //                 }
       //                 if (col.Property.PropertyType == typeof(decimal))
       //                 {
       //                     col.Property.SetValue(tnew, val.GetValue<decimal>());
       //                     return;
       //                 }
       //                 if (col.Property.PropertyType == typeof(DateTime))
       //                 {
       //                     col.Property.SetValue(tnew, val.GetValue<DateTime>());
       //                     return;
       //                 }
       //                 if (col.Property.PropertyType == typeof(DateTimeOffset))
       //                 {
       //                     col.Property.SetValue(tnew, val.GetValue<DateTimeOffset>());
       //                     return;
       //                 }
       //                 //Its a string
       //                 col.Property.SetValue(tnew, val.GetValue<string>());
       //             });

            //        yield return tnew;
                }

	        return null;
	        //Send it back

        }
    }

}
