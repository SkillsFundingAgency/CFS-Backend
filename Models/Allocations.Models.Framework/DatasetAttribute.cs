using System;

namespace Allocations.Models.Framework
{
    public class DatasetAttribute : Attribute
    {
        public string ModelName { get; }
        public string DatasetName { get; }

        public DatasetAttribute(string modelName, string datasetName)
        {
            ModelName = modelName;
            DatasetName = datasetName;
        }
    }
}