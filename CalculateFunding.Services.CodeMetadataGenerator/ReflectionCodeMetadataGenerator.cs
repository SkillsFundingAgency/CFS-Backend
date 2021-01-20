using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using CalculateFunding.Models.Code;
using CalculateFunding.Services.CodeMetadataGenerator.Interfaces;

namespace CalculateFunding.Services.CodeMetadataGenerator
{
    public class ReflectionCodeMetadataGenerator : ICodeMetadataGeneratorService
    {
        private static readonly Regex regex = new Regex("^System\\.Func`1\\[\\[System\\.Nullable`1\\[\\[(([a-z]|[A-Z]|\\+|\\.)+),");

        private static readonly string[] filteredPropertyNames = {
                                "Dictionary",
                                "DictionaryDecimalValues",
                                "DictionaryBooleanValues",
                                "DictionaryStringValues",
                                "FundingLineDictionary",
                                "FundingLineDictionaryValues"
                            };

        private static readonly List<string> filteredMethodNames = new List<string>()
                    {
                        "ToString",
                        "GetHashCode",
                        "Equals",
                        "GetType",
                        "Initialise",
                        "GetTypeCode",
                    };

        private static readonly List<string> filteredTemplateCalcMethods = new List<string>()
                    {
                        "Exclude",
                        "Avg",
                        "Sum",
                        "Min",
                        "Max"
                    };

        public IEnumerable<TypeInformation> GetTypeInformation(byte[] rawAssembly)
        {
            if (rawAssembly == null || rawAssembly.Length == 0)
            {
                return Enumerable.Empty<TypeInformation>();
            }

            List<TypeInformation> results = new List<TypeInformation>();

            Assembly assembly = Assembly.Load(rawAssembly);
            List<string> propertyNamesToFilterFromCalculationContexts = GeneratePropertyNameToFilterForCalculationContexts(assembly);

            TypeInfo[] assemblyDefinedTypes = assembly.DefinedTypes.ToArray();

            foreach (TypeInfo typeInfo in assemblyDefinedTypes)
            {
                if (typeInfo != null)
                {
                    if (typeInfo.IsNotPublic)
                    {
                        continue;
                    }

                    TypeInformation typeInformationModel = new TypeInformation()
                    {
                        Name = typeInfo.Name,
                        Description = GetAttributeProperty(typeInfo.CustomAttributes, "Description", "Description"),
                        Type = ConvertTypeName(typeInfo.FullName),
                    };

                    if (typeInfo.Name.EndsWith("Exception", StringComparison.InvariantCultureIgnoreCase))
                    {
                        continue;
                    }

                    if (typeInfo.Name.StartsWith("_Closure$", StringComparison.InvariantCultureIgnoreCase))
                    {
                        continue;
                    }

                    if (typeInfo.Name.Equals("ProjectData", StringComparison.InvariantCultureIgnoreCase))
                    {
                        continue;
                    }

                    bool isTemplateCalculationContainerClass = IsTemplateCalculationContextClass(typeInfo);

                    List<MethodInformation> methods = new List<MethodInformation>();
                    foreach (MethodInfo methodInfo in typeInfo.GetMethods())
                    {
                        if (isTemplateCalculationContainerClass && filteredTemplateCalcMethods.Contains(methodInfo.Name))
                        {
                            continue;
                        }

                        if (IsAggregateFunction(methodInfo.Name))
                        {
                            methods.Add(ConfigureAggregateFunctionMetadata(methodInfo));
                        }
                        else if (filteredMethodNames.Any(f => string.Equals(methodInfo.Name, f, StringComparison.InvariantCultureIgnoreCase)))
                        {
                            continue;
                        }
                        else
                        {
                            if (!methodInfo.IsSpecialName)
                            {
                                List<ParameterInformation> parameters = new List<ParameterInformation>();

                                foreach (ParameterInfo parameter in methodInfo.GetParameters())
                                {
                                    var parameterReturnDetails = ConvertTypeName(parameter.ParameterType);

                                    ParameterInformation parameterInformation = new ParameterInformation()
                                    {
                                        Name = parameter.Name,
                                        Description = parameter.Name,
                                        Type = parameterReturnDetails.fullReturnType,
                                        TypeClass = parameterReturnDetails.directType,
                                    };

                                    parameters.Add(parameterInformation);
                                }

                                string entityId = null;

                                bool isCustom = false;

                                var calculationAttribute = methodInfo.CustomAttributes.Where(c => c.AttributeType.Name == "CalculationAttribute").FirstOrDefault();

                                if (calculationAttribute != null)
                                {
                                    entityId = calculationAttribute.NamedArguments.Where(a => a.MemberName == "Id").FirstOrDefault().TypedValue.Value?.ToString();
                                    isCustom = true;
                                }

                                var (fullReturnType, directType, isNullable) = ConvertTypeName(methodInfo.ReturnType);

                                MethodInformation methodInformation = new MethodInformation()
                                {
                                    Name = methodInfo.Name,
                                    ReturnType = fullReturnType,
                                    ReturnTypeClass = directType,
                                    ReturnTypeIsNullable = isNullable,
                                    Parameters = parameters,
                                    EntityId = entityId,
                                    IsCustom = isCustom
                                };

                                if (string.IsNullOrWhiteSpace(methodInformation.FriendlyName))
                                {
                                    methodInformation.FriendlyName = GetAttributeProperty(methodInfo.CustomAttributes, "Calculation", "Name");
                                }

                                if (string.IsNullOrWhiteSpace(methodInformation.Description))
                                {
                                    methodInformation.Description = GetAttributeProperty(methodInfo.CustomAttributes, "Description", "Description");
                                }

                                if (methodInfo.GetCustomAttribute<System.ComponentModel.EditorBrowsableAttribute>()?.State != System.ComponentModel.EditorBrowsableState.Never)
                                {
                                    methods.Add(methodInformation);
                                }
                            }
                        }
                    }

                    FieldInfo[] fieldInfos = typeInfo.DeclaredFields.ToArray();

                    List<string> enumValues = new List<string>();
                    AddMethodsForClass(methods, fieldInfos, enumValues);

                    List<PropertyInformation> properties = new List<PropertyInformation>();

                    AddClassProperties(propertyNamesToFilterFromCalculationContexts, typeInfo, isTemplateCalculationContainerClass, properties);

                    typeInformationModel.Methods = methods;
                    typeInformationModel.Properties = properties;
                    typeInformationModel.EnumValues = enumValues;

                    results.Add(typeInformationModel);
                }
            }
            IEnumerable<TypeInformation> dataTypes = GetDefaultTypes();

            foreach (TypeInformation typeInformation in dataTypes)
            {
                results.Add(typeInformation);
            }

            IEnumerable<TypeInformation> keywords = GetKeywords();

            foreach (TypeInformation typeInformation in keywords)
            {
                results.Add(typeInformation);
            }

            return results;
        }

