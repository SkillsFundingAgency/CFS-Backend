using System;
using System.Collections.Generic;
using System.IO;
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
                        Description = typeInfo.Name,
                        Type = ConvertTypeName(typeInfo.FullName),
                    };

                    List<MethodInformation> methods = new List<MethodInformation>();
                    foreach (MethodInfo methodInfo in typeInfo.GetMethods())
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
                                Description = methodInfo.Name + " description from backend",
                                ReturnType = ConvertTypeName(methodInfo.ReturnType),
                                Parameters = parameters,
                                EntityId = entityId,
                            };

                            methods.Add(methodInformation);
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
                                Description = property.Name + " description from backend",
                            };

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
    }
}
