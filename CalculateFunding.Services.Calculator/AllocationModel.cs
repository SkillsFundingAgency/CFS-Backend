using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CalculateFunding.Models;
using CalculateFunding.Models.Results;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CalculateFunding.Services.Calculator
{
    public class AllocationModel
    {
        private readonly List<Tuple<MethodInfo, CalculationResult>> _methods = new List<Tuple<MethodInfo, CalculationResult>>();
        private readonly PropertyInfo[] _datasetSetters;
        private readonly object _instance;
        private readonly object _datasetsInstance;

        public AllocationModel(Type allocationType, Dictionary<string, Type> datasetTypes)
        {
            DatasetTypes = datasetTypes;
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

        private Dictionary<string, Type> DatasetTypes { get; }

        public IEnumerable<CalculationResult> Execute(List<ProviderSourceDataset> datasets)
        {
            foreach (var dataset in datasets)
            {
                var type = GetDatasetType(dataset.DataDefinition.Name);

                var json = JsonConvert.SerializeObject(dataset.Current.Rows.First());

                var typedRow = JsonConvert.DeserializeObject(json, type, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });

                foreach (var setter in _datasetSetters)
                {
                    setter.SetValue(_datasetsInstance, typedRow);
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
                return argument.TypedValue.Value?.ToString();
            }
            return null;
        }
    }
}