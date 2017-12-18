using Newtonsoft.Json;

namespace CalculateFunding.Models
{
    public class Reference : IIdentifiable
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }

        public Reference()
        {
            
        }

        public Reference(string id, string name)
        {
            Id = id;
            Name = name;
        }

        /// <summary>
        /// Provide debugging help!
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"({Id}, {Name})";
        }
    }
}
