using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Migrations.Specification.Etl.Migrations
{
    public class Container
    {
        public string Name { get; set; }

        public string Query { get; set; }

        public bool HasPreReqs { get; set; }

        public bool HasPost { get; set; }

        public int MaxThroughPut { get; set; }

        public string PartitionKey { get; set; }
    }
}
