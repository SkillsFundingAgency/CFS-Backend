namespace CalculateFunding.Profiling.GWTs.Dtos
{
    public class AllocationValueForDistributionPeriod
    {
        public string DistributionPeriod { get; set; }

        public decimal AllocationValue { get; set; }

        public override string ToString()
        {
            return $"Allocation Value: {AllocationValue}; Distribution Period: {DistributionPeriod}";
        }

        protected bool Equals(AllocationValueForDistributionPeriod other)
        {
            return AllocationValue == other.AllocationValue && string.Equals(DistributionPeriod, other.DistributionPeriod);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AllocationValueForDistributionPeriod) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (AllocationValue.GetHashCode() * 397) ^ (DistributionPeriod != null ? DistributionPeriod.GetHashCode() : 0);
            }
        }
    }
}