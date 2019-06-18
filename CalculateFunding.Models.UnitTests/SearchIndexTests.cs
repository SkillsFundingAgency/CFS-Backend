using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CalculateFunding.Models.UnitTests.SearchIndexModels;
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
        private string searchIndexDirectoryPath = $@"{AppDomain.CurrentDomain.BaseDirectory}..\..\..\..\DevOps\search-indexes\";

        [TestMethod]
        public void SearchIndexTest_GivenSearchModels_EnsureCorrespondingJsonSchemaExists()
        {
            // Arrange
            IList<string> ErrorLog = new List<string>();

            IEnumerable<Type> searchIndexTypes = GetTypesWithSearchIndexAttribute();

            foreach (Type type in searchIndexTypes)
            {
                CustomAttributeData indexNameMember = type.CustomAttributes.FirstOrDefault(m => m.NamedArguments.FirstOrDefault(n => n.MemberName == "IndexName") != null);

                if(indexNameMember == null)
                {
                    ErrorLog.Add($"Missing IndexName attribute on model {type.Name}");
                }

                string indexName = indexNameMember.NamedArguments.First(m => m.MemberName == "IndexName").TypedValue.Value.ToString();

                string jsonFilePath = $@"{searchIndexDirectoryPath}\{indexName}\{indexName}.json";

                if (!File.Exists(jsonFilePath))
                {
                    ErrorLog.Add($"Missing coresponding json schema for {type.Name}");
                }
            }

            //Assert
            if (ErrorLog.Any())
            {
                Assert.Fail(string.Join("\r\n", ErrorLog));
            }
        }


        [TestMethod]
        public void SearchIndexTest_GivenSearchSchemasAndModels_EnsureFieldsMatch()
        {
            // Arrange
            IList<string> ErrorLog = new List<string>();

            IEnumerable<Type> searchIndexTypes = GetTypesWithSearchIndexAttribute();

            IEnumerable<string> indexNames = Directory.GetDirectories(searchIndexDirectoryPath, "*index", SearchOption.TopDirectoryOnly)
                .Select(m => new DirectoryInfo(m).Name);

            foreach (string indexName in indexNames)
            {
                string jsonFilePath = $@"{searchIndexDirectoryPath}\{indexName}\{indexName}.json";

                string jsonText = File.ReadAllText(jsonFilePath, Encoding.UTF8);

                SearchIndexSchema searchIndexSchema = JsonConvert.DeserializeObject<SearchIndexSchema>(jsonText);

                Type searchIndexType = searchIndexTypes.FirstOrDefault(m =>
                    m.CustomAttributes.FirstOrDefault(p =>
                    p.NamedArguments.Any(n =>
                    n.TypedValue.Value.ToString() == searchIndexSchema.Name)) != null);

                IEnumerable<string> searchIndexProperties = searchIndexType.GetProperties()
                    .Select(m => m.CustomAttributes
                        .First(a => a.AttributeType.Name == "JsonPropertyAttribute")
                        .ConstructorArguments[0].Value.ToString().ToLower());

                IEnumerable<string> searchIndexJsonProperties = searchIndexSchema.Fields.Select(m => m.Name.ToLower());

                bool areEquivalent = !searchIndexProperties.Except(searchIndexJsonProperties).Any();

                if (!areEquivalent)
                {
                    ErrorLog.Add($"Index {indexName}: The model contains the following properties not found in the json Schema ({ string.Join(",", searchIndexProperties.Except(searchIndexJsonProperties))})");
                }

                areEquivalent = !searchIndexJsonProperties.Except(searchIndexProperties).Any();

                if (!areEquivalent)
                {
                    ErrorLog.Add($"Index {indexName}: The json schema contains the following properties not found in the model ({ string.Join(",", searchIndexJsonProperties.Except(searchIndexProperties))})");
                }
            }

            //Assert
            if (ErrorLog.Any())
            {
                Assert.Fail(string.Join("\r\n", ErrorLog));
            }
        }

        [TestMethod]
        public void SearchIndexTest_GivenSearchSchemasAndModels_EnsuresAttributesMatch()
        {
            //Arrange
            IList<string> ErrorLog = new List<string>();

            IEnumerable<Type> searchIndexTypes = GetTypesWithSearchIndexAttribute();

            IEnumerable<string> indexNames = Directory.GetDirectories(searchIndexDirectoryPath, "*index", SearchOption.TopDirectoryOnly)
                .Select(m => new DirectoryInfo(m).Name);

            //Act
            foreach(string indexName in indexNames)
            {
                string jsonFilePath = $@"{searchIndexDirectoryPath}\{indexName}\{indexName}.json";

                string jsonText = File.ReadAllText(jsonFilePath, Encoding.UTF8);

                SearchIndexSchema searchIndexSchema = JsonConvert.DeserializeObject<SearchIndexSchema>(jsonText);

                Type searchIndexType = searchIndexTypes.FirstOrDefault(m =>
                    m.CustomAttributes.FirstOrDefault(p =>
                    p.NamedArguments.Any(n =>
                    n.TypedValue.Value.ToString() == searchIndexSchema.Name)) != null);

                IEnumerable<PropertyInfo> searchIndexProperties = searchIndexType.GetProperties();
                
                foreach(SearchIndexField searchIndexField in searchIndexSchema.Fields)
                {
                    PropertyInfo matchedProperty = searchIndexProperties.FirstOrDefault(m => 
                        string.Equals(m.Name, searchIndexField.Name, StringComparison.InvariantCultureIgnoreCase));

                    if(matchedProperty != null)
                    {
                        IEnumerable<string> attributesEnabledInModel = matchedProperty.CustomAttributes.Where(m =>
                            m.AttributeType.Name != "JsonPropertyAttribute").Select(m => m.AttributeType.Name.Replace("Attribute", "").Replace("Is", "").ToLower());

                        IEnumerable<string> attributesEnabledInJson = searchIndexField.GetType().GetProperties().Where(p =>
                            p.PropertyType == typeof(bool) &&
                                (bool)p.GetValue(searchIndexField, null))
                                .Select(p => p.Name.ToLower());

                        IEnumerable<string> unsyncedAttributes = attributesEnabledInModel.Except(attributesEnabledInJson);

                        bool areEquivalent = !unsyncedAttributes.Any();

                        if (!areEquivalent)
                        {
                            ErrorLog.Add($"Index {indexName}: The follwoing attributes in the model for {searchIndexField.Name} are not in sync with the json schema ({string.Join(",",unsyncedAttributes)})");
                        }

                        unsyncedAttributes = attributesEnabledInJson.Except(attributesEnabledInModel);

                        areEquivalent = !unsyncedAttributes.Any();

                        if (!areEquivalent)
                        {
                            ErrorLog.Add($"Index {indexName}: The following attributes in the json schema for {searchIndexField.Name} are not in sync with the model ({string.Join(",", unsyncedAttributes)})");
                        }
                    }
                }
            }

            //Assert

            if (ErrorLog.Any())
            {
                Assert.Fail(string.Join("\r\n", ErrorLog));
            }
        }

        private static IEnumerable<Type> GetTypesWithSearchIndexAttribute()
        {
           Assembly modelsAssembly = AppDomain.CurrentDomain.GetAssemblies().
            SingleOrDefault(assembly => assembly.GetName().Name == "CalculateFunding.Models");

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
