using System;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
    [Serializable]
    public class CalculationResult
    {
        [JsonProperty("calculation")]
        public Reference Calculation { get; set; }

        [Obsolete]
        [JsonProperty("allocationLine")]
        public Reference AllocationLine { get; set; }

        [JsonProperty("value")]
        public decimal? Value { get; set; }

        [JsonProperty("exceptionType")]
        public string ExceptionType { get; set; }

        [JsonProperty("exceptionMessage")]
        public string ExceptionMessage { get; set; }

        [JsonProperty("exceptionStackTrace")]
        public string ExceptionStackTrace { get; set; }

        /// <summary>
        /// Elapsed time, used for debugging locally and shouldn't be stored in cosmos
        /// </summary>
        [JsonIgnore]
        [JsonProperty("elapsedTime")]
        public long ElapsedTime { get; set; }

        [JsonProperty("calculationType")]
        public CalculationType CalculationType { get; set; }

        [Obsolete]
        [JsonProperty("version")]
        public int Version { get; set; }
    }
}