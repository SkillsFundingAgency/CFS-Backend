using System.Collections.Generic;

namespace CalculateFunding.Models.Code
{
    public class TypeInformation
    {
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
    }
}
