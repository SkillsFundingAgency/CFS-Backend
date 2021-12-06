using System.Collections.Generic;
using System.Text;
using CalculateFunding.Services.SqlExport.Models;

namespace CalculateFunding.Services.Results.SqlExport
{
    public class SqlSchemaGenerator : ISqlSchemaGenerator
    {
        public string GenerateCreateTableSql(string tableName,
            string specificationId,
            IEnumerable<SqlColumnDefinition> fields,
            bool isSpecificationTable = false)
        {
            string createTableSql = GenerateCreateTableSql(tableName, fields, isSpecificationTable);
            string createExtendedPropertiesSql = CreateExtendedPropertiesSql(tableName, specificationId);
            
            return $@"{createTableSql}

{createExtendedPropertiesSql}";
        }

        private string CreateExtendedPropertiesSql(string tableName,
            string specificationId) =>
            $@"EXEC sys.sp_addextendedproperty   
@name = N'CFS_SpecificationId',   
@value = N'{specificationId}',   
@level0type = N'SCHEMA', @level0name = 'dbo',  
@level1type = N'TABLE',  @level1name = '{tableName}';";

        public string GenerateCreateTableSql(
            string tableName, 
            IEnumerable<SqlColumnDefinition> fields,
            bool isSpecificationTable = false)
        {
            string fieldSql = GenerateFieldSql(fields);
            string primaryKeyFieldName = isSpecificationTable ? "SpecificationId": "ProviderId";

            string sql = $@" CREATE TABLE[dbo].[{tableName}](
           
               [{primaryKeyFieldName}][varchar](128) NOT NULL,
          
              {fieldSql}
 CONSTRAINT[PK_{tableName}_1] PRIMARY KEY CLUSTERED
(
   [{primaryKeyFieldName}] ASC
)WITH(PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON[PRIMARY]
) ON[PRIMARY];";

            return sql;
        }

        private string GenerateFieldSql(IEnumerable<SqlColumnDefinition> fields)
        {
            StringBuilder sb = new StringBuilder();

            foreach (SqlColumnDefinition field in fields)
            {
                sb.Append("[");
                sb.Append(field.Name);
                sb.Append("]");
                sb.Append(field.Type);
                sb.Append(field.AllowNulls ? " NULL" : " NOT NULL");
                sb.Append(",");
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
