using CalculateFunding.Models.Datasets.Schema;
using System.Collections.Generic;

namespace CalculateFunding.Models.Datasets
{
    public class FieldDefinitionComparer : IEqualityComparer<FieldDefinition>
    {
        public bool Equals(FieldDefinition x, FieldDefinition y)
        {
            return x.Id == y.Id;
        }

        public int GetHashCode(FieldDefinition obj)
        {
            return obj.Id.GetHashCode();
        }

    }
}
