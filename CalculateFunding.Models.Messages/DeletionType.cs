using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Messages
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DeletionType
    {
        SoftDelete,
        PermanentDelete
    }

    public static class DeletionTypeCalculator
    {
        public static DeletionType ToDeletionType(this string deletionTypeProperty)
        {
            DeletionType deletionType = DeletionType.SoftDelete;

            if (int.TryParse(deletionTypeProperty, out var deletionTypeValue))
            {
                deletionType = (DeletionType) deletionTypeValue;
            }

            return deletionType;
        }
    }
}
