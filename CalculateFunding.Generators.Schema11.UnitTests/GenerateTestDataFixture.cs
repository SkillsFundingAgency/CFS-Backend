using System;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Generators.Schema11.UnitTests
{
    namespace QuickType
    {
        public class FundingExample11
        {
            [JsonProperty("$schema")] public Uri Schema { get; set; }

            [JsonProperty("schemaVersion")] public string SchemaVersion { get; set; }

            [JsonProperty("funding")] public Funding Funding { get; set; }
        }

        public class Funding
        {
            [JsonProperty("templateVersion")] public string TemplateVersion { get; set; }

            [JsonProperty("id")] public string Id { get; set; }

            [JsonProperty("fundingVersion")] public string FundingVersion { get; set; }

            [JsonProperty("status")] public string Status { get; set; }

            [JsonProperty("fundingStream")] public FundingStream FundingStream { get; set; }

            [JsonProperty("fundingPeriod")] public FundingPeriod FundingPeriod { get; set; }

            [JsonProperty("organisationGroup")] public OrganisationGroup OrganisationGroup { get; set; }

            [JsonProperty("fundingValue")] public FundingValue FundingValue { get; set; }

            [JsonProperty("providerFundings")] public ProviderFunding[] ProviderFundings { get; set; }

            [JsonProperty("groupingReason")] public GroupingReason GroupingReason { get; set; }

            [JsonProperty("statusChangedDate")] public DateTimeOffset StatusChangedDate { get; set; }

            [JsonProperty("externalPublicationDate")]
            public DateTimeOffset ExternalPublicationDate { get; set; }

            [JsonProperty("earliestPaymentAvailableDate")]
            public DateTimeOffset EarliestPaymentAvailableDate { get; set; }
        }

        public class FundingPeriod
        {
            [JsonProperty("id")] public Id Id { get; set; }

            [JsonProperty("period")]
            [JsonConverter(typeof(ParseStringConverter))]
            public long Period { get; set; }

            [JsonProperty("name")] public string Name { get; set; }

            [JsonProperty("type")] public string Type { get; set; }

            [JsonProperty("startDate")] public DateTimeOffset StartDate { get; set; }

            [JsonProperty("endDate")] public DateTimeOffset EndDate { get; set; }
        }

        public class FundingStream
        {
            [JsonProperty("code")] public string Code { get; set; }

            [JsonProperty("name")] public string Name { get; set; }
        }

        public class FundingValue
        {
            [JsonProperty("totalValue")] public long TotalValue { get; set; }

            [JsonProperty("fundingLines")] public FundingValueFundingLine[] FundingLines { get; set; }
        }

        public class FundingValueFundingLine
        {
            [JsonProperty("name")] public string Name { get; set; }

            [JsonProperty("fundingLineCode")] public string FundingLineCode { get; set; }

            [JsonProperty("value")] public long Value { get; set; }

            [JsonProperty("templateLineId")] public long TemplateLineId { get; set; }

            [JsonProperty("type")] public string Type { get; set; }

            [JsonProperty("distributionPeriods")] public DistributionPeriod[] DistributionPeriods { get; set; }

            [JsonProperty("fundingLines")] public FundingValueFundingLine[] FundingLines { get; set; }

            [JsonProperty("calculations", NullValueHandling = NullValueHandling.Ignore)]
            public PurpleCalculation[] Calculations { get; set; }
        }

        public class GroupRate
        {
            [JsonProperty("numerator")] public long Numerator { get; set; }

            [JsonProperty("denominator")] public long Denominator { get; set; }
        }

        public class PercentageChangeBetweenAandB
        {
            [JsonProperty("calculationA")] public long CalculationA { get; set; }

            [JsonProperty("calculationB")] public long CalculationB { get; set; }

            [JsonProperty("calculationAggregationType")]
            public AggregationType CalculationAggregationType { get; set; }
        }

        public class PurpleCalculation
        {
            [JsonProperty("name")] public string Name { get; set; }

            [JsonProperty("type")] public CalculationType Type { get; set; }

            [JsonProperty("aggregationType")] public AggregationType AggregationType { get; set; }

            [JsonProperty("formulaText")] public FormulaText FormulaText { get; set; }

            [JsonProperty("templateCalculationId")]
            public long TemplateCalculationId { get; set; }

            [JsonProperty("value")] public long Value { get; set; }

            [JsonProperty("valueFormat")] public ValueFormat ValueFormat { get; set; }

            [JsonProperty("calculations")] public PurpleCalculation[] Calculations { get; set; }
        }

        public class DistributionPeriod
        {
            [JsonProperty("value")] public long Value { get; set; }

            [JsonProperty("distributionPeriodId")] public Id DistributionPeriodId { get; set; }

            [JsonProperty("profilePeriods")] public ProfilePeriod[] ProfilePeriods { get; set; }
        }

        public class ProfilePeriod
        {
            [JsonProperty("type")] public ProfilePeriodType Type { get; set; }

            [JsonProperty("typeValue")] public string TypeValue { get; set; }

            [JsonProperty("year")] public long Year { get; set; }

            [JsonProperty("occurrence")] public long Occurrence { get; set; }

            [JsonProperty("profiledValue")] public long ProfiledValue { get; set; }

            [JsonProperty("distributionPeriodId")] public Id DistributionPeriodId { get; set; }
        }

        public class OrganisationGroup
        {
            [JsonProperty("groupTypeCode")] public string GroupTypeCode { get; set; }

            [JsonProperty("groupTypeIdentifier")] public GroupTypeIdentifier GroupTypeIdentifier { get; set; }

            [JsonProperty("groupTypeClassification")]
            public string GroupTypeClassification { get; set; }

            [JsonProperty("name")] public string Name { get; set; }

            [JsonProperty("searchableName")] public string SearchableName { get; set; }

            [JsonProperty("identifiers")] public GroupTypeIdentifier[] Identifiers { get; set; }
        }

        public class GroupTypeIdentifier
        {
            [JsonProperty("type")] public string Type { get; set; }

            [JsonProperty("value")] public string Value { get; set; }
        }

        public class ProviderFunding
        {
            [JsonProperty("id")] public string Id { get; set; }

            [JsonProperty("fundingVersion")] public string FundingVersion { get; set; }

            [JsonProperty("provider")] public Provider Provider { get; set; }

            [JsonProperty("fundingStreamCode")] public string FundingStreamCode { get; set; }

            [JsonProperty("fundingPeriodId")] public string FundingPeriodId { get; set; }

            [JsonProperty("fundingValue")] public FundingValue FundingValue { get; set; }

            [JsonProperty("variationReasons")] public object VariationReasons { get; set; }

            [JsonProperty("successors")] public GroupTypeIdentifier[] Successors { get; set; }

            [JsonProperty("predecessors")] public object Predecessors { get; set; }
        }

        public class Provider
        {
            [JsonProperty("identifier")]
            [JsonConverter(typeof(ParseStringConverter))]
            public long Identifier { get; set; }

            [JsonProperty("name")] public string Name { get; set; }

            [JsonProperty("searchableName")] public string SearchableName { get; set; }

            [JsonProperty("otherIdentifiers")] public GroupTypeIdentifier[] OtherIdentifiers { get; set; }

            [JsonProperty("providerVersionId")] public string ProviderVersionId { get; set; }

            [JsonProperty("providerType")] public string ProviderType { get; set; }

            [JsonProperty("providerSubType")] public string ProviderSubType { get; set; }

            [JsonProperty("providerDetails")] public ProviderDetails ProviderDetails { get; set; }
        }

        public class ProviderDetails
        {
            [JsonProperty("dateOpened")] public object DateOpened { get; set; }

            [JsonProperty("dateClosed")] public DateTimeOffset DateClosed { get; set; }

            [JsonProperty("status")] public string Status { get; set; }

            [JsonProperty("phaseOfEducation")]
            [JsonConverter(typeof(ParseStringConverter))]
            public long PhaseOfEducation { get; set; }

            [JsonProperty("localAuthorityName")] public string LocalAuthorityName { get; set; }

            [JsonProperty("openReason")] public string OpenReason { get; set; }

            [JsonProperty("closeReason")] public string CloseReason { get; set; }

            [JsonProperty("trustStatus")] public string TrustStatus { get; set; }

            [JsonProperty("trustName")] public string TrustName { get; set; }

            [JsonProperty("town")] public string Town { get; set; }

            [JsonProperty("postcode")] public string Postcode { get; set; }

            [JsonProperty("companiesHouseNumber")] public string CompaniesHouseNumber { get; set; }

            [JsonProperty("groupIdNumber")] public string GroupIdNumber { get; set; }

            [JsonProperty("rscRegionName")] public string RscRegionName { get; set; }

            [JsonProperty("rscRegionCode")] public string RscRegionCode { get; set; }

            [JsonProperty("governmentOfficeRegionName")]
            public string GovernmentOfficeRegionName { get; set; }

            [JsonProperty("governmentOfficeRegionCode")]
            public string GovernmentOfficeRegionCode { get; set; }

            [JsonProperty("districtName")] public string DistrictName { get; set; }

            [JsonProperty("districtCode")] public string DistrictCode { get; set; }

            [JsonProperty("wardName")] public string WardName { get; set; }

            [JsonProperty("wardCode")] public string WardCode { get; set; }

            [JsonProperty("censusWardName")] public string CensusWardName { get; set; }

            [JsonProperty("censusWardCode")] public string CensusWardCode { get; set; }

            [JsonProperty("middleSuperOutputAreaName")]
            public string MiddleSuperOutputAreaName { get; set; }

            [JsonProperty("middleSuperOutputAreaCode")]
            public string MiddleSuperOutputAreaCode { get; set; }

            [JsonProperty("lowerSuperOutputAreaName")]
            public string LowerSuperOutputAreaName { get; set; }

            [JsonProperty("lowerSuperOutputAreaCode")]
            public string LowerSuperOutputAreaCode { get; set; }

            [JsonProperty("parliamentaryConstituencyName")]
            public string ParliamentaryConstituencyName { get; set; }

            [JsonProperty("parliamentaryConstituencyCode")]
            public string ParliamentaryConstituencyCode { get; set; }

            [JsonProperty("countryCode")] public string CountryCode { get; set; }

            [JsonProperty("countryName")] public string CountryName { get; set; }
        }

        public enum Id
        {
            Fy1920,
            Fy2021
        }

        public enum PurpleAggregationType
        {
            GroupRate,
            None,
            PercentageChangeBetweenAandB,
            Sum
        }

        public enum AggregationType
        {
            GroupRate,
            Average,
            None,
            PercentageChangeBetweenAandB,
            Sum
        }

        public enum CalculationType
        {
            Cash,
            Number,
            PupilNumber,
            Rate
        }

        public enum ValueFormat
        {
            Currency,
            Number,
            Percentage
        }

        public enum GroupingReason
        {
            Information
        }

        public enum FormulaText
        {
            Empty,
            SomethingSomething
        }

        public enum ProfilePeriodType
        {
            CalendarMonth
        }

        internal static class Converter
        {
            public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
            {
                MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
                DateParseHandling = DateParseHandling.None,
                Converters =
                {
                    IdConverter.Singleton,
                    AggregationTypeConverter.Singleton,
                    PurpleAggregationTypeConverter.Singleton,
                    CalculationTypeConverter.Singleton,
                    ValueFormatConverter.Singleton,
                    FormulaTextConverter.Singleton,
                    ProfilePeriodTypeConverter.Singleton,
                    GroupingReasonConverter.Singleton,
                    new IsoDateTimeConverter
                    {
                        DateTimeStyles = DateTimeStyles.AssumeUniversal
                    }
                }
            };
        }

        internal class IdConverter : JsonConverter
        {
            public override bool CanConvert(Type t) => t == typeof(Id) || t == typeof(Id?);

            public override object ReadJson(JsonReader reader,
                Type t,
                object existingValue,
                JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null) return null;
                string value = serializer.Deserialize<string>(reader);
                switch (value)
                {
                    case "FY-1920":
                        return Id.Fy1920;
                    case "FY-2021":
                        return Id.Fy2021;
                }

                throw new Exception("Cannot unmarshal type Id");
            }

            public override void WriteJson(JsonWriter writer,
                object untypedValue,
                JsonSerializer serializer)
            {
                if (untypedValue == null)
                {
                    serializer.Serialize(writer, null);
                    return;
                }

                Id value = (Id) untypedValue;
                switch (value)
                {
                    case Id.Fy1920:
                        serializer.Serialize(writer, "FY-1920");
                        return;
                    case Id.Fy2021:
                        serializer.Serialize(writer, "FY-2021");
                        return;
                }

                throw new Exception("Cannot marshal type Id");
            }

            public static readonly IdConverter Singleton = new IdConverter();
        }

        internal class ParseStringConverter : JsonConverter
        {
            public override bool CanConvert(Type t) => t == typeof(long) || t == typeof(long?);

            public override object ReadJson(JsonReader reader,
                Type t,
                object existingValue,
                JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null) return null;
                string value = serializer.Deserialize<string>(reader);
                long l;
                if (long.TryParse(value, out l))
                {
                    return l;
                }

                throw new Exception("Cannot unmarshal type long");
            }

            public override void WriteJson(JsonWriter writer,
                object untypedValue,
                JsonSerializer serializer)
            {
                if (untypedValue == null)
                {
                    serializer.Serialize(writer, null);
                    return;
                }

                long value = (long) untypedValue;
                serializer.Serialize(writer, value.ToString());
            }

            public static readonly ParseStringConverter Singleton = new ParseStringConverter();
        }

        internal class AggregationTypeConverter : JsonConverter
        {
            public override bool CanConvert(Type t) => t == typeof(AggregationType) || t == typeof(AggregationType?);

            public override object ReadJson(JsonReader reader,
                Type t,
                object existingValue,
                JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null) return null;
                string value = serializer.Deserialize<string>(reader);
                switch (value)
                {
                    case "Average":
                        return AggregationType.Average;
                    case "None":
                        return AggregationType.None;
                    case "Sum":
                        return AggregationType.Sum;
                    case "GroupRate":
                        return AggregationType.GroupRate;
                    case "PercentageChangeBetweenAandB":
                        return AggregationType.PercentageChangeBetweenAandB;
                }

                throw new Exception("Cannot unmarshal type AggregationType");
            }

            public override void WriteJson(JsonWriter writer,
                object untypedValue,
                JsonSerializer serializer)
            {
                if (untypedValue == null)
                {
                    serializer.Serialize(writer, null);
                    return;
                }

                AggregationType value = (AggregationType) untypedValue;
                switch (value)
                {
                    case AggregationType.Average:
                        serializer.Serialize(writer, "Average");
                        return;
                    case AggregationType.None:
                        serializer.Serialize(writer, "None");
                        return;
                    case AggregationType.Sum:
                        serializer.Serialize(writer, "Sum");
                        return;
                }

                throw new Exception("Cannot marshal type AggregationType");
            }

            public static readonly AggregationTypeConverter Singleton = new AggregationTypeConverter();
        }

        internal class PurpleAggregationTypeConverter : JsonConverter
        {
            public override bool CanConvert(Type t) => t == typeof(PurpleAggregationType) || t == typeof(PurpleAggregationType?);

            public override object ReadJson(JsonReader reader,
                Type t,
                object existingValue,
                JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null) return null;
                string value = serializer.Deserialize<string>(reader);
                switch (value)
                {
                    case "GroupRate":
                        return PurpleAggregationType.GroupRate;
                    case "None":
                        return PurpleAggregationType.None;
                    case "PercentageChangeBetweenAandB":
                        return PurpleAggregationType.PercentageChangeBetweenAandB;
                    case "Sum":
                        return PurpleAggregationType.Sum;
                }

                throw new Exception("Cannot unmarshal type PurpleAggregationType");
            }

            public override void WriteJson(JsonWriter writer,
                object untypedValue,
                JsonSerializer serializer)
            {
                if (untypedValue == null)
                {
                    serializer.Serialize(writer, null);
                    return;
                }

                PurpleAggregationType value = (PurpleAggregationType) untypedValue;
                switch (value)
                {
                    case PurpleAggregationType.GroupRate:
                        serializer.Serialize(writer, "GroupRate");
                        return;
                    case PurpleAggregationType.None:
                        serializer.Serialize(writer, "None");
                        return;
                    case PurpleAggregationType.PercentageChangeBetweenAandB:
                        serializer.Serialize(writer, "PercentageChangeBetweenAandB");
                        return;
                    case PurpleAggregationType.Sum:
                        serializer.Serialize(writer, "Sum");
                        return;
                }

                throw new Exception("Cannot marshal type PurpleAggregationType");
            }

            public static readonly PurpleAggregationTypeConverter Singleton = new PurpleAggregationTypeConverter();
        }

        internal class CalculationTypeConverter : JsonConverter
        {
            public override bool CanConvert(Type t) => t == typeof(CalculationType) || t == typeof(CalculationType?);

            public override object ReadJson(JsonReader reader,
                Type t,
                object existingValue,
                JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null) return null;
                string value = serializer.Deserialize<string>(reader);
                switch (value)
                {
                    case "Cash":
                        return CalculationType.Cash;
                    case "Number":
                        return CalculationType.Number;
                    case "PupilNumber":
                        return CalculationType.PupilNumber;
                    case "Rate":
                        return CalculationType.Rate;
                }

                throw new Exception("Cannot unmarshal type CalculationType");
            }

            public override void WriteJson(JsonWriter writer,
                object untypedValue,
                JsonSerializer serializer)
            {
                if (untypedValue == null)
                {
                    serializer.Serialize(writer, null);
                    return;
                }

                CalculationType value = (CalculationType) untypedValue;
                switch (value)
                {
                    case CalculationType.Cash:
                        serializer.Serialize(writer, "Cash");
                        return;
                    case CalculationType.Number:
                        serializer.Serialize(writer, "Number");
                        return;
                    case CalculationType.PupilNumber:
                        serializer.Serialize(writer, "PupilNumber");
                        return;
                    case CalculationType.Rate:
                        serializer.Serialize(writer, "Rate");
                        return;
                }

                throw new Exception("Cannot marshal type CalculationType");
            }

            public static readonly CalculationTypeConverter Singleton = new CalculationTypeConverter();
        }

        internal class ValueFormatConverter : JsonConverter
        {
            public override bool CanConvert(Type t) => t == typeof(ValueFormat) || t == typeof(ValueFormat?);

            public override object ReadJson(JsonReader reader,
                Type t,
                object existingValue,
                JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null) return null;
                string value = serializer.Deserialize<string>(reader);
                switch (value)
                {
                    case "Currency":
                        return ValueFormat.Currency;
                    case "Number":
                        return ValueFormat.Number;
                    case "Percentage":
                        return ValueFormat.Percentage;
                }

                throw new Exception("Cannot unmarshal type ValueFormat");
            }

            public override void WriteJson(JsonWriter writer,
                object untypedValue,
                JsonSerializer serializer)
            {
                if (untypedValue == null)
                {
                    serializer.Serialize(writer, null);
                    return;
                }

                ValueFormat value = (ValueFormat) untypedValue;
                switch (value)
                {
                    case ValueFormat.Currency:
                        serializer.Serialize(writer, "Currency");
                        return;
                    case ValueFormat.Number:
                        serializer.Serialize(writer, "Number");
                        return;
                    case ValueFormat.Percentage:
                        serializer.Serialize(writer, "Percentage");
                        return;
                }

                throw new Exception("Cannot marshal type ValueFormat");
            }

            public static readonly ValueFormatConverter Singleton = new ValueFormatConverter();
        }

        internal class FormulaTextConverter : JsonConverter
        {
            public override bool CanConvert(Type t) => t == typeof(FormulaText) || t == typeof(FormulaText?);

            public override object ReadJson(JsonReader reader,
                Type t,
                object existingValue,
                JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null) return null;
                string value = serializer.Deserialize<string>(reader);
                switch (value)
                {
                    case "":
                        return FormulaText.Empty;
                    case "Something * something":
                        return FormulaText.SomethingSomething;
                }

                throw new Exception("Cannot unmarshal type FormulaText");
            }

            public override void WriteJson(JsonWriter writer,
                object untypedValue,
                JsonSerializer serializer)
            {
                if (untypedValue == null)
                {
                    serializer.Serialize(writer, null);
                    return;
                }

                FormulaText value = (FormulaText) untypedValue;
                switch (value)
                {
                    case FormulaText.Empty:
                        serializer.Serialize(writer, "");
                        return;
                    case FormulaText.SomethingSomething:
                        serializer.Serialize(writer, "Something * something");
                        return;
                }

                throw new Exception("Cannot marshal type FormulaText");
            }

            public static readonly FormulaTextConverter Singleton = new FormulaTextConverter();
        }

        internal class ProfilePeriodTypeConverter : JsonConverter
        {
            public override bool CanConvert(Type t) => t == typeof(ProfilePeriodType) || t == typeof(ProfilePeriodType?);

            public override object ReadJson(JsonReader reader,
                Type t,
                object existingValue,
                JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null) return null;
                string value = serializer.Deserialize<string>(reader);
                if (value == "CalendarMonth")
                {
                    return ProfilePeriodType.CalendarMonth;
                }

                throw new Exception("Cannot unmarshal type ProfilePeriodType");
            }

            public override void WriteJson(JsonWriter writer,
                object untypedValue,
                JsonSerializer serializer)
            {
                if (untypedValue == null)
                {
                    serializer.Serialize(writer, null);
                    return;
                }

                ProfilePeriodType value = (ProfilePeriodType) untypedValue;
                if (value == ProfilePeriodType.CalendarMonth)
                {
                    serializer.Serialize(writer, "CalendarMonth");
                    return;
                }

                throw new Exception("Cannot marshal type ProfilePeriodType");
            }

            public static readonly ProfilePeriodTypeConverter Singleton = new ProfilePeriodTypeConverter();
        }

        internal class GroupingReasonConverter : JsonConverter
        {
            public override bool CanConvert(Type t) => t == typeof(GroupingReason) || t == typeof(GroupingReason?);

            public override object ReadJson(JsonReader reader,
                Type t,
                object existingValue,
                JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null) return null;
                string value = serializer.Deserialize<string>(reader);
                if (value == "Information")
                {
                    return GroupingReason.Information;
                }

                throw new Exception("Cannot unmarshal type GroupingReason");
            }

            public override void WriteJson(JsonWriter writer,
                object untypedValue,
                JsonSerializer serializer)
            {
                if (untypedValue == null)
                {
                    serializer.Serialize(writer, null);
                    return;
                }

                GroupingReason value = (GroupingReason) untypedValue;
                if (value == GroupingReason.Information)
                {
                    serializer.Serialize(writer, "Information");
                    return;
                }

                throw new Exception("Cannot marshal type GroupingReason");
            }

            public static readonly GroupingReasonConverter Singleton = new GroupingReasonConverter();
        }
    }
}