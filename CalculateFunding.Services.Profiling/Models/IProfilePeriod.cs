namespace CalculateFunding.Services.Profiling.Models
{
    public interface IProfilePeriod
    {
        string TypeValue { get; set; }
        
        int Occurrence { get; set; }
        
        PeriodType Type { get; set; }
        
        int Year { get; set; }

        string DistributionPeriod { get; set; }

        decimal GetProfileValue();

        void SetProfiledValue(decimal value);
    }

    public interface IExistingProfilePeriod : IProfilePeriod
    {
        bool IsPaid { get; }
    }
}