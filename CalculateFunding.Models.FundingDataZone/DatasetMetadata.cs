using System.Collections.Generic;

namespace CalculateFunding.Models.FundingDataZone
{
    public class DatasetMetadata : Dataset
    {
        public IEnumerable<FieldMetadata> Fields { get; set; }
    }
}
