using System.Collections.Generic;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.Models
{
    public class ProviderVariationResult
    {
        public ICollection<VariationReason> VariationReasons { get; set; } = new List<VariationReason>();
        
        public bool HasProviderBeenVaried { get; set; }
    }
}