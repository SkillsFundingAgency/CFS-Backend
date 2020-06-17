using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Api.External.V3.Models
{
    public class PublishedFundingTemplate
    {
        public string MajorVersion { get; set; }
        public string MinorVersion { get; set; }        
        public string PublishNote { get; set; }
        public string AuthorName { get; set; }
        public DateTime PublishDate { get; set; }
        public string SchemaVersion { get; set; }
    }
}
