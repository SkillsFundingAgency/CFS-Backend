using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Core.Extensions
{
    [TestClass]
    public class JsonExtensionTests
    {
        [TestMethod]
        public void PrettifiesSingleLineJsonStrings()
        {
            @"{""One"":""oneEx"",""Two"":233,""Child"":{""One"":""childOneEx"",""Two"":332,""Child"":null}}"
                .Prettify()
                .Should()
                .Be(@"{
  ""One"": ""oneEx"",
  ""Two"": 233,
  ""Child"": {
    ""One"": ""childOneEx"",
    ""Two"": 332,
    ""Child"": null
  }
}");
        }
        
        [TestMethod]
        [DynamicData(nameof(JsonExamples), DynamicDataSourceType.Method)]
        public void SerialisesGraphsIntoJsonLiterals(PocoOne poco,
            bool useCamelCase,
            string expectedJson)
        {
            poco.AsJson(useCamelCase)
                .Should()
                .Be(expectedJson);
        }

        [TestMethod]
        [DynamicData(nameof(JsonExamples), DynamicDataSourceType.Method)]
        public void DeserialisesJsonLiteralsIntoGraphs(PocoOne expectedPoco,
            bool useCamelCase,
            string jsonLiteral)
        {
            jsonLiteral.AsPoco<PocoOne>(useCamelCase)
                .Should()
                .BeEquivalentTo(expectedPoco);
        }

        [TestMethod]
        [DynamicData(nameof(JsonExamples), DynamicDataSourceType.Method)]
        public void DeepCopiesSuppliedGraphs(PocoOne originalPoco,
            bool useCamelCase,
            string jsonLiteral)
        {
            PocoOne deepCopy = originalPoco.DeepCopy();

            deepCopy
                .Should()
                .BeEquivalentTo(originalPoco);

            deepCopy
                .Should()
                .NotBeSameAs(originalPoco);
        }

        public static IEnumerable<object[]> JsonExamples()
        {
            yield return new object[]
            {
                new PocoOne
                {
                    One = "one",
                    Two = 23,
                    Child = new PocoOne
                    {
                        One = "childOne",
                        Two = 32
                    }
                },
                true,
                @"{""one"":""one"",""two"":23,""child"":{""one"":""childOne"",""two"":32,""child"":null}}"
            };
            yield return new object[]
            {
                new PocoOne
                {
                    One = "oneEx",
                    Two = 233,
                    Child = new PocoOne
                    {
                        One = "childOneEx",
                        Two = 332
                    }
                },
                false,
                @"{""One"":""oneEx"",""Two"":233,""Child"":{""One"":""childOneEx"",""Two"":332,""Child"":null}}"
            };
        }

        public class PocoOne
        {
            public string One { get; set; }
            public int Two { get; set; }
            public PocoOne Child { get; set; }
        }
    }
}