using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Allocations.Models.Framework
{
    public class AllocationModel
    {
        public string ModelName { get; }
        public List<object> AllocationProcessors  = new List<object>();

        public AllocationModel(string modelName)
        {
            ModelName = modelName;
        }

        public IEnumerable<CalculationResult> Execute(string modelName, string urn, object[] datasets)
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
                    .Where(x => x.ReturnType.IsAssignableFrom(typeof(CalculationResult)));
                foreach (var executeMethod in executeMethods)
                {
                    object result = null;





                    ParameterInfo[] parameters = executeMethod.GetParameters();


                    if (parameters.Length == 0)
                    {
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
                    if (returnType.IsAssignableFrom(typeof(CalculationResult)))
                    {
                        var allocationResult = Convert.ChangeType(result, returnType) as CalculationResult;
                        yield return allocationResult;
                    }
                }

                var getters = allocationType.GetProperties().Where(x => x.CanRead).ToArray();

                foreach (var getter in getters.Where(x =>
                    x.PropertyType.IsAssignableFrom(typeof(CalculationResult))))
                {
                    var allocationResult = getter.GetValue(allocation) as CalculationResult;
                    yield return allocationResult;
                }


            }
       


        }
    }
}