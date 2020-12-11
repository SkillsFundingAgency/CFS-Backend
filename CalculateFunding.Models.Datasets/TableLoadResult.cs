using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Datasets.Schema;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Datasets
{
    public class TableLoadResult
    {
        [JsonProperty("tableDefinition")]
        public TableDefinition TableDefinition { get; set; }

        [JsonProperty("globalErrors")]
        public List<DatasetValidationError> GlobalErrors { get; set; }

        [JsonProperty("rows")]
        public List<RowLoadResult> Rows { get; set; }

        public RowLoadResult GetRowWithMatchingIdentifier(RowLoadResult other)
            => Rows?.FirstOrDefault(_ => _.HasSameIdentifier(other));

        public IEnumerable<RowLoadResult> GetRowsWhereFieldsDifferFromMatchIn(TableLoadResult other)
            => Rows.Where(_ => other.Rows.Any(row => row.HasSameIdentifier(_) &&
                                                     row.HasDifferentFieldValues(_)));
        
        public IEnumerable<RowLoadResult> GetRowsMissingFrom(TableLoadResult other)
            => Rows.Where(_ => other.GetRowWithMatchingIdentifier(_) == null);

        public void AddRows(IEnumerable<RowLoadResult> rows)
        {
            Rows ??= new List<RowLoadResult>();
            
            Rows.AddRange(rows);
        }

        public void UpdateMatchingRowsWithFieldsValuesFrom(IEnumerable<RowLoadResult> otherRows)
        {
            foreach (RowLoadResult rowLoadResult in otherRows)
            {
                RowLoadResult match = GetRowWithMatchingIdentifier(rowLoadResult);

                match.Fields = rowLoadResult.Fields;
            }
        }
    }
}