using System.Collections.Generic;

namespace CalculateFunding.Models.Code
{
    public class TypeInformation
    {
        public TypeInformation() { }

        public TypeInformation(string name)
        {
            Name = name;
            Type = "Keyword";
        }

        public TypeInformation(string name, string description)
        {
            Name = name;
            Description = description;
            Type = "DefaultType";
        }

        /// <summary>
        /// Type Name eg Class Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of the type
        /// </summary>
        public string Description { get; set; }

        public IEnumerable<MethodInformation> Methods { get; set; }

        public IEnumerable<PropertyInformation> Properties { get; set; }

        public string Type { get; set; }

        /// <summary>
        /// Enum values
        /// </summary>
        public IEnumerable<EnumValue> EnumValues { get; set; }
    }
}
