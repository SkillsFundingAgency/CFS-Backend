using System;

namespace CalculateFunding.Api.External.V2.Models
{
    [Serializable]
    public class Period
    {
        /// <summary>
        /// The id of the period
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The nameid of the period
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The (inclusive) start year of the period
        /// </summary>
        public int StartYear { get; set; }

        /// <summary>
        /// The (inclusive) end year of the period
        /// </summary>
        public int EndYear { get; set; }

    }
}