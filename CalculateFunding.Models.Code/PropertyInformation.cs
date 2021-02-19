using System.Collections.Generic;

namespace CalculateFunding.Models.Code
{
    public class PropertyInformation
    {
        public string Name { get; set; }

        public string FriendlyName { get; set; }

        public string Description { get; set; }

        public string Type { get; set; }

        public string IsAggregable { get; set; }

        public bool IsObsolete { get; set; }

        public IEnumerable<PropertyInformation> Children { get; set; }

        public string TypeClass { get; set; }

        public bool IsNullable { get; set; }
    }
}
