using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Aggregations;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.ProviderLegacy;
using CalculateFunding.Services.CalcEngine.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Activator = CalculateFunding.Services.Core.Helpers.Activator;

namespace CalculateFunding.Services.CalcEngine
{
    public class AllocationModel : IAllocationModel
    {
        private List<(MemberInfo, CalculationAttributeMetadata)> _calculationResultFuncs = new List<(MemberInfo, CalculationAttributeMetadata)>();
        private List<(MemberInfo, FundingLineAttributeMetadata)> _fundingLineResultFuncs = new List<(MemberInfo, FundingLineAttributeMetadata)>();
        private Dictionary<string, PropertyInfo> _datasetSetters = new Dictionary<string, PropertyInfo>();
        private readonly Type _allocationType;
        private readonly PropertyInfo _datasetsSetter;
        private readonly ILogger _logger;
        private readonly Activator _activator;
        private readonly Assembly _assembly;

        private Dictionary<string, Type> _datasetTypes;

        public AllocationModel(Assembly assembly, ILogger logger)
        {
            Guard.ArgumentNotNull(assembly, nameof(logger));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _datasetTypes = new Dictionary<string, Type>();

            _assembly = assembly;

            GetTypes("DatasetDefinitionId", "DatasetRelationshipId");

            _allocationType = _assembly.GetTypes().FirstOrDefault(x => x.IsClass && x.BaseType.Name.Contains("BaseCalculation"));

            _datasetsSetter = _allocationType.GetProperty("Datasets");
            foreach (PropertyInfo relationshipProperty in _datasetsSetter.PropertyType.GetProperties().Where(x => x.CanWrite).ToArray())
            {
                CustomAttributeData relationshipAttribute = relationshipProperty.GetCustomAttributesData()
                    .FirstOrDefault(x => x.AttributeType.Name == "DatasetRelationshipAttribute");
                if (relationshipAttribute != null)
                {
                    _datasetSetters.Add(GetProperty(relationshipAttribute, "Name"), relationshipProperty);
                }
            }

            IEnumerable<PropertyInfo> allocationTypeCalculationProperties =
                _allocationType.GetTypeInfo().DeclaredProperties.Where(m => m.PropertyType.BaseType.Name == "BaseCalculation");

            List<FieldInfo> calcExecuteFuncs = GetExecuteFuncs(allocationTypeCalculationProperties);
            _calculationResultFuncs = PopulateMembers(calcExecuteFuncs.ToList<MemberInfo>(), (attributes) =>
            {
                CustomAttributeData calcAttribute = attributes.FirstOrDefault(x => x.AttributeType.Name == "CalculationAttribute");
                if (calcAttribute != null)
                {
                    CalculationAttributeMetadata result = new CalculationAttributeMetadata
                    {
                        Calculation = GetReference(attributes, "Calculation"),
                        CalculationDataType = GetCalculationDataType(attributes, "Calculation")
                    };

                    return result;
                }

                return null;
            });

            Type fundingStreamType = _allocationType.GetNestedTypes().FirstOrDefault(x => x.Name.EndsWith("FundingLines"));
            List<FieldInfo> fundingLineExecuteFuncs = fundingStreamType.GetTypeInfo().DeclaredFields.Where(x => x.FieldType == typeof(Func<decimal?>)).ToList();

            _fundingLineResultFuncs = PopulateMembers(fundingLineExecuteFuncs.ToList<MemberInfo>(), (attributes) =>
            {
                CustomAttributeData fundingLineAttribute = attributes.FirstOrDefault(x => x.AttributeType.Name == "FundingLineAttribute");
                if (fundingLineAttribute != null)
                {
                    FundingLineAttributeMetadata result = new FundingLineAttributeMetadata
                    {
                        FundingLineFundingStreamId = GetFundingStream(attributes, "FundingLine"),
                        FundingLine = GetReference(attributes, "FundingLine"),
                    };

                    return result;
                }

                return null;
            });

            _logger = logger;
            _activator = new Activator();
        }

