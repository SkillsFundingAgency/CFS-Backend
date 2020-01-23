using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Jobs
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum RunningStatus
    {
        Queued, // Created and waiting to be actioned
        QueuedWithService, // Sent to the microservice to action
        InProgress, // Job is running
        Completed, // Job has completed
        Completing // Job is completing pre-completion jobs before being completed
    }
}
