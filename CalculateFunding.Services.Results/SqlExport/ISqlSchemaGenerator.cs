using CalculateFunding.Services.SqlExport.Models;
using System.Collections.Generic;

namespace CalculateFunding.Services.Results.SqlExport
{
    public interface ISqlSchemaGenerator
    {
        string GenerateCreateTableSql(string tableName, string specificationId, IEnumerable<SqlColumnDefinition> fields, bool isSpecificationTable = false);
        
        string GenerateCreateTableSql(string tableName, IEnumerable<SqlColumnDefinition> fields, bool isSpecificationTable = false);
    }
}