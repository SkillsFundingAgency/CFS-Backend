using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Datasets
{
    public class DatasetSchemaRelationshipField
    {
        public string Name { get; set; }

        public string SourceName { get; set; }

        public string SourceRelationshipName { get; set; }

        public bool IsAggregable { get; set; }

        public string FullyQualifiedSourceName
        {
            get
            {
                return $"Datasets.{SourceRelationshipName}.{SourceName}";
            }
        }
    }
}