        private void AddClassProperties(List<string> propertyNamesToFilterFromCalculationContexts, TypeInfo typeInfo, bool isTemplateCalculationContainerClass, List<PropertyInformation> properties)
        {
            foreach (PropertyInfo property in typeInfo.GetProperties())
            {
                if (!property.IsSpecialName && property.MemberType == MemberTypes.Property)

                {
                    if (filteredPropertyNames.Contains(property.Name))
                    {
                        continue;
                    }

                    if ((isTemplateCalculationContainerClass || IsFundingLineClass(typeInfo))
                        && propertyNamesToFilterFromCalculationContexts.Contains(property.Name))
                    {
                        //NB this continuation is never hit as the IsFundingLineClass evaluation is never true due
                        //the && of the field attribute check into it
                        continue;
                    }

                    if (typeInfo.Name.EndsWith("FundingLines") && property.PropertyType.Name.EndsWith("Calculations"))
                    {
                        //don't include the calculations property in the funding lines class as this causes
                        //an infinite loop in the UI code
                        continue;
                    }

                    (string fullReturnType, string directType, bool isNullable) returnDetails = ConvertTypeName(property.PropertyType);

                    PropertyInformation propertyInformation = new PropertyInformation()
                    {
                        Name = property.Name,
                        Type = returnDetails.fullReturnType,
                        TypeClass = returnDetails.directType,
                        IsNullable = returnDetails.isNullable,
                    };

                    if (string.IsNullOrWhiteSpace(propertyInformation.FriendlyName))
                    {
                        propertyInformation.FriendlyName = GetAttributeProperty(property.CustomAttributes, "DatasetRelationship", "Name");
                    }

                    if (string.IsNullOrWhiteSpace(propertyInformation.FriendlyName))
                    {
                        propertyInformation.FriendlyName = GetAttributeProperty(property.CustomAttributes, "Field", "Name");
                    }

                    if (string.IsNullOrWhiteSpace(propertyInformation.FriendlyName))
                    {
                        propertyInformation.FriendlyName = GetAttributeProperty(property.CustomAttributes, "Calculation", "Name");
                    }

                    if (string.IsNullOrWhiteSpace(propertyInformation.Description))
                    {
                        propertyInformation.Description = GetAttributeProperty(property.CustomAttributes, "Description", "Description");
                    }

                    if (string.IsNullOrWhiteSpace(propertyInformation.IsAggregable))
                    {
                        propertyInformation.IsAggregable = GetAttributeProperty(property.CustomAttributes, "IsAggregable", "IsAggregable");
                    }

                    if (property.GetCustomAttribute<System.ComponentModel.EditorBrowsableAttribute>()?.State != System.ComponentModel.EditorBrowsableState.Never)
                    {
                        properties.Add(propertyInformation);
                    }
                }
            }
        }

