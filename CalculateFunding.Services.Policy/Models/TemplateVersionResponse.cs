using System;
using CalculateFunding.Models.Policy.TemplateBuilder;

namespace CalculateFunding.Services.Policy.Models
{
    public class TemplateVersionResponse
    {
        public DateTimeOffset Date { get; set; }
        public string AuthorId { get; set; }
        public string AuthorName { get; set; }
        public string Comment { get; set; }
        public int Version { get; set; }
        public int MinorVersion { get; set; }
        public int MajorVersion { get; set; }
        public TemplateStatus Status { get; set; }
	}
}
