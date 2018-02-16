using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Specs.Messages
{
    public class AssignDefinitionRelationshipMessage
    {
        public string SpecificationId { get; set; }

        public string RelationshipId { get; set; }
    }
}