        private void AddMethodsForClass(List<MethodInformation> methods, FieldInfo[] fieldInfos, List<string> enumValues)
        {
            foreach (FieldInfo fieldInfo in fieldInfos.Where(m => m.FieldType == typeof(Func<decimal?>)
                                || m.CustomAttributes.Any(c => c.AttributeType.Name == "CalculationAttribute")
                                || m.CustomAttributes.Any(c => c.AttributeType.Name == "FundingLineAttribute")
                                || m.IsLiteral).ToList())
            {
                if (!fieldInfo.IsSpecialName)
                {
                    if (fieldInfo.IsLiteral)
                    {
                        enumValues.Add(fieldInfo.Name);
                        continue;
                    }

                    string entityId = null;

                    bool isCustom = false;
                    ApplyCalculationMetadata(fieldInfo, ref entityId, ref isCustom);
                    ApplyFundingLineMetadata(fieldInfo, ref entityId, ref isCustom);

                    (string fullReturnType, string directType, bool isNullable) returnDetails = ConvertTypeName(fieldInfo.FieldType);

                    MethodInformation methodInformation = new MethodInformation()
                    {
                        Name = fieldInfo.Name,
                        ReturnType = returnDetails.fullReturnType,
                        ReturnTypeClass = returnDetails.directType,
                        ReturnTypeIsNullable = returnDetails.isNullable,
                        EntityId = entityId,
                        IsCustom = isCustom
                    };

                    if (string.IsNullOrWhiteSpace(methodInformation.FriendlyName))
                    {
                        methodInformation.FriendlyName = GetAttributeProperty(fieldInfo.CustomAttributes, "Calculation", "Name");
                    }

                    if (string.IsNullOrWhiteSpace(methodInformation.FriendlyName))
                    {
                        methodInformation.FriendlyName = GetAttributeProperty(fieldInfo.CustomAttributes, "FundingLine", "Name");
                    }

                    if (string.IsNullOrWhiteSpace(methodInformation.Description))
                    {
                        methodInformation.Description = GetAttributeProperty(fieldInfo.CustomAttributes, "Description", "Description");
                    }

                    if (fieldInfo.GetCustomAttribute<System.ComponentModel.EditorBrowsableAttribute>()?.State != System.ComponentModel.EditorBrowsableState.Never)
                    {
                        methods.Add(methodInformation);
                    }
                }
            }
        }

        private static void ApplyCalculationMetadata(FieldInfo fieldInfo, ref string entityId, ref bool isCustom)
        {
            CustomAttributeData calculationAttribute = fieldInfo.CustomAttributes.Where(c => c.AttributeType.Name == "CalculationAttribute").FirstOrDefault();

            if (calculationAttribute != null)
            {
                entityId = calculationAttribute.NamedArguments.Where(a => a.MemberName == "Id").FirstOrDefault().TypedValue.Value?.ToString();
                isCustom = true;
            }
        }

