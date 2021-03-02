using System.Collections.Generic;
using CalculateFunding.Models.Publishing.SqlExport;

namespace CalculateFunding.Services.Publishing.SqlExport
{
    public interface ISqlSchemaGenerator
    {
        string GenerateCreateTableSql(string tableName, string fundingStreamId, string fundingPeriodId, IEnumerable<SqlColumnDefinition> fields);
        
        string GenerateCreateTableSql(string tableName, IEnumerable<SqlColumnDefinition> fields);
    }
}