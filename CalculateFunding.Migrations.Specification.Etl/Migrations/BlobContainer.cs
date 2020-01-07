using CalculateFunding.Common.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Migrations.Specification.Etl.Migrations
{
    public class BlobContainer
    {
        public IEnumerable<string> Blobs { get; set; }

        public string Name { get; set; }

        public string AccountName { get; set; }

        public string AccountKey { get; set; }
    }
}
