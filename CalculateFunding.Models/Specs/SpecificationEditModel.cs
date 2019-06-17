using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Models.Specs
{
    public class SpecificationEditModel
    {
        public SpecificationEditModel()
        {
            FundingStreamIds = Enumerable.Empty<string>();
        }
        /// <summary>
        /// Used to pass from the service to the validator for duplicate name lookup
        /// </summary>
        [JsonIgnore]
        public string SpecificationId { get; set; }

        public string ProviderVersionId { get; set; }

        public string FundingPeriodId { get; set; }

        public IEnumerable<string> FundingStreamIds { get; set; }

        public string Description { get; set; }

        public string Name { get; set; }
    }
}
