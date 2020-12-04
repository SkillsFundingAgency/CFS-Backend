using System;

namespace CalculateFunding.Models.Jobs
{
    public class Outcome
    {
        public string Description { get; set; }
        
        public OutcomeType Type { get; set; }
        
        public string JobDefinitionId { get; set; }
        
        public bool IsSuccessful { get; set; }

        protected bool Equals(Outcome other)
            => GetHashCode().Equals(other?.GetHashCode());

        public override bool Equals(object obj)
            => obj is Outcome &&
               GetHashCode().Equals(obj.GetHashCode());

        public override int GetHashCode() 
            => HashCode.Combine(Description, 
                (int) Type, 
                JobDefinitionId, 
                IsSuccessful);
    }
}