using System.ComponentModel.DataAnnotations;
using CalculateFunding.Common.Models;

namespace CalculateFunding.Models.Policy.TemplateBuilder
{
    public class TemplatePublishCommand
    {
        /// <summary>
        /// for internal use only
        /// </summary>
        public Reference Author { get; set; }
        
        /// <summary>
        /// template ID
        /// </summary>
        public string TemplateId { get; set; }
        
        /// <summary>
        /// publish notes
        /// </summary>
        [Required]
        public string Note { get; set; }
        
        /// <summary>
        /// version to be published - optional - defaults to current version
        /// </summary>
        public string Version { get; set; }
        
        /// <summary>
        /// version number to be published - internal use only
        /// </summary>
        public int VersionNumber =>
            int.TryParse(Version, out int versionNumber)
                ? versionNumber
                : 0;
    }
}