        private static void ApplyFundingLineMetadata(FieldInfo fieldInfo, ref string entityId, ref bool isCustom)
        {
            CustomAttributeData calculationAttribute = fieldInfo.CustomAttributes.Where(c => c.AttributeType.Name == "FundingLineAttribute").FirstOrDefault();

            if (calculationAttribute != null)
            {
                entityId = calculationAttribute.NamedArguments.Where(a => a.MemberName == "Id").FirstOrDefault().TypedValue.Value?.ToString();
                isCustom = true;
            }
        }

        /// <summary>
        /// Generates property names of internal implementation properties to remove from output
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        private static List<string> GeneratePropertyNameToFilterForCalculationContexts(Assembly assembly)
        {
            TypeInfo calculationsContextType = assembly.DefinedTypes.Single(t => t.Name == "CalculationContext");

            IEnumerable<TypeInfo> calculationClassTypesToFilter = assembly.DefinedTypes.Where(t => t.BaseType?.Name == "BaseCalculation");
            IEnumerable<string> calculationClassTypesToFilterNames = calculationClassTypesToFilter.Select(t => t.Name);

            List<string> propertyNamesToFilterFromCalculationContexts = new List<string>()
            {
                "Provider",
                "Datasets",
            };

            foreach (PropertyInfo prop in calculationsContextType.GetProperties())
            {
                if (calculationClassTypesToFilterNames.Contains(prop.PropertyType.Name))
                {
                    propertyNamesToFilterFromCalculationContexts.Add(prop.Name);
                }
            }

            return propertyNamesToFilterFromCalculationContexts;
        }

        private static bool IsTemplateCalculationContextClass(TypeInfo typeInfo)
        {
            return typeInfo.BaseType != null
                                        && typeInfo.BaseType.Name.EndsWith("BaseCalculation")
                                        && !typeInfo.Name.EndsWith("CalculationContext");
        }

        private bool IsFundingLineClass(TypeInfo typeInfo)
        {
            //NB when debugging this expression never evaluates to true, should probably look into this as
            //there's a whole continuation branch in the calling method that is never being hit
            return typeInfo.Name.EndsWith("FundingLines") && ContainsFieldWithAttribute(typeInfo, "FundingLine");
        }

        private bool ContainsFieldWithAttribute(TypeInfo typeInfo, string attributeName)
        {
            return typeInfo.GetFields().Any(p => p.CustomAttributes.Any(a => a.AttributeType.Name == $"{attributeName}Attribute"));
        }

        public string GetAttributeProperty(IEnumerable<CustomAttributeData> customAttibutes, string attributeName, string propertyName)
        {
            CustomAttributeData descriptionProperty = customAttibutes.Where(c => c.AttributeType.Name == $"{attributeName}Attribute").SingleOrDefault();
            if (descriptionProperty != null)
            {
                CustomAttributeNamedArgument argument = descriptionProperty.NamedArguments.Where(a => a.MemberName == propertyName).FirstOrDefault();
                return argument.TypedValue.Value?.ToString();
            }

            return null;
        }

        private (string fullReturnType, string directType, bool isNullable) ConvertTypeName(Type type)
        {
            if (type.IsGenericType && type.GenericTypeArguments != null && type.GenericTypeArguments.Length > 0)
            {
                return GenerateGenericReturnType(type);
            }

            return GenerateNonGenericReturnType(type);
        }

        private (string fullReturnType, string directType, bool isNullable) GenerateGenericReturnType(Type type)
        {
            if (regex.IsMatch(type.FullName))
            {
                string[] split = regex.Split(type.FullName);

                string directTypeReturn = ConvertTypeName(split[1]);

                string fullReturnTypeResult = $"Nullable(Of {directTypeReturn})";

                return (fullReturnTypeResult, directTypeReturn, true);
            }

            Type genericType = type.GenericTypeArguments.First();
            string name = type.Name;
            if (name.Contains("`1"))
            {
                name = name.Substring(0, name.IndexOf("`1"));
            }

            string fullReturnTypeReturn = name + "(Of " + genericType.Name + ")";

            return (fullReturnTypeReturn, genericType.Name, false);
        }

        private (string fullReturnType, string directType, bool isNullable) GenerateNonGenericReturnType(Type type)
        {
            string typeName = ConvertTypeName(type.Name);

            return (typeName, typeName, false);
        }

