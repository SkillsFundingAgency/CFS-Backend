using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Versioning;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Specs
{
    public class SpecificationVersion : VersionedItem
    {
        [JsonProperty("id")]
        public override string Id
        {
            get { return $"{SpecificationId}_version_{Version}"; }
        }

        [JsonProperty("entityId")]
        public override string EntityId
        {
            get { return $"{SpecificationId}"; }
        }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("specificationId")]
        public string SpecificationId { get; set; }

        [JsonProperty("fundingPeriod")]
        public Reference FundingPeriod { get; set; }

        [JsonProperty("providerVersionId")]
        public string ProviderVersionId { get; set; }

        [JsonProperty("fundingStreams")]
        public IEnumerable<Reference> FundingStreams { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("policies")]
        public IEnumerable<Policy> Policies { get; set; } = Enumerable.Empty<Policy>();

        [JsonProperty("dataDefinitionRelationshipIds")]
        public IEnumerable<string> DataDefinitionRelationshipIds { get; set; }

		[JsonProperty("variationDate")]
		public DateTimeOffset? VariationDate { get; set; }

        /// <summary>
        /// Gets all calculations - from top level policies and subpolicies in a flat list
        /// </summary>
        /// <returns>IEnumerable of Calculations for the specification</returns>
        public IEnumerable<Calculation> GetAllCalculations()
        {
            List<Calculation> calculations = new List<Calculation>();

            if (Policies != null)
            {
                foreach (Policy policy in Policies)
                {
                    if (policy != null && policy.Calculations != null)
                    {
                        foreach (Calculation calculation in policy.Calculations)
                        {
                            if (calculation != null)
                            {
                                calculations.Add(calculation);
                            }
                        }
                    }

                    if (policy != null && policy.SubPolicies != null)
                    {
                        foreach (Policy subPolicy in policy.SubPolicies)
                        {
                            if (subPolicy != null && subPolicy.Calculations != null)
                            {
                                foreach (Calculation calculation in subPolicy.Calculations)
                                {
                                    if (calculation != null)
                                    {
                                        calculations.Add(calculation);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return calculations;
        }

        public override VersionedItem Clone()
        {
            // Serialise to perform a deep copy
            string json = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<SpecificationVersion>(json);
        }
    }
}
