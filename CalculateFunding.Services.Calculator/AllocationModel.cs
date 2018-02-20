using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CalculateFunding.Models;
using CalculateFunding.Models.Results;
using Newtonsoft.Json;

namespace CalculateFunding.Services.Calculator
{
    public class AllocationModel
    {
        private List<Tuple<MethodInfo, CalculationResult>> _methods = new List<Tuple<MethodInfo, CalculationResult>>();
        private PropertyInfo[] _datasetSetters;
        private object _instance;
        private object _datasetsInstance;

        public AllocationModel(Type allocationType)
        {
            var datasetsSetter = allocationType.GetProperty("Datasets");
            var datasetType = datasetsSetter.PropertyType;
            _datasetSetters = datasetType.GetProperties().Where(x => x.CanWrite).ToArray();

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
	                    Calculation = GetReference(attributes, "Calculation"),
						CalculationSpecification = GetReference(attributes, "CalculationSpecification"),
                        AllocationLine = GetReference(attributes, "AllocationLine"),
                        PolicySpecifications = GetReferences(attributes, "PolicySpecification").ToList()
                    };

                    if (parameters.Length == 0)
                    {
                        _methods.Add(new Tuple<MethodInfo, CalculationResult>(executeMethod, result));
                    }
                }
            }

            _instance = Activator.CreateInstance(allocationType);
            _datasetsInstance = Activator.CreateInstance(datasetType);
            datasetsSetter.SetValue(_instance, _datasetsInstance);
        }


        public IEnumerable<CalculationResult> Execute(object[] datasets)
        {

            foreach (var dataset in datasets)
            {
                foreach (var setter in _datasetSetters)
                {
                    setter.SetValue(_datasetsInstance, dataset);
                }
            }

            foreach (var executeMethod in _methods)
            {
                var result = executeMethod.Item2;
                try
                {
                    result.Value = (decimal) executeMethod.Item1.Invoke(_instance, null);
                }
                catch (Exception e)
                {
                    result.Exception = e;
                }
                yield return result;
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