        private string ConvertTypeName(string typeFriendlyName)
        {
            if (typeFriendlyName.StartsWith("CalculationContext+"))
            {
                typeFriendlyName = typeFriendlyName.Substring(19, typeFriendlyName.Length - 19);
            }

            switch (typeFriendlyName)
            {
                case "System.String":
                    return "String";

                case "System.Integer":
                    return "Integer";

                case "System.Decimal":
                    return "Decimal";

                case "Void":
                    return null;

                default:
                    return typeFriendlyName;
            }
        }

        private bool IsAggregateFunction(string methodName)
        {
            return methodName == "Sum" || methodName == "Avg" || methodName == "Min" || methodName == "Max";
        }

        private MethodInformation ConfigureAggregateFunctionMetadata(MethodInfo methodInfo)
        {
            string description = "";

            switch (methodInfo.Name)
            {
                case "Sum":
                    description = "Sums all values for selected dataset field";
                    break;
                case "Avg":
                    description = "Averages all values for selected dataset field";
                    break;
                case "Min":
                    description = "Gets the minimum of all values for selected dataset field";
                    break;
                case "Max":
                    description = "Gets the maximum of all values for selected dataset field";
                    break;
                default:
                    throw new ArgumentException("Invalid aggregate function name was provided");
            }

            (string fullReturnType, string directType, bool isNullable) returnDetails = ConvertTypeName(methodInfo.ReturnType);

            return new MethodInformation()
            {

                Name = methodInfo.Name,
                ReturnType = returnDetails.fullReturnType,
                ReturnTypeClass = returnDetails.directType,
                ReturnTypeIsNullable = returnDetails.isNullable,
                Parameters = new[] { new ParameterInformation()
                    {
                        Name = "field",
                        Description = "Selected dataset field",
                        Type = "DatasetField"
                    }
                },
                Description = description
            };
        }

        private static IEnumerable<TypeInformation> GetDefaultTypes()
        {
            return new[]
            {
                new TypeInformation("Boolean", "True or False"),
                new TypeInformation("Byte", "0 through 255 (unsigned)"),
                new TypeInformation("Char", "0 through 65535 (unsigned)"),
                new TypeInformation("Date", "0:00:00 (midnight) on January 1, 0001 through 11:59:59 PM on December 31, 9999"),
                new TypeInformation("Decimal", "0 through +/-79,228,162,514,264,337,593,543,950,335 (+/-7.9...E+28) with no decimal point; 0 through +/-7.9228162514264337593543950335 with 28 places to the right of the decimal"),
                new TypeInformation("Double", "-1.79769313486231570E+308 through -4.94065645841246544E-324, for negative values 4.94065645841246544E-324 through 1.79769313486231570E+308, for positive values"),
                new TypeInformation("Integer", "-2,147,483,648 through 2,147,483,647 (signed)"),
                new TypeInformation("Long", "-9,223,372,036,854,775,808 through 9,223,372,036,854,775,807(signed)"),
                new TypeInformation("Object", "Any type can be stored in a variable of type Object"),
                new TypeInformation("SByte", "-128 through 127 (signed)"),
                new TypeInformation("Short", "-32,768 through 32,767 (signed)"),
                new TypeInformation("Single", "-3.4028235E+38 through -1.401298E-45 for negative values; 1.401298E-45 through 3.4028235E+38 for positive values"),
                new TypeInformation("String", "0 to approximately 2 billion Unicode characters"),
                new TypeInformation("UInteger", "0 through 4,294,967,295 (unsigned)"),
                new TypeInformation("ULong", "0 through 18,446,744,073,709,551,615 (unsigned)"),
                new TypeInformation("UShort", "0 through 65,535 (unsigned)"),
            };
        }

        private static IEnumerable<TypeInformation> GetKeywords()
        {
            return new[]
            {
                new TypeInformation("If"),
                new TypeInformation("ElseIf"),
                new TypeInformation("EndIf"),
                new TypeInformation("Then"),
                new TypeInformation("If-Then"),
                new TypeInformation("If-Then-Else"),
                new TypeInformation("If-Then-ElseIf-Then"),
            };
        }
    }
}
