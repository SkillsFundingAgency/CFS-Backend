using System.ServiceModel.Channels;

namespace CalculateFunding.Services.Profiling.Models
{
    public class ReProfileContext
    {
        /// <summary>
        /// Reprofile request
        /// </summary>
        public ReProfileRequest Request { get; set; }

        /// <summary>
        /// Profile pattern configuration
        /// </summary>
        public FundingStreamPeriodProfilePattern ProfilePattern { get; set; }

        /// <summary>
        /// Full year profile for the current value of the funding stream
        /// </summary>
        public AllocationProfileResponse ProfileResult { get; set; }
    }
}
