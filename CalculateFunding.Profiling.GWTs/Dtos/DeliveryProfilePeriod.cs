using Newtonsoft.Json;

namespace CalculateFunding.Profiling.GWTs.Dtos
{
	public class DeliveryProfilePeriod
    {
        public DeliveryProfilePeriod(string period, int occurrence, string periodType, int periodYear, decimal profileValue, string distributionPeriod)
        {
            Period = period;
            Occurrence = occurrence;
            PeriodType = periodType;
            PeriodYear = periodYear;
            ProfileValue = profileValue;
            DistributionPeriod = distributionPeriod;
        }

        public string Period { get; }

        public int Occurrence { get; }

        public string PeriodType { get; }

        public int PeriodYear { get; }

        public decimal ProfileValue { get; }

        public string DistributionPeriod { get; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

        protected bool Equals(DeliveryProfilePeriod other)
        {
            return string.Equals(DistributionPeriod, other.DistributionPeriod) && Occurrence == other.Occurrence && string.Equals(Period, other.Period) && string.Equals(PeriodType, other.PeriodType) && PeriodYear == other.PeriodYear && ProfileValue == other.ProfileValue;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DeliveryProfilePeriod) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (DistributionPeriod != null ? DistributionPeriod.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Occurrence;
                hashCode = (hashCode * 397) ^ (Period != null ? Period.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (PeriodType != null ? PeriodType.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ PeriodYear;
                hashCode = (hashCode * 397) ^ ProfileValue.GetHashCode();
                return hashCode;
            }
        }
    }
}