using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Repositories.Providers
{
    public class MergeStatementGenerator
    {
        public string MergeStatement = @"

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
    
    public static string SchemaName { get; set; }
    public string MergeStatementTemplate = $@"

MERGE [{SchemaName}].[Providers] AS tar
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
}
}
