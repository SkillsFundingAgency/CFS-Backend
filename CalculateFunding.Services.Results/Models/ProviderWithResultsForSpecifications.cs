using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Services.Results.Models
{
    public class ProviderWithResultsForSpecifications : IIdentifiable
    {
        private bool _isDirty;
        
        [JsonProperty("id")] 
        public string Id => Provider.Id;
            
        [JsonProperty("provider")]
        public ProviderInformation Provider { get; set; }
        
        [JsonProperty("specifications")]
        public ICollection<SpecificationInformation> Specifications { get; set; }

        public bool GetIsDirty() 
            => _isDirty || Specifications?.Any(_ => _.IsDirty) == true;

        public void MergeSpecificationInformation(SpecificationInformation specificationInformation)
        {
            Specifications ??= new List<SpecificationInformation>();

            SpecificationInformation existingSpecificationInformation = Specifications.SingleOrDefault(_ => _.Id == specificationInformation.Id);

            if (existingSpecificationInformation == null)
            {
                _isDirty = true;

                Specifications.Add(specificationInformation);
            }
            else
            {
                existingSpecificationInformation.MergeMutableInformation(specificationInformation);   
            }
        }
    }
}