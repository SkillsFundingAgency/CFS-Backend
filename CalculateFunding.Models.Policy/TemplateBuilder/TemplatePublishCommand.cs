using CalculateFunding.Common.Models;

namespace CalculateFunding.Models.Policy.TemplateBuilder
{
    public class TemplatePublishCommand
    {
        public Reference Author { get; set; }
        public string TemplateId { get; set; }
        public string Note { get; set; }
        public string Version { get; set; }
        public int VersionNumber =>
            int.TryParse(Version, out int versionNumber)
                ? versionNumber
                : 0;
    }
}