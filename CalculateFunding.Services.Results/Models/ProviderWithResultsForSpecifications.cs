using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Services.Results.Models
{
    public class ProviderWithResultsForSpecifications : IIdentifiable
    {
        [JsonProperty("id")] 
        public string Id => Provider.Id;
            
        [JsonProperty("provider")]
        public ProviderInformation Provider { get; set; }
        
        [JsonProperty("specifications")]
        public IEnumerable<SpecificationInformation> Specifications { get; set; }

        public void MergeSpecificationInformation(SpecificationInformation specificationInformation)
        {
            Specifications ??= new List<SpecificationInformation>();

            SpecificationInformation existingSpecificationInformation = Specifications.SingleOrDefault(_ => _.Id == specificationInformation.Id);

            if (existingSpecificationInformation == null)
            {
                Specifications = Specifications.Concat(new[]
                {
                    specificationInformation
                }).ToList();
            }
            else
            {
                existingSpecificationInformation.MergeMutableInformation(specificationInformation);   
            }
        }
    }
}