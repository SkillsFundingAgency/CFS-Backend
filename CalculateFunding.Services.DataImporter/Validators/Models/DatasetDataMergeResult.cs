using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CalculateFunding.Services.DataImporter.Models
{
    public class DatasetDataMergeResult
    {
        public DatasetDataMergeResult()
        {
            TablesMergeResults = new List<DatasetDataTableMergeResult>();
        }

        public string ErrorMessage { get; set; }

        public List<DatasetDataTableMergeResult> TablesMergeResults { get; private set; }

        public bool HasErrors => !string.IsNullOrWhiteSpace(ErrorMessage);

        public int TotalRowsCreated => TablesMergeResults.Sum(x => x.NewRowsCount);
        public int TotalRowsAmended => TablesMergeResults.Sum(x => x.UpdatedRowsCount);

        public string GetMergeResultsMessage()
        {
            StringBuilder messageBuilder = new StringBuilder();
            foreach (DatasetDataTableMergeResult result in TablesMergeResults.Where(x => x.NewRowsCount > 0 || x.UpdatedRowsCount > 0))
            {
                messageBuilder.AppendLine($"Worksheet '{result.TableDefinitionName}' merged. Updated rows count - {result.UpdatedRowsCount}. New rows count - {result.NewRowsCount}.");
            }

            return messageBuilder.ToString();
        }
    }
}
