using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Allocations.Models.Framework
{
    public static class AllocationFactory
    {
        static AllocationFactory()
        {
            var concreteClassTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes())
                .Where(x => x.IsClass && !x.IsAbstract).ToArray();

            var datasetTypes = concreteClassTypes.Where(x => x.CustomAttributes.Any(attr => attr.AttributeType == typeof(DatasetAttribute)));
            DatasetTypes = new Dictionary<string, Type>();
            foreach (var type in datasetTypes)
            {
                DatasetTypes.Add(type.GetCustomAttribute<DatasetAttribute>().DatasetName, type);
            }
            

            var allocationTypes = concreteClassTypes.Where(x => x.CustomAttributes.Any(attr => attr.AttributeType == typeof(AllocationAttribute)));
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

        private static Dictionary<string, Type> DatasetTypes { get; set; }
        private static Dictionary<string, AllocationModel> AllocationTypes { get; set; }

        public static Type GetDatasetType(string datasetName)
        {
            if (DatasetTypes.ContainsKey(datasetName))
            {
                return DatasetTypes[datasetName];
            }
            throw new NotImplementedException($"{datasetName} is not defined");
        }

        public static object CreateDataset(string datasetName)
        {
            if (DatasetTypes.ContainsKey(datasetName))
            {
                return Activator.CreateInstance(DatasetTypes[datasetName]);
            }
            throw new NotImplementedException($"{datasetName} is not defined");
        }

        public static AllocationModel CreateAllocationModel(string modelName)
        {
            if (!AllocationTypes.ContainsKey(modelName)) throw new NotImplementedException($"{modelName} is not defined");

            return AllocationTypes[modelName];


        }


    }


    public class AllocationModel
    {
        public string ModelName { get; }
        internal List<object> AllocationProcessors  = new List<object>();

        public AllocationModel(string modelName)
        {
            ModelName = modelName;
        }

        public IEnumerable<ProviderAllocation> Execute(string modelName, string urn, object[] datasets)
        {
            foreach (var allocation in AllocationProcessors)
            {
                var allocationType = allocation.GetType();
                var setters = allocationType.GetProperties().Where(x => x.CanWrite).ToArray();

                foreach (var dataset in datasets)
                {
                    foreach (var setter in setters.Where(x => x.PropertyType == dataset.GetType()))
                    {
                        setter.SetValue(allocation, dataset);
                    }
                }

                var executeMethods = allocationType.GetMethods()
                    .Where(x => x.ReturnType.IsAssignableFrom(typeof(ProviderAllocation)));
                foreach (var executeMethod in executeMethods)
                {
                    object result = null;





                    ParameterInfo[] parameters = executeMethod.GetParameters();


                    if (parameters.Length == 0)
                    {
                        // This works fine
                        result = executeMethod.Invoke(allocation, null);
                    }
                    else
                    {
                        object[] parametersArray = new object[parameters.Length];
                        for (var i = 0; i < parametersArray.Length; i++)
                        {
                            var parameterInfo = parameters[i];
                            var dataset = datasets.FirstOrDefault(x => x.GetType() == parameterInfo.ParameterType);
                            parametersArray[i] = dataset;
                        }
                        result = executeMethod.Invoke(allocation, parametersArray);
                    }

                    var returnType = executeMethod.ReturnType;
                    if (returnType.IsAssignableFrom(typeof(ProviderAllocation)))
                    {
                        var allocationResult = Convert.ChangeType(result, returnType) as ProviderAllocation;
                        allocationResult.ModelName = modelName;
                        allocationResult.URN = urn;
                        yield return allocationResult;
                    }
                }

                var getters = allocationType.GetProperties().Where(x => x.CanRead).ToArray();

                foreach (var getter in getters.Where(x =>
                    x.PropertyType.IsAssignableFrom(typeof(ProviderAllocation))))
                {
                    var allocationResult = getter.GetValue(allocation) as ProviderAllocation;
                    allocationResult.ModelName = modelName;
                    allocationResult.URN = urn;
                    yield return allocationResult;
                }


            }
       


        }
    }
}
