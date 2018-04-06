using System;
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
        /// <returns>A string representing the reference</returns>
        public override string ToString()
        {
            return $"({Id}, {Name})";
        }

        /// <summary>
        /// This is designed to be used by subclasses to obtain a reference. If you just assign the subclass JSON.NET can try to serialize the whole object, not just the reference
        /// </summary>
        /// <returns>A new reference</returns>
        public Reference GetReference()
        {
            return new Reference(Id, Name);
        }

        public static string NewId()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}
