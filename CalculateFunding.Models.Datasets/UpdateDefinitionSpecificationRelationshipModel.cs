using System.Collections.Generic;

namespace CalculateFunding.Models.Datasets
{
    public class UpdateDefinitionSpecificationRelationshipModel
    {
        public string Description { get; set; }

        public string RelationshipId { get; set; }

        public string SpecificationId { get; set; }

        public string TargetSpecificationId { get; set; }

        public IEnumerable<uint> FundingLineIds { get; set; }

        public IEnumerable<uint> CalculationIds { get; set; }
    }
}
