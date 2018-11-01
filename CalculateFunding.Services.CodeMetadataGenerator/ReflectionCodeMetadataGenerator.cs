using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CalculateFunding.Models.Code;
using CalculateFunding.Services.CodeMetadataGenerator.Interfaces;

namespace CalculateFunding.Services.CodeMetadataGenerator
{
    public class ReflectionCodeMetadataGenerator : ICodeMetadataGeneratorService
    {
        public IEnumerable<TypeInformation> GetTypeInformation(byte[] rawAssembly)
        {
            if (rawAssembly == null || rawAssembly.Length == 0)
            {
                return Enumerable.Empty<TypeInformation>();
            }

            // Used for outputting assembly to local filesystem for debugging
            //using (FileStream fs = new FileStream("c:\\dev\\out.dll", FileMode.Create))
            //{
            //    fs.Write(rawAssembly, 0, rawAssembly.Length);
            //    fs.Flush();
            //    fs.Close();
            //}

            List<TypeInformation> results = new List<TypeInformation>();

            Assembly assembly = Assembly.Load(rawAssembly);

            foreach (TypeInfo typeInfo in assembly.DefinedTypes)
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

                    List<MethodInformation> methods = new List<MethodInformation>();
                    foreach (MethodInfo methodInfo in typeInfo.GetMethods())
                    {
                        if (IsAggregateFunction(methodInfo.Name))
                        {
                            methods.Add(ConfigureAggregateFunctionMetadata(methodInfo));
                        }
                        else
                        {
                            if (!methodInfo.IsSpecialName)
                            {
                                List<ParameterInformation> parameters = new List<ParameterInformation>();

                                foreach (ParameterInfo parameter in methodInfo.GetParameters())
                                {
                                    ParameterInformation parameterInformation = new ParameterInformation()
                                    {
                                        Name = parameter.Name,
                                        Description = parameter.Name,
                                        Type = ConvertTypeName(parameter.ParameterType),
                                    };

                                    parameters.Add(parameterInformation);
                                }

                                string entityId = null;

                                var calculationAttribute = methodInfo.CustomAttributes.Where(c => c.AttributeType.Name == "CalculationAttribute").FirstOrDefault();

                                if (calculationAttribute != null)
                                {
                                    entityId = calculationAttribute.NamedArguments.Where(a => a.MemberName == "Id").FirstOrDefault().TypedValue.Value?.ToString();
                                }

                                MethodInformation methodInformation = new MethodInformation()
                                {
                                    Name = methodInfo.Name,
                                    ReturnType = ConvertTypeName(methodInfo.ReturnType),
                                    Parameters = parameters,
                                    EntityId = entityId,
                                };

                                if (string.IsNullOrWhiteSpace(methodInformation.FriendlyName))
                                {
                                    methodInformation.FriendlyName = GetAttributeProperty(methodInfo.CustomAttributes, "Calculation", "Name");
                                }

                                if (string.IsNullOrWhiteSpace(methodInformation.Description))
                                {
                                    methodInformation.Description = GetAttributeProperty(methodInfo.CustomAttributes, "Description", "Description");
                                }

                                methods.Add(methodInformation);
                            }
                        }
                    }

                    List<PropertyInformation> properties = new List<PropertyInformation>();

                    foreach (PropertyInfo property in typeInfo.GetProperties())
                    {
                        if (!property.IsSpecialName && property.MemberType == MemberTypes.Property)
                        {
                            PropertyInformation propertyInformation = new PropertyInformation()
                            {
                                Name = property.Name,
                                Type = ConvertTypeName(property.PropertyType),
                            };

                            if (string.IsNullOrWhiteSpace(propertyInformation.FriendlyName))
                            {
                                propertyInformation.FriendlyName = GetAttributeProperty(property.CustomAttributes, "Field", "Name");
                            }

                            if (string.IsNullOrWhiteSpace(propertyInformation.FriendlyName))
                            {
                                propertyInformation.FriendlyName = GetAttributeProperty(property.CustomAttributes, "DatasetRelationship", "Name");
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

                            properties.Add(propertyInformation);
                        }
                    }

                    typeInformationModel.Methods = methods;
                    typeInformationModel.Properties = properties;

                    results.Add(typeInformationModel);
                }
            }
            return results;
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

        private string ConvertTypeName(Type type)
        {
            if (type.IsGenericType && type.GenericTypeArguments != null && type.GenericTypeArguments.Length > 0)
            {
                Type genericType = type.GenericTypeArguments.First();
                string name = type.Name;
                if (name.Contains("`1"))
                {
                    name = name.Substring(0, name.IndexOf("`1"));
                }
                return name + "(Of " + genericType.Name + ")";
            }

            return ConvertTypeName(type.Name);
        }

        private string ConvertTypeName(string typeFriendlyName)
        {
            switch (typeFriendlyName)
            {
                case "System.String":
                    return "String";

                case "System.Integer":
                    return "Integer";

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

            return new MethodInformation()
            {
                Name = methodInfo.Name,
                ReturnType = ConvertTypeName(methodInfo.ReturnType),
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
    }
}
