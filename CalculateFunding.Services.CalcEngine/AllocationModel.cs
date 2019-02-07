using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Aggregations;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Calculator.Interfaces;
using CalculateFunding.Services.CodeGeneration.VisualBasic;
using Serilog;

namespace CalculateFunding.Services.Calculator
{
    public class AllocationModel : IAllocationModel
    {
        private readonly List<Tuple<MethodInfo, CalculationResult>> _methods = new List<Tuple<MethodInfo, CalculationResult>>();
        private readonly Dictionary<string, PropertyInfo> _datasetSetters = new Dictionary<string, PropertyInfo>();
        private readonly object _instance;
        private readonly object _datasetsInstance;
        private readonly object _providerInstance;
        private readonly ILogger _logger;

        public AllocationModel(Type allocationType, Dictionary<string, Type> datasetTypes, ILogger logger)
        {
            DatasetTypes = datasetTypes;
            PropertyInfo datasetsSetter = allocationType.GetProperty("Datasets");
            Type datasetType = datasetsSetter.PropertyType;
            foreach (PropertyInfo relationshipProperty in datasetType.GetProperties().Where(x => x.CanWrite).ToArray())
            {
                CustomAttributeData relationshipAttribute = relationshipProperty.GetCustomAttributesData()
                    .FirstOrDefault(x => x.AttributeType.Name == "DatasetRelationshipAttribute");
                if (relationshipAttribute != null)
                {
                    _datasetSetters.Add(GetProperty(relationshipAttribute, "Name"), relationshipProperty);
                }
            }

            PropertyInfo providerSetter = allocationType.GetProperty("Provider");
            Type providerType = providerSetter.PropertyType;

            IEnumerable<MethodInfo> executeMethods = allocationType.GetMethods().Where(x => x.ReturnType == typeof(Nullable<decimal>));

            foreach (MethodInfo executeMethod in executeMethods)
            {
                ParameterInfo[] parameters = executeMethod.GetParameters();

                IList<CustomAttributeData> attributes = executeMethod.GetCustomAttributesData();
                CustomAttributeData calcAttribute = attributes.FirstOrDefault(x => x.AttributeType.Name == "CalculationAttribute");
                if (calcAttribute != null)
                {
                    CalculationResult result = new CalculationResult
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
            _providerInstance = Activator.CreateInstance(providerType);
            _logger = logger;
        }

        public object Instance
        {
            get
            {
                return _instance;
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
                    Type type = DatasetTypes[datasetName];
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

        public IEnumerable<CalculationResult> Execute(List<ProviderSourceDataset> datasets, ProviderSummary providerSummary, IEnumerable<CalculationAggregation> aggregationValues = null, IEnumerable<string> calcsToProcess = null)
        {
         
            HashSet<string> datasetNamesUsed = new HashSet<string>();
            foreach (ProviderSourceDataset dataset in datasets)
            {
                Type type = GetDatasetType(dataset.DataDefinition.Name);

                if (_datasetSetters.TryGetValue(dataset.DataRelationship.Name, out PropertyInfo setter))
                {
                    datasetNamesUsed.Add(dataset.DataRelationship.Name);
                    if (dataset.DataGranularity == DataGranularity.SingleRowPerProvider)
                    {
                        object row = PopulateRow(type, dataset.Current.Rows.First());
                        setter.SetValue(_datasetsInstance, row);
                    }
                    else
                    {
                        Type constructGeneric = typeof(List<>).MakeGenericType(type);
                        object list = Activator.CreateInstance(constructGeneric);
                        MethodInfo addMethod = list.GetType().GetMethod("Add");
                        Type itemType = list.GetType().GenericTypeArguments.First();
                        object[] rows = dataset.Current.Rows.Select(x => PopulateRow(itemType, x)).ToArray();
                        foreach (object row in rows)
                        {
                            addMethod.Invoke(list, new[] { row });
                        }

                        setter.SetValue(_datasetsInstance, list);
                    }
                }
            }

            PropertyInfo providerSetter = _instance.GetType().GetProperty("Provider");

            object provider = PopulateProvider(providerSummary, providerSetter);
            providerSetter.SetValue(_instance, provider);


            if (aggregationValues != null)
            {
                PropertyInfo aggregationsSetter = _instance.GetType().GetProperty("Aggregations");

                if (aggregationsSetter != null)
                {
                    Type propType = aggregationsSetter.PropertyType;

                    object data = Activator.CreateInstance(propType);

                    MethodInfo add = data.GetType().GetMethod("Add", new[] { typeof(String), typeof(Decimal) });

                    foreach (CalculationAggregation aggregations in aggregationValues)
                    {
                        foreach (AggregateValue aggregatedValue in aggregations.Values)
                        {
                            add.Invoke(data, new object[] { aggregatedValue.FieldReference, aggregatedValue.Value.HasValue ? aggregatedValue.Value.Value : 0 });
                        }
                    }

                    aggregationsSetter.SetValue(_instance, data);
                }
            }

            // Add default object for any missing datasets to help reduce null exceptions
            foreach (string key in _datasetSetters.Keys.Where(x => !datasetNamesUsed.Contains(x)))
            {
                if (_datasetSetters.TryGetValue(key, out PropertyInfo setter))
                {
                    setter.SetValue(_datasetsInstance, Activator.CreateInstance(setter.PropertyType));
                }
            }

            PropertyInfo calcResultsSetter = _instance.GetType().GetProperty("CalcResultsCache");

            if (calcResultsSetter != null)
            {
                Type propType = calcResultsSetter.PropertyType;

                object data = Activator.CreateInstance(propType);

                calcResultsSetter.SetValue(_instance, data);
            }

            IList<CalculationResult> calculationResults = new List<CalculationResult>();

            foreach (Tuple<MethodInfo, CalculationResult> executeMethod in _methods)
            {
                if (!calcsToProcess.IsNullOrEmpty())
                {
                    if (!calcsToProcess.Contains(VisualBasicTypeGenerator.GenerateIdentifier(executeMethod.Item2.Calculation.Name)))
                    {
                        continue;
                    }
                }

                CalculationResult result = executeMethod.Item2;
                try
                {
                    result.Value = (decimal?)executeMethod.Item1.Invoke(_instance, null);
                }
                catch (Exception exception)
                {
                    result.Exception = new CaclulationResultException
                    {
                        ExceptionType = exception.GetType().Name,
                        Message = exception.Message,
                        InnerException = exception.InnerException != null ? new CaclulationResultException
                        {
                            ExceptionType = exception.InnerException.GetType().Name,
                            Message = exception.InnerException.Message
                        } : null
                    };

                    _logger.Error(exception, $"Failed to create result for calculation id '{result.Calculation.Id}'");
                }
             
                calculationResults.Add(result);
            }

            return calculationResults;
        }

        private object PopulateProvider(ProviderSummary providerSummary, PropertyInfo providerSetter)
        {
            Type type = providerSetter.PropertyType;

            object data = Activator.CreateInstance(type);

            foreach (PropertyInfo property in type.GetProperties().Where(x => x.CanWrite).ToArray())
            {

                switch (property.Name)
                {
                    case "DateOpened":
                        property.SetValue(data, providerSummary.DateOpened.HasValue ? providerSummary.DateOpened.Value.Date : (DateTime?)null);
                        break;
                    case "ProviderType":
                        property.SetValue(data, providerSummary.ProviderType.EmptyIfNull());
                        break;
                    case "ProviderSubType":
                        property.SetValue(data, providerSummary.ProviderSubType.EmptyIfNull());
                        break;
                    case "Name":
                        property.SetValue(data, providerSummary.Name.EmptyIfNull());
                        break;
                    case "UKPRN":
                        property.SetValue(data, providerSummary.UKPRN.EmptyIfNull());
                        break;
                    case "URN":
                        property.SetValue(data, providerSummary.URN.EmptyIfNull());
                        break;
                    case "UPIN":
                        property.SetValue(data, providerSummary.UPIN.EmptyIfNull());
                        break;
                    case "DfeEstablishmentNumber":
                        property.SetValue(data, providerSummary.DfeEstablishmentNumber.EmptyIfNull());
                        break;
                    case "EstablishmentNumber":
                        property.SetValue(data, providerSummary.EstablishmentNumber.EmptyIfNull());
                        break;
                    case "LegalName":
                        property.SetValue(data, providerSummary.LegalName.EmptyIfNull());
                        break;
                    case "Authority":
                        property.SetValue(data, providerSummary.Authority.EmptyIfNull());
                        break;
                    case "DateClosed":
                        property.SetValue(data, providerSummary.DateClosed.HasValue ? providerSummary.DateClosed.Value.Date : (DateTime?)null);
                        break;
                    case "LACode":
                        property.SetValue(data, providerSummary.LACode.EmptyIfNull());
                        break;
                    case "CrmAccountId":
                        property.SetValue(data, providerSummary.CrmAccountId.EmptyIfNull());
                        break;
                    case "NavVendorNo":
                        property.SetValue(data, providerSummary.NavVendorNo.EmptyIfNull());
                        break;
                    case "Status":
                        property.SetValue(data, providerSummary.Status.EmptyIfNull());
                        break;
                    case "PhaseOfEducation":
                        property.SetValue(data, providerSummary.PhaseOfEducation.EmptyIfNull());
                        break;
                    default:
                        break;
                }
            }

            return data;
        }

        private object PopulateRow(Type type, Dictionary<string, object> row)
        {
            object data = Activator.CreateInstance(type);
            foreach (PropertyInfo property in type.GetProperties().Where(x => x.CanWrite).ToArray())
            {
                CustomAttributeData fieldAttribute = property.GetCustomAttributesData()
                    .FirstOrDefault(x => x.AttributeType.Name == "FieldAttribute");
                if (fieldAttribute != null)
                {
                    string propertyName = GetProperty(fieldAttribute, "Name");

                    if (row.TryGetValue(propertyName, out object value))
                    {
                        string propType = property.PropertyType.ToString();

                        if (propType == "System.Decimal")
                        {
                            value = Convert.ToDecimal(value);
                        }

                        if (propType == "System.Int32")
                        {
                            value = Convert.ToInt32(value);
                        }

                        if (propType == "System.Int64")
                        {
                            value = Convert.ToInt64(value);
                        }

                        property.SetValue(data, value);
                    }
                }
            }
            return data;
        }

        private static IEnumerable<Reference> GetReferences(IList<CustomAttributeData> attributes, string attributeName)
        {
            foreach (CustomAttributeData attribute in attributes.Where(x => x.AttributeType.Name.StartsWith(attributeName)))
            {
                yield return new Reference(GetProperty(attribute, "Id"), GetProperty(attribute, "Name"));
            }
        }

        private static Reference GetReference(IList<CustomAttributeData> attributes, string attributeName)
        {
            CustomAttributeData attribute = attributes.FirstOrDefault(x => x.AttributeType.Name.StartsWith(attributeName));
            if (attribute != null)
            {
                return new Reference(GetProperty(attribute, "Id"), GetProperty(attribute, "Name"));
            }
            return null;
        }

        private static string GetProperty(CustomAttributeData attribute, string propertyName)
        {
            CustomAttributeNamedArgument argument = attribute.NamedArguments.FirstOrDefault(x => x.MemberName == propertyName);
            if (argument != null)
            {
                return argument.TypedValue.Value?.ToString();
            }
            return null;
        }
    }
}

