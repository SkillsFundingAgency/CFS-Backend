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

                    var parameters = executeMethod.GetParameters();

                    var attributes = executeMethod.GetCustomAttributesData();
                    var calcAttribute = attributes.FirstOrDefault(x => x.AttributeType.Name == "CalculationAttribute");
                    if (calcAttribute != null)
                    {
                        var result = new CalculationResult
                        {
                            CalculationId = GetProperty(calcAttribute, "Id"),
                            CalculationSpecification = GetReference(attributes, "CalculationSpecification"),
                            AllocationLine = GetReference(attributes, "AllocationLine"),
                            PolicySpecifications = GetReferences(attributes, "PolicySpecification").ToList()
                        };

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


        private static IEnumerable<Reference> GetReferences(IList<CustomAttributeData> attributes, string attributeName)
        {
            foreach (var attribute in attributes.Where(x => x.AttributeType.Name.StartsWith(attributeName)))
            {
                yield return new Reference(GetProperty(attribute, "Id"), GetProperty(attribute, "Name"));
            }
        }

        private static Reference GetReference(IList<CustomAttributeData> attributes, string attributeName)
        {
            var attribute = attributes.FirstOrDefault(x => x.AttributeType.Name.StartsWith(attributeName));
            if (attribute != null)
            {
                return new Reference(GetProperty(attribute, "Id"), GetProperty(attribute, "Name"));
            }
            return null;
        }

        private static string GetProperty(CustomAttributeData attribute, string propertyName)
        {
            var argument = attribute.NamedArguments.FirstOrDefault(x => x.MemberName == propertyName);
            if (argument != null)
            {
                return argument.TypedValue.Value.ToString();
            }
            return null;
        }
    }
}