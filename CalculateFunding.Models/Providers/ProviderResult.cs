using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Results;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Providers
{


    public class ProviderService
    {
        public ProviderCommand Create(Provider item)
        {
            return new ProviderCommand("CREATE", item);
        }

    }

    public class ProviderCommand : Command<Provider>
    {
        public Provider Item { get; }

        public ProviderCommand(string verb, Provider item)
        {
            Verb = verb;
            Item = item;
        }
    }

    public class ProviderBulkCommand : Command<Provider>
    {

    }

    public class Command<T> : DocumentEntity
    {
        public override string Id => Guid.NewGuid().ToString("N");

        [JsonProperty("eventDate")]
        public DateTime EventDate { get; set; }

        [JsonProperty("username")]
        public string UserName { get; set; }

        [JsonProperty("datasetName")]
        public string DatasetName { get; set; }

        [JsonProperty("verb")]
        public string Verb { get; set; }

        [JsonProperty("data")]
        public object Data { get; set; }
    }

    public class Provider: DocumentEntity
    {
        public override string Id => $"{DocumentType}-{URN}".ToSlug();

        [JsonProperty("urn")]
        public string URN { get; set; }

        [JsonProperty("ukprn")]
        public string UKPRN { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("authority")]
        public Reference Authority { get; set; }

        [JsonProperty("openedDate")]
        public DateTime? OpenDate { get; set; }
        [JsonProperty("closedDate")]
        public DateTime? CloseDate { get; set; }

        [JsonProperty("phaseOfEducation")]
        public string PhaseOfEducation { get; set; }
    }
}