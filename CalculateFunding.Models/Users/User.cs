using CalculateFunding.Common.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Users
{
    public class User : IIdentifiable
    {
        [JsonProperty("id")]
        public string Id
        {
            get
            {
                return UserId;
            }
        }

        [JsonProperty("userId")]
        public string UserId
        {
            get; set;
        }

        [JsonProperty("name")]
        public string Name
        {
            get; set;
        }

        [JsonProperty("userName")]
        public string Username { get; set; }

        [JsonProperty("hasConfirmedSkills")]
        public bool HasConfirmedSkills { get; set; }
    }
}