        private object CreateInstance(Type type, params object[] args)
        {
            ConstructorInfo ctor = type.GetConstructors().First();
            ObjectActivator createdActivator = _activator.GetActivator(ctor, type.FullName);

            return createdActivator(args);
        }

        private void GetTypes(params string[] fields)
        {
            IEnumerable<Type> types = Enumerable.Empty<Type>();

            foreach (string fieldName in fields)
            {
                types = _assembly.GetTypes().Where(x => x.GetFields().Any(p => p.IsStatic && p.Name == fieldName));

                foreach (var type in types)
                {
                    FieldInfo field = type.GetField(fieldName);
                    string definitionName = field.GetValue(null).ToString();

                    _datasetTypes.Add(definitionName, type);
                }
            }
        }

        private List<(MemberInfo, T)> PopulateMembers<T>(List<MemberInfo> executeFuncs, Func<IList<CustomAttributeData>, T> func) where T : class
        {
            List<(MemberInfo, T)> funcs = new List<(MemberInfo, T)>();

            foreach (MemberInfo members in executeFuncs)
            {
                IList<CustomAttributeData> attributes = members.GetCustomAttributesData();
                if (func(attributes) != null)
                {
                    funcs.Add((members, func(attributes)));
                }
            }

            return funcs;
        }

        private List<FieldInfo> GetExecuteFuncs(IEnumerable<PropertyInfo> allocationTypeCalculationProperties)
        {
            List<FieldInfo> executeFuncs = new List<FieldInfo>();
            foreach (PropertyInfo propertyInfo in allocationTypeCalculationProperties)
            {
                IEnumerable<FieldInfo> fieldInfos = propertyInfo.PropertyType.GetTypeInfo().DeclaredFields;

                executeFuncs.AddRange(fieldInfos);
            }

            return executeFuncs;
        }

        public Type DatasetType(string datasetName) => _datasetTypes.ContainsKey(datasetName) ? _datasetTypes[datasetName] : throw new NotImplementedException();

