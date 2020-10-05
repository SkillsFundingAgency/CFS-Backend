using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Graph
{
    public class DatasetDataFieldRelationship
    {
        public const string ToIdField = "HasDatasetField";
        public const string FromIdField = "IsInDataset";

        public Dataset Dataset { get; set; }
        public DataField DataField { get; set; }
    }
}
