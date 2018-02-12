using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Datasets
{
    public class DefinitionSpecificationRelationship : Reference
    {
        public Reference DatasetDefinition { get; set; }

        public Reference Specification { get; set; }

        public string Description { get; set; }
    }
}
