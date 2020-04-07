using System;
using System.Collections.Generic;

namespace CalculateFunding.Models.Specs
{
    public class ReportMetadata
    {
        public string Name { get; set; }
        public string BlobName { get; set; }
        public ReportType Type { get; set; }
        public IDictionary<string, string> Identifier { get; set; }
        public string Category { get; set; }
        public DateTimeOffset? LastModified { get; set; }
        public string Format { get; set; }
        public string Size { get; set; }
    }
}
