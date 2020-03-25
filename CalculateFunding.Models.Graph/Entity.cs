using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Graph
{
    public class Entity<TRelatedNode>
    {
        public string Id { get; set; }
        public IEnumerable<TRelatedNode> Relationships { get; set; }
    }
}
