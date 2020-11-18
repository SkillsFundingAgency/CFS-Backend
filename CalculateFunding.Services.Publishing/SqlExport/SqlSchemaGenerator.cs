using System.Collections.Generic;
using System.Text;
using CalculateFunding.Models.Publishing.SqlExport;

namespace CalculateFunding.Services.Publishing.SqlExport
{
    public class SqlSchemaGenerator : ISqlSchemaGenerator
    {
        public string GenerateCreateTableSql(string tableName, IEnumerable<SqlColumnDefinition> fields)
        {

            string fieldSql = GenerateFieldSql(fields);

            string sql = $@" CREATE TABLE[dbo].[{tableName}](
           
               [PublishedProviderId][varchar](128) NOT NULL,
          
              {fieldSql}
 CONSTRAINT[PK_{tableName}_1] PRIMARY KEY CLUSTERED
(

   [PublishedProviderId] ASC
)WITH(PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON[PRIMARY]
) ON[PRIMARY]";

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
