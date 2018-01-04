using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CalculateFunding.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Services.Calculator
{
    public class AllocationModel
    {
        public List<object> AllocationProcessors  = new List<object>();




        public IEnumerable<CalculationResult> Execute(object[] datasets)
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

                var executeMethods = allocationType.GetMethods().Where(x => x.ReturnType == typeof(decimal));
                foreach (var executeMethod in executeMethods)
                {

                    ParameterInfo[] parameters = executeMethod.GetParameters();

                    var attribute = executeMethod.GetCustomAttributesData().FirstOrDefault(x => x.AttributeType.Name == "CalculationAttribute");
                    if (attribute != null)
                    {
                        var result = CreateResult(attribute);

                        if (parameters.Length == 0)
                        {
                            try
                            {
                                result.Value = (decimal)executeMethod.Invoke(allocation, null);
                            }
                            catch (Exception e)
                            {
                                result.Exception = e;
                            }

                        }

  
                        yield return result;
                    }
                }


            }

        }

        private static CalculationResult CreateResult(CustomAttributeData attribute)
        {
            var result = new CalculationResult();
            foreach (var argument in attribute.NamedArguments)
            {
                switch (argument.MemberName)
                {
                    case "CalculationId":
                        result.CalculationId = argument.TypedValue.ToString();
                        break;
                    case "CalculationName":
                        result.CalculationName = argument.TypedValue.ToString();
                        break;
                    case "PolicyId":
                        result.PolicyId = argument.TypedValue.ToString();
                        break;
                    case "PolicyName":
                        result.PolicyName = argument.TypedValue.ToString();
                        break;
                    case "AllocationLineId":
                        result.AllocationLineId = argument.TypedValue.ToString();
                        break;
                    case "AllocationLineame":
                        result.AllocationLineName = argument.TypedValue.ToString();
                        break;
                }
            }
            return result;
        }
    }
}