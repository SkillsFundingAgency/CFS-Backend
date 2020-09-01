using System;
// ReSharper disable NonReadonlyMemberInGetHashCode

namespace CalculateFunding.Services.Publishing.Profiling
{
    public class ProfileTotal
    {
        public int Year { get; set; }
        
        public string TypeValue { get; set; }
        
        public int Occurrence { get; set; }
        
        public decimal Value { get; set; }
        
        public bool IsPaid { get; set; }

        public int InstallmentNumber { get; set; }

        public decimal? ProfileRemainingPercentage { get; set; }

        public DateTimeOffset? ActualDate { get; set; }

        public override bool Equals(object obj)
        {
            return GetHashCode().Equals(obj?.GetHashCode());
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Year, 
                TypeValue, 
                Occurrence, 
                Value,
                IsPaid,
                InstallmentNumber,
                ProfileRemainingPercentage,
                ActualDate);
        }
    }
}