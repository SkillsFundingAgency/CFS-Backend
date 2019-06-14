﻿using Newtonsoft.Json;

namespace CalculateFunding.Models.Specs
{
    public class CalculationVersionComparisonModel
    {
        public string CalculationId { get; set; }

        public string SpecificationId { get; set; }

        public Calculation Current { get; set; }

        public Calculation Previous { get; set; }

        [JsonIgnore]
        public bool RequiresCalculationRun => Current.IsPublic != Previous.IsPublic;

        [JsonIgnore]
        public bool HasChanges
        {
            get
            {
                return (Current.Name != Previous.Name) 
                    || (Current.Description != Previous.Description) 
                    || (Current.CalculationType != Previous.CalculationType)
                    || (Current.IsPublic != Previous.IsPublic)
                    || (Current.AllocationLine?.Id != Previous.AllocationLine?.Id);
            }
        }
    }
}