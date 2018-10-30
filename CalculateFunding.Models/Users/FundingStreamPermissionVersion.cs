using CalculateFunding.Models.Versioning;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Users
{
    public class FundingStreamPermissionVersion : VersionedItem
    {
        [JsonProperty("id")]
        public override string Id
        {
            get { return $"{EntityId}_version_{Version}"; }
        }

        [JsonProperty("entityId")]
        public override string EntityId => Permission.Id;

        [JsonProperty("permission")]
        public FundingStreamPermission Permission { get; set; }

        [JsonProperty("userId")]
        public string UserId { get; set; }

        public override VersionedItem Clone()
        {
            // Serialise to perform a deep copy
            string json = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<FundingStreamPermissionVersion>(json);
        }
    }
}
