using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Graph
{
    public class DatasetDatasetDefinitionRelationship
    {
        public const string ToIdField = "HasDataset";
        public const string FromIdField = "IsForSchema";

        public Dataset Dataset { get; set; }
        public DatasetDefinition DatasetDefinition { get; set; }
    }
}
