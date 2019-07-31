using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Models.Specs
{
    public class SpecificationVersionComparisonModel
    {
        public string Id { get; set; }

        public SpecificationVersion Current { get; set; }

        public SpecificationVersion Previous { get; set; }

        [JsonIgnore]
        public bool HasNameChange
        {
            get
            {
                return !string.Equals(Current.Name, Previous.Name);
            }
        }

        [JsonIgnore]
        public bool HasNoChanges
        {
            get
            {
                return !HasFundingPeriodIdChanged && !HasFundingStreamsChanged;
            }
        }

        [JsonIgnore]
        bool HasFundingPeriodIdChanged
        {
            get
            {
                return Current.FundingPeriod.Id != Previous.FundingPeriod.Id;
            }
        }

        [JsonIgnore]
        bool HasFundingStreamsChanged
        {
            get
            {
                IEnumerable<string> currentfundingStreamIds = Current.FundingStreams != null ? Current.FundingStreams.Select(m => m.Id) : Enumerable.Empty<string>();
                IEnumerable<string> previousfundingStreamIds = Previous.FundingStreams != null ? Previous.FundingStreams.Select(m => m.Id) : Enumerable.Empty<string>();

                return !currentfundingStreamIds.OrderBy(m => m).SequenceEqual(previousfundingStreamIds.OrderBy(m => m));
            }
        }

    }
}