        public CalculationResultContainer Execute(IDictionary<string, ProviderSourceDataset> datasets, ProviderSummary providerSummary,
            IEnumerable<CalculationAggregation> aggregationValues = null)
        {
            object instance = CreateInstance(_allocationType);

            object datasetsInstance = CreateInstance(_datasetsSetter.PropertyType);
            _datasetsSetter.SetValue(instance, datasetsInstance);

            HashSet<string> datasetNamesUsed = new HashSet<string>();
            foreach (KeyValuePair<string, ProviderSourceDataset> datasetItem in datasets)
            {
                if (datasetItem.Value != null && !string.IsNullOrWhiteSpace(datasetItem.Value.DataRelationship?.Name))
                {
                    Type type = GetDatasetType(datasetItem.Value);

                    if (_datasetSetters.TryGetValue(datasetItem.Value.DataRelationship.Name, out PropertyInfo setter))
                    {
                        datasetNamesUsed.Add(datasetItem.Value.DataRelationship.Name);

                        SetDatasetInstanceValue(datasetItem.Value, type, setter, datasetsInstance);
                    }
                }
            }

            PropertyInfo providerSetter = instance.GetType().GetProperty("Provider");

            object provider = PopulateProvider(providerSummary, providerSetter);
            providerSetter.SetValue(instance, provider);

            if (!aggregationValues.IsNullOrEmpty())
            {
                SetInstanceAggregationsValues(instance, aggregationValues);
            }

            SetMissingDatasetDefaultObjects(datasetNamesUsed, _datasetSetters, datasetsInstance);

            // get all calculation results
            dynamic calculations = instance;
            ValueTuple<Dictionary<string, string[]>, Dictionary<string, string[]>> results =
                calculations.MainCalc(true);

            IEnumerable<CalculationResult> calculationResults = ProcessCalculationResults(results.Item1, _calculationResultFuncs);
            IEnumerable<FundingLineResult> fundingLineResults = ProcessFundingLineResults(results.Item2, _fundingLineResultFuncs);

            return new CalculationResultContainer
            {
                CalculationResults = calculationResults,
                FundingLineResults = fundingLineResults
            };
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
                    setter.SetValue(datasetsInstance, CreateInstance(setter.PropertyType));
                }
            }
        }

        private IList<CalculationResult> ProcessCalculationResults(Dictionary<string, string[]> results,
            List<(MemberInfo, CalculationAttributeMetadata)> funcs)
        {
            IList<CalculationResult> calculationResults = new List<CalculationResult>();
            foreach (KeyValuePair<string, string[]> calcResult in results)
            {
                if (calcResult.Value.Length < 1)
                {
                    _logger.Error("The number of items returned from the key value pair in the calculation results is under the minimum required.");
                    continue;
                }

                (MemberInfo field, CalculationAttributeMetadata metadata) = funcs.FirstOrDefault(m =>
                    string.Equals(m.Item2.Calculation.Id, calcResult.Key, StringComparison.InvariantCultureIgnoreCase));

                if (field != null)
                {
                    CalculationResult calculationResult = ProcessCalculationResult(metadata, calcResult);

                    calculationResults.Add(calculationResult);
                }
            }

            return calculationResults;
        }

        private IList<FundingLineResult> ProcessFundingLineResults(Dictionary<string, string[]> results,
            List<(MemberInfo, FundingLineAttributeMetadata)> funcs)
        {
            IList<FundingLineResult> fundingLineResults = new List<FundingLineResult>();

            foreach (KeyValuePair<string, string[]> flResult in results)
            {
                if (flResult.Value.Length < 1)
                {
                    _logger.Error("The number of items returned from the key value pair in the calculation results is under the minimum required.");
                    continue;
                }

                (MemberInfo field, FundingLineAttributeMetadata metadata) = funcs.FirstOrDefault(m =>
                    string.Equals($"{m.Item2.FundingLineFundingStreamId}-{m.Item2.FundingLine.Id}", flResult.Key, StringComparison.InvariantCultureIgnoreCase));

                if (field != null)
                {
                    FundingLineResult fundingLineResult = ProcessFundingLineResult(metadata, flResult);
                    fundingLineResults.Add(fundingLineResult);
                }
            }

            return fundingLineResults;
        }

        private static CalculationResult ProcessCalculationResult(CalculationAttributeMetadata metadata, KeyValuePair<string, string[]> calcResult)
        {
            const int exceptionTypeIndex = 1;
            const int exceptionMessageIndex = 2;
            const int exceptionStackTraceIndex = 3;
            const int exceptionElapsedTimeIndex = 4;

            CalculationResult calculationResult = new CalculationResult()
            {
                Calculation = metadata.Calculation,
                CalculationDataType = metadata.CalculationDataType,
            };

            SetCalculationResultValue(calculationResult, calcResult);

            if (calcResult.Value.Length > exceptionMessageIndex)
            {
                calculationResult.ExceptionType = calcResult.Value[exceptionTypeIndex];
                calculationResult.ExceptionMessage = calcResult.Value[exceptionMessageIndex];

                if (calcResult.Value.Length > exceptionStackTraceIndex) calculationResult.ExceptionStackTrace = calcResult.Value[exceptionStackTraceIndex];

                if (calcResult.Value.Length > exceptionElapsedTimeIndex)
                {
                    if (TimeSpan.TryParse(calcResult.Value[exceptionElapsedTimeIndex], out TimeSpan ts))
                    {
                        calculationResult.ElapsedTime = ts.Ticks;
                    }
                }
            }

            return calculationResult;
        }

        private static FundingLineResult ProcessFundingLineResult(FundingLineAttributeMetadata metadata, KeyValuePair<string, string[]> flResult)
        {
            const int valueIndex = 0;
            const int exceptionTypeIndex = 1;
            const int exceptionMessageIndex = 2;
            const int exceptionStackTraceIndex = 3;

            FundingLineResult fundingLineResult = new FundingLineResult()
            {
                FundingLine = metadata.FundingLine,
                FundingLineFundingStreamId = metadata.FundingLineFundingStreamId,
            };

            fundingLineResult.Value = flResult.Value[valueIndex].GetValueOrNull<decimal>();

            if (flResult.Value.Length > exceptionMessageIndex)
            {
                fundingLineResult.ExceptionType = flResult.Value[exceptionTypeIndex];
                fundingLineResult.ExceptionMessage = flResult.Value[exceptionMessageIndex];

                if (flResult.Value.Length > exceptionStackTraceIndex)
                {
                    fundingLineResult.ExceptionStackTrace = flResult.Value[exceptionStackTraceIndex];
                }
            }

            return fundingLineResult;
        }

        private static void SetCalculationResultValue(CalculationResult calculationResult, KeyValuePair<string, string[]> calcResult)
        {
            const int valueIndex = 0;

            calculationResult.Value = calculationResult.CalculationDataType switch
            {
                CalculationDataType.Decimal => calcResult.Value[valueIndex].GetValueOrNull<decimal>(),
                CalculationDataType.String => calcResult.Value[valueIndex],
                CalculationDataType.Boolean => calcResult.Value[valueIndex].GetValueOrNull<bool>(),
                CalculationDataType.Enum => calcResult.Value[valueIndex],
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        private void SetInstanceAggregationsValues(object instance, IEnumerable<CalculationAggregation> aggregationValues)
        {
            PropertyInfo aggregationsSetter = instance.GetType().GetProperty("Aggregations");

            if (aggregationsSetter != null)
            {
                Type propType = aggregationsSetter.PropertyType;

                object data = CreateInstance(propType);

                MethodInfo add = data.GetType().GetMethod("Add", new[] { typeof(String), typeof(Decimal) });

                foreach (CalculationAggregation aggregations in aggregationValues)
                {
                    foreach (AggregateValue aggregatedValue in aggregations.Values)
                    {
                        add.Invoke(data, new object[] { aggregatedValue.FieldReference, aggregatedValue.Value.HasValue ? aggregatedValue.Value.Value : 0 });
                    }
                }

                aggregationsSetter.SetValue(instance, data);
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

                object list = CreateInstance(constructGeneric);

                MethodInfo addMethod = list.GetType().GetMethod("Add");

                Type itemType = list.GetType().GenericTypeArguments.First();

                object[] rows = dataset.Current.Rows.Select(x => PopulateRow(itemType, x)).ToArray();

                foreach (object row in rows)
                {
                    addMethod.Invoke(list, new[] { row });
                }

                setter.SetValue(datasetsInstance, list);
            }
        }

        private Type GetDatasetType(ProviderSourceDataset dataset)
        {
            if (!string.IsNullOrWhiteSpace(dataset.DataDefinitionId))
            {
                return DatasetType(dataset.DataDefinitionId);
            }
            else
            {
                return DatasetType(dataset.DataDefinition.Id);
            }
        }

        private object PopulateProvider(ProviderSummary providerSummary, PropertyInfo providerSetter)
        {
            Type type = providerSetter.PropertyType;

            object data = CreateInstance(type);

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
                    case "ProviderTypeCode":
                        property.SetValue(data, providerSummary.ProviderTypeCode.EmptyIfNull());
                        break;

                    case "ProviderSubTypeCode":
                        property.SetValue(data, providerSummary.ProviderSubTypeCode.EmptyIfNull());
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

                    case "LondonRegionCode":
                        property.SetValue(data, providerSummary.LondonRegionCode.EmptyIfNull());
                        break;

                    case "LodonRegionName":
                        property.SetValue(data, providerSummary.LondonRegionName.EmptyIfNull());
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

                    case "ReasonEstablishmentOpened":
                        property.SetValue(data, providerSummary.ReasonEstablishmentOpened.EmptyIfNull());
                        break;

                    case "ReasonEstablishmentClosed":
                        property.SetValue(data, providerSummary.ReasonEstablishmentClosed.EmptyIfNull());
                        break;

                    case "FurtherEducationTypeCode":
                        property.SetValue(data, providerSummary.FurtherEducationTypeCode.EmptyIfNull());
                        break;

                    case "FurtherEducationTypeName":
                        property.SetValue(data, providerSummary.FurtherEducationTypeName.EmptyIfNull());
                        break;

                    case "HasSuccessor":
                        property.SetValue(data, providerSummary.Successors.AnyWithNullCheck());
                        break;

                    case "HasPredecessor":
                        property.SetValue(data, providerSummary.Predecessors.AnyWithNullCheck());
                        break;

                    case "PreviousLaCode":
                        property.SetValue(data, providerSummary.PreviousLaCode.EmptyIfNull());
                        break;

                    case "PreviousLaName":
                        property.SetValue(data, providerSummary.PreviousLaName.EmptyIfNull());
                        break;

                    case "PreviousEstablishmentNumber":
                        property.SetValue(data, providerSummary.PreviousEstablishmentNumber.EmptyIfNull());
                        break;

                    case "PhaseOfEducationCode":
                        property.SetValue(data, providerSummary.PhaseOfEducationCode.EmptyIfNull());
                        break;

                    case "StatutoryLowAge":
                        property.SetValue(data, providerSummary.StatutoryLowAge.EmptyIfNull());
                        break;

                    case "StatutoryHighAge":
                        property.SetValue(data, providerSummary.StatutoryHighAge.EmptyIfNull());
                        break;

                    case "OfficialSixthFormCode":
                        property.SetValue(data, providerSummary.OfficialSixthFormCode.EmptyIfNull());
                        break;

                    case "OfficialSixthFormName":
                        property.SetValue(data, providerSummary.OfficialSixthFormName.EmptyIfNull());
                        break;

                    case "StatusCode":
                        property.SetValue(data, providerSummary.StatusCode.EmptyIfNull());
                        break;

                    case "ReasonEstablishmentOpenedCode":
                        property.SetValue(data, providerSummary.ReasonEstablishmentOpenedCode.EmptyIfNull());
                        break;

                    case "ReasonEstablishmentClosedCode":
                        property.SetValue(data, providerSummary.ReasonEstablishmentClosedCode.EmptyIfNull());
                        break;

                    default:
                        break;
                }
            }

            return data;
        }

        private object PopulateRow(Type type, Dictionary<string, object> row)
        {
            object data = CreateInstance(type);
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

                    int keyValue;

                    if (row.Count() > 0)
                    {
                        bool isNumber = int.TryParse(row.Keys.First(), out keyValue);

                        propertyName = GetProperty(fieldAttribute, isNumber ? "Id" : "Name");
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

        private static Reference GetReference(IList<CustomAttributeData> attributes, string attributeName)
        {
            CustomAttributeData attribute = attributes.FirstOrDefault(x => x.AttributeType.Name.StartsWith(attributeName));
            if (attribute != null)
            {
                return new Reference(GetProperty(attribute, "Id"), GetProperty(attribute, "Name"));
            }
            return null;
        }

        private static CalculationDataType GetCalculationDataType(IList<CustomAttributeData> attributes, string attributeName)
        {
            // Setting initial value as Decimal for backward compatibility
            CalculationDataType calculationDataType = CalculationDataType.Decimal;

            CustomAttributeData attribute = attributes.FirstOrDefault(x => x.AttributeType.Name.StartsWith(attributeName));
            if (attribute != null)
            {
                string calculationDataTypeString = GetProperty(attribute, "CalculationDataType");
                Enum.TryParse(calculationDataTypeString, out calculationDataType);
            }

            return calculationDataType;
        }

        private static string GetFundingStream(IList<CustomAttributeData> attributes, string attributeName)
        {
            CustomAttributeData attribute = attributes.FirstOrDefault(x => x.AttributeType.Name.StartsWith(attributeName));
            if (attribute != null)
            {
                return GetProperty(attribute, "FundingStream");
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
