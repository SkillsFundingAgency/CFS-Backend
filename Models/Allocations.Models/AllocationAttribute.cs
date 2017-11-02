using System;

namespace Allocations.Models
{
    public class AllocationAttribute : Attribute
    {
        public string ModelName { get; }

        public AllocationAttribute(string modelName)
        {
            ModelName = modelName;
        }
    }
}