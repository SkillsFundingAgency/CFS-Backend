namespace CalculateFunding.Services.Profiling.Models
{
    public class ProfileRequestPeriodValue
    {
	    public ProfileRequestPeriodValue()
	    {
	    }

	    public ProfileRequestPeriodValue(string distributionPeriod, decimal allocationValue)
        {
            DistributionPeriod = distributionPeriod;
            AllocationValue = allocationValue;
        }

	    public string DistributionPeriod { get; set;  }

        public decimal AllocationValue { get; set;  }

	    public override string ToString()
	    {
		    return $"{nameof(DistributionPeriod)}: {DistributionPeriod}, {nameof(AllocationValue)}: {AllocationValue}";
	    }
	}
}