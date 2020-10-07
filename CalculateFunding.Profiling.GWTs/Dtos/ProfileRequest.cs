using System;
using Newtonsoft.Json;

namespace CalculateFunding.Profiling.GWTs.Dtos
{
	public class ProfileRequest
    {
        public AllocationOrganisation AllocationOrganisation { get; set; }

        public string FundingStreamPeriod { get; set; }

        public DateTime? AllocationStartDate { get; set; }

        public DateTime? AllocationEndDate { get; set; }

        public AllocationValueForDistributionPeriod[] AllocationValueByDistributionPeriod { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

        protected bool Equals(ProfileRequest other)
        {
            return AllocationEndDate.Equals(other.AllocationEndDate) 
                   && Equals(AllocationOrganisation, other.AllocationOrganisation) 
                   && AllocationStartDate.Equals(other.AllocationStartDate) 
                   && Equals(AllocationValueByDistributionPeriod, other.AllocationValueByDistributionPeriod) 
                   && string.Equals(FundingStreamPeriod, other.FundingStreamPeriod);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ProfileRequest) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = AllocationEndDate.GetHashCode();
                hashCode = (hashCode * 397) ^ (AllocationOrganisation != null ? AllocationOrganisation.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ AllocationStartDate.GetHashCode();
                hashCode = (hashCode * 397) ^ (AllocationValueByDistributionPeriod != null ? AllocationValueByDistributionPeriod.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (FundingStreamPeriod != null ? FundingStreamPeriod.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}