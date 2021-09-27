using System;

namespace CalculateFunding.Models.Policy
{
    public class PublishedFundingTemplate
    {
        public string TemplateVersion { get; set; }

        public string PublishNote { get; set; }

        public string AuthorId { get; set; }

        public string AuthorName { get; set; }

        public DateTime PublishDate { get; set; }

        public string SchemaVersion { get; set; }
    }
}
