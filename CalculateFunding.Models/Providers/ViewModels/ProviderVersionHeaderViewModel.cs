using CalculateFunding.Models.Providers;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace CalculateFunding.Models.Providers.ViewModels
{
    public class ProviderVersionHeaderViewModel
    {
        [JsonProperty("versionType")]
        private string ProviderVersionTypeString;

        public string Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        [JsonIgnore]

        public ProviderVersionType VersionType {
            get
            {
                if (Enum.TryParse<ProviderVersionType>(ProviderVersionTypeString, true, out var result))
                {
                    return result;
                }

                return ProviderVersionType.Missing;
            }
            set
            {
                ProviderVersionTypeString = value.ToString();
            }
        }

        public DateTimeOffset Created { get; set; }
    }
}
