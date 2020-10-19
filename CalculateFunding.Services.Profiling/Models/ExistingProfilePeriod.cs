using System.ComponentModel.DataAnnotations;

namespace CalculateFunding.Services.Profiling.Models
{
    public class ExistingProfilePeriod : IExistingProfilePeriod
    {
        [Required]
        public string TypeValue { get; set; }

        [Required]
        public int Occurrence { get; set; }

        [Required]
        public PeriodType Type { get; set; }

        [Required]
        public int Year { get; set; }

        /// <summary>
        /// Profile value. If this value is null, then the reprofiling should generate the value, otherwise the value is considered by either paid or to be set to this value
        /// </summary>
        public decimal? ProfileValue { get; set; }

        [Required]
        public string DistributionPeriod { get; set; }

        public decimal GetProfileValue() => ProfileValue.GetValueOrDefault();

        public void SetProfiledValue(decimal value)
            => ProfileValue = value;

        public bool IsPaid => ProfileValue.HasValue;
    }
}
