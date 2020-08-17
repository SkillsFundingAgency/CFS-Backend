using CalculateFunding.Common.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Calcs
{
    [Serializable]
    public class CalculationResultResponse
    {
        [JsonProperty("calculation")]
        public Reference Calculation { get; set; }

        [JsonProperty("value")]
        public object Value { get; set; }

        [JsonProperty("exceptionType")]
        public string ExceptionType { get; set; }

        [JsonProperty("exceptionMessage")]
        public string ExceptionMessage { get; set; }

        [JsonProperty("exceptionStackTrace")]
        public string ExceptionStackTrace { get; set; }

        [JsonProperty("calculationType")]
        public CalculationType CalculationType { get; set; }

        [JsonProperty("calculationValueType")]
        public CalculationValueType CalculationValueType { get; set; }

        [JsonProperty("calculationDataType")]
        public CalculationDataType CalculationDataType { get; set; }
    }
}
