using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.Policy;
using CalculateFunding.Models.Providers;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Models.Specs;
using CalculateFunding.Models.UnitTests.SearchIndexModels;
using CalculateFunding.Models.Users;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace CalculateFunding.Models.UnitTests
{
#if NCRUNCH
    [Ignore]
#endif
    [TestClass]
    public class SearchIndexTests
    {
        private string searchIndexDirectoryPath = $@"{AppDomain.CurrentDomain.BaseDirectory}..\..\..\..\DevOps\search-indexes";

        [TestMethod]
        public void SearchIndexTest_GivenSearchModels_EnsureCorrespondingJsonSchemaExists()
        {
            // Arrange
            IList<string> ErrorLog = new List<string>();

            IEnumerable<Type> searchIndexTypes = GetTypesWithSearchIndexAttribute();

            foreach (Type type in searchIndexTypes)
            {
                try
                {
                    CustomAttributeData indexNameMember = type.CustomAttributes.FirstOrDefault(m => m.NamedArguments.FirstOrDefault(n => n.MemberName == "IndexName") != null);

                    if (indexNameMember == null)
                    {
                        ErrorLog.Add($"Missing IndexName attribute on model {type.Name}");
                    }
                    else
                    {
                        string indexName = indexNameMember.NamedArguments.First(m => m.MemberName == "IndexName").TypedValue.Value.ToString();

                        if (indexName.ToLowerInvariant() != type.Name.ToLowerInvariant())
                        {
                            ErrorLog.Add($"Index name {indexName} does not match type name {type.Name}");
                        }

                        if (!indexName.EndsWith("index"))
                        {
                            ErrorLog.Add($"Index name {indexName} is not valid");
                        }

                        string jsonFilePath = $@"{searchIndexDirectoryPath}\{indexName}\{indexName}.json";

                        if (!File.Exists(jsonFilePath))
                        {
                            ErrorLog.Add($"Missing corresponding json schema for {type.Name}");
                        }
                    }
                }
                catch (Exception e)
                {
                    ErrorLog.Add($"Unexpected error checking '{type.Name}': {e.Message}{Environment.NewLine}{e.StackTrace}");
                }
            }

            //Assert
            if (ErrorLog.Any())
            {
                Assert.Fail(string.Join(Environment.NewLine, ErrorLog));
            }
        }

        [TestMethod]
        public void SearchIndexTest_GivenSearchSchemasAndModels_EnsureFieldsMatch()
        {
            // Arrange
            IList<string> ErrorLog = new List<string>();
            UserIndex userIndex = new UserIndex();
            DatasetDefinitionIndex datasetDefinitionIndex = new DatasetDefinitionIndex();
            ProvidersIndex providersIndex = new ProvidersIndex();
            PublishedFundingIndex publishedfundingindex = new PublishedFundingIndex();
            PublishedProviderIndex publishedProviderIndex = new PublishedProviderIndex();
            SpecificationIndex specificationindex = new SpecificationIndex();
            TemplateIndex templateIndex = new TemplateIndex();           

            IEnumerable<Type> searchIndexTypes = GetTypesWithSearchIndexAttribute();

            IEnumerable<string> indexNames = Directory
                .GetDirectories(searchIndexDirectoryPath, "*index", SearchOption.TopDirectoryOnly)
                .Select(m => new DirectoryInfo(m).Name);

            //Act
            foreach (string indexName in indexNames)
            {
                try
                {
                    string jsonFilePath = $@"{searchIndexDirectoryPath}\{indexName}\{indexName}.json";

                    string jsonText = File.ReadAllText(jsonFilePath, Encoding.UTF8);
                    SearchIndexSchema searchIndexSchema = JsonConvert.DeserializeObject<SearchIndexSchema>(jsonText);
                    if (searchIndexSchema?.Name == null)
                    {
                        ErrorLog.Add(string.IsNullOrWhiteSpace(jsonText)
                            ? $"{indexName} json is blank"
                            : $"{indexName} json name is not available");
                    }
                    else if (searchIndexSchema.Name != indexName)
                    {
                        ErrorLog.Add($"Expected to find index { indexName }, but found { searchIndexSchema.Name }");
                    }
                    else
                    {
                        Type searchIndexType = searchIndexTypes
                            .FirstOrDefault(m => m.CustomAttributes
                                                     .FirstOrDefault(p => p.NamedArguments
                                                         .Any(n => n.TypedValue.Value.ToString() == searchIndexSchema.Name)) != null);

                        IEnumerable<string> searchIndexProperties = searchIndexType.GetProperties()
                            .Select(m => m.CustomAttributes
                                .FirstOrDefault(a => a.AttributeType.Name == "JsonPropertyAttribute")
                                ?.ConstructorArguments[0].Value.ToString().ToLower())
                            .Where(p => p != null);

                        IEnumerable<string> searchIndexJsonProperties = searchIndexSchema.Fields
                            .Select(m => m.Name.ToLower())
                            .Where(p => p != null);

                        if (!searchIndexProperties.Any())
                        {
                            ErrorLog.Add($"Index {indexName}: The model contains no properties");
                        }
                        else if (!searchIndexJsonProperties.Any())
                        {
                            ErrorLog.Add($"Index {indexName}: The json contains no properties");
                        }
                        else
                        {
                            IEnumerable<string> notInJson = searchIndexProperties.Except(searchIndexJsonProperties);
                            if (notInJson.Any())
                            {
                                string properties = string.Join(",", notInJson);
                                ErrorLog.Add($"Index {indexName}: The model contains the following properties not found in the json schema ({properties})");
                            }

                            IEnumerable<string> notInModel = searchIndexJsonProperties.Except(searchIndexProperties);
                            if (notInModel.Any())
                            {
                                string properties = string.Join(",", notInModel);
                                ErrorLog.Add($"Index {indexName}: The json schema contains the following properties not found in the model ({properties})");
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    ErrorLog.Add($"Unexpected error checking '{indexName}': {e.Message}{Environment.NewLine}{ e.StackTrace }");
                }
            }

            //Assert
            if (ErrorLog.Any())
            {
                Assert.Fail(string.Join(Environment.NewLine, ErrorLog));
            }
        }

        [TestMethod]
        public void SearchIndexTest_GivenSearchSchemasAndModels_EnsuresAttributesMatch()
        {
            //Arrange
            IList<string> ErrorLog = new List<string>();
            UserIndex userIndex = new UserIndex();
            DatasetDefinitionIndex datasetDefinitionIndex = new DatasetDefinitionIndex();
            ProvidersIndex providersIndex = new ProvidersIndex();
            PublishedFundingIndex publishedfundingindex = new PublishedFundingIndex();
            PublishedProviderIndex publishedProviderIndex = new PublishedProviderIndex();
            SpecificationIndex specificationindex = new SpecificationIndex();
            TemplateIndex templateIndex = new TemplateIndex();
           

            IEnumerable<Type> searchIndexTypes = GetTypesWithSearchIndexAttribute();

            IEnumerable<string> indexNames = Directory
                .GetDirectories(searchIndexDirectoryPath, "*index", SearchOption.TopDirectoryOnly)
                .Select(m => new DirectoryInfo(m).Name);
            
            //Act
            foreach (string indexName in indexNames)
            {
                try
                {
                    string jsonFilePath = $@"{searchIndexDirectoryPath}\{indexName}\{indexName}.json";

                    string jsonText = File.ReadAllText(jsonFilePath, Encoding.UTF8);

                    SearchIndexSchema searchIndexSchema = JsonConvert.DeserializeObject<SearchIndexSchema>(jsonText);
                    if (searchIndexSchema?.Name == null)
                    {
                        ErrorLog.Add(string.IsNullOrWhiteSpace(jsonText)
                            ? $"{indexName} json is blank"
                            : $"{indexName} json name is not available");
                    }
                    else if (searchIndexSchema.Name != indexName)
                    {
                        ErrorLog.Add($"Expected to find index {indexName}, but found {searchIndexSchema.Name}");
                    }
                    else
                    {
                        Type searchIndexType = searchIndexTypes.FirstOrDefault(m =>
                            m.CustomAttributes.FirstOrDefault(p =>
                                p.NamedArguments.Any(n =>
                                    n.TypedValue.Value.ToString() == searchIndexSchema.Name)) != null);

                        IEnumerable<PropertyInfo> searchIndexProperties = searchIndexType.GetProperties();

                        foreach (SearchIndexField searchIndexField in searchIndexSchema.Fields)
                        {
                            PropertyInfo matchedProperty = searchIndexProperties.FirstOrDefault(m =>
                                string.Equals(m.CustomAttributes
                                                  .SingleOrDefault(x => x.AttributeType.Name == "JsonPropertyAttribute")
                                                  ?.ConstructorArguments[0].ToString().Replace("\"", "")
                                              ?? m.Name,
                                    searchIndexField.Name,
                                    StringComparison.InvariantCultureIgnoreCase));

                            if (matchedProperty == null)
                            {
                                ErrorLog.Add($"{indexName}: {searchIndexField.Name} did not match any properties");
                            }
                            else
                            {
                                CheckIndexFieldTypes(matchedProperty, searchIndexField, ErrorLog, indexName);
                                CheckIndexAttributes(matchedProperty, searchIndexField, ErrorLog, indexName);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    ErrorLog.Add($"Unexpected error checking '{indexName}': {e.Message}{Environment.NewLine}{e.StackTrace}");
                }
            }

            //Assert
            if (ErrorLog.Any())
            {
                Assert.Fail(string.Join(Environment.NewLine, ErrorLog));
            }
        }

        private void CheckIndexFieldTypes(PropertyInfo matchedProperty, SearchIndexField searchIndexField, IList<string> ErrorLog, string indexName)
        {
            bool modelIsCollection = false;
            bool jsonIsCollection = false;

            string modelType = matchedProperty.PropertyType.Name == "Nullable`1"
                ? matchedProperty.PropertyType.GenericTypeArguments[0].Name
                : matchedProperty.PropertyType.Name;
            if (modelType.EndsWith("[]"))
            {
                modelType = modelType.Replace("[]", "");
                modelIsCollection = true;
            }
            else if(matchedProperty.PropertyType.Name == "IEnumerable`1")
            {
                modelType = matchedProperty.PropertyType.GenericTypeArguments[0].Name;
                modelIsCollection = true;
            }

            string jsonType = searchIndexField.Type;

            if (jsonType.StartsWith("Collection("))
            {
                jsonType = jsonType.Substring(11, jsonType.Length - 12);
                jsonIsCollection = true;
            }
            jsonType = jsonType.Split(new[] { '.' })[1];

            if (modelType.ToLowerInvariant() != jsonType.ToLowerInvariant() || modelIsCollection != jsonIsCollection)
            {
                ErrorLog.Add($"Expected {indexName}.{ matchedProperty.Name } to be of type { (modelIsCollection ? " collection of " : "") }{ modelType }, but found { (jsonIsCollection ? " collection of " : "") } { jsonType }");
            }
        }

        private void CheckIndexAttributes(PropertyInfo matchedProperty, SearchIndexField searchIndexField, IList<string> ErrorLog, string indexName)
        {
            IEnumerable<string> attributesEnabledInModel = matchedProperty.CustomAttributes
                .Where(m => !(new[] { "JsonPropertyAttribute", "JsonIgnoreAttribute" }).Contains(m.AttributeType.Name))
                .Select(m => m.AttributeType.Name.Replace("Attribute", "").Replace("Is", "").ToLower());

            IEnumerable<string> attributesEnabledInJson = searchIndexField.GetType().GetProperties()
                .Where(p => p.PropertyType == typeof(bool) &&
                            (bool)p.GetValue(searchIndexField, null))
                .Select(p => p.Name.ToLower());

            IEnumerable<string> unsyncedAttributes = attributesEnabledInModel.Except(attributesEnabledInJson);

            if (unsyncedAttributes.Any())
            {
                string attributes = string.Join(",", unsyncedAttributes);
                ErrorLog.Add(
                    $"Index {indexName}: {searchIndexField.Name} Json attributes ({attributes}) are false but should be true");
            }

            unsyncedAttributes = attributesEnabledInJson.Except(attributesEnabledInModel);

            if (unsyncedAttributes.Any())
            {
                string attributes = string.Join(",", unsyncedAttributes);
                ErrorLog.Add(
                    $"Index {indexName}: {searchIndexField.Name} Json attributes ({attributes}) are true but should be false");
            }
        }
            

        private static IEnumerable<Type> GetTypesWithSearchIndexAttribute()
        {
            foreach (var assemblyName in Assembly.GetExecutingAssembly().GetReferencedAssemblies())
            {
                Assembly modelsAssembly = Assembly.Load(assemblyName);
                foreach (Type type in modelsAssembly.GetTypes())
                {
                    if (type.GetCustomAttributes(typeof(SearchIndexAttribute), true).Length > 0)
                    {
                        yield return type;
                    }
                }
            }
        }
    }
}
