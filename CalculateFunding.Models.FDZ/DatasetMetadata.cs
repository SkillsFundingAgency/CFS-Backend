using System.Collections.Generic;

namespace CalculateFunding.Models.FDZ
{
    public class DatasetMetadata : Dataset
    {
        public IEnumerable<FieldMetadata> Fields { get; set; }
    }
}
