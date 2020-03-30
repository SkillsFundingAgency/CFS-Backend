using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Graph
{
    public class Entity<TNode, TRelationship>
    {
        public TNode Node { get; set; }
        public IEnumerable<TRelationship> Relationships { get; set; }
    }
}
