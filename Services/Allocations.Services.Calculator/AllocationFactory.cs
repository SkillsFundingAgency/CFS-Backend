using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Allocations.Models;
using Allocations.Models.Specs;

namespace Allocations.Services.Calculator
{
    public class AllocationFactory
    {
        private readonly BudgetAssemblyGenerator _datasetTypeGenerator = new BudgetAssemblyGenerator();
        private readonly ProductTypeGenerator _productTypeGenerator = new ProductTypeGenerator();
        public AllocationFactory(Budget budget)
        {
            var budgetAssembly = _datasetTypeGenerator.GenerateAssembly(budget);
            var datasetTypes = budgetAssembly.GetTypes().Where(x => x.CustomAttributes.Any(attr => attr.AttributeType == typeof(DatasetAttribute)));
            DatasetTypes = new Dictionary<string, Type>();
            foreach (var type in datasetTypes)
            {
                Console.WriteLine($"Adding {type}");
                DatasetTypes.Add(type.GetCustomAttribute<DatasetAttribute>().DatasetName, type);
            }


            var allocationTypes = budgetAssembly.GetTypes().Where(x => x.CustomAttributes.Any(attr => attr.AttributeType == typeof(AllocationAttribute)));
            AllocationTypes = new Dictionary<string, AllocationModel>();
            foreach (var type in allocationTypes)
            {
                var modelName = type.GetCustomAttribute<AllocationAttribute>().ModelName;
                if (!AllocationTypes.TryGetValue(modelName, out var model))
                {
                    model = new AllocationModel(modelName);
                    AllocationTypes.Add(modelName, model);
                }
                model.AllocationProcessors.Add(Activator.CreateInstance(type));
            }

        }

        private Dictionary<string, Type> DatasetTypes { get; set; }
        private Dictionary<string, AllocationModel> AllocationTypes { get; set; }

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
                    Console.WriteLine($"Creating {type}");
                    return Activator.CreateInstance(type);
                }
                catch (ReflectionTypeLoadException e)
                {
                    throw new Exception(string.Join(", ", e.LoaderExceptions.Select(x => x.Message)));
                }
            }
            throw new NotImplementedException($"{datasetName} is not defined");
        }

        public AllocationModel CreateAllocationModel(string modelName)
        {
            if (!AllocationTypes.ContainsKey(modelName)) throw new NotImplementedException($"{modelName} is not defined");

            return AllocationTypes[modelName];


        }


    }
}
