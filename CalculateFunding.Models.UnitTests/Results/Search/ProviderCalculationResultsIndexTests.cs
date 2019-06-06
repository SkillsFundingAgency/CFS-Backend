using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CalculateFunding.Models.Results.Search;
using Microsoft.Azure.Search;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace CalculateFunding.Models.UnitTests.Results.Search
{
    [TestClass]
    public class ProviderCalculationResultsIndexTests
    {
        [TestMethod]
        [DataRow("a", "b", "a_b")]
        [DataRow("Embiggen", "Cromulent", "Embiggen_Cromulent")]
        public void Id_PopulatedCorrectly(string specificationId, string providerId, string Id)
        {
            ProviderCalculationResultsIndex resultsIndex = new ProviderCalculationResultsIndex
            {
                SpecificationId = specificationId,
                ProviderId = providerId
            };

            Assert.AreEqual(Id, resultsIndex.Id);
        }

        [TestMethod]
#if NCRUNCH
        [Ignore]
#endif
        public void ProviderCalculationResultsIndex_AttributesMatchJson()
        {
            string jsonPath = AppDomain.CurrentDomain.BaseDirectory + "..\\..\\..\\..\\DevOps\\search-indexes\\providercalculationresultsindex\\providercalculationresultsindex.json";

            using (StreamReader reader = new StreamReader(jsonPath))
            {
                JObject json = JObject.Parse(reader.ReadToEnd());

                IEnumerable<string[]> properties = new List<string[]>
                {
                    new[] {"id", "Id"},
                    new[] {"specificationId", "SpecificationId"},
                    new[] {"specificationName", "SpecificationName"},
                    new[] {"providerId", "ProviderId"},
                    new[] {"providerName", "ProviderName"},
                    new[] {"providerType", "ProviderType"},
                    new[] {"localAuthority", "LocalAuthority"},
                    new[] {"providerSubType", "ProviderSubType"},
                    new[] {"lastUpdatedDate", "LastUpdatedDate"},
                    new[] {"ukPrn", "UKPRN"},
                    new[] {"urn", "URN"},
                    new[] {"upin", "UPIN"},
                    new[] {"establishmentNumber", "EstablishmentNumber"},
                    new[] {"openDate", "OpenDate"},
                    new[] {"calculationId", "CalculationId"},
                    new[] {"calculationName", "CalculationName"},
                    new[] {"calculationResult", "CalculationResult"},
                    new[] {"calculationException", "CalculationException"},
                    new[] {"calculationExceptionType", "CalculationExceptionType"},
                    new[] {"calculationExceptionMessage", "CalculationExceptionMessage"}
                };

                List<string> errors = new List<string>();

                foreach (string[] property in properties)
                {
                    CheckProperty<IsSearchableAttribute>(json, property[0], "searchable", property[1], errors);
                    CheckProperty<IsFacetableAttribute>(json, property[0], "facetable", property[1], errors);
                    CheckProperty<IsFilterableAttribute>(json, property[0], "filterable", property[1], errors);
                    CheckProperty<IsSortableAttribute>(json, property[0], "sortable", property[1], errors);
                }

                if (errors.Any()) throw new Exception(string.Join(Environment.NewLine, errors));
            }
        }

        private void CheckProperty<T>(JObject json, string jsonToken, string jsonAttribute, string pocoProperty, List<string> errors) where T : Attribute
        {
            bool tokenValue = json.SelectToken($"$.fields[?(@.name=='{jsonToken}')].{jsonAttribute}").Value<bool>();

            T attr = typeof(ProviderCalculationResultsIndex)
                .GetProperty(pocoProperty)
                .GetCustomAttribute<T>(false);

            try
            {
                Assert.AreEqual(tokenValue, attr != null, $"{pocoProperty}.{jsonAttribute} in JSON is {tokenValue}, which does not match the POCO");
            }
            catch (Exception ex)
            {
                errors.Add(ex.Message);
            }
        }
    }
}
