using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Datasets
{
    public class CreateDefinitionSpecificationRelationshipModel
    {
        public string DatasetDefinitionId { get; set; }

        public string SpecificationId { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }
    }
}
