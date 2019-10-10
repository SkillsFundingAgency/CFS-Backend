using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Aggregations;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.CalcEngine.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using Serilog;

namespace CalculateFunding.Services.CalcEngine
{
    public class AllocationModel : IAllocationModel
    {
        private readonly List<Tuple<FieldInfo, CalculationResult>> _funcs = new List<Tuple<FieldInfo, CalculationResult>>();
        private readonly Dictionary<string, PropertyInfo> _datasetSetters = new Dictionary<string, PropertyInfo>();
        private readonly object _instance;
        private readonly object _datasetsInstance;
        private readonly object _providerInstance;
        private readonly ILogger _logger;
        private MethodInfo _mainMethod;
        private readonly IFeatureToggle _featureToggle;

        public AllocationModel(Type allocationType, Dictionary<string, Type> datasetTypes, ILogger logger, IFeatureToggle featureToggle)
        {
            Guard.ArgumentNotNull(allocationType, nameof(allocationType));
            Guard.ArgumentNotNull(datasetTypes, nameof(datasetTypes));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(featureToggle, nameof(featureToggle));

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

            IEnumerable<PropertyInfo> allocationTypeCalculationProperties = allocationType.GetTypeInfo().DeclaredProperties.Where(m => m.PropertyType.BaseType.Name == "BaseCalculation");

            List<FieldInfo> executeFuncs = GetExecuteFuncs(allocationTypeCalculationProperties);

            _mainMethod = allocationType.GetMethods().FirstOrDefault(x => x.Name == "MainCalc");

            _funcs = PopulateFuncs(executeFuncs);

            _instance = Activator.CreateInstance(allocationType);
            _datasetsInstance = Activator.CreateInstance(datasetType);
            datasetsSetter.SetValue(_instance, _datasetsInstance);
            _providerInstance = Activator.CreateInstance(providerType);
            _logger = logger;
            _featureToggle = featureToggle;
        }

        private List<Tuple<FieldInfo, CalculationResult>> PopulateFuncs(List<FieldInfo> executeFuncs)
        {
            List<Tuple<FieldInfo, CalculationResult>> funcs = new List<Tuple<FieldInfo, CalculationResult>>();

            foreach (FieldInfo executeFunc in executeFuncs)
            {
                IList<CustomAttributeData> attributes = executeFunc.GetCustomAttributesData();
                CustomAttributeData calcAttribute = attributes.FirstOrDefault(x => x.AttributeType.Name == "CalculationAttribute");
                if (calcAttribute != null)
                {
                    CalculationResult result = new CalculationResult
                    {
                        Calculation = GetReference(attributes, "Calculation"),
                    };

                    funcs.Add(new Tuple<FieldInfo, CalculationResult>(executeFunc, result));
                }
            }

            return funcs;
        }

