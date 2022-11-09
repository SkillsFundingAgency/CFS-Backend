using System.Collections.Generic;
using System.IO;
using System.Text;
using CalculateFunding.Models.Providers;
using CalculateFunding.Tests.Common.Helpers;
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
        public void CompareDeserialisesStreamIntoGraphsWithPreviousImplementation()
        {
            var provA = new Provider
            {
                Name = new RandomString(),
                ProviderId = new RandomString(),
                ProviderProfileIdType = new RandomString(),
                UKPRN = new RandomString(),
                URN = new RandomString(),
                Authority = new RandomString(),
                UPIN = new RandomString(),
                ProviderSubType = new RandomString(),
                EstablishmentNumber = new RandomString(),
                ProviderType = new RandomString(),
                DateOpened = System.DateTime.Now,
                DateClosed = System.DateTime.Now,
                LACode = new RandomString(),
                LAOrg = new RandomString(),
                CrmAccountId = new RandomString(),
                LegalName = new RandomString(),
                NavVendorNo = new RandomString(),
                DfeEstablishmentNumber = new RandomString(),
                Status = new RandomString(),
                PhaseOfEducation = new RandomString(),
                ReasonEstablishmentClosed = new RandomString(),
                ReasonEstablishmentOpened = new RandomString(),
                Successor = new RandomString(),
                TrustStatus = Models.ProviderLegacy.TrustStatus.SupportedByATrust,
                TrustName = new RandomString(),
                TrustCode = new RandomString(),
                CompaniesHouseNumber = new RandomString(),
                GroupIdNumber = new RandomString(),
                RscRegionName = new RandomString(),
                RscRegionCode = new RandomString(),
                GovernmentOfficeRegionName = new RandomString(),
                GovernmentOfficeRegionCode = new RandomString(),
                DistrictName = new RandomString(),
                DistrictCode = new RandomString(),
                WardName = new RandomString(),
                WardCode = new RandomString(),
                CensusWardName = new RandomString(),
                CensusWardCode = new RandomString(),
                MiddleSuperOutputAreaName = new RandomString(),
                MiddleSuperOutputAreaCode = new RandomString(),
                LowerSuperOutputAreaName = new RandomString(),
                LowerSuperOutputAreaCode = new RandomString(),
                ParliamentaryConstituencyName = new RandomString(),
                ParliamentaryConstituencyCode = new RandomString(),
                CountryCode = new RandomString(),
                CountryName = new RandomString(),
                LocalGovernmentGroupTypeCode = new RandomString(),
                LocalGovernmentGroupTypeName = new RandomString(),
                Street = new RandomString(),
                Locality = new RandomString(),
                Address3 = new RandomString()
            };
            var provB = new Provider
            {
                Name = new RandomString(),
                ProviderId = new RandomString(),
                ProviderProfileIdType = new RandomString(),
                UKPRN = new RandomString(),
                URN = new RandomString(),
                Authority = new RandomString(),
                UPIN = new RandomString(),
                ProviderSubType = new RandomString(),
                EstablishmentNumber = new RandomString(),
                ProviderType = new RandomString(),
                DateOpened = System.DateTime.Now,
                DateClosed = System.DateTime.Now,
                LACode = new RandomString(),
                LAOrg = new RandomString(),
                CrmAccountId = new RandomString(),
                LegalName = new RandomString(),
                NavVendorNo = new RandomString(),
                DfeEstablishmentNumber = new RandomString(),
                Status = new RandomString(),
                PhaseOfEducation = new RandomString(),
                ReasonEstablishmentClosed = new RandomString(),
                ReasonEstablishmentOpened = new RandomString(),
                Successor = new RandomString(),
                TrustStatus = Models.ProviderLegacy.TrustStatus.SupportedByATrust,
                TrustName = new RandomString(),
                TrustCode = new RandomString(),
                CompaniesHouseNumber = new RandomString(),
                GroupIdNumber = new RandomString(),
                RscRegionName = new RandomString(),
                RscRegionCode = new RandomString(),
                GovernmentOfficeRegionName = new RandomString(),
                GovernmentOfficeRegionCode = new RandomString(),
                DistrictName = new RandomString(),
                DistrictCode = new RandomString(),
                WardName = new RandomString(),
                WardCode = new RandomString(),
                CensusWardName = new RandomString(),
                CensusWardCode = new RandomString(),
                MiddleSuperOutputAreaName = new RandomString(),
                MiddleSuperOutputAreaCode = new RandomString(),
                LowerSuperOutputAreaName = new RandomString(),
                LowerSuperOutputAreaCode = new RandomString(),
                ParliamentaryConstituencyName = new RandomString(),
                ParliamentaryConstituencyCode = new RandomString(),
                CountryCode = new RandomString(),
                CountryName = new RandomString(),
                LocalGovernmentGroupTypeCode = new RandomString(),
                LocalGovernmentGroupTypeName = new RandomString(),
                Street = new RandomString(),
                Locality = new RandomString(),
                Address3 = new RandomString()
            };

            var pv = new ProviderVersion
            {
                Name = new RandomString(),
                Version = 10,
                FundingStream = new RandomString(),
                ProviderVersionTypeString = new RandomString(),
                Providers = new Provider[] {provA, provB}
            };

            Stream stream = new MemoryStream(pv.AsJsonBytes());
            Stream stream2 = new MemoryStream(pv.AsJsonBytes());

            string previousImplementation;

            using (BinaryReader reader = new BinaryReader(stream))
            {
                previousImplementation = Encoding.UTF8.GetString(reader.ReadBytes((int)stream.Length))
                    .AsPoco<ProviderVersion>().AsJson();
            }
            
            string currentImplementation = stream2.AsPoco<ProviderVersion>().AsJson();

            Assert.AreEqual(previousImplementation, currentImplementation);
        }

        [TestMethod]
        [DynamicData(nameof(JsonExamples), DynamicDataSourceType.Method)]
        public void DeepCopiesSuppliedGraphs(PocoOne originalPoco,
            bool useCamelCase,
            string jsonLiteral)
        {
            PocoOne deepCopy = originalPoco.DeepCopy(useCamelCase);

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