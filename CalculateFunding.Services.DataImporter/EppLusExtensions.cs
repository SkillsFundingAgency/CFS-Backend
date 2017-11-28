using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OfficeOpenXml;

namespace Allocations.Services.DataImporter
{
    public static class EppLusExtensions
    {
        public static IEnumerable<TTarget> ConvertSheetToObjects<TTarget>(this ExcelWorksheet worksheet) where TTarget : new()
        {
            bool ColumnOnly(CustomAttributeData y) => y.AttributeType == typeof(SourceColumnAttribute);
            int i = 1;
            var columns = typeof(TTarget)
                    .GetProperties()
                    .OrderBy(x => x.MetadataToken)
                    .Where(x => x.CustomAttributes.Any(ColumnOnly))
            .Select(p => new
            {
                Property = p,
                ColumnName = p.GetCustomAttributes<SourceColumnAttribute>().First().ColumnName.ToLowerInvariant()
        }).ToList();


            var rows = worksheet.Cells
                .Select(cell => cell.Start.Row)
                .Distinct()
                .OrderBy(x => x);

            var headerRow = worksheet.Cells.First();
            var start = worksheet.Dimension.Start;
            var end = worksheet.Dimension.End;
            var columnNames = new HashSet<string>(columns.Select(x => x.ColumnName).Distinct());
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


 

            foreach (var row in rows.Skip(1))
            {
               
                    var tnew = new TTarget();
                    columns.ForEach(col =>
                    {
                    //This is the real wrinkle to using reflection - Excel stores all numbers as double including int
                    var val = worksheet.Cells[row, headerDictionary[col.ColumnName]];
                    //If it is numeric it is a double since that is how excel stores all numbers
                    if (val.Value == null)
                        {
                            col.Property.SetValue(tnew, null);
                            return;
                        }
                        if (col.Property.PropertyType == typeof(Int32))
                        {
                            col.Property.SetValue(tnew, val.GetValue<int>());
                            return;
                        }
                        if (col.Property.PropertyType == typeof(double))
                        {
                            col.Property.SetValue(tnew, val.GetValue<double>());
                            return;
                        }
                        if (col.Property.PropertyType == typeof(decimal))
                        {
                            col.Property.SetValue(tnew, val.GetValue<decimal>());
                            return;
                        }
                        if (col.Property.PropertyType == typeof(DateTime))
                        {
                            col.Property.SetValue(tnew, val.GetValue<DateTime>());
                            return;
                        }
                        if (col.Property.PropertyType == typeof(DateTimeOffset))
                        {
                            col.Property.SetValue(tnew, val.GetValue<DateTimeOffset>());
                            return;
                        }
                        //Its a string
                        col.Property.SetValue(tnew, val.GetValue<string>());
                    });

                    yield return tnew;
                }


            //Send it back

        }
    }

}
