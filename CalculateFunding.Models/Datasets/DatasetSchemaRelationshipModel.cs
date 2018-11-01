using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Datasets
{
    public class DatasetSchemaRelationshipModel
    {
        public string DefinitionId { get; set; }
        public string RelationshipId { get; set; }
        public string RelationshipName { get; set; }
        public IEnumerable<DatasetSchemaRelationshipField> Fields { get; set; }
    }
}
