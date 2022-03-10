using System.Collections.Generic;
using System.Linq;
using System.Text;
using CalculateFunding.Services.SqlExport.Models;

namespace CalculateFunding.Services.Publishing.SqlExport
{
    public class SqlSchemaGenerator : ISqlSchemaGenerator
    {
        public string GenerateCreateTableSql(string tableName,
            string fundingStreamId,
            string fundingPeriodId,
            IEnumerable<SqlColumnDefinition> fields)
        {
            string createTableSql = GenerateCreateTableSql(tableName, fields);
            string createExtendedPropertiesSql = CreateExtendedPropertiesSql(tableName, fundingStreamId, fundingPeriodId);
            
            return $@"{createTableSql}

{createExtendedPropertiesSql}";
        }

        private string CreateExtendedPropertiesSql(string tableName,
            string fundingStreamId,
            string fundingPeriodId) =>
            $@"EXEC sys.sp_addextendedproperty   
@name = N'CFS_FundingStreamId_FundingPeriodId',   
@value = N'{fundingStreamId}_{fundingPeriodId}',   
@level0type = N'SCHEMA', @level0name = 'dbo',  
@level1type = N'TABLE',  @level1name = '{tableName}';";

        public string GenerateCreateTableSql(string tableName, IEnumerable<SqlColumnDefinition> fields)
        {

            string fieldSql = GenerateFieldSql(fields);

            string primaryKey = string.Join(",", fields.Where(_ => _.PrimaryKeyMember).Select(_ => $"[{_.Name}]"));

            primaryKey = primaryKey != "" ? $",{primaryKey}" : "";

            string sql = $@" CREATE TABLE[dbo].[{tableName}](
           
               [PublishedProviderId][varchar](128) NOT NULL,
          
              {fieldSql}
 CONSTRAINT[PK_{tableName}_1] PRIMARY KEY CLUSTERED
(

   [PublishedProviderId]{primaryKey} ASC
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
