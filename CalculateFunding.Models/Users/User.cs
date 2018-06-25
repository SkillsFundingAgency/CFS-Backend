using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Users
{
    public class User : IIdentifiable
    {
        [JsonProperty("id")]
        public string Id
        {
            get
            {
                return Username;
            }
        }

        [JsonProperty("userName")]
        public string Username { get; set; }

        [JsonProperty("hasConfirmedSkills")]
        public bool HasConfirmedSkills { get; set; }
    }
}
