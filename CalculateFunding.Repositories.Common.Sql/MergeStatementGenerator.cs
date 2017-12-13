using System;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Repositories.Common.Sql
{
    public class MergeStatementGenerator
    {
        public MergeStatementGenerator()
        {
            SchemaName = "dbo";
        }

    public string GetMergeStatement() => $@"

MERGE [{SchemaName}].[{TargetTableName}] AS tar
USING (SELECT * FROM [dbo].[{SourceTableName}] WHERE [{CommandIdColumnName}] = @CommandId) AS src
ON tar.[{KeyColumnName}] = src.[{KeyColumnName}]
WHEN MATCHED 
{RepeatForColumns("    AND tar.[{0}] <> src.[{0}]")}
THEN
   UPDATE SET
{RepeatForColumns("    tar.[{0}] = src.[{0}]", ",")}
WHEN NOT MATCHED THEN
   INSERT
   (
      [{KeyColumnName}],
	  [CreatedAt],
	  [UpdatedAt],
	  [Deleted],
{RepeatForColumns("    [{0}]", ",")}
   )
   VALUES
   (
      src.[{KeyColumnName}],
	  src.[CreatedAt],
	  src.[UpdatedAt],
	  src.[Deleted],
{RepeatForColumns("    src.[{0}]", ",")}
   )
{GetDeleteClause()}
OUTPUT
   $action as Action,
   @CommandId as {CommandIdColumnName},
      inserted.[{KeyColumnName}],
	  inserted.[CreatedAt],
	  inserted.[UpdatedAt],
	  inserted.[Deleted],
      NULL AS Timestamp,
{RepeatForColumns("    inserted.[{0}]", ",")}
;
";
        public List<string> ColumnNames { get; set; }

        public string RepeatForColumns(string formatString, string separator = "")
        {
            var filteredColumns = ColumnNames.Where(x => x != KeyColumnName && !StandardColumns.Contains(x));
            return string.Join(separator + Environment.NewLine, filteredColumns.Select(x => string.Format(formatString, x)));
        }

        public string[] StandardColumns => new[] {"CreatedAt", "UpdatedAt", "Deleted", "Timestamp"};

        private string GetDeleteClause()
        {
            if (DeleteIfNotMatched)
            {
                return @"WHEN NOT MATCHED BY SOURCE THEN
                    DELETE";
            }
            return string.Empty;
        }

        public bool DeleteIfNotMatched { get; set; }

        public string KeyColumnName { get; set; }

        public string CommandIdColumnName { get; set; }

        public string SourceTableName { get; set; }

        public string SchemaName { get; set; }
        public string TargetTableName { get; set; }
    }
}
