using System.ComponentModel.DataAnnotations;

namespace CalculateFunding.Models.FDZ
{
    public class FieldMetadata
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public FieldType FieldType { get; set; }

        [Required]
        public bool Required { get; set; }

        [Required]
        public bool IsTableKey { get; set; }
    }
}
