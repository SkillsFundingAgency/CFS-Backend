using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CalculateFunding.Repositories.Providers
{
    public class MergeStatementGenerator
    {
        public MergeStatementGenerator()
        {
            SchemaName = "dbo";
        }
        private string MergeStatement = @"

MERGE [dbo].[Providers] AS tar
USING (SELECT * FROM [dbo].[ProviderCommandCandidates] WHERE [ProviderCommandId] = '6516f069-0c3b-4533-aa96-6a23e76c7f84') AS src
ON tar.[URN] = src.[URN]
WHEN MATCHED 
	AND tar.Name <> src.Name
THEN
   UPDATE SET
      tar.Name = src.Name
WHEN NOT MATCHED THEN
   INSERT
   (
      [URN],
      [Name],
	  [CreatedAt],
	  [UpdatedAt],
	  [Deleted]
   )
   VALUES
   (
      src.[URN], 
	  src.[Name],
	  src.[CreatedAt],
	  src.[UpdatedAt],
	  src.[Deleted]
   )
WHEN NOT MATCHED BY SOURCE THEN
   DELETE
OUTPUT
   $action,
   inserted.*,
   deleted.*;
";
    

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
      [URN],
	  [CreatedAt],
	  [UpdatedAt],
	  [Deleted],
{RepeatForColumns("    [{0}]", ",")}
   )
   VALUES
   (
      src.[URN],
	  src.[CreatedAt],
	  src.[UpdatedAt],
	  src.[Deleted],
{RepeatForColumns("    src.[{0}]", ",")}
   )
{GetDeleteClause()}
OUTPUT
   $action,
   inserted.*,
   deleted.*;
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