        private List<FieldInfo> GetExecuteFuncs(IEnumerable<PropertyInfo> allocationTypeCalculationProperties)
        {
            List<FieldInfo> executeFuncs = new List<FieldInfo>();
            foreach (PropertyInfo propertyInfo in allocationTypeCalculationProperties)
            {
                IEnumerable<FieldInfo> fieldInfos = propertyInfo.PropertyType.GetTypeInfo().DeclaredFields.Where(x => x.FieldType == typeof(Func<decimal?>));

                executeFuncs.AddRange(fieldInfos);
            }

            return executeFuncs;
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

        public IEnumerable<CalculationResult> Execute(List<ProviderSourceDataset> datasets, ProviderSummary providerSummary, IEnumerable<CalculationAggregation> aggregationValues = null)
        {
            HashSet<string> datasetNamesUsed = new HashSet<string>();
            foreach (ProviderSourceDataset dataset in datasets)
            {
                Type type = GetDatasetType(dataset);

                if (_datasetSetters.TryGetValue(dataset.DataRelationship.Name, out PropertyInfo setter))
                {
                    datasetNamesUsed.Add(dataset.DataRelationship.Name);

                    SetDatasetInstanceValue(dataset, type, setter, _datasetsInstance);
                }
            }

            PropertyInfo providerSetter = _instance.GetType().GetProperty("Provider");

            object provider = PopulateProvider(providerSummary, providerSetter);
            providerSetter.SetValue(_instance, provider);
            
            if (!aggregationValues.IsNullOrEmpty())
            {
                SetInstanceAggregationsValues(aggregationValues);
            }

            SetMissingDatasetDefaultObjects(datasetNamesUsed, _datasetSetters, _datasetsInstance);

            SetInstanceCalcResults(_instance);

            Dictionary<string, string[]> results = (Dictionary<string, string[]>)_mainMethod.Invoke(_instance, null);

            return ProcessCalculationResults(results, _funcs);
        }

        private void SetInstanceCalcResults(object instance)
        {
            PropertyInfo calcResultsSetter = instance.GetType().GetProperty("CalcResultsCache");

            if (calcResultsSetter != null)
            {
                Type propType = calcResultsSetter.PropertyType;

                object data = Activator.CreateInstance(propType);

                calcResultsSetter.SetValue(instance, data);
            }
        }

        /// <summary>
        /// Add default object for any missing datasets to help reduce null exceptions
        /// </summary>
        private void SetMissingDatasetDefaultObjects(HashSet<string> datasetNamesUsed, Dictionary<string, PropertyInfo> datasetSetters, object datasetsInstance)
        {
            foreach (string key in datasetSetters.Keys.Where(x => !datasetNamesUsed.Contains(x)))
            {
                if (datasetSetters.TryGetValue(key, out PropertyInfo setter))
                {
                    setter.SetValue(datasetsInstance, Activator.CreateInstance(setter.PropertyType));
                }
            }
        }

        private IList<CalculationResult> ProcessCalculationResults(Dictionary<string, string[]> results,
            List<Tuple<FieldInfo, CalculationResult>> funcs)
        {
            IList<CalculationResult> calculationResults = new List<CalculationResult>();

            foreach (KeyValuePair<string, string[]> calcResult in results)
            {
                if (calcResult.Value.Length < 1)
                {
                    _logger.Error("The number of items returned from the key value pair in the calculation results is under the minimum required.");
                    continue;
                }

                Tuple<FieldInfo, CalculationResult> func = funcs.FirstOrDefault(m =>
                    string.Equals(m.Item2.Calculation.Id, calcResult.Key, StringComparison.InvariantCultureIgnoreCase));

                if (func != null)
                {
                    CalculationResult calculationResult = func.Item2;

                    ProcessCalculationResult(calculationResult, calcResult);

                    calculationResults.Add(calculationResult);
                }
            }

            return calculationResults;
        }

        private static void ProcessCalculationResult(CalculationResult calculationResult, KeyValuePair<string, string[]> calcResult)
        {
            const int valueIndex = 0;
            const int exceptionTypeIndex = 1;
            const int exceptionMessageIndex = 2;
            const int exceptionStackTraceIndex = 3;
            const int exceptionElapsedTimeIndex = 4;

            calculationResult.Value = calcResult.Value[valueIndex].GetValueOrNull<decimal>();

            if (calcResult.Value.Length > exceptionMessageIndex)
            {
                calculationResult.ExceptionType = calcResult.Value[exceptionTypeIndex];
                calculationResult.ExceptionMessage = calcResult.Value[exceptionMessageIndex];

                if(calcResult.Value.Length > exceptionStackTraceIndex) calculationResult.ExceptionStackTrace = calcResult.Value[exceptionStackTraceIndex];

                if (calcResult.Value.Length > exceptionElapsedTimeIndex)
                {
                    if (TimeSpan.TryParse(calcResult.Value[exceptionElapsedTimeIndex], out TimeSpan ts))
                    {
                        calculationResult.ElapsedTime = ts.Ticks;
                    }
                }
            }
        }

        private void SetInstanceAggregationsValues(IEnumerable<CalculationAggregation> aggregationValues)
        {
            PropertyInfo aggregationsSetter = _instance.GetType().GetProperty("Aggregations");

            if (aggregationsSetter != null)
            {
                Type propType = aggregationsSetter.PropertyType;

                object data = Activator.CreateInstance(propType);

                MethodInfo add = data.GetType().GetMethod("Add", new[] {typeof(String), typeof(Decimal)});

                foreach (CalculationAggregation aggregations in aggregationValues)
                {
                    foreach (AggregateValue aggregatedValue in aggregations.Values)
                    {
                        add.Invoke(data, new object[] {aggregatedValue.FieldReference, aggregatedValue.Value.HasValue ? aggregatedValue.Value.Value : 0});
                    }
                }

                aggregationsSetter.SetValue(_instance, data);
            }
        }

        private void SetDatasetInstanceValue(ProviderSourceDataset dataset, Type type, PropertyInfo setter, object datasetsInstance)
        {
            if (dataset.DataGranularity == DataGranularity.SingleRowPerProvider)
            {
                object row = PopulateRow(type, dataset.Current.Rows.First());

                setter.SetValue(datasetsInstance, row);
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
                    addMethod.Invoke(list, new[] {row});
                }

                setter.SetValue(datasetsInstance, list);
            }
        }

