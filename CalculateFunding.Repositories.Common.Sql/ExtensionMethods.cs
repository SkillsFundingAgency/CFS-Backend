using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace CalculateFunding.Repositories.Common.Sql
{
    public static class ExtensionMethods
    {
        private static List<Type> Types
        {
            get
            {
                return new List<Type>
                {
                    typeof (String),
                    typeof (int?),
                    typeof (long?),
                    typeof (Guid?),
                    typeof (double?),
                    typeof (decimal?),
                    typeof (float?),
                    typeof (Single?),
                    typeof (bool?),
                    typeof (DateTime?),
                    typeof (DateTimeOffset?),
                    typeof (int),
                    typeof (long),
                    typeof (Guid),
                    typeof (double),
                    typeof (decimal),
                    typeof (float),
                    typeof (Single),
                    typeof (bool),
                    typeof (DateTime),
                    typeof (DateTimeOffset),
                    typeof (DBNull)
                };
            }
        }

        public static IEnumerable<SqlBulkCopyColumnMapping> GetColumnMappings<T>(this IEnumerable<T> source)
        {
            return typeof(T).GetProperties().Where(x => Types.Contains(x.PropertyType)).Select(x => new SqlBulkCopyColumnMapping(x.Name, x.Name));
        }


        public static DataTable ToDataTable<T>(this IEnumerable<T> source)
        {
            using (var dt = new DataTable())
            {
                var toList = source.ToList();

                var properties = typeof(T).GetProperties();

                for (var index = 0; index < properties.Count(); index++)
                {
                    var info = typeof(T).GetProperties()[index];
                    if (Types.Contains(info.PropertyType))
                    {
                        var type = (info.PropertyType.IsGenericType && info.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>) ? Nullable.GetUnderlyingType(info.PropertyType) : info.PropertyType);

                        dt.Columns.Add(new DataColumn(info.Name, type));
                    }
                }

                for (var index = 0; index < toList.Count; index++)
                {
                    var t = toList[index];
                    var row = dt.NewRow();
                    foreach (var info in properties)
                    {
                        if (Types.Contains(info.PropertyType))
                        {   
                            
                            row[info.Name] = info.GetValue(t, null) ?? DBNull.Value;
                        }
                    }
                    dt.Rows.Add(row);
                }

                return dt;
            }
        }
    }
}