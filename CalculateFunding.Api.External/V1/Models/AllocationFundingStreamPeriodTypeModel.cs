using CalculateFunding.Models.External;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace CalculateFunding.Api.External.V1.Models
{
    /// <summary>
    /// Represents a funding stream period type
    /// </summary>
    [Serializable]
    public class AllocationFundingStreamPeriodTypeModel
    {
        /// <summary>
        /// The id of the period type
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The name of the period type
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The start day of the period type
        /// </summary>
        public int StartDay { get; set; }

        /// <summary>
        /// The start month of the period type
        /// </summary>
        public int StartMonth { get; set; }

        /// <summary>
        /// The end day of the period type
        /// </summary>
        public int EndDay { get; set; }

        /// <summary>
        /// The end month of the period type
        /// </summary>
        public int EndMonth { get; set; }
    }
}