        private Type GetDatasetType(ProviderSourceDataset dataset)
        {
            if (_featureToggle.IsUseFieldDefinitionIdsInSourceDatasetsEnabled())
            {
                if (!string.IsNullOrWhiteSpace(dataset.DataDefinitionId))
                {
                    return GetDatasetType(dataset.DataDefinitionId);
                }
                else
                {
                    return GetDatasetType(dataset.DataDefinition.Id);
                }
            }
            else
            {
                return GetDatasetType(dataset.DataDefinition.Name);
            }
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

                    case "LocalAuthorityName":
                        property.SetValue(data, providerSummary.LocalAuthorityName.EmptyIfNull());
                        break;

                    case "CompaniesHouseNumber":
                        property.SetValue(data, providerSummary.CompaniesHouseNumber.EmptyIfNull());
                        break;

                    case "GroupIdNumber":
                        property.SetValue(data, providerSummary.GroupIdNumber.EmptyIfNull());
                        break;

                    case "RscRegionName":
                        property.SetValue(data, providerSummary.RscRegionName.EmptyIfNull());
                        break;

                    case "RscRegionCode":
                        property.SetValue(data, providerSummary.RscRegionCode.EmptyIfNull());
                        break;

                    case "GovernmentOfficeRegionName":
                        property.SetValue(data, providerSummary.GovernmentOfficeRegionName.EmptyIfNull());
                        break;

                    case "GovernmentOfficeRegionCode":
                        property.SetValue(data, providerSummary.GovernmentOfficeRegionCode.EmptyIfNull());
                        break;

                    case "DistrictName":
                        property.SetValue(data, providerSummary.DistrictName.EmptyIfNull());
                        break;

                    case "DistrictCode":
                        property.SetValue(data, providerSummary.DistrictCode.EmptyIfNull());
                        break;

                    case "WardName":
                        property.SetValue(data, providerSummary.WardName.EmptyIfNull());
                        break;

                    case "WardCode":
                        property.SetValue(data, providerSummary.WardCode.EmptyIfNull());
                        break;

                    case "CensusWardName":
                        property.SetValue(data, providerSummary.CensusWardName.EmptyIfNull());
                        break;

                    case "CensusWardCode":
                        property.SetValue(data, providerSummary.CensusWardCode.EmptyIfNull());
                        break;

                    case "MiddleSuperOutputAreaName":
                        property.SetValue(data, providerSummary.MiddleSuperOutputAreaName.EmptyIfNull());
                        break;

                    case "MiddleSuperOutputAreaCode":
                        property.SetValue(data, providerSummary.MiddleSuperOutputAreaCode.EmptyIfNull());
                        break;

                    case "LowerSuperOutputAreaName":
                        property.SetValue(data, providerSummary.LowerSuperOutputAreaName.EmptyIfNull());
                        break;

                    case "LowerSuperOutputAreaCode":
                        property.SetValue(data, providerSummary.LowerSuperOutputAreaCode.EmptyIfNull());
                        break;

                    case "ParliamentaryConstituencyName":
                        property.SetValue(data, providerSummary.ParliamentaryConstituencyName.EmptyIfNull());
                        break;

                    case "ParliamentaryConstituencyCode":
                        property.SetValue(data, providerSummary.ParliamentaryConstituencyCode.EmptyIfNull());
                        break;

                    case "CountryCode":
                        property.SetValue(data, providerSummary.CountryCode.EmptyIfNull());
                        break;

                    case "CountryName":
                        property.SetValue(data, providerSummary.CountryName.EmptyIfNull());
                        break;

                    case "LocalGovernmentGroupTypeCode":
                        property.SetValue(data, providerSummary.LocalGovernmentGroupTypeCode.EmptyIfNull());
                        break;

                    case "LocalGovernmentGroupTypeName":
                        property.SetValue(data, providerSummary.LocalGovernmentGroupTypeName.EmptyIfNull());
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
                // the dataset relationship exists for the current provider so the dataset needs to return true for the HasValue implementation
                if (property.Name == "HasValue")
                {
                    property.SetValue(data, true);
                    continue;
                }

                CustomAttributeData fieldAttribute = property.GetCustomAttributesData()
                    .FirstOrDefault(x => x.AttributeType.Name == "FieldAttribute");

                if (fieldAttribute != null)
                {
                    string propertyName = "";

                    if (_featureToggle.IsUseFieldDefinitionIdsInSourceDatasetsEnabled())
                    {
                        int keyValue;

                        if (row.Count() > 0)
                        {
                            bool isNumber = int.TryParse(row.Keys.First(), out keyValue);

                            propertyName = GetProperty(fieldAttribute, isNumber ? "Id" : "Name");
                        }
                    }
                    else
                    {
                        propertyName = GetProperty(fieldAttribute, "Name");
                    }

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

                        if (propType == "System.Nullable`1[System.Int32]")
                        {
                            // A catch(Exception) was surrounding this code, but I removed it, but not sure why, as it's not documented in here
                            // My hunch is the persisted / provided value is not in the correct property format
                            if (value != null)
                            {
                                value = int.TryParse(value.ToString(), out int outValue) ? (int?)outValue : null;
                            }
                        }

                        if (propType == "System.Nullable`1[System.Decimal]")
                        {
                            // A catch(Exception) was surrounding this code, but I removed it, but not sure why, as it's not documented in here
                            // My hunch is the persisted / provided value is not in the correct property format
                            if (value != null)
                            {
                                value = decimal.TryParse(value.ToString(), out decimal outValue) ? (decimal?)outValue : null;
                            }
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

