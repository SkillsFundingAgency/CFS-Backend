using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Allocations.Models;

namespace Allocations.Services.Calculator
{
    public class AllocationFactory
    {

        private Dictionary<string, Type> DatasetTypes { get; }
        private AllocationModel AllocationModel { get; }

        public AllocationFactory(Assembly budgetAssembly)
        {

            var datasetTypes = budgetAssembly.GetTypes().Where(x => x.CustomAttributes.Any(attr => attr.AttributeType == typeof(DatasetAttribute)));
            DatasetTypes = new Dictionary<string, Type>();
            foreach (var type in datasetTypes)
            {
                DatasetTypes.Add(type.GetCustomAttribute<DatasetAttribute>().DatasetName, type);
            }


            var allocationTypes = budgetAssembly.GetTypes().Where(x => x.CustomAttributes.Any(attr => attr.AttributeType == typeof(AllocationAttribute)));
            AllocationModel = new AllocationModel();
            foreach (var type in allocationTypes)
            {
                AllocationModel.AllocationProcessors.Add(Activator.CreateInstance(type));
            }

        }


        public Type GetDatasetType(string datasetName)
        {
            if (DatasetTypes.ContainsKey(datasetName))
            {
                return DatasetTypes[datasetName];
            }
            throw new NotImplementedException($"{datasetName} is not defined");
        }

        public object CreateDataset(string datasetName)
        {
            if (DatasetTypes.ContainsKey(datasetName))
            {
                try
                {
                    var type = DatasetTypes[datasetName];
                    return Activator.CreateInstance(type);
                }
                catch (ReflectionTypeLoadException e)
                {
                    throw new Exception(string.Join(", ", e.LoaderExceptions.Select(x => x.Message)));
                }
            }
            throw new NotImplementedException($"{datasetName} is not defined");
        }

        public AllocationModel CreateAllocationModel()
        {
            return AllocationModel;
        }


    }
}
