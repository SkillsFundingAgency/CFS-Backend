using System;

namespace Allocations.Models.Framework
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