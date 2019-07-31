using System.Collections.Generic;
using System.Dynamic;
using CalculateFunding.Services.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Core.UnitTests.Helpers
{
    [TestClass]
    public class CsvUtilsTests
    {
#if NCRUNCH
        [Ignore]
#endif
        [TestMethod]
        [DynamicData(nameof(CreateCsvExpandoTestCases), DynamicDataSourceType.Method)]
        public void CreateCsvExpando_CreatesAsExpected(IEnumerable<ExpandoObject> input, string qualifier, CsvHeaderBehaviour headerBehaviour, string output)
        {
            CsvUtils utils = new CsvUtils();

            string result = utils.CreateCsvExpando(input, qualifier, headerBehaviour);

            Assert.AreEqual(output, result);
        }

        private static IEnumerable<object[]> CreateCsvExpandoTestCases()
        {
            yield return new object[] { new ExpandoObject[0], "", CsvHeaderBehaviour.WriteIfData, "" };

            dynamic expando1 = new ExpandoObject();
            expando1.Name = "Elizabeth";
            expando1.Country = "UK";

            dynamic expando2 = new ExpandoObject();
            expando2.Name = "Donald";
            expando2.Country = "USA";

            IEnumerable<ExpandoObject> expandoData = new List<ExpandoObject> { expando1, expando2 };

            yield return new object[] { expandoData, "", CsvHeaderBehaviour.WriteAlways, "Name,Country\r\nElizabeth,UK\r\nDonald,USA\r\n" };
            yield return new object[] { expandoData, "\"", CsvHeaderBehaviour.WriteAlways, "\"Name\",\"Country\"\r\n\"Elizabeth\",\"UK\"\r\n\"Donald\",\"USA\"\r\n" };
            yield return new object[] { expandoData, "", CsvHeaderBehaviour.WriteNever, "Elizabeth,UK\r\nDonald,USA\r\n" };
        }
    }
}
